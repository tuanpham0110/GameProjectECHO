using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectEcho.Editor
{
    /// <summary>Liệt kê chi phí render tĩnh của scene Office: đèn, bóng, renderer, static flag, material, occlusion.</summary>
    public static class RenderAudit
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [MenuItem("Tools/Project ECHO/Audit Rendering", priority = 40)]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(OfficeLevelTools.ScenePath, OpenSceneMode.Single);
            var report = new StringBuilder();
            report.AppendLine("# Audit render Office_Level01");
            report.AppendLine();
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int casters = renderers.Count(r => r.shadowCastingMode != ShadowCastingMode.Off);
            int batchingStatic = renderers.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic));
            int occluder = renderers.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.OccluderStatic));
            int occludee = renderers.Count(r => GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.OccludeeStatic));
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            var meshes = renderers.Select(r => r.GetComponent<MeshFilter>()?.sharedMesh).Where(m => m != null).Distinct().ToArray();
            long triangles = renderers.Sum(r => (long)(r.GetComponent<MeshFilter>()?.sharedMesh is Mesh m ? m.triangles.Length / 3 : 0));
            report.AppendLine($"- MeshRenderer bật: {renderers.Length}, đổ bóng: {casters}, BatchingStatic: {batchingStatic}, OccluderStatic: {occluder}, OccludeeStatic: {occludee}");
            report.AppendLine($"- Material khác nhau: {materials.Length}, shader: {string.Join(", ", materials.Select(m => m.shader.name).Distinct())}");
            report.AppendLine($"- Mesh khác nhau: {meshes.Length}, tổng tam giác nếu vẽ hết: {triangles:N0}");
            report.AppendLine($"- Occlusion data: {(StaticOcclusionCulling.isRunning ? "đang bake" : StaticOcclusionCulling.umbraDataSize > 0 ? $"{StaticOcclusionCulling.umbraDataSize:N0} byte" : "không có")}");
            report.AppendLine($"- Instancing bật trên {materials.Count(m => m.enableInstancing)} material");
            report.AppendLine();

            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            report.AppendLine($"## Đèn ({lights.Length})");
            report.AppendLine();
            report.AppendLine("| Tên | Loại | Chế độ | Bóng | Tầm (m) | Cường độ | Vị trí |");
            report.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
            foreach (var light in lights.OrderBy(l => l.type).ThenBy(l => l.name))
            {
                Vector3 p = light.transform.position;
                report.AppendLine(string.Format(Invariant, "| {0} | {1} | {2} | {3} | {4:0.0} | {5:0.00} | ({6:0.0}; {7:0.0}; {8:0.0}) |",
                    light.name, light.type, light.lightmapBakeType, light.shadows, light.range, light.intensity, p.x, p.y, p.z));
            }
            report.AppendLine();
            report.AppendLine("## Renderer theo nhóm tên (20 nhóm nhiều nhất)");
            report.AppendLine();
            foreach (var group in renderers.GroupBy(r => Prefix(r.name)).OrderByDescending(g => g.Count()).Take(20))
                report.AppendLine($"- {group.Key}: {group.Count()}");
            Directory.CreateDirectory("Docs/Technical/Playtest");
            File.WriteAllText("Docs/Technical/Playtest/Office_Level01_RenderAudit.md", report.ToString(), new UTF8Encoding(false));
            Debug.Log("Project ECHO: audit render xong.\n" + report);
        }

        private static string Prefix(string name)
        {
            int cut = name.IndexOfAny(new[] { '.', '_', ' ' });
            return cut > 0 ? name.Substring(0, cut) : name;
        }
    }
}
