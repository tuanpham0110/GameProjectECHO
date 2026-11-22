using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectEcho
{
    /// <summary>
    /// Đo hiệu năng trong bản build: chạy ProjectECHO.exe với <c>-echoPerf</c> (tùy chọn <c>-echoPerfOut &lt;tệp&gt;</c>).
    /// Bắt đầu lượt, đứng ở sảnh và phòng server, quay camera bốn hướng, ghi frame time rồi thoát. Hunter đứng yên để không
    /// bắt TAY giữa chừng; vSync tắt để đo frame time thật. Chạy bình thường (không có tham số) thì không làm gì.
    /// </summary>
    public sealed class PerformanceProbe : MonoBehaviour
    {
        private const float Warmup = 2, Sample = 3;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly StringBuilder report = new();
        private string folder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-echoPerf") < 0) return;
            Application.runInBackground = true;
            DontDestroyOnLoad(new GameObject("Performance probe").AddComponent<PerformanceProbe>().gameObject);
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private IEnumerator Start()
        {
            string output = Argument("-echoPerfOut") ?? Path.Combine(Application.persistentDataPath, "perf.md");
            folder = Path.GetDirectoryName(Path.GetFullPath(output));
            Directory.CreateDirectory(folder);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            LevelManager level = null;
            while (level == null)
            {
                level = FindFirstObjectByType<LevelManager>();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1);
            var game = level.game;
            report.AppendLine("# Đo hiệu năng Office_Level01");
            report.AppendLine();
            report.AppendLine($"- Thời điểm: {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"- Màn hình: {Screen.width}×{Screen.height}, {(Screen.fullScreen ? "toàn màn hình" : "cửa sổ")}, vSync tắt");
            report.AppendLine($"- GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsMemorySize} MB)");
            report.AppendLine($"- CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} luồng), RAM {SystemInfo.systemMemorySize} MB");
            report.AppendLine($"- Chất lượng: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            report.AppendLine();

            yield return Capture("01_main_menu");
            game.OpenBriefing();
            yield return null;
            yield return Capture("02_briefing");
            game.Select(Skill.BurstSprint);
            game.Confirm();
            game.StartRun();
            var hunter = level.hunter;
            hunter.enabled = false;
            if (hunter.TryGetComponent<NavMeshAgent>(out var agent) && agent.isOnNavMesh) agent.isStopped = true;

            report.AppendLine("| Vị trí | Hướng camera | FPS trung bình | Frame time TB (ms) | p95 (ms) | Tối đa (ms) | Batches | SetPass | Tam giác |");
            report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            var spots = new (string name, string shot, Vector3 position, float firstYaw)[]
            {
                ("Sảnh lễ tân (Player Spawn)", "03_hud_lobby", level.playerSpawn.position, 0),
                ("Phòng server (giữa hai dãy rack)", "04_server_room", new Vector3(22.3f, 0, -23.6f), 0),
                ("Data Relay (đủ dữ liệu)", "05_relay_ready", level.relay.transform.position + new Vector3(-1.6f, 0, 1.6f), 135)
            };
            var overall = new Dictionary<string, List<float>>();
            foreach (var (name, shot, raw, firstYaw) in spots)
            {
                if (shot == "05_relay_ready")
                    foreach (var data in level.interactions.OfType<DataPickup>()) game.Collect(data.dataId, data.displayName);
                Vector3 position = NavMesh.SamplePosition(raw, out var hit, 2, NavMesh.AllAreas) ? hit.position : raw;
                var frames = new List<float>();
                for (int turn = 0; turn < 4; turn++)
                {
                    float yaw = (firstYaw + turn * 90) % 360;
                    level.player.Teleport(position, Quaternion.Euler(0, yaw, 0));
                    yield return new WaitForSecondsRealtime(Warmup);
                    using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
                    using var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
                    using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
                    var window = new List<float>();
                    float end = Time.realtimeSinceStartup + Sample;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        window.Add(Time.unscaledDeltaTime * 1000);
                    }
                    frames.AddRange(window);
                    Row(name, Heading(yaw), window, Stat(batches), Stat(setPass), Stat(triangles));
                    if (turn == 0) yield return Capture(shot);
                }
                overall[name] = frames;
            }
            report.AppendLine();
            report.AppendLine("Tổng hợp theo vị trí (gộp bốn hướng):");
            report.AppendLine();
            foreach (var pair in overall)
            {
                float mean = pair.Value.Average();
                report.AppendLine(string.Format(Invariant, "- {0}: {1:0} FPS, {2:0.0} ms trung bình, p95 {3:0.0} ms.",
                    pair.Key, 1000 / mean, mean, Percentile(pair.Value, 0.95f)));
            }
            game.TogglePause();
            yield return null;
            yield return Capture("06_pause");
            File.WriteAllText(output, report.ToString(), new UTF8Encoding(false));
            Debug.Log("Project ECHO: đo hiệu năng xong → " + output + "\n" + report);
            Application.Quit();
        }

        private void Row(string spot, string heading, List<float> frames, string batches, string setPass, string triangles)
        {
            float mean = frames.Average();
            report.AppendLine(string.Format(Invariant, "| {0} | {1} | {2:0} | {3:0.0} | {4:0.0} | {5:0.0} | {6} | {7} | {8} |",
                spot, heading, 1000 / mean, mean, Percentile(frames, 0.95f), frames.Max(), batches, setPass, triangles));
        }

        private static string Stat(ProfilerRecorder recorder) => recorder.Valid && recorder.LastValue > 0
            ? recorder.LastValue.ToString("N0", Invariant) : "n/a";

        private static string Heading(float yaw) => new[] { "Bắc", "Đông bắc", "Đông", "Đông nam", "Nam", "Tây nam", "Tây", "Tây bắc" }
            [Mathf.RoundToInt(yaw / 45) % 8];

        private static float Percentile(List<float> values, float p)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[Mathf.Clamp(Mathf.CeilToInt(p * sorted.Count) - 1, 0, sorted.Count - 1)];
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return null;
        }
    }
}
