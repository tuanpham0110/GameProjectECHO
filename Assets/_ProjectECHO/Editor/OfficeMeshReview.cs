using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectEcho.Editor
{
    internal static class OfficeMeshReview
    {
        private const string Menu = "Tools/Project ECHO/";

        [MenuItem(Menu + "Review Office Geometry", priority = 20)]
        public static void ExportReview()
        {
            var model = OfficeLevelTools.FindOfficeModel();
            if (model == null) throw new InvalidOperationException("Project ECHO: Không tìm thấy FBX Office.");
            var meshes = model.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh != null).ToArray();
            var report = new StringBuilder("# Office FBX · đề xuất collider\n\n");
            report.AppendLine($"Asset `{AssetDatabase.GetAssetPath(model)}` có {meshes.Length} MeshFilter. Tên chỉ dùng để ưu tiên rà soát.\n");
            report.AppendLine("| Đường dẫn trong model | Kích thước local của mesh (m) | Đề xuất |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var filter in meshes)
            {
                string path = AnimationUtility.CalculateTransformPath(filter.transform, model.transform).Replace("|", "\\|");
                report.AppendLine($"| {path} | {filter.sharedMesh.bounds.size.ToString("F2")} | {Recommendation(filter)} |");
            }
            const string output = "Docs/Technical/OfficeImportReport.md";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, report.ToString(), Encoding.UTF8);
            Debug.Log($"Project ECHO: Đã ghi {output}.");
        }

        [MenuItem(Menu + "Create Colliders For Selected Office Meshes", priority = 21)]
        public static void AddApprovedColliders()
        {
            int added = 0;
            foreach (var selected in Selection.gameObjects)
            {
                if (!selected.scene.IsValid() || !selected.TryGetComponent<MeshFilter>(out var filter) ||
                    filter.sharedMesh == null || selected.GetComponent<Collider>() != null) continue;
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(selected);
                if (source == null || !AssetDatabase.GetAssetPath(source).StartsWith(OfficeLevelTools.OfficeFolder + "/", StringComparison.Ordinal)) continue;
                if (LooksRectangular(filter))
                {
                    var box = Undo.AddComponent<BoxCollider>(selected);
                    Bounds bounds = filter.sharedMesh.bounds;
                    box.center = bounds.center;
                    box.size = Vector3.Max(bounds.size, Vector3.one * 0.04f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                }
                else
                {
                    var mesh = Undo.AddComponent<MeshCollider>(selected);
                    mesh.sharedMesh = filter.sharedMesh;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mesh);
                }
                added++;
            }
            Debug.Log($"Project ECHO: Đã tạo {added} collider trên mesh Office được chọn trong scene.");
        }

        private static string Recommendation(MeshFilter filter)
        {
            if (Regex.IsMatch(filter.name, "handle|trim|letter|logo|decal|cushion", RegexOptions.IgnoreCase))
                return "Chi tiết nhỏ, xác nhận trước khi thêm collider";
            if (Regex.IsMatch(filter.name, "door|gate|drawer", RegexOptions.IgnoreCase))
                return "Kiểm tra pivot/cụm chuyển động rồi gán BoxCollider và component tương tác";
            if (LooksRectangular(filter)) return "BoxCollider nếu mesh là mặt chặn đã xác nhận";
            if (Regex.IsMatch(filter.name, "floor|wall|stair|partition|ceiling", RegexOptions.IgnoreCase))
                return "Kiểm tra biên/tầng, MeshCollider tĩnh hoặc proxy đơn giản";
            return "Chưa phân loại, cần kiểm tra";
        }

        private static bool LooksRectangular(MeshFilter filter)
        {
            var mesh = filter.sharedMesh;
            if (mesh.vertexCount is < 4 or > 36 || mesh.subMeshCount != 1) return false;
            string name = filter.name;
            if (!Regex.IsMatch(name, @"(^|[^a-z])(floor|wall|ceiling|panel)([^a-z]|$)", RegexOptions.IgnoreCase)) return false;
            var size = mesh.bounds.size;
            float minimum = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            float maximum = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (maximum <= 0.3f || minimum >= maximum * 0.12f) return false;

            int thinAxis = size.x == minimum ? 0 : size.y == minimum ? 1 : 2;
            int widthAxis = (thinAxis + 1) % 3;
            int lengthAxis = (thinAxis + 2) % 3;
            if (Mathf.Min(size[widthAxis], size[lengthAxis]) < 0.2f) return false;
            int occupiedCorners = 0;
            var bounds = mesh.bounds;
            float tolerance = maximum * 0.01f;
            foreach (Vector3 point in mesh.vertices)
            {
                var corner = point - bounds.center;
                if (Mathf.Abs(Mathf.Abs(corner[widthAxis]) - bounds.extents[widthAxis]) > tolerance ||
                    Mathf.Abs(Mathf.Abs(corner[lengthAxis]) - bounds.extents[lengthAxis]) > tolerance) return false;
                int index = (corner[widthAxis] > 0 ? 1 : 0) | (corner[lengthAxis] > 0 ? 2 : 0);
                occupiedCorners |= 1 << index;
            }
            return occupiedCorners == 15;
        }
    }
}
