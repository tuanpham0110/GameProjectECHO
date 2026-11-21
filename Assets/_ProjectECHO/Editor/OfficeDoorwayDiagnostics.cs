using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectEcho.Editor
{
    /// <summary>
    /// Mỗi biển EXIT trong FBX treo trên một ô cửa. Công cụ đo độ rộng thông thủy và liệt kê collider chặn ô cửa,
    /// giúp phân biệt cánh cửa đóng, khung/ngưỡng cửa hay tường liền không có lỗ.
    /// </summary>
    public static class OfficeDoorwayDiagnostics
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [MenuItem("Tools/Project ECHO/Diagnose Doorways", priority = 23)]
        public static void Diagnose()
        {
            var scene = SceneManager.GetActiveScene();
            var environment = scene.GetRootGameObjects().Select(r => r.transform.Find(GeneratedGroups.Environment)).FirstOrDefault(t => t != null);
            if (environment == null) throw new InvalidOperationException("Project ECHO: Mở scene Office đã chạy Generate Level.");
            Physics.SyncTransforms();
            var signs = environment.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.StartsWith("EXIT", StringComparison.OrdinalIgnoreCase) && r.GetComponentInParent<Collider>() == null).ToArray();
            var report = new StringBuilder("# Office · chẩn đoán ô cửa theo biển EXIT\n\n");
            report.AppendLine("Đo ở cao độ 0,3–1,7 m dưới mỗi biển. `Thông thủy` là khoảng trống dọc tường; `Chặn tâm` là collider cắt capsule r = 0,3 m tại tâm ô cửa.\n");
            report.AppendLine("| Biển | Tâm ô cửa | Hướng tường | Thông thủy (m) | Chặn tâm |");
            report.AppendLine("| --- | --- | --- | --- | --- |");
            var hits = new Collider[24];
            foreach (var sign in signs)
            {
                Bounds b = sign.bounds;
                bool wallAlongZ = b.size.x < b.size.z;
                Vector3 along = wallAlongZ ? Vector3.forward : Vector3.right;
                Vector3 center = new(b.center.x, 0, b.center.z);
                float width = ClearWidth(center, along);
                int count = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * 0.35f, center + Vector3.up * 1.65f, 0.3f, hits, 1, QueryTriggerInteraction.Ignore);
                string blockers = string.Join(", ", hits.Take(count).Where(c => c.bounds.max.y > 0.06f)
                    .Select(c => $"`{c.name}` {Size(c.bounds.size)} @{Size(c.bounds.center)}").Distinct());
                report.AppendLine($"| {sign.name} | {Size(center)} | {(wallAlongZ ? "dọc Z" : "dọc X")} | {width.ToString("0.00", Invariant)} | {(blockers.Length == 0 ? "trống" : blockers)} |");
            }
            Directory.CreateDirectory(OfficeLevelTools.ReportFolder);
            File.WriteAllText(Path.Combine(OfficeLevelTools.ReportFolder, "OfficeDoorways.md"), report.ToString(), Encoding.UTF8);
            Debug.Log($"Project ECHO: Đã chẩn đoán {signs.Length} ô cửa.");
        }

        private static float ClearWidth(Vector3 center, Vector3 along)
        {
            float Extent(Vector3 direction)
            {
                for (float d = 0; d < 3; d += 0.05f)
                {
                    Vector3 p = center + direction * d;
                    if (Physics.CheckCapsule(p + Vector3.up * 0.35f, p + Vector3.up * 1.65f, 0.02f, 1, QueryTriggerInteraction.Ignore)) return d;
                }
                return 3;
            }
            return Extent(along) + Extent(-along);
        }

        private static string Size(Vector3 v) => $"({v.x.ToString("0.##", Invariant)}, {v.y.ToString("0.##", Invariant)}, {v.z.ToString("0.##", Invariant)})";

        /// <summary>Điểm vào batch: mở scene Office rồi chẩn đoán.</summary>
        public static void DiagnoseOffice()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(OfficeLevelTools.ScenePath);
            Diagnose();
        }
    }
}
