using System;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ProjectEcho.Editor
{
    internal static class OfficeSceneFactory
    {
        private const string Root = OfficeLevelTools.Root;
        internal static void CreateBlockout(Transform root)
        {
            var architecture = Node("Architecture", root);
            var floor = Paint("Concrete", new Color(0.17f, 0.23f, 0.26f));
            var wall = Paint("Walls", new Color(0.25f, 0.32f, 0.36f));
            var cover = Paint("Shelving", new Color(0.11f, 0.20f, 0.24f));
            var stripe = Paint("Floor marking", new Color(0.1f, 0.66f, 0.61f), 0.22f);
            Box("Concrete floor", architecture, new Vector3(0, -0.2f, 0), new Vector3(38, 0.4f, 28), floor);
            foreach (float x in new[] { -19f, 19f })
                Box("Perimeter wall", architecture, new Vector3(x, 1.7f, 0), new Vector3(0.45f, 3.4f, 28), wall);
            foreach (float z in new[] { -14f, 14f })
                Box("Perimeter wall", architecture, new Vector3(0, 1.7f, z), new Vector3(38, 3.4f, 0.45f), wall);

            foreach (var segment in new[] { new Vector2(-9, 8), new Vector2(8.5f, 9), new Vector2(1.9f, 1.8f) })
                Box("Central wall", architecture, new Vector3(0, 1.55f, segment.x), new Vector3(0.38f, 3.1f, segment.y), wall);
            foreach (var segment in new[] { new Vector2(-16, 5), new Vector2(-5.5f, 7), new Vector2(5, 6), new Vector2(16, 5) })
                Box("Office partition", architecture, new Vector3(segment.x, 1.55f, 1), new Vector3(segment.y, 3.1f, 0.32f), wall);
            foreach (var p in new[] { new Vector2(-12, -1.8f), new Vector2(-5, 6), new Vector2(9.7f, -6), new Vector2(10, 7) })
                Box("Tall shelving", architecture, new Vector3(p.x, 0.95f, p.y), new Vector3(2.9f, 1.9f, 0.8f), cover);
            Box("Low obstacle", architecture, new Vector3(-9, 0.2f, -8), new Vector3(1.7f, 0.4f, 0.8f), cover);
            Box("Cover near west wall", architecture, new Vector3(-15, 1.15f, 7), new Vector3(2.6f, 2.3f, 0.4f), cover);
            Box("Cover near east wall", architecture, new Vector3(14, 1.0f, -9), new Vector3(0.5f, 2, 2.2f), cover);
            Box("Relay deck marking", architecture, new Vector3(-15, 0.013f, 10.1f), new Vector3(4.8f, 0.02f, 0.12f), stripe, false);
            Box("Exit floor marking", architecture, new Vector3(12.5f, 0.013f, 11), new Vector3(3, 0.02f, 0.12f), stripe, false);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.21f, 0.26f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.034f, 0.062f, 0.083f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.027f;
            var fill = Node("Ambient light", root, new Vector3(0, 5, 0)).gameObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.64f;
            fill.color = new Color(0.59f, 0.77f, 1);
            fill.transform.rotation = Quaternion.Euler(57, -32, 0);
            fill.shadows = LightShadows.Soft;
            var lightMarks = Node("Ceiling fixtures", root);
            foreach (var point in new[] { new Vector3(-12, 3.8f, -7), new Vector3(-12, 3.8f, 8), new Vector3(12, 3.8f, -7), new Vector3(12, 3.8f, 8), new Vector3(0, 3.8f, -1) })
            {
                Box("Fixture", lightMarks, point, new Vector3(1.9f, 0.11f, 0.18f), stripe, false);
                PointLight(lightMarks, point - Vector3.up * 0.15f, new Color(0.26f, 0.88f, 1), 15, 10);
            }
            PointLight(lightMarks, new Vector3(12.5f, 2.25f, 11.5f), new Color(1, 0.51f, 0.19f), 11, 6);
        }

        /// <summary>
        /// Dựng hệ thống gameplay theo LevelLayout dưới <paramref name="world"/>. Gọi lại an toàn: công cụ xóa các nhóm
        /// sinh tự động trước khi dựng lại. <paramref name="environment"/> là instance model (null với blockout).
        /// </summary>
        internal static LevelManager SetupGameplay(Transform world, LevelLayout layout, LevelRules rules, Transform environment)
        {
            var settings = Node(GeneratedGroups.Systems, world);
            var markers = Node(GeneratedGroups.Markers, world);
            var objects = Node(GeneratedGroups.Objects, world);
            var playerSpawn = Node("Player Spawn", markers, layout.playerSpawn.position);
            playerSpawn.rotation = layout.playerSpawn.Rotation;
            var hunterSpawn = Node("Hunter Spawn", markers, layout.hunterSpawn.position);
            hunterSpawn.rotation = layout.hunterSpawn.Rotation;
            var route = Node("Patrol Waypoints", markers);
            var waypoints = layout.waypoints.Select((p, i) => Node($"Waypoint {i + 1:00}", route, p)).ToArray();
            var level = settings.gameObject.AddComponent<LevelManager>();
            var game = settings.gameObject.AddComponent<GameManager>();
            game.level = level;
            level.game = game;
            level.rules = rules;
            level.playerSpawn = playerSpawn;
            level.hunterSpawn = hunterSpawn;
            level.waypoints = waypoints;
            level.navigation = world.GetComponent<NavMeshSurface>();

            var player = Instance<PlayerController>("TAY", objects, playerSpawn.position, MakePlayer);
            player.transform.rotation = playerSpawn.rotation;
            var hunter = Instance<HunterBrain>("Hunter", objects, hunterSpawn.position, MakeHunter);
            hunter.transform.rotation = hunterSpawn.rotation;
            hunter.level = level;
            level.player = player;
            level.hunter = hunter;
            level.cameraView = MakeCamera(settings, player, playerSpawn, level.worldMask);
            player.equipment.sensorMarker = Box("Sensor ping", objects, Vector3.zero, Vector3.one * 0.19f,
                Paint("Sensor", new Color(1, 0.35f, 0.23f), 1.2f), false);
            player.equipment.sensorMarker.SetActive(false);

            var objectives = Node("Objectives", objects);
            UpgradeDataPrefab();
            foreach (var point in layout.data)
            {
                var data = Instance<DataPickup>("Mission Data", objectives, point.position + Vector3.up * 0.75f, MakeData);
                data.name = $"Data · {point.displayName}";
                data.dataId = point.id;
                data.displayName = point.displayName;
            }
            level.relay = Instance<DataRelay>("Data Relay", objectives, layout.relay.position + Vector3.up * 0.02f, MakeRelay);
            level.relay.transform.rotation = layout.relay.Rotation;
            level.exit = Instance<ExitGate>("Exit Gate", objectives, layout.exit.position + Vector3.up * 0.02f, MakeExit);
            level.exit.transform.rotation = layout.exit.Rotation;
            var supports = Node("Support items", objects);
            foreach (var point in layout.supports)
            {
                string prefab = point.item == SupportItem.Emp ? "EMP Mine Pickup" : "Stun Grenade Pickup";
                if (point.item == SupportItem.Armor) continue;
                Instance<SupportPickup>(prefab, supports, point.position + Vector3.up * 0.65f, () => MakeSupport(point.item));
            }

            var doors = Node("Doors", objects);
            foreach (var point in layout.doors) PlaceDoor(doors, point, environment);
            var hides = Node("Hide spots", objects);
            foreach (var point in layout.hideSpots) PlaceHide(hides, point);
            level.interactions = objects.GetComponentsInChildren<Interactable>(true);
            MakeHud(settings, game);
            return level;
        }

        /// <summary>Đối tượng động (cửa, cổng) không được bake vào NavMesh tĩnh; trạng thái đóng do NavMeshObstacle xử lý.</summary>
        private static void NotBaked(GameObject target)
        {
            var modifier = target.GetComponent<NavMeshModifier>() ?? target.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }

        private static void PlaceDoor(Transform parent, LevelLayout.DoorPoint point, Transform environment)
        {
            string label = string.IsNullOrWhiteSpace(point.name) ? "Door" : point.name;
            var paths = point.modelPaths ?? Array.Empty<string>();
            if (paths.Length == 0)
            {
                var placeholder = Instance<OfficeDoor>("Partition Door", parent, point.center, MakeDoor);
                placeholder.name = label;
                if (point.size.x > point.size.z) placeholder.transform.rotation = Quaternion.Euler(0, 90, 0);
                placeholder.startsOpen = point.startsOpen;
                placeholder.locked = point.locked;
                NotBaked(placeholder.gameObject);
                return;
            }

            var visuals = paths.Select(path => environment != null ? environment.Find(path) : null).ToArray();
            int missing = Array.IndexOf(visuals, null);
            if (missing >= 0)
                throw new InvalidOperationException($"Project ECHO: Không tìm thấy cánh cửa '{paths[missing]}' trong model.");
            if (point.size == Vector3.zero)
            {
                // Đo khung cửa từ chính các cánh trong model; mặt mỏng nới tối thiểu 0,15 m để collider chặn chắc chắn.
                var renderers = visuals.SelectMany(v => v.GetComponentsInChildren<Renderer>(true)).ToArray();
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                point.center = new Vector3(bounds.center.x, point.center.y, bounds.center.z);
                point.size = new Vector3(Mathf.Max(bounds.size.x + 0.04f, 0.15f), bounds.size.y, Mathf.Max(bounds.size.z + 0.04f, 0.15f));
            }
            var root = Node(label, parent, point.center);
            var door = root.gameObject.AddComponent<OfficeDoor>();
            door.visuals = visuals.Select(v => v.gameObject).ToArray();
            door.startsOpen = point.startsOpen;
            door.locked = point.locked;
            var panel = Node("Door collider", root, Vector3.up * point.size.y * 0.5f).gameObject;
            panel.AddComponent<BoxCollider>().size = point.size;
            door.panel = panel;
            door.obstacle = root.gameObject.AddComponent<NavMeshObstacle>();
            door.obstacle.shape = NavMeshObstacleShape.Box;
            door.obstacle.center = Vector3.up * point.size.y * 0.5f;
            door.obstacle.size = point.size + new Vector3(0.1f, 0, 0.1f);
            door.obstacle.carving = true;
            door.obstacle.enabled = !point.startsOpen && point.locked;
            panel.SetActive(!point.startsOpen);
            NotBaked(root.gameObject);
        }

        private static PlayerController MakePlayer()
        {
            var root = new GameObject("TAY") { layer = 2, tag = "Player" };
            var motor = root.AddComponent<CharacterController>();
            motor.height = 1.8f;
            motor.radius = 0.33f;
            motor.center = Vector3.up * 0.9f;
            motor.skinWidth = 0.04f;
            motor.stepOffset = 0.3f;
            var player = root.AddComponent<PlayerController>();
            player.input = root.AddComponent<PlayerInput>();
            player.tuning = Settings<PlayerTuning>("TAY_Tuning");
            player.interaction = root.AddComponent<InteractionSystem>();
            player.interaction.player = player;
            player.equipment = root.AddComponent<PlayerEquipment>();
            player.equipment.player = player;
            player.body = Shape("Body", root.transform, Vector3.up * 0.9f, new Vector3(0.62f, 0.9f, 0.62f),
                Paint("TAY suit", new Color(0.19f, 0.48f, 0.58f)), PrimitiveType.Capsule, false).GetComponent<Renderer>();
            var light = Node("Scanner", root.transform, new Vector3(0.22f, 1.38f, 0.28f)).gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.3f, 0.92f, 1);
            light.range = 9;
            light.spotAngle = 50;
            light.intensity = 4;
            light.enabled = false;
            player.equipment.scannerLight = light;
            return player;
        }

        private static HunterBrain MakeHunter()
        {
            var root = new GameObject("Hunter") { layer = 2 };
            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = 1.8f;
            agent.radius = 0.4f;
            agent.angularSpeed = 540;
            agent.acceleration = 16;
            agent.stoppingDistance = 0.25f;
            agent.autoRepath = true;
            agent.enabled = false;
            var hunter = root.AddComponent<HunterBrain>();
            hunter.profile = Settings<HunterProfile>("Hunter_Office_Profile");
            Shape("Chassis", root.transform, new Vector3(0, 0.96f, 0), new Vector3(0.76f, 0.96f, 0.76f),
                Paint("Hunter chassis", new Color(0.48f, 0.17f, 0.13f)), PrimitiveType.Capsule, false);
            Box("Visor", root.transform, new Vector3(0, 1.52f, 0.35f), new Vector3(0.42f, 0.12f, 0.08f),
                Paint("Hunter visor", new Color(1, 0.19f, 0.11f), 1.8f), false);
            return hunter;
        }

        private static DataPickup MakeData()
        {
            var root = new GameObject("Mission Data");
            Box("Data cartridge", root.transform, Vector3.zero, new Vector3(0.44f, 0.2f, 0.32f),
                Paint("Data cartridge", new Color(0.14f, 0.92f, 0.83f), 0.55f), false);
            Shape("Signal", root.transform, Vector3.up * 0.24f, Vector3.one * 0.1f,
                Paint("Data signal", new Color(0.35f, 1, 0.96f), 0.8f), PrimitiveType.Sphere, false);
            AddBeacon(root.transform);
            return root.AddComponent<DataPickup>();
        }

        /// <summary>
        /// Cột tín hiệu mảnh nhô lên trên bàn ghế để người chơi thấy gói dữ liệu từ xa (playtest: gói nằm sau màn hình,
        /// tủ chỉ lộ ra ở 2,5–7 m). Dùng lại vật liệu Data signal, không collider, không đổ bóng.
        /// </summary>
        private static void AddBeacon(Transform root)
        {
            var beacon = Shape(DataBeacon, root, Vector3.up * 0.95f, new Vector3(0.035f, 0.7f, 0.035f),
                Paint("Data signal", new Color(0.35f, 1, 0.96f), 0.8f), PrimitiveType.Cylinder, false);
            beacon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private const string DataBeacon = "Beacon";

        /// <summary>Prefab Mission Data đời trước chưa có cột tín hiệu: thêm vào prefab gốc để mọi Level dùng chung.</summary>
        private static void UpgradeDataPrefab()
        {
            string path = $"{Root}/Prefabs/Mission Data.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.transform.Find(DataBeacon) != null) return;
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                AddBeacon(contents.transform);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static DataRelay MakeRelay()
        {
            var root = new GameObject("Data Relay");
            Box("Cabinet", root.transform, Vector3.up * 0.62f, new Vector3(1.24f, 1.24f, 0.62f),
                Paint("Relay casing", new Color(0.17f, 0.29f, 0.39f)), false);
            Box("Screen", root.transform, new Vector3(0, 0.98f, -0.32f), new Vector3(0.83f, 0.27f, 0.04f),
                Paint("Relay screen", new Color(0.13f, 1, 0.88f), 1.25f), false);
            return root.AddComponent<DataRelay>();
        }

        private static ExitGate MakeExit()
        {
            var root = new GameObject("Exit Gate");
            var exit = root.AddComponent<ExitGate>();
            var dark = Paint("Gate frame", new Color(0.19f, 0.3f, 0.35f));
            Box("Jamb", root.transform, new Vector3(-1.4f, 1.2f, 0), new Vector3(0.22f, 2.4f, 0.55f), dark);
            Box("Jamb", root.transform, new Vector3(1.4f, 1.2f, 0), new Vector3(0.22f, 2.4f, 0.55f), dark);
            Box("Lintel", root.transform, new Vector3(0, 2.45f, 0), new Vector3(3, 0.24f, 0.55f), dark);
            exit.barrier = Box("Gate panel", root.transform, Vector3.up * 1.14f, new Vector3(2.6f, 2.28f, 0.21f),
                Paint("Locked gate", new Color(0.54f, 0.28f, 0.1f), 0.2f));
            exit.openIndicator = Box("Gate open indicator", root.transform, new Vector3(0, 2.45f, -0.29f),
                new Vector3(1.65f, 0.10f, 0.03f), Paint("Open gate", new Color(0.18f, 1, 0.44f), 1.1f), false);
            exit.openIndicator.SetActive(false);
            exit.passage = Node("Exit passage", root.transform, new Vector3(0, 0, 0.96f));
            return exit;
        }

        private static OfficeDoor MakeDoor()
        {
            var root = new GameObject("Partition Door");
            var door = root.AddComponent<OfficeDoor>();
            var frame = Paint("Door frame", new Color(0.30f, 0.41f, 0.45f));
            foreach (float z in new[] { -1.4f, 1.4f })
                Box("Door jamb", root.transform, new Vector3(0, 1.25f, z), new Vector3(0.31f, 2.5f, 0.2f), frame);
            door.panel = Box("Door panel", root.transform, new Vector3(0, 1.14f, 0), new Vector3(0.25f, 2.3f, 2.7f),
                Paint("Door panel", new Color(0.32f, 0.49f, 0.55f)));
            door.panel.SetActive(false);
            door.obstacle = root.AddComponent<NavMeshObstacle>();
            door.obstacle.shape = NavMeshObstacleShape.Box;
            door.obstacle.center = new Vector3(0, 1.14f, 0);
            door.obstacle.size = new Vector3(0.36f, 2.3f, 2.72f);
            door.obstacle.carving = true;
            door.obstacle.enabled = false;
            return door;
        }

        private static SupportPickup MakeSupport(SupportItem item)
        {
            var root = new GameObject(item.ToString());
            var support = root.AddComponent<SupportPickup>();
            support.item = item;
            var color = item == SupportItem.Emp ? new Color(0.18f, 0.53f, 1) : new Color(1, 0.64f, 0.24f);
            Shape("Item", root.transform, Vector3.zero, new Vector3(0.32f, 0.14f, 0.32f),
                Paint(item + " pickup", color, 0.6f), PrimitiveType.Cylinder, false);
            return support;
        }

        private static void PlaceHide(Transform parent, LevelLayout.HidePoint point)
        {
            var spot = Node(point.absolute ? "Enclosed hide spot" : "Relative hide spot", parent, point.outside).gameObject.AddComponent<HideSpot>();
            spot.absolute = point.absolute;
            spot.inside = Node("Inside", spot.transform, point.inside - point.outside);
            Vector3 facing = point.outside - point.inside;
            facing.y = 0;
            if (facing.sqrMagnitude > 0.0001f) spot.inside.rotation = Quaternion.LookRotation(facing);
            spot.outside = Node("Outside", spot.transform);
            if (facing.sqrMagnitude > 0.0001f) spot.outside.rotation = Quaternion.LookRotation(facing);
            Box("Hide spot marker", spot.transform, new Vector3(0, 0.04f, 0), new Vector3(0.34f, 0.07f, 0.34f),
                Paint("Hide marker", new Color(0.26f, 0.64f, 0.45f), 0.5f), false);
            if (!point.buildLocker) return;
            var material = Paint("Locker frame", new Color(0.15f, 0.32f, 0.32f));
            var shell = Node("Locker", spot.inside);
            shell.localRotation = Quaternion.Inverse(spot.inside.localRotation) * Quaternion.identity;
            foreach (float z in new[] { -0.7f, 0.7f })
                Box("Locker side", shell, new Vector3(0, 0.95f, z), new Vector3(1.2f, 1.9f, 0.15f), material);
            Box("Locker back", shell, new Vector3(-0.54f, 0.95f, 0), new Vector3(0.13f, 1.9f, 1.4f), material);
            Box("Locker roof", shell, new Vector3(0, 1.9f, 0), new Vector3(1.2f, 0.12f, 1.4f), material);
        }

        private static ThirdPersonCamera MakeCamera(Transform root, PlayerController player, Transform spawn, LayerMask worldMask)
        {
            var camera = Node("Main Camera", root, spawn.position + new Vector3(0, 1.6f, -3.3f)).gameObject;
            camera.tag = "MainCamera";
            var output = camera.AddComponent<Camera>();
            output.nearClipPlane = 0.04f;
            output.farClipPlane = 95;
            output.clearFlags = CameraClearFlags.SolidColor;
            output.backgroundColor = new Color(0.032f, 0.055f, 0.076f);
            camera.AddComponent<AudioListener>();
            camera.AddComponent<CinemachineBrain>();
            var rig = Node("Cinemachine TAY", root).gameObject.AddComponent<CinemachineCamera>();
            var pivot = Node("Camera pivot", root, spawn.position + Vector3.up * 1.58f).gameObject.AddComponent<ThirdPersonCamera>();
            pivot.player = player;
            pivot.cameraRig = rig;
            rig.Follow = pivot.transform;
            var follow = rig.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.CameraDistance = 3.1f;
            follow.ShoulderOffset = new Vector3(0.52f, 0.14f, 0);
            follow.VerticalArmLength = 0.22f;
            follow.CameraSide = 1;
            follow.Damping = new Vector3(0.08f, 0.12f, 0.12f);
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = true, CollisionFilter = worldMask, CameraRadius = 0.18f,
                DampingFromCollision = 0.28f, DampingIntoCollision = 0
            };
            return pivot;
        }

        private static void MakeHud(Transform root, GameManager game)
        {
            var ui = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.transform.SetParent(root, false);
            ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var eventRoot = Node("Event System", root).gameObject;
            var events = eventRoot.AddComponent<EventSystem>();
            eventRoot.AddComponent<InputSystemUIInputModule>();
            var hud = ui.AddComponent<GameplayHud>();
            hud.eventSystem = events;
            hud.game = game;
        }

        private static T Instance<T>(string name, Transform parent, Vector3 position, Func<T> create) where T : Component
        {
            string path = $"{Root}/Prefabs/{name}.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                var staging = create().gameObject;
                try { asset = PrefabUtility.SaveAsPrefabAsset(staging, path); }
                finally { UnityEngine.Object.DestroyImmediate(staging); }
            }
            if (asset == null || !asset.TryGetComponent<T>(out _))
                throw new InvalidOperationException($"Prefab không hợp lệ: {path}");
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            placed.transform.position = position;
            return placed.GetComponent<T>();
        }

        internal static T Settings<T>(string name) where T : ScriptableObject
        {
            string path = $"{Root}/Settings/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Transform Node(string name, Transform parent, Vector3 local = default)
        {
            var transform = new GameObject(name).transform;
            transform.SetParent(parent, false);
            transform.localPosition = local;
            return transform;
        }

        private static void PointLight(Transform parent, Vector3 position, Color color, float range, float intensity)
        {
            var light = Node("Point Light", parent, position).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = true) =>
            Shape(name, parent, position, size, material, PrimitiveType.Cube, collider);

        private static GameObject Shape(string name, Transform parent, Vector3 position, Vector3 size,
            Material material, PrimitiveType type, bool collider)
        {
            var result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = size;
            result.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            return result;
        }

        private static Material Paint(string name, Color color, float emission = 0)
        {
            string path = $"{OfficeLevelTools.AssetsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Không tìm được shader URP/Lit.");
            material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", 0.26f);
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
