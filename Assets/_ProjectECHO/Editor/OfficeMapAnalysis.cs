using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ProjectEcho.Editor
{
    /// <summary>
    /// Đo hình học thực của FBX Office trong một preview scene tách biệt, xuất ảnh mặt bằng và danh sách đối tượng
    /// để bố trí gameplay dựa trên dữ liệu đo được thay vì suy đoán theo tên.
    /// </summary>
    public static class OfficeMapAnalysis
    {
        internal const string OutputFolder = "Docs/Technical/OfficeMap";
        private const float Cell = 0.25f;
        private const int Scale = 3;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [MenuItem("Tools/Project ECHO/Analyze Office Map", priority = 22)]
        public static void Analyze()
        {
            var model = OfficeLevelTools.FindOfficeModel();
            if (model == null) throw new InvalidOperationException("Project ECHO: Không tìm thấy FBX Office.");
            Directory.CreateDirectory(OutputFolder);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var filters = instance.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
                foreach (var filter in filters)
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                Physics.SyncTransforms();
                var physics = preview.GetPhysicsScene();
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                var report = new StringBuilder();
                report.AppendLine("# Office FBX · số đo hình học\n");
                report.AppendLine($"Sinh bởi **Tools → Project ECHO → Analyze Office Map** ({DateTime.Now:yyyy-MM-dd HH:mm}). Model đặt tại gốc tọa độ, không xoay.\n");
                report.AppendLine($"- Bounds: min {V(bounds.min)}, max {V(bounds.max)}, size {V(bounds.size)}.");
                report.AppendLine($"- MeshFilter: {filters.Length}, Renderer: {renderers.Length}, đang active: {renderers.Count(r => r.gameObject.activeInHierarchy)}.");
                Raster(instance.transform, physics, bounds, report);
                RenderTopDown(preview, instance, bounds);
                Inventory(instance.transform, model, report);
                File.WriteAllText(Path.Combine(OutputFolder, "OfficeMapAnalysis.md"), report.ToString(), Encoding.UTF8);
                Debug.Log($"Project ECHO: Đã ghi phân tích Office vào {OutputFolder}.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void Raster(Transform root, PhysicsScene physics, Bounds bounds, StringBuilder report)
        {
            int width = Mathf.CeilToInt(bounds.size.x / Cell);
            int depth = Mathf.CeilToInt(bounds.size.z / Cell);
            var ground = new float[width, depth];
            var top = new float[width, depth];
            var kind = new byte[width, depth]; // 0 trống, 1 đi được, 2 vật thấp, 3 vật cao, 4 tường
            var hits = new RaycastHit[64];
            var overlaps = new Collider[32];
            var groundHistogram = new SortedDictionary<float, int>();
            var ceilingHistogram = new SortedDictionary<float, int>();
            var blockers = new Dictionary<string, int>();
            float rayTop = bounds.max.y + 1;
            float rayLength = bounds.size.y + 2;

            for (int i = 0; i < width; i++)
            for (int j = 0; j < depth; j++)
            {
                float x = bounds.min.x + (i + 0.5f) * Cell;
                float z = bounds.min.z + (j + 0.5f) * Cell;
                int count = physics.Raycast(new Vector3(x, rayTop, z), Vector3.down, hits, rayLength, ~0, QueryTriggerInteraction.Collide);
                float floor = float.PositiveInfinity;
                for (int h = 0; h < count; h++)
                    if (Owned(hits[h].collider, root) && hits[h].normal.y > 0.7f) floor = Mathf.Min(floor, hits[h].point.y);
                if (float.IsPositiveInfinity(floor))
                {
                    kind[i, j] = 0;
                    continue;
                }
                ground[i, j] = floor;
                Bump(groundHistogram, Mathf.Round(floor * 10) / 10);
                float ceiling = float.PositiveInfinity;
                float obstacleTop = floor;
                for (int h = 0; h < count; h++)
                {
                    if (!Owned(hits[h].collider, root)) continue;
                    float y = hits[h].point.y;
                    if (y > floor + 2.05f) ceiling = Mathf.Min(ceiling, y);
                    else if (y > floor + 0.08f) obstacleTop = Mathf.Max(obstacleTop, y);
                }
                if (!float.IsPositiveInfinity(ceiling)) Bump(ceilingHistogram, Mathf.Round((ceiling - floor) * 10) / 10);
                top[i, j] = obstacleTop - floor;

                var body = new Vector3(x, floor + 0.3f + 0.75f, z);
                int touching = physics.OverlapBox(body, new Vector3(Cell * 0.45f, 0.75f, Cell * 0.45f), overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                string blocker = null;
                for (int o = 0; o < touching; o++)
                    if (Owned(overlaps[o], root)) { blocker = overlaps[o].name; break; }
                if (blocker == null) kind[i, j] = 1;
                else
                {
                    Bump(blockers, Group(blocker));
                    bool wall = blocker.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0;
                    kind[i, j] = wall ? (byte)4 : top[i, j] < 1.1f ? (byte)2 : (byte)3;
                }
            }

            float minGround = groundHistogram.Count > 0 ? groundHistogram.Keys.First() : 0;
            var image = new Texture2D(width * Scale, depth * Scale, TextureFormat.RGB24, false);
            for (int i = 0; i < width; i++)
            for (int j = 0; j < depth; j++)
            {
                Color32 color = kind[i, j] switch
                {
                    0 => new Color32(12, 12, 14, 255),
                    1 => Walkable(ground[i, j] - minGround),
                    2 => new Color32(226, 142, 48, 255),
                    3 => new Color32(170, 52, 52, 255),
                    _ => new Color32(40, 64, 150, 255)
                };
                float x = bounds.min.x + i * Cell;
                float z = bounds.min.z + j * Cell;
                bool major = Mathf.Abs(Mathf.Repeat(x + 0.001f, 10)) < Cell || Mathf.Abs(Mathf.Repeat(z + 0.001f, 10)) < Cell;
                bool minor = Mathf.Abs(Mathf.Repeat(x + 0.001f, 5)) < Cell || Mathf.Abs(Mathf.Repeat(z + 0.001f, 5)) < Cell;
                bool origin = Mathf.Abs(x) < Cell * 0.5f || Mathf.Abs(z) < Cell * 0.5f;
                for (int a = 0; a < Scale; a++)
                for (int b = 0; b < Scale; b++)
                {
                    Color32 pixel = color;
                    if ((a == 0 && (major || minor || origin)) || (b == 0 && (major || minor || origin)))
                        pixel = origin ? new Color32(0, 255, 120, 255) : major ? Darken(color, 0.45f) : Darken(color, 0.75f);
                    image.SetPixel(i * Scale + a, j * Scale + b, pixel);
                }
            }
            image.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, "office_walkable.png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);

            int walkable = 0, low = 0, tall = 0, walls = 0, empty = 0;
            foreach (byte k in kind)
                switch (k) { case 0: empty++; break; case 1: walkable++; break; case 2: low++; break; case 3: tall++; break; default: walls++; break; }
            report.AppendLine($"\n## Lưới {Cell} m ({width} × {depth} ô)\n");
            report.AppendLine($"Ảnh `office_walkable.png`: {Scale} px mỗi ô, trái = X min ({F(bounds.min.x)}), dưới = Z min ({F(bounds.min.z)}), trên = Z max. Vạch đậm mỗi 10 m, vạch mờ mỗi 5 m, vạch xanh lá là X = 0 / Z = 0.");
            report.AppendLine("Màu: xám = đi được (sáng hơn = sàn cao hơn), cam = vật thấp < 1,1 m, đỏ = vật cao, xanh dương = mesh tên Wall, đen = không có sàn.\n");
            report.AppendLine($"- Ô đi được {walkable}, vật thấp {low}, vật cao {tall}, tường {walls}, không có sàn {empty}.");
            report.AppendLine("- Phân bố cao độ sàn (m → số ô): " + string.Join(", ", groundHistogram.Where(p => p.Value > 20).Select(p => $"{F(p.Key)}→{p.Value}")));
            report.AppendLine("- Phân bố chiều cao trần so với sàn (m → số ô): " + string.Join(", ", ceilingHistogram.Where(p => p.Value > 20).Select(p => $"{F(p.Key)}→{p.Value}")));
            report.AppendLine("- Nhóm chặn nhiều ô nhất: " + string.Join(", ", blockers.OrderByDescending(p => p.Value).Take(40).Select(p => $"{p.Key} ({p.Value})")));
            WriteGrid(kind, ground, bounds, width, depth);
        }

        private static void WriteGrid(byte[,] kind, float[,] ground, Bounds bounds, int width, int depth)
        {
            var text = new StringBuilder();
            text.AppendLine($"# cell={F(Cell)} minX={F(bounds.min.x)} minZ={F(bounds.min.z)} width={width} depth={depth}");
            text.AppendLine("# rows from maxZ to minZ; . walk, o low, # tall, W wall, space no floor; digits not used");
            for (int j = depth - 1; j >= 0; j--)
            {
                for (int i = 0; i < width; i++)
                    text.Append(kind[i, j] switch { 0 => ' ', 1 => '.', 2 => 'o', 3 => '#', _ => 'W' });
                text.AppendLine();
            }
            File.WriteAllText(Path.Combine(OutputFolder, "office_grid.txt"), text.ToString());
            var heights = new StringBuilder();
            heights.AppendLine("# ground height per cell (cm), same layout; -1 = no floor");
            for (int j = depth - 1; j >= 0; j--)
            {
                for (int i = 0; i < width; i++)
                    heights.Append(kind[i, j] == 0 ? "-1" : Mathf.RoundToInt(ground[i, j] * 100).ToString(Invariant)).Append(i + 1 < width ? "," : "");
                heights.AppendLine();
            }
            File.WriteAllText(Path.Combine(OutputFolder, "office_ground_cm.csv"), heights.ToString());
        }

        private static void RenderTopDown(UnityEngine.SceneManagement.Scene preview, GameObject instance, Bounds bounds)
        {
            GameObject cameraObject = null, lightObject = null;
            RenderTexture target = null;
            Texture2D image = null;
            try
            {
                lightObject = new GameObject("Analysis light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, preview);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                light.transform.rotation = Quaternion.Euler(80, 20, 0);
                cameraObject = new GameObject("Analysis camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = preview;
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.z;
                camera.aspect = bounds.size.x / bounds.size.z;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                float eye = bounds.min.y + 2.4f;
                camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, eye, bounds.center.z), Quaternion.Euler(90, 0, 0));
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = bounds.size.y + 2;
                int w = Mathf.RoundToInt(bounds.size.x * 24), h = Mathf.RoundToInt(bounds.size.z * 24);
                target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                else camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(OutputFolder, "office_topdown.png"), image.EncodeToPNG());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Project ECHO: Không render được ảnh top-down. {exception.Message}");
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                if (target != null) Object.DestroyImmediate(target);
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);
                if (lightObject != null) Object.DestroyImmediate(lightObject);
            }
        }

        private static void Inventory(Transform root, GameObject model, StringBuilder report)
        {
            report.AppendLine("\n## Nhóm cấp 1 trong model (world bounds, m)\n");
            report.AppendLine("| Tên | Tâm | Kích thước | Mesh | Active |");
            report.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (Transform child in root)
            {
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    var lightInfo = child.GetComponentInChildren<Light>(true);
                    if (lightInfo != null) report.AppendLine($"| {Escape(child.name)} | {V(child.position)} | light {lightInfo.type} | 0 | {child.gameObject.activeSelf} |");
                    continue;
                }
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                report.AppendLine($"| {Escape(child.name)} | {V(b.center)} | {V(b.size)} | {renderers.Length} | {child.gameObject.activeSelf} |");
            }

            report.AppendLine("\n## Mesh lớn (cạnh lớn nhất ≥ 2,5 m)\n");
            report.AppendLine("| Đường dẫn | Tâm | Kích thước |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var size = renderer.bounds.size;
                if (Mathf.Max(size.x, size.y, size.z) < 2.5f) continue;
                report.AppendLine($"| {Escape(AnimationUtility.CalculateTransformPath(renderer.transform, root))} | {V(renderer.bounds.center)} | {V(size)} |");
            }

            report.AppendLine("\n## Animation clip trong FBX\n");
            string path = AssetDatabase.GetAssetPath(model);
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)))
            {
                var targets = AnimationUtility.GetCurveBindings(clip).Select(binding => binding.path).Distinct().ToArray();
                report.AppendLine($"- `{Escape(clip.name)}` dài {F(clip.length)} s, {targets.Length} đối tượng: {string.Join(", ", targets.Take(12).Select(Escape))}{(targets.Length > 12 ? ", …" : "")}");
            }
        }

        private static bool Owned(Collider collider, Transform root) => collider != null && collider.transform.IsChildOf(root);

        private static string Group(string name)
        {
            int cut = name.IndexOfAny(new[] { '.', '_', ' ' });
            return cut > 0 ? name[..cut] : name;
        }

        private static void Bump<T>(IDictionary<T, int> map, T key) => map[key] = map.TryGetValue(key, out int value) ? value + 1 : 1;

        private static Color32 Walkable(float height)
        {
            byte level = (byte)Mathf.Clamp(150 + height * 60, 120, 245);
            return height > 0.3f ? new Color32((byte)(level * 0.7f), level, (byte)(level * 0.7f), 255) : new Color32(level, level, level, 255);
        }

        private static Color32 Darken(Color32 c, float k) => new((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), 255);
        private static string F(float value) => value.ToString("0.##", Invariant);
        private static string V(Vector3 v) => $"({F(v.x)}, {F(v.y)}, {F(v.z)})";
        private static string Escape(string text) => text.Replace("|", "\\|");
    }
}
