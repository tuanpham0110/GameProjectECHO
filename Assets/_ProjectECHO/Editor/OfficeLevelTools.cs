using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectEcho.Editor
{
    /// <summary>Tên các nhóm do công cụ sinh ra; Setup Gameplay xóa và dựng lại chúng mỗi lần chạy.</summary>
    internal static class GeneratedGroups
    {
        internal const string Level = "Level";
        internal const string Environment = "Environment";
        internal const string Collision = "Collision";
        internal const string Boundary = "Boundary";
        internal const string Systems = "Level systems";
        internal const string Markers = "Authoring markers";
        internal const string Objects = "Gameplay objects";
        internal static readonly string[] Gameplay = { Systems, Markers, Objects };
        internal static readonly string[] Legacy = { "Gameplay Blockout", "Office FBX Review · inactive until approved" };
    }

    /// <summary>Một Level dựng được: scene + luật + bố trí. Thêm Level mới bằng cách khai báo thêm một target.</summary>
    internal sealed class LevelTarget
    {
        public string ScenePath { get; }
        public string RulesName { get; }
        public string LayoutName { get; }
        public Action<LevelLayout> Defaults { get; }
        public string SceneName => Path.GetFileNameWithoutExtension(ScenePath);

        public LevelTarget(string scene, string rules, string layout, Action<LevelLayout> defaults)
        {
            ScenePath = scene;
            RulesName = rules;
            LayoutName = layout;
            Defaults = defaults;
        }
    }

    public static class OfficeLevelTools
    {
        public const string Root = "Assets/_ProjectECHO";
        public const string ScenePath = Root + "/Scenes/Office_Level01.unity";
        public const string SandboxPath = Root + "/Scenes/Sandbox/Blockout_Sandbox.unity";
        internal const string OfficeFolder = Root + "/Art/Environments/Office";
        internal const string AssetsFolder = Root + "/Generated";
        internal const string ReportFolder = "Docs/Technical/OfficeMap";
        private const string Menu = "Tools/Project ECHO/";
        private const float AgentRadius = 0.4f, AgentHeight = 1.8f, AgentClimb = 0.3f;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        internal static readonly LevelTarget Office = new(ScenePath, "Office_Level01_Rules", "Office_Level01_Layout", LevelLayouts.FillOffice);
        internal static readonly LevelTarget Sandbox = new(SandboxPath, "Office_Level01_Rules", "Blockout_Sandbox_Layout", LevelLayouts.FillBlockout);

        // ───────────────────────────── Menu ─────────────────────────────

        [MenuItem(Menu + "Build Vertical Slice", priority = 1)]
        public static void BuildVerticalSlice()
        {
            Build(Office);
            RegisterScene();
            Debug.Log("Project ECHO: Office_Level01 đã sẵn sàng để nhấn Play.");
        }

        [MenuItem(Menu + "Build Blockout Sandbox", priority = 2)]
        public static void BuildSandbox() => Build(Sandbox);

        /// <summary>Điểm vào batch mode: dựng cả hai scene, kiểm tra và chụp ảnh xem trước.</summary>
        public static void BuildAllForVerification()
        {
            Build(Sandbox);
            Build(Office);
            RegisterScene();
            CaptureLevelPreviews();
        }

        [MenuItem(Menu + "Validate Project", priority = 10)]
        public static void ValidateProject()
        {
            CreateFolders();
            var problems = new List<string>();
            GameObject model = FindOfficeModel();
            if (model == null) problems.Add($"Không tìm được FBX Office trong {OfficeFolder}.");
            else Debug.Log($"Project ECHO: Office FBX hợp lệ tại {AssetDatabase.GetAssetPath(model)}.");
            if (GraphicsSettings.currentRenderPipeline == null) problems.Add("URP chưa được gán.");
            var layout = Layout(Office);
            var rules = OfficeSceneFactory.Settings<LevelRules>(Office.RulesName);
            if (layout.data.Length != rules.requiredData) problems.Add($"Layout có {layout.data.Length} dữ liệu, LevelRules yêu cầu {rules.requiredData}.");
            if (layout.data.Select(d => d.id).Distinct().Count() != layout.data.Length) problems.Add("ID dữ liệu trong layout bị trùng.");
            if (layout.waypoints.Length < 2) problems.Add("Layout cần ít nhất hai waypoint.");
            var agent = NavMesh.GetSettingsByID(0);
            if (Mathf.Abs(agent.agentRadius - AgentRadius) > 0.01f || Mathf.Abs(agent.agentClimb - AgentClimb) > 0.01f)
                problems.Add($"Agent NavMesh đang là r={agent.agentRadius}, climb={agent.agentClimb}; Bake Navigation sẽ đặt r={AgentRadius}, climb={AgentClimb}.");
            foreach (var problem in problems) Debug.LogWarning("Project ECHO: " + problem);
            if (problems.Count == 0) Debug.Log("Project ECHO: Project hợp lệ.");
        }

        [MenuItem(Menu + "Generate Level", priority = 11)]
        public static void GenerateLevel()
        {
            if (!PrepareScene(Office, true, out var scene)) return;
            GenerateEnvironment(scene, Office);
            Save(scene, Office);
        }

        [MenuItem(Menu + "Setup Gameplay", priority = 12)]
        public static void SetupGameplay()
        {
            if (!PrepareScene(Office, true, out var scene)) return;
            GenerateEnvironment(scene, Office);
            SetupGameplay(scene, Office);
            Save(scene, Office);
        }

        [MenuItem(Menu + "Bake Navigation", priority = 13)]
        public static void BakeNavigation()
        {
            if (PrepareScene(Office, false, out var scene)) BakeNavigation(scene, Office);
        }

        [MenuItem(Menu + "Run Validation", priority = 14)]
        public static void RunValidation()
        {
            if (PrepareScene(Office, false, out var scene)) CheckScene(scene, Office);
        }

        [MenuItem(Menu + "Capture Level Previews", priority = 15)]
        public static void CaptureLevelPreviews()
        {
            if (PrepareScene(Office, false, out var scene)) LevelPreviews.Capture(scene, Office);
        }

        [MenuItem(Menu + "Build Windows x64", priority = 30)]
        public static void BuildWindows()
        {
            if (!PrepareScene(Office, false, out var scene)) return;
            Save(scene, Office);
            CheckScene(scene, Office);
            RegisterScene();
            PlayerSettings.bakeCollisionMeshes = true;
            Directory.CreateDirectory("Builds/Windows");
            string output = Path.GetFullPath("Builds/Windows/ProjectECHO.exe");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Project ECHO: build lỗi ({report.summary.result}).");
            Debug.Log($"Project ECHO: Windows x64 tại {output} ({report.summary.totalSize:N0} byte).");
        }

        // ───────────────────────────── Pipeline ─────────────────────────────

        private static void Build(LevelTarget target)
        {
            if (!PrepareScene(target, true, out var scene)) return;
            GenerateEnvironment(scene, target);
            SetupGameplay(scene, target);
            BakeNavigation(scene, target);
            CheckScene(scene, target);
        }

        internal static void CreateFolders()
        {
            Folder(Root, "Scenes");
            Folder(Root + "/Scenes", "Sandbox");
            Folder(Root, "Prefabs");
            Folder(Root, "Settings");
            Folder(Root, "Generated");
        }

        private static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        internal static LevelLayout Layout(LevelTarget target)
        {
            string path = $"{Root}/Settings/{target.LayoutName}.asset";
            var layout = AssetDatabase.LoadAssetAtPath<LevelLayout>(path);
            if (layout != null) return layout;
            CreateFolders();
            layout = ScriptableObject.CreateInstance<LevelLayout>();
            target.Defaults(layout);
            AssetDatabase.CreateAsset(layout, path);
            AssetDatabase.SaveAssets();
            return layout;
        }

        private static bool PrepareScene(LevelTarget target, bool create, out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Project ECHO: Dừng Play trước khi chỉnh sửa level.");
            if (scene.path == target.ScenePath) return true;
            if (scene.isDirty && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (scene.isDirty && Application.isBatchMode && !string.IsNullOrEmpty(scene.path))
                throw new InvalidOperationException("Project ECHO: Scene đang mở có chỉnh sửa chưa lưu.");
            CreateFolders();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(target.ScenePath) != null)
                scene = EditorSceneManager.OpenScene(target.ScenePath, OpenSceneMode.Single);
            else if (create)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Save(scene, target);
            }
            else throw new InvalidOperationException($"Project ECHO: Hãy tạo {target.SceneName} trước.");
            return true;
        }

        private static void GenerateEnvironment(Scene scene, LevelTarget target)
        {
            var layout = Layout(target);
            RemoveLegacyRoots(scene);
            var level = LevelRoot(scene);
            var environment = Child(level, GeneratedGroups.Environment);
            if (layout.environment == LevelEnvironment.Blockout)
            {
                if (environment.childCount == 0) OfficeSceneFactory.CreateBlockout(environment);
                return;
            }

            var model = FindModel(layout);
            if (model == null) throw new InvalidOperationException($"Project ECHO: Không tìm thấy model '{layout.modelName}' trong {layout.modelFolder}.");
            var instance = ModelInstance(environment, model);
            int colliders = BuildCollision(instance, layout, target);
            BuildBoundary(environment, layout);
            ApplyInteriorLighting();
            Debug.Log($"Project ECHO: Gắn {model.name} ({instance.GetComponentsInChildren<MeshFilter>(true).Length} mesh), sinh {colliders} MeshCollider và tường biên.");
        }

        /// <summary>Scene đời trước (blockout + nhóm review) được sao lưu ra ngoài Assets rồi mới gỡ.</summary>
        private static void RemoveLegacyRoots(Scene scene)
        {
            var legacy = scene.GetRootGameObjects().Where(o => GeneratedGroups.Legacy.Contains(o.name)).ToArray();
            if (legacy.Length == 0) return;
            if (!string.IsNullOrEmpty(scene.path) && File.Exists(scene.path))
            {
                Directory.CreateDirectory("Backups");
                string backup = Path.Combine("Backups", $"{Path.GetFileNameWithoutExtension(scene.path)}-{DateTime.Now:yyyyMMdd-HHmmss}.unity");
                File.Copy(scene.path, backup, true);
                Debug.Log($"Project ECHO: Đã sao lưu scene cũ vào {backup} trước khi gỡ nhóm blockout/review.");
            }
            foreach (var root in legacy) Object.DestroyImmediate(root);
        }

        private static Transform LevelRoot(Scene scene)
        {
            var root = scene.GetRootGameObjects().FirstOrDefault(o => o.name == GeneratedGroups.Level);
            if (root == null)
            {
                root = new GameObject(GeneratedGroups.Level);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            var surface = root.GetComponent<NavMeshSurface>() ?? root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1;
            surface.agentTypeID = 0;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.1f;
            return root.transform;
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Transform ModelInstance(Transform environment, GameObject model)
        {
            foreach (Transform child in environment)
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == model) return child;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, environment);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            return instance.transform;
        }

        /// <summary>
        /// Sinh prefab chỉ chứa MeshCollider, tái tạo đúng hierarchy và transform của model để không phải sửa FBX hay
        /// thêm override vào hàng nghìn node. Bỏ chi tiết nhỏ, vật treo trần và cánh cửa (cửa có collider riêng).
        /// </summary>
        private static int BuildCollision(Transform model, LevelLayout layout, LevelTarget target)
        {
            var doors = layout.doors.SelectMany(d => d.modelPaths ?? Array.Empty<string>()).Select(model.Find).Where(t => t != null).ToArray();
            var proxy = new GameObject(GeneratedGroups.Collision);
            var map = new Dictionary<Transform, Transform> { [model] = proxy.transform };
            int count = 0;
            try
            {
                foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || !filter.TryGetComponent<Renderer>(out var renderer)) continue;
                    if (doors.Any(door => filter.transform.IsChildOf(door))) continue;
                    string path = AnimationUtility.CalculateTransformPath(filter.transform, model);
                    if (layout.collisionExclusions.Any(x => !string.IsNullOrEmpty(x) && path.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                    Bounds bounds = renderer.bounds;
                    bool huge = Mathf.Max(bounds.size.x, bounds.size.z) > 10;
                    if (Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) < layout.minColliderSize) continue;
                    if (bounds.min.y > layout.maxColliderBottom && !huge) continue;
                    var node = Mirror(filter.transform, map).gameObject;
                    node.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    // Trần/mái chỉ để chặn camera và tầm nhìn, không phải bề mặt đi lại của Hunter.
                    if (bounds.min.y > layout.maxColliderBottom) node.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                    count++;
                }
                string asset = $"{AssetsFolder}/{target.SceneName}_Collision.prefab";
                var saved = PrefabUtility.SaveAsPrefabAsset(proxy, asset);
                var environment = model.parent;
                var existing = environment.Find(GeneratedGroups.Collision);
                if (existing == null)
                {
                    var placed = (GameObject)PrefabUtility.InstantiatePrefab(saved, environment);
                    placed.name = GeneratedGroups.Collision;
                    placed.transform.SetLocalPositionAndRotation(model.localPosition, model.localRotation);
                }
                return count;
            }
            finally
            {
                Object.DestroyImmediate(proxy);
            }
        }

        private static Transform Mirror(Transform source, Dictionary<Transform, Transform> map)
        {
            if (map.TryGetValue(source, out var mirrored)) return mirrored;
            var parent = Mirror(source.parent, map);
            var node = new GameObject(source.name).transform;
            node.SetParent(parent, false);
            node.localPosition = source.localPosition;
            node.localRotation = source.localRotation;
            node.localScale = source.localScale;
            map[source] = node;
            return node;
        }

        /// <summary>Mặt sàn rộng hơn khối tường và không có tường bao; tường vô hình giữ TAY trong vùng sàn.</summary>
        private static void BuildBoundary(Transform environment, LevelLayout layout)
        {
            var old = environment.Find(GeneratedGroups.Boundary);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var boundary = new GameObject(GeneratedGroups.Boundary).transform;
            boundary.SetParent(environment, false);
            var ignore = boundary.gameObject.AddComponent<NavMeshModifier>();
            ignore.ignoreFromBuild = true;
            Rect r = layout.walkBounds;
            float h = layout.boundaryHeight, t = 1;
            Wall("West", new Vector3(r.xMin - t / 2, h / 2, r.center.y), new Vector3(t, h, r.height + 2 * t));
            Wall("East", new Vector3(r.xMax + t / 2, h / 2, r.center.y), new Vector3(t, h, r.height + 2 * t));
            Wall("South", new Vector3(r.center.x, h / 2, r.yMin - t / 2), new Vector3(r.width + 2 * t, h, t));
            Wall("North", new Vector3(r.center.x, h / 2, r.yMax + t / 2), new Vector3(r.width + 2 * t, h, t));

            void Wall(string name, Vector3 center, Vector3 size)
            {
                var wall = new GameObject("Boundary " + name);
                wall.transform.SetParent(boundary, false);
                wall.transform.localPosition = center;
                wall.AddComponent<BoxCollider>().size = size;
            }
        }

        private static void ApplyInteriorLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.40f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.03f, 0.05f, 0.07f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
        }

        private static void SetupGameplay(Scene scene, LevelTarget target)
        {
            var layout = Layout(target);
            var rules = OfficeSceneFactory.Settings<LevelRules>(target.RulesName);
            var level = LevelRoot(scene);
            foreach (string group in GeneratedGroups.Gameplay)
            {
                var old = level.Find(group);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var environment = level.Find(GeneratedGroups.Environment);
            Transform model = null;
            if (layout.environment == LevelEnvironment.Model && environment != null)
            {
                var asset = FindModel(layout);
                foreach (Transform child in environment)
                    if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == asset) model = child;
            }
            OfficeSceneFactory.SetupGameplay(level, layout, rules, model);
        }

        private static void ConfigureAgent()
        {
            var settingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset").FirstOrDefault();
            if (settingsAsset == null) return;
            var serialized = new SerializedObject(settingsAsset);
            var list = serialized.FindProperty("m_Settings");
            if (list == null || list.arraySize == 0) return;
            var agent = list.GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue = AgentRadius;
            agent.FindPropertyRelative("agentHeight").floatValue = AgentHeight;
            agent.FindPropertyRelative("agentClimb").floatValue = AgentClimb;
            agent.FindPropertyRelative("agentSlope").floatValue = 45;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BakeNavigation(Scene scene, LevelTarget target)
        {
            var level = Components<LevelManager>(scene).SingleOrDefault();
            if (level == null || level.navigation == null) throw new InvalidOperationException("Project ECHO: Chạy Setup Gameplay trước.");
            ConfigureAgent();
            Physics.SyncTransforms();
            string navPath = $"{AssetsFolder}/{target.SceneName}_NavMesh.asset";
            var surface = level.navigation;
            var previous = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            surface.RemoveData();
            surface.BuildNavMesh();
            var baked = surface.navMeshData;
            if (baked == null || NavMesh.CalculateTriangulation().vertices.Length == 0)
            {
                surface.RemoveData();
                if (baked != null && baked != previous) Object.DestroyImmediate(baked);
                surface.navMeshData = previous;
                surface.AddData();
                throw new InvalidOperationException("Project ECHO: Không bake được NavMesh. Kiểm tra collider và các sàn hoạt động.");
            }
            if (previous == null) AssetDatabase.CreateAsset(baked, navPath);
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(baked, previous);
                Object.DestroyImmediate(baked);
                surface.navMeshData = previous;
                surface.AddData();
                EditorUtility.SetDirty(previous);
            }
            SnapMarkers(level);
            Save(scene, target);
        }

        /// <summary>Đặt marker dạng điểm lên NavMesh gần nhất để tọa độ layout không cần chính xác tới từng cm.</summary>
        private static void SnapMarkers(LevelManager level)
        {
            var points = new List<(Transform transform, float lift)>
            {
                (level.playerSpawn, 0.04f), (level.hunterSpawn, 0.05f), (level.relay.transform, 0.02f)
            };
            points.AddRange(level.waypoints.Select(w => (w, 0.03f)));
            foreach (var target in level.interactions)
            {
                switch (target)
                {
                    case DataPickup: points.Add((target.transform, 0.75f)); break;
                    case SupportPickup: points.Add((target.transform, 0.65f)); break;
                }
            }
            foreach (var (transform, lift) in points)
            {
                Vector3 floor = transform.position - Vector3.up * lift;
                if (!NavMesh.SamplePosition(floor, out var hit, 2f, NavMesh.AllAreas)) continue;
                Undo.RecordObject(transform, "Snap marker");
                transform.position = hit.position + Vector3.up * lift;
            }
            level.player.transform.SetPositionAndRotation(level.playerSpawn.position, level.playerSpawn.rotation);
            level.hunter.transform.SetPositionAndRotation(level.hunterSpawn.position, level.hunterSpawn.rotation);
        }

        private static void CheckScene(Scene scene, LevelTarget target)
        {
            var errors = new List<string>();
            var report = new StringBuilder();
            report.AppendLine($"# Kiểm tra {target.SceneName}\n");
            report.AppendLine($"Sinh bởi **Tools → Project ECHO → Run Validation** lúc {DateTime.Now:yyyy-MM-dd HH:mm}.\n");
            var levels = Components<LevelManager>(scene).ToArray();
            if (levels.Length != 1) errors.Add("Scene cần đúng một LevelManager.");
            else
            {
                var level = levels[0];
                if (level.game == null || level.player == null || level.hunter == null || level.cameraView == null ||
                    level.relay == null || level.exit == null || level.rules == null || level.playerSpawn == null ||
                    level.hunterSpawn == null || level.navigation == null)
                    errors.Add("Thiếu tham chiếu bắt buộc trong LevelManager.");
                else
                {
                    ValidateLayout(level, errors, report);
                    Directory.CreateDirectory(ReportFolder);
                    Rect area = Layout(target).walkBounds;
                    area = Rect.MinMaxRect(area.xMin - 1, area.yMin - 1, area.xMax + 1, area.yMax + 1);
                    try { LevelPreviews.ExportNavMesh(Path.Combine(ReportFolder, $"navmesh_{target.SceneName}.png"), level, area); }
                    catch (Exception exception) { Debug.LogWarning($"Project ECHO: Không xuất được ảnh NavMesh. {exception.Message}"); }
                }
                if (Components<GameplayHud>(scene).Count() != 1) errors.Add("Scene cần đúng một GameplayHud.");
            }
            report.AppendLine(errors.Count == 0 ? "\n**Kết quả: đạt.**" : "\n**Kết quả: lỗi**\n\n" + string.Join("\n", errors.Select(e => "- " + e)));
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(Path.Combine(ReportFolder, $"{target.SceneName}_Validation.md"), report.ToString(), Encoding.UTF8);
            if (errors.Count > 0) throw new InvalidOperationException("Project ECHO: " + string.Join("\n", errors));
            Debug.Log($"Project ECHO: Validation đạt cho {scene.path}.");
        }

        private static void ValidateLayout(LevelManager level, List<string> errors, StringBuilder report)
        {
            var targets = level.interactions ?? Array.Empty<Interactable>();
            var data = targets.OfType<DataPickup>().ToArray();
            bool invalidIds = data.Select(p => p.dataId).Any(string.IsNullOrWhiteSpace) ||
                data.Select(p => p.dataId).Distinct().Count() != data.Length;
            if (invalidIds || data.Length != level.rules.requiredData)
                errors.Add("Số lượng hoặc ID dữ liệu không khớp cấu hình.");
            if (!targets.Contains(level.relay) || !targets.Contains(level.exit)) errors.Add("Relay/cổng chưa gán tương tác.");
            if (level.waypoints == null || level.waypoints.Length < 2 || level.waypoints.Any(t => t == null))
                errors.Add("Cần ít nhất hai waypoint hợp lệ.");
            if (level.navigation.navMeshData == null || NavMesh.CalculateTriangulation().vertices.Length == 0)
            {
                errors.Add("Cần NavMesh đã bake.");
                return;
            }

            var motor = level.player.GetComponent<CharacterController>();
            Vector3 feet = level.playerSpawn.position;
            float radius = motor.radius;
            if (Physics.CheckCapsule(feet + Vector3.up * (radius + 0.12f), feet + Vector3.up * (motor.height - radius), radius - 0.02f,
                    level.worldMask, QueryTriggerInteraction.Ignore))
                errors.Add("Player Spawn chồng lên collider.");

            report.AppendLine("| Từ | Tới | Trạng thái | Độ dài đường NavMesh (m) |");
            report.AppendLine("| --- | --- | --- | --- |");
            var destinations = targets.Where(t => t != null && t is not OfficeDoor).Select(t => t is HideSpot h && h.outside != null ? h.outside : t.transform)
                .Concat(level.waypoints ?? Array.Empty<Transform>()).Append(level.playerSpawn).Append(level.hunterSpawn).ToArray();
            foreach (var origin in new[] { level.playerSpawn, level.hunterSpawn })
            {
                if (!NavMesh.SamplePosition(origin.position, out var start, 1.25f, NavMesh.AllAreas))
                {
                    errors.Add($"{origin.name} ngoài NavMesh.");
                    continue;
                }
                foreach (var target in destinations.Where(t => t != null && t != origin))
                    PathRow(origin.name, start.position, target, errors, report);
            }
            if (NavMesh.SamplePosition(level.relay.transform.position, out var relay, 1.5f, NavMesh.AllAreas))
                PathRow("Data Relay", relay.position, level.exit.transform, errors, report);
            else errors.Add("Data Relay ngoài NavMesh.");
        }

        private static void PathRow(string from, Vector3 start, Transform target, List<string> errors, StringBuilder report)
        {
            var path = new NavMeshPath();
            string label = target.parent != null && target.name is "Outside" ? target.parent.name : target.name;
            bool ok = NavMesh.SamplePosition(target.position, out var hit, 1.5f, NavMesh.AllAreas) &&
                      NavMesh.CalculatePath(start, hit.position, NavMesh.AllAreas, path) &&
                      path.status == NavMeshPathStatus.PathComplete;
            float length = 0;
            if (ok) for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            report.AppendLine($"| {from} | {label} | {(ok ? "đạt" : "**không có đường**")} | {(ok ? length.ToString("0.0", Invariant) : "–")} |");
            if (!ok) errors.Add($"Không có đường NavMesh từ {from} tới {label}.");
        }

        private static void RegisterScene()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0 && scenes[0].path == ScenePath && scenes[0].enabled &&
                scenes.Count(item => item.path == ScenePath) == 1) return;
            var registered = scenes.Where(item => item.path != ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(registered).ToArray();
        }

        internal static void Save(Scene scene, LevelTarget target)
        {
            if (!EditorSceneManager.SaveScene(scene, target.ScenePath))
                throw new IOException($"Project ECHO: Không lưu được {target.ScenePath}.");
            AssetDatabase.SaveAssets();
        }

        internal static GameObject FindOfficeModel() => FindModel(Layout(Office));

        private static GameObject FindModel(LevelLayout layout)
        {
            if (string.IsNullOrEmpty(layout.modelFolder) || !AssetDatabase.IsValidFolder(layout.modelFolder)) return null;
            var models = AssetDatabase.FindAssets("t:Model", new[] { layout.modelFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).ToArray();
            string preferred = models.FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == layout.modelName);
            return preferred != null || models.Length == 1 ? AssetDatabase.LoadAssetAtPath<GameObject>(preferred ?? models[0]) : null;
        }

        internal static IEnumerable<T> Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
