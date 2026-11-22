using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectEcho.Tests
{
    /// <summary>
    /// Playtest tự động Office_Level01 qua input thật: bàn phím và chuột ảo của Input System đi qua cùng đường
    /// PlayerInput/InputSystemUIInputModule như người chơi. Bot biết vị trí mục tiêu (không mô phỏng việc tìm kiếm),
    /// Hunter chạy AI thật. Nhật ký ghi ở Builds/Validation/Playtest_Office.md.
    /// </summary>
    [Category("Playtest")]
    public sealed class OfficePlaythroughTests
    {
        private const string ScenePath = "Assets/_ProjectECHO/Scenes/Office_Level01.unity";
        private const float Speed = 2;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private LevelManager level;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings previousSettings;
        private InputSettings botSettings;
        private readonly HashSet<Key> held = new();
        private readonly StringBuilder log = new();
        private readonly List<string> issues = new();
        private Vector2 look;
        private Vector2 pointer;
        private bool leftButton;
        private string profile;
        private HunterState lastHunter;
        private GameState lastState;
        private int lastThreat;
        private int captures;
        private int detections;
        private int stuckEvents;
        private float evadeSeconds;
        private float runStarted;

        private GameManager Game => level.game;
        private PlayerController Player => level.player;
        private Vector3 Feet => Player.transform.position;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            previousSettings = InputSystem.settings;
            botSettings = Object.Instantiate(previousSettings);
            botSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            botSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = botSettings;
            keyboard = InputSystem.AddDevice<Keyboard>("ECHO Bot Keyboard");
            mouse = InputSystem.AddDevice<Mouse>("ECHO Bot Mouse");
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(ScenePath), LoadSceneMode.Single);
#endif
            level = Object.FindFirstObjectByType<LevelManager>();
            Assert.That(level, Is.Not.Null);
            profile = Path.Combine(Path.GetTempPath(), "EchoPlaytest-" + Guid.NewGuid().ToString("N"));
            Game.LoadProfile(new SaveSystem(profile));
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            WriteReport();
            held.Clear();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (previousSettings != null) InputSystem.settings = previousSettings;
            if (profile != null && Directory.Exists(profile)) Directory.Delete(profile, true);
            yield return null;
        }

        [UnityTest, Timeout(1_500_000)]
        public IEnumerator PlayerCompletesOfficeRunFromMenuWithKeyboardAndMouse()
        {
            Line("## Lượt 1 · từ menu đến cổng thoát");
            yield return StartFromMenu();
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Playing), "Không vào được lượt chơi bằng bàn phím.");
            Assert.That(Game.Equipped, Is.EqualTo(Skill.BurstSprint));
            Time.timeScale = Speed;
            yield return Snapshot("Bắt đầu lượt");
            yield return Tap(Key.T);
            yield return Snapshot("Scanner bật (phím T)");
            yield return Tap(Key.T);

            bool relayInterrupted = false, firstDelivery = true;
            while (Game.IsRunning)
            {
                while (!Game.Mission.CanExit && Game.IsRunning)
                {
                    yield return CollectAll();
                    if (!Game.IsRunning) break;
                    yield return Transmit(!relayInterrupted);
                    relayInterrupted = true;
                }
                if (!Game.IsRunning) break;
                if (firstDelivery)
                {
                    firstDelivery = false;
                    yield return Snapshot("Truyền xong");
                    Assert.That(HudText("relayLabel"), Does.Contain("CỔNG THOÁT").And.Contain(" m · "), "HUD phải chỉ đường tới cổng sau khi truyền.");
                    Assert.That(HudText("objective"), Does.StartWith("3/3"));
                }
                int captured = captures;
                yield return Escape();
                if (captures != captured && Game.IsRunning)
                {
                    yield return Snapshot("Bị bắt sau khi truyền");
                    Line($"- {Stamp()} Bị bắt trên đường về cổng: còn {Game.Mission.ValidData} gói, Relay {Game.Mission.Relay}, cổng {(level.exit.IsOpen ? "mở" : "đóng")}. Làm lại chặng 1–2.");
                }
            }
            Time.timeScale = 1;
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Complete), "Không thoát được qua cổng.");
            Line($"- Hoàn tất sau {Elapsed():0} s, còn {Game.Timer.Remaining:0} s. Bị bắt {captures} lần, Hunter phát hiện {detections} lần, né {evadeSeconds:0} s, kẹt {stuckEvents} lần.");
            yield return Snapshot("Màn hình kết quả");

            Line("");
            Line("## Lượt 2 · retry, bị bắt, hồi sinh, nhặt lại, tạm dừng, lượt mới");
            yield return Choose("Chuẩn bị lượt mới");
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Briefing), "Nút Chuẩn bị lượt mới không mở sảnh.");
            yield return StartFromBriefing();
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Playing));
            Assert.That(Game.Mission.ValidData, Is.EqualTo(0));
            Assert.That(level.interactions.OfType<DataPickup>().All(p => p.gameObject.activeSelf), Is.True);
            Time.timeScale = Speed;
            foreach (var data in Remaining().Take(2).ToArray()) yield return Collect(data);
            int before = Game.Mission.ValidData;
            Line($"- Có {before} gói, cố ý đi vào tầm Hunter.");
            yield return GetCaught();
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Playing), "Không hồi sinh sau khi bị bắt.");
            Assert.That(Flat(Feet - level.playerSpawn.position), Is.LessThan(0.5f), "Không trở về Player Spawn.");
            Assert.That(Game.Mission.ValidData, Is.EqualTo(Mathf.Max(0, before - level.rules.captureLoss)));
            yield return Snapshot("Sau hồi sinh");
            foreach (var data in Remaining().Take(2).ToArray()) yield return Collect(data);
            Assert.That(Game.Mission.ValidData, Is.GreaterThanOrEqualTo(before), "Không nhặt lại được dữ liệu bị mất.");
            Line($"- Nhặt lại xong, có {Game.Mission.ValidData} gói.");

            Time.timeScale = 1;
            yield return Tap(Key.Escape);
            yield return Frames(3);
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Paused), "Esc không tạm dừng.");
            float paused = Game.Timer.Remaining;
            yield return Frames(30);
            Assert.That(Game.Timer.Remaining, Is.EqualTo(paused), "Đồng hồ chạy khi tạm dừng.");
            yield return Tap(Key.Escape);
            yield return Frames(3);
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Playing), "Esc không tiếp tục.");
            Line("- Esc tạm dừng (đồng hồ đứng) và Esc tiếp tục.");
            yield return Tap(Key.Escape);
            yield return Frames(3);
            yield return Choose("Chuẩn bị lượt mới");
            yield return StartFromBriefing();
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Playing));
            Assert.That(Game.Mission.ValidData, Is.EqualTo(0));
            Assert.That(Game.Timer.Remaining, Is.EqualTo(level.rules.timeLimit).Within(0.5f));
            Line("- Từ menu tạm dừng chọn Chuẩn bị lượt mới: lượt mới bắt đầu với 0 dữ liệu, đồng hồ đầy.");
        }

        // ───────────────────────── Kịch bản ─────────────────────────

        private IEnumerator StartFromMenu()
        {
            Assert.That(Game.State.Current, Is.EqualTo(GameState.MainMenu));
            yield return Snapshot("Menu chính");
            yield return Choose("Chuẩn bị nhiệm vụ");
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Briefing), "Không mở được sảnh chọn kỹ năng.");
            yield return StartFromBriefing();
        }

        private IEnumerator StartFromBriefing()
        {
            Line($"- Sảnh: chọn sẵn {Game.Selected}, đã xác nhận {Game.Confirmed}.");
            if (Game.Selected != Skill.BurstSprint) yield return Choose("Burst Sprint", true);
            if (!Game.CanStart) yield return Choose("Xác nhận kỹ năng");
            yield return Choose("Bắt đầu nhiệm vụ");
            runStarted = Time.time;
            captures = detections = stuckEvents = 0;
            evadeSeconds = 0;
        }

        private IEnumerator CollectAll()
        {
            while (Game.IsRunning && Game.Mission.ValidData < Game.Mission.RequiredData)
            {
                var next = Remaining().FirstOrDefault();
                if (next == null) break;
                yield return Collect(next);
            }
        }

        private IEnumerable<DataPickup> Remaining()
        {
            var path = new NavMeshPath();
            return level.interactions.OfType<DataPickup>().Where(p => p.gameObject.activeSelf)
                .OrderBy(p => PathLength(Feet, p.transform.position, path));
        }

        private IEnumerator Collect(DataPickup data)
        {
            int before = Game.Mission.ValidData;
            float start = Time.time;
            Vector3 from = Feet;
            float route = PathLength(from, data.transform.position, new NavMeshPath());
            float firstSeen = -1;
            int captured = captures;
            yield return GoTo(data.transform.position, 1.0f, data.displayName, 120, () =>
            {
                if (firstSeen < 0 && CanSee(data.transform.position)) firstSeen = Flat(Feet - data.transform.position);
                return !data.gameObject.activeSelf;
            });
            if (!data.gameObject.activeSelf && !Game.Mission.HasData(data.dataId)) yield break;
            int attempts = 0;
            while (Game.IsRunning && data.gameObject.activeSelf && attempts < 8)
            {
                if (captures != captured) yield break;
                yield return Face(data.transform.position, 0.8f, () => Player.interaction.Target == data);
                if (Player.interaction.Target == data)
                {
                    yield return Tap(Key.F);
                    yield return Frames(2);
                    if (!data.gameObject.activeSelf) break;
                }
                attempts++;
                Vector3 offset = Quaternion.Euler(0, attempts * 45, 0) * Vector3.forward * 1.2f;
                if (NavMesh.SamplePosition(data.transform.position + offset, out var hit, 0.6f, NavMesh.AllAreas))
                    yield return GoTo(hit.position, 0.3f, data.displayName + " (góc khác)", 15);
            }
            bool ok = Game.Mission.ValidData > before || Game.Mission.HasData(data.dataId);
            string seen = firstSeen < 0 ? "không thấy trước khi tới" : $"thấy từ {firstSeen:0.0} m";
            Line($"- {Stamp()} {(ok ? "Nhặt" : "KHÔNG nhặt được")} **{data.displayName}** tại {P(data.transform.position)}: " +
                 $"tuyến {route:0.0} m từ {P(from)}, {Time.time - start:0.0} s, {seen}, góc thử thêm {attempts}. Có {Game.Mission.ValidData}/{Game.Mission.RequiredData}.");
            if (!ok && captures == captured) Issue($"Không nhặt được {data.displayName} tại {P(data.transform.position)} sau {attempts} góc tiếp cận.");
            if (attempts > 0 && ok) Issue($"{data.displayName} tại {P(data.transform.position)} cần {attempts} góc tiếp cận thêm mới hiện lời nhắc F.");
            if (ok && Game.Mission.ValidData == Game.Mission.RequiredData)
            {
                yield return Snapshot("Đủ dữ liệu");
                if (!(HudText("relayLabel") ?? "").Contains(" m · ")) Issue("HUD chưa chỉ đường tới Data Relay ngay khi đủ dữ liệu.");
            }
        }

        private IEnumerator Transmit(bool testLeaving)
        {
            Vector3 relay = level.relay.transform.position;
            yield return GoTo(relay, 1.3f, "Data Relay", 120);
            if (!Game.IsRunning || Game.Mission.ValidData < Game.Mission.RequiredData) yield break;
            yield return Face(relay, 1, () => Player.interaction.Target == level.relay);
            if (Player.interaction.Target != level.relay) Issue($"Không có lời nhắc F tại Data Relay khi đứng ở {P(Feet)}.");
            yield return Tap(Key.F);
            yield return Frames(2);
            if (Game.Mission.Relay != RelayState.Transmitting)
            {
                Issue($"Nhấn F tại Relay không bắt đầu truyền (trạng thái {Game.Mission.Relay}).");
                yield break;
            }
            Line($"- {Stamp()} Bắt đầu truyền tại {P(Feet)}.");
            yield return Snapshot("Đang truyền");
            int captured = captures;
            if (testLeaving)
            {
                yield return HoldRelay(6, captured);
                if (captures != captured) yield break;
                float progress = Game.Mission.RelayProgress;
                Vector3 away = FindAway(relay, level.rules.relayRadius + 2.5f);
                yield return GoTo(away, 0.5f, "rời vùng Relay", 20, () => captures != captured);
                if (captures != captured) yield break;
                float outside = Game.Mission.RelayProgress;
                yield return Wait(3, () => captures != captured);
                if (captures != captured) yield break;
                yield return Snapshot("Ngoài vùng Relay");
                Line($"- {Stamp()} Rời vùng tới {P(Feet)} ({Flat(Feet - relay):0.0} m): tiến độ {progress:P0} → {Game.Mission.RelayProgress:P0} sau 3 s ngoài vùng.");
                if (Game.Mission.RelayProgress > outside + 0.001f) Issue("Tiến độ Relay vẫn tăng khi đứng ngoài vùng.");
                if (Game.Mission.RelayProgress < outside - 0.001f) Issue("Tiến độ Relay bị mất khi rời vùng.");
                yield return GoTo(relay, 1.3f, "quay lại Relay", 30, () => captures != captured);
            }
            float until = Time.time + level.rules.relaySeconds * 4 + 20;
            while (Game.IsRunning && Game.Mission.Relay == RelayState.Transmitting && Time.time < until && captures == captured)
            {
                if (Threatened()) yield return Evade("Data Relay (đang truyền)");
                else if (!level.IsInRelayRange) yield return GoTo(relay, 1.3f, "quay lại vùng Relay", 30, () => captures != captured);
                else yield return Frame();
            }
            Line($"- {Stamp()} Relay: {Game.Mission.Relay}, {Game.Mission.RelayProgress:P0}.");
        }

        private IEnumerator HoldRelay(float seconds, int captured)
        {
            float until = Time.time + seconds;
            while (Time.time < until && Game.IsRunning && captures == captured && Game.Mission.Relay == RelayState.Transmitting)
            {
                if (Threatened())
                {
                    yield return Evade("Data Relay (đang truyền)");
                    yield return GoTo(level.relay.transform.position, 1.3f, "quay lại vùng Relay", 30, () => captures != captured);
                }
                else yield return Frame();
            }
        }

        private IEnumerator Escape()
        {
            var exit = level.exit;
            Vector3 passage = exit.passage.position;
            int captured = captures;
            yield return GoTo(passage, 0.45f, "Cổng thoát", 150, () => !Game.IsRunning || captures != captured);
            if (captures != captured) yield break;
            if (Game.IsRunning)
            {
                yield return Face(exit.transform.position, 0.6f, () => Player.interaction.Target == exit);
                if (Player.interaction.Target == exit) yield return Tap(Key.F);
                yield return Frames(3);
            }
            Line($"- {Stamp()} Tại cổng {P(Feet)}: trạng thái {Game.State.Current}.");
        }

        private IEnumerator GetCaught()
        {
            int before = captures;
            int data = Game.Mission.ValidData;
            float until = Time.time + 90;
            while (captures == before && Time.time < until && Game.IsRunning)
                yield return GoTo(level.hunter.transform.position, 0.8f, "Hunter", 4, () => captures != before);
            if (captures == before)
            {
                Issue("Bot không bị Hunter bắt trong 90 s khi chủ động đi tới; dùng TryCapture trực tiếp.");
                Game.TryCapture();
            }
            yield return Snapshot("Ngay khi bị bắt");
            yield return Wait(level.rules.captureSeconds + 0.3f);
            Line($"- {Stamp()} Bị bắt: {data} → {Game.Mission.ValidData} gói, hồi sinh tại {P(Feet)}, " +
                 $"{level.interactions.OfType<DataPickup>().Count(p => p.gameObject.activeSelf)} gói hiện lại trên map.");
        }

        // ───────────────────────── Di chuyển ─────────────────────────

        private IEnumerator GoTo(Vector3 target, float arrive, string label, float timeout, Func<bool> abort = null)
        {
            var path = new NavMeshPath();
            Vector3[] corners = null;
            int corner = 0;
            float repath = 0, end = Time.time + timeout, lastMove = Time.time;
            Vector3 anchor = Feet;
            int stuckHere = 0;
            while (true)
            {
                if (abort != null && abort()) break;
                if (!Game.IsRunning) break;
                if (!Game.CanAct)
                {
                    corners = null;
                    lastMove = Time.time;
                    anchor = Feet;
                    ReleaseMove();
                    yield return Frame();
                    continue;
                }
                Vector3 feet = Feet;
                if (Flat(feet - target) <= arrive) break;
                if (Time.time > end)
                {
                    if (label == "Hunter") break;
                    Issue($"Hết {timeout:0} s mà chưa tới {label}: đang ở {P(feet)}, còn {Flat(feet - target):0.0} m.");
                    break;
                }
                repath -= Time.deltaTime;
                if (corners == null || repath <= 0)
                {
                    repath = 0.4f;
                    corners = null;
                    if (NavMesh.SamplePosition(feet, out var a, 1.5f, NavMesh.AllAreas) &&
                        NavMesh.SamplePosition(target, out var b, 2.0f, NavMesh.AllAreas) &&
                        NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.corners.Length > 0)
                    {
                        corners = path.corners;
                        corner = Mathf.Min(1, corners.Length - 1);
                    }
                    else corners = new[] { target };
                }
                while (corner < corners.Length - 1 && Flat(corners[corner] - feet) < 0.4f) corner++;
                Vector3 next = corner < corners.Length ? corners[corner] : target;
                if (corner >= corners.Length - 1 && Flat(next - target) > arrive) next = target;
                if (label != "Hunter" && Threatened())
                {
                    yield return Evade(label);
                    corners = null;
                    lastMove = Time.time;
                    anchor = Feet;
                    end += 10;
                    continue;
                }
                Steer(next - feet, false);

                if (Player.interaction.Target is OfficeDoor door && !door.IsOpen && !door.locked &&
                    Flat(door.transform.position - feet) < 1.8f)
                {
                    Line($"- {Stamp()} Mở cửa **{door.name}** trên đường tới {label}.");
                    yield return Tap(Key.F);
                }

                if (Flat(Feet - anchor) > 0.5f)
                {
                    anchor = Feet;
                    lastMove = Time.time;
                }
                else if (Time.time - lastMove > 1.6f)
                {
                    stuckEvents++;
                    stuckHere++;
                    var nearDoor = level.interactions.OfType<OfficeDoor>().OrderBy(d => Flat(d.transform.position - Feet)).First();
                    Issue($"Kẹt trên đường tới {label} tại {P(Feet)}, góc kế tiếp {P(next)} (cửa gần nhất {nearDoor.name} {(nearDoor.IsOpen ? "mở" : "đóng")}, " +
                          $"{Flat(nearDoor.transform.position - Feet):0.0} m). Collider chạm: {Touching()}.");
                    if (stuckHere >= 4) break;
                    held.Add(Key.D);
                    yield return Tap(Key.Space);
                    yield return Wait(0.4f);
                    held.Remove(Key.D);
                    lastMove = Time.time;
                    anchor = Feet;
                    corners = null;
                }
                yield return Frame();
            }
            ReleaseMove();
        }

        private bool Threatened()
        {
            var hunter = level.hunter;
            float distance = Flat(hunter.transform.position - Feet);
            return (hunter.Current == HunterState.Chase && distance < 10) || (hunter.IsTrackingPlayer && distance < 6);
        }

        /// <summary>Như người chơi thận trọng: khi bị phát hiện, chạy (Shift, Burst) về phía xa Hunter cho tới khi mất dấu.</summary>
        private IEnumerator Evade(string label)
        {
            var hunter = level.hunter;
            float start = Time.time, calm = 0, repick = 0, until = Time.time + 14;
            int captured = captures;
            Vector3 refuge = Feet;
            var path = new NavMeshPath();
            Line($"  - {Stamp()} Né Hunter ({Flat(hunter.transform.position - Feet):0.0} m) khi đang tới {label}.");
            while (Time.time < until && Game.CanAct && captures == captured)
            {
                Vector3 feet = Feet, threat = hunter.transform.position;
                bool danger = hunter.Current == HunterState.Chase || hunter.IsTrackingPlayer || Flat(threat - feet) < 5;
                calm = danger ? 0 : calm + Time.deltaTime;
                if (calm > 1.5f) break;
                repick -= Time.deltaTime;
                if (repick <= 0 || Flat(feet - refuge) < 1)
                {
                    repick = 0.6f;
                    float best = float.NegativeInfinity;
                    for (int i = 0; i < 16; i++)
                    {
                        Vector3 probe = feet + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * 9;
                        if (!NavMesh.SamplePosition(probe, out var hit, 2, NavMesh.AllAreas)) continue;
                        float length = PathLength(feet, hit.position, path);
                        if (float.IsInfinity(length) || length > 20 || path.corners.Length < 2) continue;
                        Vector3 first = path.corners[1] - feet;
                        float away = Vector3.Dot(first.normalized, (feet - threat).normalized);
                        float score = Flat(hit.position - threat) + away * 4 - length * 0.15f;
                        if (score <= best) continue;
                        best = score;
                        refuge = hit.position;
                    }
                }
                Vector3 next = refuge;
                if (PathLength(feet, refuge, path) < float.PositiveInfinity && path.corners.Length > 1)
                    next = path.corners[1];
                Steer(next - feet, true);
                if (Game.Equipped == Skill.BurstSprint && Flat(threat - feet) < 6 && !Player.equipment.SkillActive && Player.equipment.SkillSeconds <= 0)
                    yield return Tap(Key.E);
                yield return Frame();
            }
            ReleaseMove();
            evadeSeconds += Time.time - start;
            Line($"  - {Stamp()} {(captures != captured ? "Né thất bại" : "Đã cắt đuôi")} sau {Time.time - start:0.0} s, tại {P(Feet)}.");
        }

        private string Touching()
        {
            var motor = Player.GetComponent<CharacterController>();
            Vector3 feet = Feet;
            var hits = Physics.OverlapCapsule(feet + Vector3.up * (motor.radius + 0.05f), feet + Vector3.up * (motor.height - motor.radius),
                motor.radius + 0.12f, level.worldMask, QueryTriggerInteraction.Ignore);
            var names = hits.Where(h => !h.transform.IsChildOf(Player.transform)).Select(h => PathOf(h.transform)).Distinct().Take(5).ToArray();
            return names.Length == 0 ? "không có" : string.Join(", ", names);
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (int i = 0; i < 3 && t.parent != null; i++)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private void Steer(Vector3 direction, bool sprint)
        {
            direction.y = 0;
            float desired = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(level.cameraView.Heading.eulerAngles.y, desired);
            float limit = 540 * Mathf.Max(Time.deltaTime, 1 / 120f);
            look.x += Mathf.Clamp(delta, -limit, limit) / Player.tuning.mouseSensitivity;
            Hold(Key.W, Mathf.Abs(delta) < 50);
            Hold(Key.LeftShift, sprint && Mathf.Abs(delta) < 30);
        }

        private IEnumerator Face(Vector3 point, float seconds, Func<bool> done)
        {
            ReleaseMove();
            float until = Time.time + seconds;
            while (Time.time < until && Game.CanAct && !done())
            {
                Vector3 direction = point - Feet;
                direction.y = 0;
                float delta = Mathf.DeltaAngle(level.cameraView.Heading.eulerAngles.y, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
                look.x += Mathf.Clamp(delta, -12, 12) / Player.tuning.mouseSensitivity;
                yield return Frame();
            }
        }

        private Vector3 FindAway(Vector3 centre, float radius)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector3 probe = centre + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * radius;
                if (NavMesh.SamplePosition(probe, out var hit, 0.8f, NavMesh.AllAreas) && Flat(hit.position - centre) > level.rules.relayRadius + 0.8f &&
                    PathLength(centre, hit.position, new NavMeshPath()) < radius * 2.5f)
                    return hit.position;
            }
            return level.playerSpawn.position;
        }

        private bool CanSee(Vector3 point)
        {
            Vector3 eye = Player.Eye;
            Vector3 offset = point - eye;
            if (offset.magnitude > 25) return false;
            Vector3 facing = level.cameraView.Heading * Vector3.forward;
            if (Vector3.Angle(facing, Vector3.ProjectOnPlane(offset, Vector3.up)) > 45) return false;
            return !Physics.Linecast(eye, point, level.worldMask, QueryTriggerInteraction.Ignore);
        }

        // ───────────────────────── Input ─────────────────────────

        private IEnumerator Choose(string button, bool optional = false)
        {
            yield return Frames(3);
            var events = EventSystem.current;
            for (int step = 0; step < 14 && Selected(events) != button; step++)
            {
                yield return Tap(step < 7 ? Key.DownArrow : Key.UpArrow);
                yield return Frames(2);
            }
            string selected = Selected(events);
            GameState before = Game.State.Current;
            Skill confirmed = Game.Confirmed, chosen = Game.Selected;
            if (selected == button)
            {
                yield return Tap(Key.Enter);
                yield return Frames(3);
            }
            bool changed = Game.State.Current != before || Game.Confirmed != confirmed || Game.Selected != chosen || button == "Burst Sprint";
            if (selected == button && changed)
            {
                Line($"- Menu: phím mũi tên tới **{button}**, Enter.");
                yield break;
            }
            var target = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == button && b.interactable);
            if (target == null)
            {
                if (!optional) Issue($"Không tìm thấy nút '{button}' đang bật (đang chọn '{selected}').");
                yield break;
            }
            Issue($"Bàn phím không kích hoạt được '{button}' (đang chọn '{selected}'), thử chuột.");
            pointer = RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
            yield return Frames(2);
            leftButton = true;
            yield return Frames(2);
            leftButton = false;
            yield return Frames(3);
        }

        private static string Selected(EventSystem events) =>
            events != null && events.currentSelectedGameObject != null ? events.currentSelectedGameObject.name : "";

        private void Hold(Key key, bool down)
        {
            if (down) held.Add(key);
            else held.Remove(key);
        }

        private void ReleaseMove()
        {
            held.Remove(Key.W);
            held.Remove(Key.LeftShift);
            held.Remove(Key.D);
        }

        private IEnumerator Tap(Key key)
        {
            held.Add(key);
            yield return Frame();
            held.Remove(key);
            yield return Frame();
        }

        private IEnumerator Frame()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(held.ToArray()));
            var state = new MouseState { position = pointer, delta = look };
            if (leftButton) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state);
            look = Vector2.zero;
            yield return null;
            Monitor();
        }

        private IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return Frame();
        }

        private IEnumerator Wait(float seconds, Func<bool> abort = null)
        {
            float until = Time.time + seconds;
            while (Time.time < until && (abort == null || !abort())) yield return Frame();
        }

        // ───────────────────────── Quan sát ─────────────────────────

        private void Monitor()
        {
            if (level == null) return;
            var hunter = level.hunter;
            if (Game.IsRunning && hunter.Current != lastHunter)
            {
                if (hunter.Current == HunterState.Chase)
                {
                    detections++;
                    Line($"  - {Stamp()} Hunter phát hiện TAY: TAY {P(Feet)}, Hunter {P(hunter.transform.position)}, " +
                         $"{Flat(hunter.transform.position - Feet):0.0} m, cấp {Game.Mission.Threat}.");
                }
                lastHunter = hunter.Current;
            }
            if (Game.State.Current != lastState)
            {
                if (Game.State.Current == GameState.Captured)
                {
                    captures++;
                    Line($"  - {Stamp()} **Bị bắt** tại {P(Feet)} (Hunter {P(hunter.transform.position)}). {Game.Status}");
                }
                if (Game.State.Current == GameState.Failed) Issue($"Hết giờ tại {P(Feet)} với {Game.Mission.ValidData} dữ liệu, Relay {Game.Mission.Relay}.");
                lastState = Game.State.Current;
            }
            if (Game.IsRunning && Game.Mission.Threat != lastThreat)
            {
                lastThreat = Game.Mission.Threat;
                Line($"  - {Stamp()} Hunter lên cấp {lastThreat}.");
            }
        }

        private IEnumerator Snapshot(string moment)
        {
            yield return Frames(2);
            Hud(moment);
        }

        private static string HudText(string field)
        {
            var hud = Object.FindFirstObjectByType<GameplayHud>();
            return typeof(GameplayHud).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hud) is Text t
                && t != null && t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text) ? t.text.Replace("\n", " / ") : null;
        }

        private void Hud(string moment)
        {
            var parts = new[] { "heading", "subheading", "objective", "progress", "warning", "prompt", "toast", "relayLabel" }
                .Select(f => (f, HudText(f))).Where(p => p.Item2 != null).Select(p => $"{p.f}: “{p.Item2}”");
            Line($"  - HUD · {moment}: {string.Join(" · ", parts)}");
        }

        private float Elapsed() => Game.IsRunning || Game.State.Current is GameState.Complete or GameState.Failed
            ? level.rules.timeLimit - Game.Timer.Remaining : 0;

        private string Stamp()
        {
            int t = Mathf.FloorToInt(Elapsed());
            return $"[{t / 60:00}:{t % 60:00}]";
        }

        private void Issue(string message)
        {
            issues.Add($"{Stamp()} {message}");
            Line($"  - ⚠ {Stamp()} {message}");
        }

        private void Line(string text)
        {
            log.AppendLine(text);
            Debug.Log("[Playtest] " + text);
        }

        private void WriteReport()
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Validation");
            Directory.CreateDirectory(folder);
            var report = new StringBuilder();
            report.AppendLine("# Playtest tự động Office_Level01");
            report.AppendLine();
            report.AppendLine($"Chạy lúc {DateTime.Now:yyyy-MM-dd HH:mm}. Input: bàn phím và chuột ảo của Input System (W/Shift/E/F/Esc, mũi tên + Enter trong menu). " +
                              $"Bot đi theo NavMesh tới mục tiêu gần nhất, Hunter chạy AI thật. Thời gian trong ngoặc là thời gian trong game (x{Speed} tốc độ thực).");
            report.AppendLine();
            report.Append(log);
            report.AppendLine();
            report.AppendLine("## Vấn đề ghi nhận");
            if (issues.Count == 0) report.AppendLine("- Không có.");
            foreach (var issue in issues) report.AppendLine("- " + issue);
            File.WriteAllText(Path.Combine(folder, "Playtest_Office.md"), report.ToString(), new UTF8Encoding(false));
        }

        private static float PathLength(Vector3 from, Vector3 to, NavMeshPath path)
        {
            if (!NavMesh.SamplePosition(from, out var a, 1.5f, NavMesh.AllAreas) || !NavMesh.SamplePosition(to, out var b, 2, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                return float.PositiveInfinity;
            float length = 0;
            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return length;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
        private static string P(Vector3 v) => string.Format(Invariant, "({0:0.0}; {1:0.0})", v.x, v.z);
    }
}
