using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectEcho.Editor
{
    /// <summary>
    /// Chụp ảnh từ các điểm gameplay chính để rà soát ánh sáng, collider và bố trí mà không cần mở Play Mode.
    /// Ảnh ghi vào Docs/Technical/OfficeMap/preview_*.png.
    /// </summary>
    internal static class LevelPreviews
    {
        private const int Width = 1280, Height = 720;

        internal static void Capture(Scene scene, LevelTarget target)
        {
            var level = OfficeLevelTools.Components<LevelManager>(scene).SingleOrDefault();
            if (level == null) throw new InvalidOperationException("Project ECHO: Scene chưa có LevelManager.");
            Directory.CreateDirectory(OfficeLevelTools.ReportFolder);
            string prefix = Path.Combine(OfficeLevelTools.ReportFolder, $"preview_{target.SceneName}_");

            Shot(prefix + "spawn.png", Behind(level.playerSpawn, 3.1f, 1.75f), level.playerSpawn.position + level.playerSpawn.forward * 6 + Vector3.up * 1.2f);
            var relay = level.relay.transform;
            Shot(prefix + "relay.png", relay.position + relay.rotation * new Vector3(2.2f, 2.0f, -1.6f), relay.position + Vector3.up * 0.7f);
            Shot(prefix + "exit.png", Behind(level.exit.transform, 5f, 2.2f), level.exit.transform.position + Vector3.up * 1.2f);
            Shot(prefix + "hunter.png", Behind(level.hunterSpawn, 4f, 2f), level.hunterSpawn.position + Vector3.up * 1f);
            var data = level.interactions.OfType<DataPickup>().FirstOrDefault();
            if (data != null) Shot(prefix + "data.png", data.transform.position + new Vector3(2.2f, 1.2f, -2.2f), data.transform.position);
            Overview(prefix + "overview.png", level);
            Debug.Log($"Project ECHO: Đã chụp ảnh xem trước vào {OfficeLevelTools.ReportFolder}.");
        }

        /// <summary>
        /// Xuất NavMesh đã bake dưới dạng ảnh: vùng liên thông với Player Spawn màu teal, các đảo tách rời màu cam/đỏ.
        /// Điểm: trắng = Player Spawn, đỏ = Hunter, vàng = dữ liệu, xanh dương = Relay, xanh lá = cổng, tím = waypoint.
        /// </summary>
        internal static void ExportNavMesh(string file, LevelManager level, Rect area)
        {
            const float pixelsPerMeter = 8;
            var triangulation = NavMesh.CalculateTriangulation();
            var vertices = triangulation.vertices;
            var indices = triangulation.indices;
            var parent = new int[vertices.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
            void Union(int a, int b) { a = Find(a); b = Find(b); if (a != b) parent[a] = b; }
            var welded = new System.Collections.Generic.Dictionary<Vector3Int, int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var key = Vector3Int.RoundToInt(vertices[i] * 50);
                if (welded.TryGetValue(key, out int other)) Union(i, other);
                else welded[key] = i;
            }
            for (int t = 0; t < indices.Length; t += 3)
            {
                Union(indices[t], indices[t + 1]);
                Union(indices[t], indices[t + 2]);
            }
            int reachable = -1;
            if (NavMesh.SamplePosition(level.playerSpawn.position, out var spawnHit, 2, NavMesh.AllAreas))
            {
                float best = float.MaxValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    float d = (vertices[i] - spawnHit.position).sqrMagnitude;
                    if (d < best) { best = d; reachable = Find(i); }
                }
            }

            int width = Mathf.CeilToInt(area.width * pixelsPerMeter), height = Mathf.CeilToInt(area.height * pixelsPerMeter);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(16, 18, 22, 255);
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float wx = area.xMin + x / pixelsPerMeter, wz = area.yMin + y / pixelsPerMeter;
                if (Mathf.Abs(Mathf.Repeat(wx, 5)) < 1 / pixelsPerMeter || Mathf.Abs(Mathf.Repeat(wz, 5)) < 1 / pixelsPerMeter)
                    pixels[y * width + x] = new Color32(40, 44, 52, 255);
            }
            for (int t = 0; t < indices.Length; t += 3)
            {
                Vector3 a = vertices[indices[t]], b = vertices[indices[t + 1]], c = vertices[indices[t + 2]];
                if ((a.y + b.y + c.y) / 3 > level.playerSpawn.position.y + 0.45f) continue;
                int component = Find(indices[t]);
                Color32 color = component == reachable ? new Color32(40, 190, 175, 255)
                    : (Color32)Color.HSVToRGB(Mathf.Repeat(component * 0.0731f, 0.12f), 0.85f, 0.95f);
                Vector2 pa = Pixel(a), pb = Pixel(b), pc = Pixel(c);
                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, pb.x, pc.x))), maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(pa.x, pb.x, pc.x)));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, pb.y, pc.y))), maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(pa.y, pb.y, pc.y)));
                for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    if (Inside(new Vector2(x + 0.5f, y + 0.5f), pa, pb, pc)) pixels[y * width + x] = color;
            }

            void Dot(Vector3 world, Color32 color, int radius)
            {
                Vector2 p = Pixel(world);
                for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                {
                    int px = Mathf.RoundToInt(p.x) + x, py = Mathf.RoundToInt(p.y) + y;
                    if (px >= 0 && py >= 0 && px < width && py < height) pixels[py * width + px] = color;
                }
            }
            foreach (var waypoint in level.waypoints) Dot(waypoint.position, new Color32(190, 90, 255, 255), 3);
            foreach (var data in level.interactions.OfType<DataPickup>()) Dot(data.transform.position, new Color32(255, 230, 40, 255), 4);
            Dot(level.relay.transform.position, new Color32(60, 120, 255, 255), 6);
            Dot(level.exit.transform.position, new Color32(60, 255, 90, 255), 6);
            Dot(level.hunterSpawn.position, new Color32(255, 40, 40, 255), 5);
            Dot(level.playerSpawn.position, Color.white, 5);

            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.SetPixels32(pixels);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            Object.DestroyImmediate(image);

            Vector2 Pixel(Vector3 world) => new((world.x - area.xMin) * pixelsPerMeter, (world.z - area.yMin) * pixelsPerMeter);
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool negative = d1 < 0 || d2 < 0 || d3 < 0, positive = d1 > 0 || d2 > 0 || d3 > 0;
            return !(negative && positive);
        }

        private static Vector3 Behind(Transform anchor, float distance, float height) =>
            anchor.position - anchor.forward * distance + Vector3.up * height;

        private static void Shot(string file, Vector3 eye, Vector3 focus)
        {
            Render(file, camera =>
            {
                camera.fieldOfView = 60;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye));
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 120;
            }, Width, Height);
        }

        private static void Overview(string file, LevelManager level)
        {
            var boundsSource = level.navigation.navMeshData != null ? level.navigation.navMeshData.sourceBounds : new Bounds(Vector3.zero, Vector3.one * 60);
            Bounds bounds = boundsSource;
            int width = 1100;
            int height = Mathf.RoundToInt(width * bounds.size.z / Mathf.Max(1, bounds.size.x));
            Render(file, camera =>
            {
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.z;
                camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 4.3f, bounds.center.z), Quaternion.Euler(90, 0, 0));
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10;
            }, width, Mathf.Clamp(height, 200, 2000));
        }

        private static void Render(string file, Action<Camera> configure, int width, int height)
        {
            var cameraObject = new GameObject("Preview camera") { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.03f, 0.05f, 0.07f);
                camera.targetTexture = target;
                configure(camera);
                camera.aspect = (float)width / height;
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                else camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(file, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
