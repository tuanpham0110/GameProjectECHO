using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace ProjectEcho
{
    /// <summary>
    /// Đo hiệu năng trong bản build: <c>ProjectECHO.exe -echoPerf [-echoPerfOut tệp.md] [-echoPerfVariant a,b]</c>.
    /// Phần 1 đứng ở sảnh, phòng server, Relay và quay camera bốn hướng (Hunter đứng yên). Phần 2 chạy hết tuyến 8 điểm hai
    /// vòng ở Lockdown, Hunter đuổi nhưng không bắt (số đo theo từng chặng). Phần 3 chạy 60 s theo luật thật (bị bắt, hồi
    /// sinh) để tìm khựng. Ghi frame time, CPU main/render thread và GPU (FrameTimingManager),
    /// số batch rồi thoát. Biến thể (<c>noshadows</c>, <c>nossao</c>, <c>nosmallshadows</c>, <c>hidesmall</c>) tắt thử từng
    /// yếu tố để quy chi phí. Chạy bình thường (không có tham số) thì không làm gì.
    /// </summary>
    public sealed class PerformanceProbe : MonoBehaviour
    {
        private const float Warmup = 2, Sample = 3, SmallProp = 0.35f, RouteSeconds = 60, FullRouteLimit = 300, LegLimit = 45;
        private const int FullLaps = 2;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly StringBuilder report = new();
        private readonly FrameTiming[] timing = new FrameTiming[1];
        private string folder;
        private bool screenshots;

        /// <summary>Số đo một khung hình: tổng, CPU main, CPU render thread, GPU (ms) và số batch.</summary>
        private struct Frame
        {
            public float total, main, render, gpu, batches, setPass, triangles, casters;
        }

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
            var variants = (Argument("-echoPerfVariant") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim().ToLowerInvariant()).ToArray();
            screenshots = variants.Length == 0;
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
            report.AppendLine($"- Biến thể: {(variants.Length == 0 ? "không (cấu hình phát hành)" : string.Join(", ", variants))}");
            report.AppendLine($"- Màn hình: {Screen.width}×{Screen.height}, {(Screen.fullScreen ? "toàn màn hình" : "cửa sổ")}, vSync tắt");
            report.AppendLine($"- GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsMemorySize} MB)");
            report.AppendLine($"- CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} luồng), RAM {SystemInfo.systemMemorySize} MB");
            report.AppendLine($"- Chất lượng: {QualitySettings.names[QualitySettings.GetQualityLevel()]}, FrameTimingManager: {(FrameTimingManager.IsFeatureEnabled() ? "bật" : "tắt")}, " +
                $"GPU Resident Drawer: {GraphicsSettings.currentRenderPipeline?.GetType().GetProperty("gpuResidentDrawerMode")?.GetValue(GraphicsSettings.currentRenderPipeline)}");
            report.AppendLine($"- Renderer bật: {FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r => r.enabled)}, " +
                $"đổ bóng: {FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r => r.enabled && r.shadowCastingMode != ShadowCastingMode.Off)}");
            Apply(variants);
            report.AppendLine();

            if (screenshots) yield return Capture("01_main_menu");
            game.OpenBriefing();
            yield return null;
            if (screenshots) yield return Capture("02_briefing");
            game.Select(Skill.BurstSprint);
            game.Confirm();
            game.StartRun();
            level.player.input.enabled = false;
            var hunter = level.hunter;
            var agent = hunter.GetComponent<NavMeshAgent>();
            hunter.enabled = false;
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (screenshots) yield return CaptureChecks(level);

            report.AppendLine("## Góc nhìn cố định (Hunter đứng yên)");
            report.AppendLine();
            report.AppendLine("| Vị trí | Hướng | FPS | TB (ms) | p95 (ms) | Tối đa (ms) | CPU main | CPU render | GPU | Batches | SetPass | Tam giác | Shadow casters |");
            report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            var spots = new (string name, string shot, Vector3 position, float firstYaw)[]
            {
                ("Sảnh lễ tân (Player Spawn)", "03_hud_lobby", level.playerSpawn.position, 0),
                ("Phòng server (giữa hai dãy rack)", "04_server_room", new Vector3(22.3f, 0, -23.6f), 0),
                ("Data Relay (đủ dữ liệu)", "05_relay_ready", level.relay.transform.position + new Vector3(-1.6f, 0, 1.6f), 135)
            };
            var overall = new List<(string, List<Frame>)>();
            foreach (var (name, shot, raw, firstYaw) in spots)
            {
                if (shot == "05_relay_ready")
                    foreach (var data in level.interactions.OfType<DataPickup>()) game.Collect(data.dataId, data.displayName);
                Vector3 position = NavMesh.SamplePosition(raw, out var hit, 2, NavMesh.AllAreas) ? hit.position : raw;
                var frames = new List<Frame>();
                for (int turn = 0; turn < 4; turn++)
                {
                    float yaw = (firstYaw + turn * 90) % 360;
                    level.player.Teleport(position, Quaternion.Euler(0, yaw, 0));
                    yield return new WaitForSecondsRealtime(Warmup);
                    var window = new List<Frame>();
                    yield return Record(Sample, window, null);
                    frames.AddRange(window);
                    Row(name, Heading(yaw), window);
                    if (turn == 0 && screenshots) yield return Capture(shot);
                }
                overall.Add((name, frames));
            }
            report.AppendLine();
            foreach (var (name, frames) in overall) Summary(name + " (4 hướng)", frames);

            hunter.enabled = true;
            if (agent.isOnNavMesh) agent.isStopped = false;
            var loop = new[]
            {
                new Vector3(23, 0, -27), new Vector3(5, 0, -41), new Vector3(-12, 0, -38), new Vector3(-11, 0, -12),
                new Vector3(-9.5f, 0, 5), new Vector3(10, 0, -1), new Vector3(15, 0, -9), new Vector3(22, 0, -20), new Vector3(23, 0, -27)
            };

            // Phần 2: hết tuyến, giữ Lockdown. Hunter vẫn thấy, nghe, đuổi và áp sát nhưng không bắt, nên TAY giữ 6/6 dữ liệu.
            report.AppendLine();
            report.AppendLine($"## Hết tuyến {FullLaps} vòng, giữ Lockdown, Hunter đuổi nhưng không bắt");
            report.AppendLine();
            var full = loop.Take(1).Concat(Enumerable.Range(0, FullLaps).SelectMany(_ => loop.Skip(1))).ToArray();
            game.captureSuppressed = true;
            level.player.Teleport(full[0], Quaternion.identity);
            yield return new WaitForSecondsRealtime(1);
            var sustained = new List<Frame>();
            var legs = new List<int>();
            var near = new List<float>();
            var chasing = new List<bool>();
            var fullStats = new RouteStats();
            int lowestThreat = game.Mission.Threat;
            yield return Record(FullRouteLimit, sustained, () =>
            {
                Drive(level, full, fullStats);
                lowestThreat = Mathf.Min(lowestThreat, game.Mission.Threat);
                legs.Add(fullStats.waypoint);
                near.Add(Flat(hunter.transform.position - level.player.transform.position));
                chasing.Add(hunter.Current == HunterState.Chase);
                if (chasing[^1] && hunter.IsTrackingPlayer && near[^1] <= hunter.profile.captureRange)
                {
                    if (!fullStats.touching) fullStats.touches++;
                    fullStats.touching = true;
                }
                else fullStats.touching = false;
            }, () => fullStats.waypoint >= full.Length);
            game.captureSuppressed = false;
            Summary("Hết tuyến", sustained);
            RouteFacts(sustained, fullStats, full.Length, 0);
            report.AppendLine(string.Format(Invariant,
                "- Cấp Hunter thấp nhất trong lúc đo: {0} ({1}); Hunter ở trạng thái Chase {2:0}% thời gian; áp sát trong tầm bắt {3} lần (không bắt); bỏ qua {4} điểm do kẹt.",
                lowestThreat, lowestThreat >= 4 ? "Lockdown suốt lượt" : "có lúc hạ cấp", 100f * chasing.Count(c => c) / Mathf.Max(1, chasing.Count),
                fullStats.touches, fullStats.skipped));
            report.AppendLine();
            report.AppendLine("| Chặng | Từ → tới | Thời gian (s) | TB (ms) | p95 (ms) | Tối đa (ms) | Khung > 25 ms | GPU TB | Batches TB | Hunter cách TB (m) | Hunter Chase |");
            report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            for (int leg = 1; leg < full.Length; leg++)
            {
                var index = Enumerable.Range(0, sustained.Count).Where(i => legs[i] == leg).ToList();
                if (index.Count == 0) continue;
                var part = index.Select(i => sustained[i]).ToList();
                float mean = part.Average(f => f.total);
                report.AppendLine(string.Format(Invariant, "| {0} | {1} → {2} | {3:0.0} | {4:0.0} | {5:0.0} | {6:0.0} | {7} | {8} | {9} | {10:0.0} | {11:0}% |",
                    leg, Point(full[leg - 1]), Point(full[leg]), part.Sum(f => f.total) / 1000, mean, Percentile(part.Select(f => f.total), 0.95f),
                    part.Max(f => f.total), part.Count(f => f.total > 25), Mean(part, f => f.gpu, "0.0"), Mean(part, f => f.batches, "N0"),
                    index.Average(i => near[i]), 100f * index.Count(i => chasing[i]) / index.Count));
            }

            // Phần 3: luật thật, có bắt và hồi sinh, để thấy khựng lúc bị bắt/hồi sinh.
            report.AppendLine();
            report.AppendLine("## Chạy 60 s theo luật thật (có bắt, hồi sinh, mất dữ liệu)");
            report.AppendLine();
            foreach (var data in level.interactions.OfType<DataPickup>()) game.Collect(data.dataId, data.displayName);
            level.player.Teleport(loop[0], Quaternion.identity);
            yield return new WaitForSecondsRealtime(1);
            int capturesBefore = game.Captures;
            var run = new List<Frame>();
            var stats = new RouteStats();
            yield return Record(RouteSeconds, run, () => Drive(level, loop, stats));
            Summary("Vòng chạy", run);
            RouteFacts(run, stats, loop.Length, game.Captures - capturesBefore);

            if (screenshots)
            {
                game.TogglePause();
                yield return null;
                yield return Capture("06_pause");
            }
            File.WriteAllText(output, report.ToString(), new UTF8Encoding(false));
            Debug.Log("Project ECHO: đo hiệu năng xong → " + output + "\n" + report);
            Application.Quit();
        }

        private void RouteFacts(List<Frame> run, RouteStats stats, int points, int captures)
        {
            int Over(float ms) => run.Count(f => f.total > ms);
            report.AppendLine(string.Format(Invariant,
                "- {0} khung trong {1:0} s; khung > 25 ms: {2}, > 33 ms: {3}, > 50 ms: {4}; p99 {5:0.0} ms. Quãng đường {6:0} m, tới {7}/{8} điểm, Hunter đuổi {9} lần, bị bắt {10} lần, mở {11} cửa.",
                run.Count, run.Sum(f => f.total) / 1000, Over(25), Over(33.3f), Over(50), Percentile(run.Select(f => f.total), 0.99f),
                stats.distance, Mathf.Min(stats.waypoint - 1, points - 1), points - 1, stats.chases, captures, stats.doors));
            var worst = run.OrderByDescending(f => f.total).Take(5)
                .Select(f => string.Format(Invariant, "{0:0.0} ms (main {1:0.0}, render {2:0.0}, GPU {3:0.0})", f.total, f.main, f.render, f.gpu));
            report.AppendLine("- 5 khung nặng nhất: " + string.Join("; ", worst) + ".");
        }

        private static string Point(Vector3 p) => string.Format(Invariant, "({0:0.#}; {1:0.#})", p.x, p.z);

        private sealed class RouteStats
        {
            public int waypoint = 1, chases, doors, touches, skipped;
            public bool touching;
            public float distance, repath, noise, signal, legTime;
            public Vector3[] corners;
            public int corner;
            public Vector3 last;
            public HunterState hunter;
        }

        /// <summary>Lái TAY theo đường NavMesh với tốc độ chạy, phát tiếng bước chân, mở cửa đóng, gợi Hunter theo dấu.</summary>
        private static void Drive(LevelManager level, Vector3[] route, RouteStats s)
        {
            var player = level.player;
            var hunter = level.hunter;
            if (hunter.Current == HunterState.Chase && s.hunter != HunterState.Chase) s.chases++;
            s.hunter = hunter.Current;
            if (!level.game.CanAct || s.waypoint >= route.Length) return;
            float delta = Time.deltaTime;
            Vector3 feet = player.transform.position;
            if (s.last != Vector3.zero && (feet - s.last).sqrMagnitude < 4) s.distance += Vector3.Distance(feet, s.last);
            s.last = feet;
            Vector3 goal = route[s.waypoint];
            s.legTime += delta;
            bool stuck = s.legTime > LegLimit;
            if (Flat(goal - feet) < 1.2f || stuck)
            {
                if (stuck) s.skipped++;
                s.waypoint++;
                s.legTime = 0;
                s.corners = null;
                return;
            }
            s.repath -= delta;
            if (s.corners == null || s.repath <= 0)
            {
                s.repath = 0.5f;
                var path = new NavMeshPath();
                if (NavMesh.SamplePosition(feet, out var a, 1.5f, NavMesh.AllAreas) && NavMesh.SamplePosition(goal, out var b, 2, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.corners.Length > 1)
                {
                    s.corners = path.corners;
                    s.corner = 1;
                }
                else s.corners = new[] { feet, goal };
            }
            while (s.corner < s.corners.Length - 1 && Flat(s.corners[s.corner] - feet) < 0.4f) s.corner++;
            Vector3 direction = s.corners[Mathf.Min(s.corner, s.corners.Length - 1)] - feet;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.0001f) return;
            float yaw = Mathf.MoveTowardsAngle(level.cameraView.Heading.eulerAngles.y, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 420 * delta);
            level.cameraView.SetYaw(yaw);
            player.ApplyMovement(direction.normalized * (player.tuning.sprintSpeed * delta));
            foreach (var target in level.interactions)
                if (target is OfficeDoor door && !door.IsOpen && !door.locked && Flat(door.transform.position - feet) < 1.6f)
                {
                    door.Interact(player);
                    s.doors++;
                }
            s.noise -= delta;
            if (s.noise <= 0)
            {
                s.noise = 0.29f;
                level.EmitNoise(feet, 13);
            }
            s.signal -= delta;
            if (s.signal <= 0)
            {
                s.signal = 4;
                hunter.ReceiveScannerSignal(feet, 60);
            }
        }

        private IEnumerator Record(float seconds, List<Frame> frames, Action perFrame, Func<bool> done = null)
        {
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            using var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            using var casters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && (done == null || !done()))
            {
                perFrame?.Invoke();
                yield return null;
                FrameTimingManager.CaptureFrameTimings();
                bool timed = FrameTimingManager.GetLatestTimings(1, timing) > 0;
                frames.Add(new Frame
                {
                    total = Time.unscaledDeltaTime * 1000,
                    main = timed ? (float)timing[0].cpuMainThreadFrameTime : float.NaN,
                    render = timed ? (float)timing[0].cpuRenderThreadFrameTime : float.NaN,
                    gpu = timed ? (float)timing[0].gpuFrameTime : float.NaN,
                    batches = batches.Valid ? batches.LastValue : float.NaN,
                    setPass = setPass.Valid ? setPass.LastValue : float.NaN,
                    triangles = triangles.Valid ? triangles.LastValue : float.NaN,
                    casters = casters.Valid ? casters.LastValue : float.NaN
                });
            }
        }

        private void Row(string spot, string heading, List<Frame> frames)
        {
            float mean = frames.Average(f => f.total);
            report.AppendLine(string.Format(Invariant, "| {0} | {1} | {2:0} | {3:0.0} | {4:0.0} | {5:0.0} | {6} | {7} | {8} | {9} | {10} | {11} | {12} |",
                spot, heading, 1000 / mean, mean, Percentile(frames.Select(f => f.total), 0.95f), frames.Max(f => f.total),
                Mean(frames, f => f.main, "0.0"), Mean(frames, f => f.render, "0.0"), Mean(frames, f => f.gpu, "0.0"),
                Mean(frames, f => f.batches, "N0"), Mean(frames, f => f.setPass, "N0"), Mean(frames, f => f.triangles, "N0"), Mean(frames, f => f.casters, "N0")));
        }

        private void Summary(string name, List<Frame> frames)
        {
            float mean = frames.Average(f => f.total);
            report.AppendLine(string.Format(Invariant, "- {0}: {1:0} FPS, {2:0.0} ms TB, p95 {3:0.0} ms, tối đa {4:0.0} ms; CPU main {5}, render {6}, GPU {7} ms; batches TB {8}.",
                name, 1000 / mean, mean, Percentile(frames.Select(f => f.total), 0.95f), frames.Max(f => f.total),
                Mean(frames, f => f.main, "0.0"), Mean(frames, f => f.render, "0.0"), Mean(frames, f => f.gpu, "0.0"), Mean(frames, f => f.batches, "N0")));
        }

        private static string Mean(List<Frame> frames, Func<Frame, float> pick, string format)
        {
            var values = frames.Select(pick).Where(v => !float.IsNaN(v) && v > 0).ToArray();
            return values.Length == 0 ? "n/a" : values.Average().ToString(format, Invariant);
        }

        /// <summary>Tắt thử từng yếu tố để quy chi phí. Chỉ dùng khi đo, không lưu lại.</summary>
        private void Apply(string[] variants)
        {
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            bool Small(Renderer r) => Mathf.Max(r.bounds.size.x, r.bounds.size.y, r.bounds.size.z) < SmallProp;
            foreach (string variant in variants)
            {
                switch (variant)
                {
                    case "noshadows":
                        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) light.shadows = LightShadows.None;
                        break;
                    case "nossao":
                        SetRendererFeature("ScreenSpaceAmbientOcclusion", false);
                        break;
                    case "nosmallshadows":
                        foreach (var r in renderers.Where(Small)) r.shadowCastingMode = ShadowCastingMode.Off;
                        break;
                    case "hidesmall":
                        foreach (var r in renderers.Where(Small)) r.enabled = false;
                        break;
                    case "gpuocclusion":
                        SetAssetProperty("gpuResidentDrawerEnableOcclusionCullingInCameras", true);
                        break;
                    case "ssaodepth":
                        SetSsao(depthSource: true, afterOpaque: true);
                        break;
                    default:
                        if (variant.StartsWith("smallmesh=") && float.TryParse(variant.Substring(10), NumberStyles.Float, Invariant, out float percent))
                            SetAssetProperty("smallMeshScreenPercentage", percent);
                        break;
                }
            }
            if (variants.Length > 0)
                report.AppendLine($"- Vật nhỏ (cạnh lớn nhất < {SmallProp} m): {renderers.Count(Small)} renderer");
        }

        private void SetAssetProperty(string property, object value)
        {
            var asset = GraphicsSettings.currentRenderPipeline;
            var info = asset?.GetType().GetProperty(property);
            if (info == null || !info.CanWrite)
            {
                report.AppendLine($"- Không đặt được {property}");
                return;
            }
            info.SetValue(asset, Convert.ChangeType(value, info.PropertyType, Invariant));
            report.AppendLine($"- {property} = {info.GetValue(asset)}");
        }

        /// <summary>SSAO đọc độ sâu sau pass opaque thay vì prepass DepthNormals vẽ lại cả scene.</summary>
        private void SetSsao(bool depthSource, bool afterOpaque)
        {
            foreach (var feature in Features("ScreenSpaceAmbientOcclusion"))
            {
                var settings = feature.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(feature);
                if (settings == null) continue;
                var source = settings.GetType().GetField("Source", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var after = settings.GetType().GetField("AfterOpaque", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                source?.SetValue(settings, Enum.ToObject(source.FieldType, depthSource ? 0 : 1));
                after?.SetValue(settings, afterOpaque);
                report.AppendLine($"- SSAO: Source = {source?.GetValue(settings)}, AfterOpaque = {after?.GetValue(settings)}");
            }
        }

        private static IEnumerable<object> Features(string typeName)
        {
            var asset = GraphicsSettings.currentRenderPipeline;
            var list = asset?.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(asset) as Array;
            foreach (var data in list?.Cast<object>() ?? Enumerable.Empty<object>())
            {
                if (data?.GetType().GetProperty("rendererFeatures")?.GetValue(data) is not IEnumerable features) continue;
                foreach (var feature in features.Cast<object>().Where(f => f != null && f.GetType().Name.Contains(typeName)))
                    yield return feature;
            }
        }

        private void SetRendererFeature(string typeName, bool active)
        {
            var asset = GraphicsSettings.currentRenderPipeline;
            var list = asset?.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(asset) as Array;
            int changed = 0;
            foreach (var data in list?.Cast<object>() ?? Enumerable.Empty<object>())
            {
                if (data?.GetType().GetProperty("rendererFeatures")?.GetValue(data) is not IEnumerable features) continue;
                foreach (var feature in features.Cast<object>().Where(f => f != null && f.GetType().Name.Contains(typeName)))
                {
                    feature.GetType().GetMethod("SetActive")?.Invoke(feature, new object[] { active });
                    changed++;
                }
            }
            report.AppendLine($"- {typeName}: {(active ? "bật" : "tắt")} {changed} feature");
        }

        /// <summary>Ảnh kiểm tra hiển thị: gói dữ liệu được Scanner tô sáng (MaterialPropertyBlock) và cửa FBX vừa mở.</summary>
        private IEnumerator CaptureChecks(LevelManager level)
        {
            var data = level.interactions.OfType<DataPickup>().OrderBy(d => (d.transform.position - level.playerSpawn.position).sqrMagnitude).First();
            if (Viewpoint(data.transform.position, 1.8f, out var dataView))
            {
                level.player.Teleport(dataView.position, dataView.rotation);
                data.SetScanned(true);
                yield return Capture("07_data_scanned");
                data.SetScanned(false);
            }
            var door = level.interactions.OfType<OfficeDoor>().FirstOrDefault(d => !d.IsOpen && !d.locked);
            if (door != null && Viewpoint(door.Point, 2.6f, out var doorView))
            {
                level.player.Teleport(doorView.position, doorView.rotation);
                yield return Capture("08_door_closed");
                door.Interact(level.player);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Capture("09_door_open");
                door.ResetRun();
            }
            var exit = level.exit.transform;
            if (Viewpoint(exit.position + Vector3.up * 1.2f, 4.5f, out var exitView))
            {
                level.player.Teleport(exitView.position, exitView.rotation);
                yield return Capture("10_exit_gate");
            }
        }

        private static bool Viewpoint(Vector3 target, float distance, out Pose pose)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector3 probe = target + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
                if (!NavMesh.SamplePosition(probe + Vector3.down, out var hit, 1.2f, NavMesh.AllAreas) ||
                    new Vector2(hit.position.x - probe.x, hit.position.z - probe.z).sqrMagnitude > 0.25f) continue;
                Vector3 eye = hit.position + Vector3.up * 1.58f;
                if (Physics.Linecast(eye, target, out var block, ~0, QueryTriggerInteraction.Ignore) && (block.point - target).sqrMagnitude > 0.36f) continue;
                Vector3 facing = target - hit.position;
                facing.y = 0;
                pose = new Pose(hit.position, Quaternion.LookRotation(facing));
                return true;
            }
            pose = default;
            return false;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private static string Heading(float yaw) => new[] { "Bắc", "Đông bắc", "Đông", "Đông nam", "Nam", "Tây nam", "Tây", "Tây bắc" }
            [Mathf.RoundToInt(yaw / 45) % 8];

        private static float Percentile(IEnumerable<float> values, float p)
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
