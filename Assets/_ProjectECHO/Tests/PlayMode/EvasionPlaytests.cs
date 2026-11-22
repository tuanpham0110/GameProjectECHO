using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjectEcho.Tests
{
    /// <summary>
    /// Thử các cơ chế né Hunter bằng bàn phím ảo của Input System (C, T, F, 1/2, G, E, W đi qua đúng PlayerInput như
    /// người chơi). Test chỉ đặt sẵn vị trí TAY/Hunter để dựng tình huống, còn thao tác và AI Hunter chạy thật.
    /// Nhật ký ghi ở Builds/Validation/Playtest_Evasion.md.
    /// </summary>
    [Category("Playtest")]
    public abstract class EvasionPlaytestBase
    {
        protected static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly StringBuilder Report = new();
        protected abstract string ScenePath { get; }
        protected LevelManager level;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings previousSettings;
        private readonly HashSet<Key> held = new();
        private string profile;

        protected GameManager Game => level.game;
        protected PlayerController Player => level.player;
        protected HunterBrain Hunter => level.hunter;
        protected NavMeshAgent Agent => level.hunter.GetComponent<NavMeshAgent>();
        protected Vector3 Feet => Player.transform.position;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1;
            previousSettings = InputSystem.settings;
            var settings = Object.Instantiate(previousSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            keyboard = InputSystem.AddDevice<Keyboard>("ECHO Evasion Keyboard");
            mouse = InputSystem.AddDevice<Mouse>("ECHO Evasion Mouse");
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(ScenePath), LoadSceneMode.Single);
#endif
            level = Object.FindFirstObjectByType<LevelManager>();
            Assert.That(level, Is.Not.Null);
            profile = Path.Combine(Path.GetTempPath(), "EchoEvasion-" + Guid.NewGuid().ToString("N"));
            Game.LoadProfile(new SaveSystem(profile));
            // Bản sao profile Hunter không sai số tín hiệu để tình huống lặp lại được (không sửa asset gốc).
            Hunter.profile = Object.Instantiate(Hunter.profile);
            Hunter.profile.signalUncertainty = 0;
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            held.Clear();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (previousSettings != null) InputSystem.settings = previousSettings;
            if (profile != null && Directory.Exists(profile)) Directory.Delete(profile, true);
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Validation");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Playtest_Evasion.md"),
                $"# Thử cơ chế né Hunter ({DateTime.Now:yyyy-MM-dd HH:mm})\n\nInput: bàn phím ảo Input System. Thời gian là thời gian trong game.\n\n" + Report,
                new UTF8Encoding(false));
            yield return null;
        }

        // ───────────── Dựng tình huống ─────────────

        protected void StartRun(Skill skill)
        {
            Game.OpenBriefing();
            Game.Select(skill);
            Game.Confirm();
            Game.StartRun();
            Assert.That(Game.CanAct, Is.True, "Không bắt đầu được lượt.");
            Assert.That(Game.Equipped, Is.EqualTo(skill));
        }

        /// <summary>Như người chơi: Esc mở menu tạm dừng, rồi "Chuẩn bị lượt mới" và bắt đầu lại với kỹ năng đã chọn.</summary>
        protected IEnumerator NewRun(Skill skill)
        {
            yield return Tap(Key.Escape);
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Paused), "Esc không mở menu tạm dừng.");
            StartRun(skill);
            yield return Frame();
        }

        protected void PlaceTay(Vector3 position, Vector3 lookAt)
        {
            Vector3 face = lookAt - position;
            face.y = 0;
            Player.Teleport(OnMesh(position), Quaternion.LookRotation(face.sqrMagnitude > 0.001f ? face : Vector3.forward));
            Physics.SyncTransforms();
        }

        protected void PlaceHunter(Vector3 position, Vector3 lookAt)
        {
            Assert.That(Agent.Warp(OnMesh(position)), Is.True, $"Không đặt được Hunter tại {P(position)}.");
            Vector3 face = lookAt - Hunter.transform.position;
            face.y = 0;
            if (face.sqrMagnitude > 0.001f) Hunter.transform.rotation = Quaternion.LookRotation(face);
        }

        /// <summary>
        /// Hunter quay lưng ở <paramref name="from"/>, nhận tín hiệu tại chỗ TAY rồi tự quay lại điều tra, thấy và đuổi TAY.
        /// Đặt Hunter nhìn thẳng vào TAY thì Hunter tuần tra hay quay đi theo tuyến trước khi kịp phản ứng.
        /// </summary>
        protected IEnumerator DrawHunter(Vector3 from, float timeout)
        {
            Hunter.enabled = true;
            PlaceHunter(from, from + (from - Feet));
            yield return null;
            Hunter.ReceiveScannerSignal(Feet, 100);
            var states = new List<string>();
            yield return Wait(timeout, () =>
            {
                string state = $"{Hunter.Current}{(Hunter.IsTrackingPlayer ? "+thấy" : "")}";
                if (states.Count == 0 || states[^1] != state) states.Add(state);
                return Hunter.Current == HunterState.Chase && Hunter.IsTrackingPlayer || Game.Captures > 0;
            });
            Line($"[{Clock()}] Hunter từ {P(from)}: {string.Join(" → ", states)}; cách TAY {Flat(Hunter.transform.position - Feet):0.0} m.");
        }

        /// <summary>Hunter đứng yên (tắt AI) ở xa, để thử thao tác không bị Hunter xen vào.</summary>
        protected void ParkHunter(Vector3 position)
        {
            PlaceHunter(position, position + Vector3.forward);
            Hunter.enabled = false;
            Agent.isStopped = true;
        }

        protected static Vector3 OnMesh(Vector3 point) =>
            NavMesh.SamplePosition(point, out var hit, 1.5f, NavMesh.AllAreas) ? hit.position : point;

        protected bool HunterSeesTay(Vector3 hunterFeet) =>
            HunterPerception.IsClear(hunterFeet + Vector3.up * Hunter.profile.eyeHeight, Player.Eye, Hunter.profile.obstructionMask & level.worldMask);

        /// <summary>Điểm trên NavMesh cách mục tiêu khoảng <paramref name="distance"/> m, nhìn thông tới mục tiêu từ tầm mắt.</summary>
        protected Vector3? Around(Vector3 target, float distance, Func<Vector3, bool> accept)
        {
            foreach (float d in new[] { distance, distance * 0.85f, distance * 1.15f, distance * 0.7f })
                for (int i = 0; i < 16; i++)
                {
                    Vector3 probe = target + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * d;
                    // Mục tiêu có thể nằm cao hơn sàn (vật phẩm trên bàn): dò NavMesh theo chiều dọc, chỉ nhận điểm lệch ngang ít.
                    if (!NavMesh.SamplePosition(probe, out var hit, 1.5f, NavMesh.AllAreas) || Flat(hit.position - probe) > 0.35f) continue;
                    if (accept(hit.position)) return hit.position;
                }
            return null;
        }

        /// <summary>Hướng đi thẳng được <paramref name="length"/> m từ gần <paramref name="near"/> mà không chạm vật cản.</summary>
        protected (Vector3 start, Vector3 direction)? OpenLane(Vector3 near, float length)
        {
            Vector3 start = OnMesh(near);
            for (int i = 0; i < 16; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward;
                if (NavMesh.Raycast(start, start + direction * length, out _, NavMesh.AllAreas)) continue;
                if (Physics.SphereCast(start + Vector3.up * 0.6f, 0.45f, direction, out _, length, level.worldMask, QueryTriggerInteraction.Ignore) ||
                    Physics.SphereCast(start + Vector3.up * 1.4f, 0.45f, direction, out _, length, level.worldMask, QueryTriggerInteraction.Ignore)) continue;
                return (start, direction);
            }
            return null;
        }

        protected bool Clear(Vector3 from, Vector3 to) =>
            HunterPerception.IsClear(from, to, level.worldMask);

        /// <summary>Giống InteractionSystem: tia từ mắt tới vật chỉ được chạm chính vật đó.</summary>
        protected bool Reaches(Vector3 eye, Interactable target)
        {
            Vector3 offset = target.Point - eye;
            return offset.magnitude <= Player.tuning.interactionRange &&
                   (!Physics.Raycast(eye, offset.normalized, out var hit, offset.magnitude, level.worldMask, QueryTriggerInteraction.Ignore) ||
                    hit.transform.IsChildOf(target.transform));
        }

        // ───────────── Input ─────────────

        protected IEnumerator Frame()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(held.ToArray()));
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
        }

        protected IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return Frame();
        }

        protected IEnumerator Tap(Key key)
        {
            held.Add(key);
            yield return Frame();
            held.Remove(key);
            yield return Frame();
        }

        protected void Hold(Key key, bool down)
        {
            if (down) held.Add(key);
            else held.Remove(key);
        }

        /// <summary>Chờ theo thời gian game cho tới khi điều kiện đúng; trả về thời gian đã chờ qua callback.</summary>
        protected IEnumerator Wait(float seconds, Func<bool> until = null)
        {
            float end = Time.time + seconds;
            while (Time.time < end && (until == null || !until())) yield return Frame();
        }

        // ───────────── Nhật ký ─────────────

        protected void Section(string title)
        {
            Report.AppendLine().AppendLine("## " + title).AppendLine();
            Debug.Log("[Evasion] " + title);
        }

        protected void Line(string text)
        {
            Report.AppendLine("- " + text);
            Debug.Log("[Evasion] " + text);
        }

        protected string Clock() => MissionGuide.Clock(level.rules.timeLimit - Game.Timer.Remaining);
        protected static string P(Vector3 v) => string.Format(Invariant, "({0:0.0}; {1:0.0})", v.x, v.z);
        protected static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
        protected string Hud(string field) => (typeof(GameplayHud).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(Object.FindFirstObjectByType<GameplayHud>()) as UnityEngine.UI.Text)?.text ?? "";
    }

    public sealed class OfficeEvasionPlaytests : EvasionPlaytestBase
    {
        protected override string ScenePath => "Assets/_ProjectECHO/Scenes/Office_Level01.unity";

        private HideSpot Spot(Vector3 inside) =>
            level.interactions.OfType<HideSpot>().OrderBy(s => (s.inside.position - inside).sqrMagnitude).First();

        private void CollectAll()
        {
            foreach (var data in level.interactions.OfType<DataPickup>()) Game.Collect(data.dataId, data.displayName);
            Assert.That(Game.Mission.Threat, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator LockerProtectsUnseenEntryEvenInLockdownAndCExits()
        {
            Section("Tủ kín (−4; −31,1), vào khi Hunter chưa thấy, Lockdown");
            StartRun(Skill.BurstSprint);
            CollectAll();
            var spot = Spot(new Vector3(-4, 0, -31.1f));
            ParkHunter(new Vector3(22, 0, -4));
            PlaceTay(spot.outside.position, spot.inside.position);
            yield return Wait(0.3f);
            Assert.That(Player.interaction.NearbyHide, Is.EqualTo(spot), "Không có lời nhắc C ở trước tủ.");
            Line($"[{Clock()}] Đứng trước tủ, HUD: “{Hud("prompt")}”. Bấm C.");
            yield return Tap(Key.C);
            yield return Wait(spot.entryDelay + 0.3f);
            Assert.That(Player.IsHiding && Player.IsSafelyHidden, Is.True, "Vào tủ khi chưa bị thấy phải được bảo vệ tuyệt đối.");
            Line($"[{Clock()}] Đã vào tủ sau {spot.entryDelay:0.0} s, an toàn tuyệt đối. HUD: “{Hud("prompt")}”.");
            StringAssert.Contains("an toàn", Hud("prompt"));

            // Hunter Lockdown đi tới ngay trước tủ (nhận tín hiệu tại cửa tủ) và lùng sục quanh đó.
            var path = new NavMeshPath();
            Vector3? approach = Around(spot.outside.position, 6, p =>
                NavMesh.CalculatePath(p, OnMesh(spot.outside.position), NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete);
            Assert.That(approach.HasValue, Is.True, "Không có điểm Hunter đi tới được cửa tủ.");
            Hunter.enabled = true;
            PlaceHunter(approach.Value, spot.outside.position);
            yield return null;
            Hunter.ReceiveScannerSignal(spot.outside.position, 100);
            Line($"[{Clock()}] Hunter Lockdown xuất phát từ {P(Hunter.transform.position)}, nhận tín hiệu tại cửa tủ.");
            Time.timeScale = 4;
            bool chased = false;
            float closest = float.PositiveInfinity;
            yield return Wait(36, () =>
            {
                chased |= Hunter.Current == HunterState.Chase;
                closest = Mathf.Min(closest, Flat(Hunter.transform.position - Feet));
                return Game.Captures > 0;
            });
            Time.timeScale = 1;
            Line($"[{Clock()}] Hunter Lockdown điều tra cửa tủ, tới gần nhất {closest:0.0} m; nấp {Player.HiddenSeconds:0} s; Chase: {(chased ? "có" : "không")}; " +
                 $"bị bắt {Game.Captures} lần; Hunter {Hunter.Current} {Hunter.NavigationIssue}.");
            Assert.That(Game.Captures, Is.Zero, "Hunter không được bắt TAY trong tủ kín khi chưa thấy lúc vào.");
            Assert.That(chased, Is.False);
            Assert.That(closest, Is.LessThan(3), "Tình huống cần Hunter tới sát tủ.");

            ParkHunter(new Vector3(22, 0, -4));
            yield return Tap(Key.C);
            yield return Wait(spot.exitDelay + 0.3f);
            Assert.That(Player.IsHiding, Is.False);
            Assert.That(Flat(Feet - spot.outside.position), Is.LessThan(0.5f));
            Line($"[{Clock()}] Bấm C, ra khỏi tủ sau {spot.exitDelay:0.0} s, đứng tại cửa tủ {P(Feet)}.");
        }

        [UnityTest]
        public IEnumerator HunterThatSeesLockerEntryPullsTayOut()
        {
            Section("Tủ kín (−4; −31,1), vào khi Hunter đang thấy TAY (cấp 1)");
            StartRun(Skill.BurstSprint);
            var spot = Spot(new Vector3(-4, 0, -31.1f));
            PlaceTay(spot.outside.position, spot.inside.position);
            Vector3? watch = Around(spot.outside.position, 7, p => Flat(p - spot.outside.position) < 7.6f && HunterSeesTay(p));
            Assert.That(watch.HasValue, Is.True, "Không tìm được chỗ Hunter nhìn thấy cửa tủ.");
            yield return DrawHunter(watch.Value, 4);
            Assert.That(Hunter.Current, Is.EqualTo(HunterState.Chase), "Hunter phải phát hiện TAY trước khi TAY vào tủ.");
            Line($"[{Clock()}] Hunter ở {P(Hunter.transform.position)}, cách {Flat(Hunter.transform.position - Feet):0.0} m, đã phát hiện và đang đuổi TAY. HUD: “{Hud("warning")}”. Bấm C.");
            yield return Tap(Key.C);
            yield return Wait(spot.entryDelay + 0.2f, () => Game.Captures > 0);
            Line($"[{Clock()}] Sau khi vào: trong tủ {(Player.IsHiding ? "có" : "không")}, an toàn {(Player.IsSafelyHidden ? "có" : "không")}, " +
                 $"Hunter {Hunter.Current}, cách {Flat(Hunter.transform.position - Feet):0.0} m. HUD: “{Hud("prompt")}”.");
            Assert.That(Game.Captures == 0 && Player.IsHiding, Is.True, "Tình huống cần TAY vào được tủ trước khi Hunter tới.");
            Assert.That(Player.IsSafelyHidden, Is.False, "Hunter đã thấy lúc vào thì tủ không được bảo vệ.");
            StringAssert.Contains("đã thấy", Hud("prompt"));
            yield return Wait(12, () => Game.Captures > 0);
            Line($"[{Clock()}] Sau 12 s: Hunter {Hunter.Current}, cách cửa tủ {Flat(Hunter.transform.position - spot.outside.position):0.0} m, bị bắt {Game.Captures} lần.");
            Assert.That(Game.Captures, Is.EqualTo(1), "Hunter đã thấy TAY vào tủ phải tới lôi TAY ra (D09).");
        }

        [UnityTest]
        public IEnumerator RelativeHideOverThirtySecondsDrawsHunterToCheck()
        {
            Section("Nấp sau vật cản (−10,75; −18,6) quá 30 s");
            StartRun(Skill.BurstSprint);
            var spot = Spot(new Vector3(-10.75f, 0, -18.6f));
            Assert.That(spot.absolute, Is.False);
            PlaceTay(spot.outside.position, spot.inside.position);
            Vector3 start = OnMesh(new Vector3(-12, 0, -38));
            PlaceHunter(start, start + Vector3.forward);
            yield return Wait(0.3f);
            Assert.That(Player.interaction.NearbyHide, Is.EqualTo(spot),
                $"Không có lời nhắc C: TAY {P(Feet)} (cửa chỗ nấp {P(spot.outside.position)}), chạm đất {Player.Grounded}, gần nhất {Player.interaction.NearbyHide?.name ?? "không"}.");
            yield return Tap(Key.C);
            yield return Wait(spot.entryDelay + 0.3f);
            Assert.That(Player.IsHiding, Is.True, $"Bấm C không vào chỗ nấp, chạm đất {Player.Grounded}.");
            Assert.That(Player.IsSafelyHidden, Is.False);
            Line($"[{Clock()}] Bấm C, nấp sau vật cản. HUD: “{Hud("prompt")}”. Hunter tuần tra từ {P(start)}.");
            Time.timeScale = 4;
            bool investigated = false, seenEarly = false;
            float checkedAt = -1;
            string warningPrompt = null;
            yield return Wait(level.hunter.profile.relativeHideCheckSeconds + 25, () =>
            {
                if (!investigated && Hunter.IsTrackingPlayer && Player.HiddenSeconds < Hunter.profile.relativeHideCheckSeconds) seenEarly = true;
                if (warningPrompt == null && Player.HiddenSeconds > Hunter.profile.relativeHideCheckSeconds - 5) warningPrompt = Hud("prompt");
                if (!investigated && Hunter.Current == HunterState.Investigate && Hunter.LastKnownPosition is Vector3 at &&
                    Flat(at - spot.outside.position) < 0.5f && Player.HiddenSeconds >= Hunter.profile.relativeHideCheckSeconds)
                {
                    investigated = true;
                    checkedAt = Player.HiddenSeconds;
                }
                return Game.Captures > 0 || (investigated && Hunter.Current is HunterState.Patrol);
            });
            Time.timeScale = 1;
            Line($"[{Clock()}] Hunter tới kiểm tra chỗ nấp: {(investigated ? $"có, lúc đã nấp {checkedAt:0} s" : "không")}; thấy TAY trước mốc 30 s: {(seenEarly ? "có" : "không")}; " +
                 $"bị bắt {Game.Captures} lần, Hunter {Hunter.Current}. HUD 5 s trước mốc: “{warningPrompt}”.");
            Assert.That(investigated || seenEarly, Is.True, "Nấp tương đối quá 30 s phải khiến Hunter tới kiểm tra (D13).");
            StringAssert.Contains("Hunter kiểm tra sau", warningPrompt ?? "");
        }

        [UnityTest]
        public IEnumerator ScannerHeldLongEnoughSignalsHunterOnce()
        {
            Section("Scanner (T) bật liên tục");
            StartRun(Skill.BurstSprint);
            // Hai bên vách tây phòng server: Hunter không thấy TAY nhưng ở trong bán kính tín hiệu 15 m.
            PlaceTay(new Vector3(14.5f, 0, -20), new Vector3(10, 0, -20));
            ParkHunter(new Vector3(18.5f, 0, -20));
            Assert.That(HunterSeesTay(Hunter.transform.position), Is.False);
            yield return Tap(Key.T);
            Assert.That(Player.equipment.ScannerOn, Is.True);
            Line($"[{Clock()}] Bấm T, Scanner bật. HUD: “{Hud("skill")}”.");
            float turnedOn = Time.time, signalled = -1;
            string countdown = null;
            Time.timeScale = 4;
            yield return Wait(Player.tuning.scannerExposure + 2, () =>
            {
                if (countdown == null && Time.time - turnedOn > Player.tuning.scannerExposure - 6) countdown = Hud("skill");
                if (signalled < 0 && Hunter.Current == HunterState.Investigate) signalled = Time.time - turnedOn;
                return signalled >= 0;
            });
            Time.timeScale = 1;
            Line($"[{Clock()}] Hunter nhận tín hiệu sau {signalled:0.0} s bật liên tục (mốc {Player.tuning.scannerExposure:0} s), điều tra {P(Hunter.LastKnownPosition ?? Vector3.zero)}. " +
                 $"Thông báo: “{Game.Status}”. HUD vài giây trước đó: “{countdown}”.");
            Assert.That(signalled, Is.InRange(Player.tuning.scannerExposure - 0.2f, Player.tuning.scannerExposure + 0.5f));
            Assert.That(Flat(Hunter.LastKnownPosition.Value - Feet), Is.LessThan(0.5f));
            StringAssert.Contains("phát tín hiệu sau", countdown);
            yield return Frames(2);
            StringAssert.Contains("Hunter đã bắt sóng", Hud("skill"));
            yield return Tap(Key.T);
            Assert.That(Player.equipment.ScannerOn, Is.False);
            Line($"[{Clock()}] Bấm T lần nữa, Scanner tắt.");
        }

        [UnityTest]
        public IEnumerator EmpAndStunAffectHunterThenItRecovers()
        {
            Section("EMP Mine và Stun Grenade");
            StartRun(Skill.BurstSprint);
            ParkHunter(new Vector3(22, 0, -4));
            foreach (var pickup in level.interactions.OfType<SupportPickup>().Where(p => p.item != SupportItem.Armor))
            {
                Vector3? stand = Around(pickup.transform.position, 1.2f, p => Reaches(p + Vector3.up * 1.58f, pickup));
                if (!stand.HasValue)
                {
                    bool onMesh = NavMesh.SamplePosition(pickup.transform.position, out var nearest, 4, NavMesh.AllAreas);
                    var blockers = Physics.OverlapSphere(pickup.Point, 0.6f, level.worldMask, QueryTriggerInteraction.Ignore).Select(c => c.name).Distinct().Take(6);
                    Assert.Fail($"Không có chỗ đứng cạnh {pickup.Prompt}: vật ở {pickup.transform.position}, điểm tương tác {pickup.Point}; " +
                                $"NavMesh gần nhất {(onMesh ? $"{nearest.position} cách {Vector3.Distance(nearest.position, pickup.transform.position):0.00} m" : "không có trong 4 m")}; " +
                                $"collider quanh vật: {string.Join(", ", blockers)}.");
                }
                PlaceTay(stand.Value, pickup.transform.position);
                yield return Wait(0.3f);
                Assert.That(Player.interaction.Target, Is.EqualTo(pickup), $"Không có lời nhắc F ở {pickup.Prompt}.");
                yield return Tap(Key.F);
                Line($"[{Clock()}] Bấm F nhặt {pickup.Prompt} tại {P(pickup.transform.position)}. Thông báo: “{Game.Status}”. HUD: “{Hud("items")}”.");
            }
            Assert.That(Game.Inventory.Count(SupportItem.Emp), Is.EqualTo(2));
            Assert.That(Game.Inventory.Count(SupportItem.Stun), Is.EqualTo(2));

            // EMP: đặt mìn trên lối đi, Hunter đi qua.
            Vector3 lane = OnMesh(new Vector3(9, 0, -27.4f));
            PlaceTay(lane, lane + Vector3.right);
            yield return Tap(Key.Digit1);
            Assert.That(Player.equipment.SelectedItem, Is.EqualTo(SupportItem.Emp));
            StringAssert.Contains("Chọn EMP Mine", Game.Status);
            yield return Tap(Key.G);
            var mine = Object.FindObjectsByType<SupportDevice>(FindObjectsSortMode.None).SingleOrDefault();
            Assert.That(mine, Is.Not.Null, "Bấm G không đặt EMP.");
            Line($"[{Clock()}] Bấm 1 rồi G: đặt EMP tại {P(mine.transform.position)}. Thông báo: “{Game.Status}”. HUD: “{Hud("items")}”.");
            StringAssert.Contains("Đã đặt EMP", Game.Status);
            Vector3 minePosition = mine.transform.position;
            PlaceTay(level.playerSpawn.position, level.playerSpawn.position + Vector3.forward);
            Hunter.enabled = true;
            PlaceHunter(minePosition + new Vector3(-6, 0, 0), minePosition);
            yield return null;
            Hunter.ReceiveScannerSignal(minePosition + new Vector3(3, 0, 0), 100);
            float normal = 0;
            yield return Wait(8, () =>
            {
                if (Hunter.EmpRemaining <= 0) normal = Agent.speed;
                return Hunter.EmpRemaining > 0;
            });
            Assert.That(Hunter.EmpRemaining, Is.GreaterThan(0), $"Hunter đi qua EMP mà không bị ảnh hưởng (Hunter {P(Hunter.transform.position)}, mìn {P(minePosition)}).");
            yield return Frames(2);
            float slowed = Agent.speed;
            Line($"[{Clock()}] EMP kích hoạt khi Hunter đi qua: tốc độ {normal:0.00} → {slowed:0.00} m/s trong {Player.tuning.empSeconds:0} s. " +
                 $"Thông báo: “{Game.Status}”. HUD: “{Hud("warning")}”.");
            Assert.That(slowed, Is.EqualTo(normal * 0.5f).Within(0.05f));
            StringAssert.Contains("EMP kích hoạt", Game.Status);
            StringAssert.Contains("EMP: HUNTER CHẬM", Hud("warning"));
            yield return Wait(Player.tuning.empSeconds + 1, () => Hunter.EmpRemaining <= 0);
            yield return Frames(2);
            Line($"[{Clock()}] Hết EMP, tốc độ về {Agent.speed:0.00} m/s.");
            Assert.That(Agent.speed, Is.EqualTo(normal).Within(0.05f));

            // Stun: ném vào Hunter đang tới gần.
            Hunter.enabled = false;
            Vector3 thrower = OnMesh(new Vector3(9, 0, -27.4f));
            PlaceTay(thrower, thrower + Vector3.right);
            Hunter.enabled = true;
            PlaceHunter(thrower + new Vector3(6, 0, 0), thrower);
            yield return Tap(Key.Digit2);
            Assert.That(Player.equipment.SelectedItem, Is.EqualTo(SupportItem.Stun));
            yield return Tap(Key.G);
            Line($"[{Clock()}] Bấm 2 rồi G: ném Stun về phía Hunter cách {Flat(Hunter.transform.position - Feet):0.0} m. Thông báo: “{Game.Status}”.");
            StringAssert.Contains("Đã ném Stun", Game.Status);
            yield return Wait(2, () => Hunter.Current == HunterState.Stunned || Game.Captures > 0);
            Assert.That(Hunter.Current, Is.EqualTo(HunterState.Stunned), "Stun không làm Hunter choáng.");
            float stunnedAt = Time.time;
            yield return Frame();
            Line($"[{Clock()}] Hunter choáng. Thông báo: “{Game.Status}”. HUD: “{Hud("warning")}”.");
            StringAssert.Contains("Stun trúng", Game.Status);
            StringAssert.Contains("HUNTER BỊ CHOÁNG", Hud("warning"));
            PlaceTay(level.playerSpawn.position, level.playerSpawn.position + Vector3.forward);
            yield return Wait(Player.tuning.stunSeconds + 2, () => Hunter.Current is not (HunterState.Stunned or HunterState.Recovery));
            float down = Time.time - stunnedAt;
            yield return Frame();
            Line($"[{Clock()}] Hunter hồi phục sau {down:0.0} s ({Hunter.Current}), Enrage {Hunter.EnrageRemaining:0.0} s, tốc độ {Agent.speed:0.00} m/s. HUD: “{Hud("warning")}”.");
            Assert.That(down, Is.InRange(Player.tuning.stunSeconds, Player.tuning.stunSeconds + Hunter.profile.recoverySeconds + 0.3f));
            Assert.That(Hunter.EnrageRemaining, Is.GreaterThan(0));
            StringAssert.Contains("HUNTER NỔI GIẬN", Hud("warning"));

            // Ném quả Stun thứ hai khi Hunter đang nổi giận: miễn choáng và có thông báo.
            Vector3 hunterAt = Hunter.transform.position;
            Vector3? near = Around(hunterAt, 6, p => Clear(p + Vector3.up * 1.58f, hunterAt + Vector3.up));
            Assert.That(near.HasValue, Is.True);
            PlaceTay(near.Value, hunterAt);
            yield return Tap(Key.G);
            yield return Wait(1.3f);
            Line($"[{Clock()}] Ném Stun thứ hai khi Hunter nổi giận ({Hunter.EnrageRemaining:0.0} s còn lại): Hunter {Hunter.Current}. Thông báo: “{Game.Status}”.");
            Assert.That(Hunter.Current, Is.Not.EqualTo(HunterState.Stunned));
            Assert.That(Game.Status, Does.Contain("không có tác dụng").Or.Contain("nổ trượt"));
            PlaceTay(level.playerSpawn.position, level.playerSpawn.position + Vector3.forward);
            yield return Wait(Player.tuning.enrageSeconds + 1, () => Hunter.EnrageRemaining <= 0);
            Assert.That(Hunter.EnrageRemaining, Is.Zero);
            Assert.That(Game.Inventory.Count(SupportItem.Stun), Is.Zero);
            yield return Tap(Key.G);
            Line($"[{Clock()}] Bấm G khi hết Stun. Thông báo: “{Game.Status}”.");
            StringAssert.Contains("Không còn Stun", Game.Status);
        }

        [UnityTest]
        public IEnumerator BurstSprintCooldownSurvivesCaptureAndResetsOnNewRun()
        {
            Section("Burst Sprint (E), bị bắt và lượt mới");
            StartRun(Skill.BurstSprint);
            ParkHunter(new Vector3(22, 0, -4));
            (Vector3 start, Vector3 direction)? lane = null;
            foreach (var near in new[] { level.playerSpawn.position, new Vector3(5, 0, -12), new Vector3(9, 0, -27.4f), new Vector3(-8, 0, -20) })
                if ((lane = OpenLane(near, 7)) != null) break;
            Assert.That(lane.HasValue, Is.True, "Không tìm được làn chạy thẳng 7 m.");
            PlaceTay(lane.Value.start, lane.Value.start + lane.Value.direction);
            Line($"[{Clock()}] Làn đo từ {P(lane.Value.start)} theo hướng {P(lane.Value.direction)}.");
            Hold(Key.W, true);
            yield return Wait(0.6f);
            Vector3 from = Feet;
            yield return Wait(0.5f);
            float walk = Flat(Feet - from) / 0.5f;
            yield return Tap(Key.E);
            Assert.That(Player.equipment.SkillActive, Is.True, "Bấm E không kích hoạt Burst Sprint.");
            from = Feet;
            yield return Wait(0.5f);
            float burst = Flat(Feet - from) / 0.5f;
            Hold(Key.W, false);
            Line($"[{Clock()}] Đi bộ {walk:0.0} m/s, bấm E: {burst:0.0} m/s (×{burst / walk:0.00}). HUD: “{Hud("skill")}”.");
            Assert.That(burst / walk, Is.EqualTo(Player.tuning.burstMultiplier).Within(0.12f));
            yield return Wait(Player.tuning.burstDuration, () => !Player.equipment.SkillActive);
            Assert.That(Player.equipment.SkillSeconds, Is.EqualTo(Player.tuning.burstCooldown).Within(0.5f));
            Line($"[{Clock()}] Hết hiệu lực, hồi chiêu {Player.equipment.SkillSeconds:0} s. HUD: “{Hud("skill")}”.");
            float before = Player.equipment.SkillSeconds;
            yield return Tap(Key.E);
            Assert.That(Player.equipment.SkillActive, Is.False);
            Line($"[{Clock()}] Bấm E khi đang hồi: không kích hoạt. Thông báo: “{Game.Status}”.");
            StringAssert.Contains("đang hồi", Game.Status);

            // Lượt mới xóa hồi chiêu; bị bắt khi đang Burst thì kết thúc hiệu lực và vào hồi chiêu như thường.
            yield return NewRun(Skill.BurstSprint);
            Assert.That(Player.equipment.SkillSeconds, Is.Zero, "Lượt mới phải xóa hồi chiêu.");
            Line($"[{Clock()}] Chuẩn bị lượt mới: hồi chiêu {Player.equipment.SkillSeconds:0} s (trước đó {before:0} s).");
            yield return Wait(level.rules.spawnProtection + 0.2f);
            yield return Tap(Key.T);
            yield return Tap(Key.E);
            Assert.That(Player.equipment.SkillActive, Is.True);
            Vector3? close = Around(Feet, 4, HunterSeesTay);
            Assert.That(close.HasValue, Is.True, "Không có chỗ Hunter nhìn thấy TAY ở sảnh.");
            yield return DrawHunter(close.Value, 3);
            yield return Wait(4, () => Game.State.Current == GameState.Captured);
            Assert.That(Game.State.Current, Is.EqualTo(GameState.Captured), "Hunter cạnh TAY phải bắt được TAY.");
            Line($"[{Clock()}] Bị Hunter bắt khi đang Burst. Burst còn hiệu lực: {(Player.equipment.SkillActive ? "có" : "không")}, hồi chiêu {Player.equipment.SkillSeconds:0} s, " +
                 $"Scanner {(Player.equipment.ScannerOn ? "bật" : "tắt")}. Thông báo: “{Game.Status}”.");
            Assert.That(Player.equipment.SkillActive, Is.False);
            Assert.That(Player.equipment.SkillSeconds, Is.EqualTo(Player.tuning.burstCooldown).Within(0.5f));
            Assert.That(Player.equipment.ScannerOn, Is.False);
            Hunter.enabled = false;
            yield return Wait(level.rules.captureSeconds + 0.5f, () => Game.CanAct);
            yield return Wait(0.2f);
            Assert.That(Game.CanAct, Is.True);
            Line($"[{Clock()}] Hồi sinh tại {P(Feet)}, hồi chiêu còn {Player.equipment.SkillSeconds:0} s. HUD: “{Hud("skill")}”.");
            Assert.That(Player.equipment.SkillSeconds, Is.GreaterThan(Player.tuning.burstCooldown - 3));
        }
    }

    public sealed class SandboxEvasionPlaytests : EvasionPlaytestBase
    {
        protected override string ScenePath => "Assets/_ProjectECHO/Scenes/Sandbox/Blockout_Sandbox.unity";

        /// <summary>Cấu hình kiểm thử riêng: bản sao luật mở cả ba kỹ năng (D06), không sửa asset gốc.</summary>
        private void UnlockAllSkills()
        {
            level.rules = Object.Instantiate(level.rules);
            level.rules.playtestSkills = new[] { Skill.BurstSprint, Skill.Dash, Skill.HunterSensor };
        }

        [UnityTest]
        public IEnumerator DashVaultsLowObstacleAndReportsBlockedDash()
        {
            Section("Sandbox · Dash (E)");
            UnlockAllSkills();
            StartRun(Skill.Dash);
            ParkHunter(new Vector3(16, 0, 10));
            PlaceTay(new Vector3(-9, 0, -10.2f), new Vector3(-9, 0, 0));
            yield return Wait(0.3f);
            float z = Feet.z;
            yield return Tap(Key.E);
            yield return Wait(0.5f);
            Line($"[{Clock()}] Bấm E trước vật thấp (−9; −8, cao 0,4 m) từ {P(Feet)}: lướt từ z {z:0.0} tới {Feet.z:0.0}, hồi chiêu {Player.equipment.SkillSeconds:0} s. " +
                 $"Thông báo: “{Game.Status}”. HUD: “{Hud("skill")}”.");
            Assert.That(Feet.z, Is.GreaterThan(-7.5f), "Dash phải vượt qua vật thấp.");
            Assert.That(Player.equipment.SkillSeconds, Is.EqualTo(Player.tuning.dashCooldown).Within(0.6f));

            yield return NewRun(Skill.Dash);
            ParkHunter(new Vector3(16, 0, 10));
            yield return Wait(0.2f);
            // Tường biên phía tây ở x = −19: đứng cách 1,5 m và lướt vào tường.
            PlaceTay(new Vector3(-17.3f, 0, 0), new Vector3(-30, 0, 0));
            yield return Wait(0.3f);
            float x = Feet.x;
            yield return Tap(Key.E);
            yield return Wait(0.5f);
            Line($"[{Clock()}] Bấm E khi tường cách ~1,5 m: x {x:0.00} → {Feet.x:0.00}, hồi chiêu {Player.equipment.SkillSeconds:0} s. Thông báo: “{Game.Status}”.");
            Assert.That(Feet.x, Is.GreaterThan(-19), "Dash không được xuyên tường.");
        }

        [UnityTest]
        public IEnumerator HunterSensorShowsHunterWithinRangeThenCoolsDown()
        {
            Section("Sandbox · Hunter Sensor (E)");
            UnlockAllSkills();
            StartRun(Skill.HunterSensor);
            PlaceTay(new Vector3(-6, 0, -10), new Vector3(0, 0, -10));
            ParkHunter(new Vector3(5, 0, -10));
            yield return Tap(Key.E);
            Assert.That(Player.equipment.SkillActive, Is.True);
            Assert.That(Player.equipment.SensorPosition.HasValue, Is.True, "Hunter cách 11 m phải hiện trên Sensor.");
            Line($"[{Clock()}] Bấm E, Hunter cách {Flat(Hunter.transform.position - Feet):0.0} m (sau tường giữa). HUD: “{Hud("skill")}”.");
            ParkHunter(new Vector3(16, 0, 10));
            yield return Wait(Player.tuning.sensorInterval + 0.2f);
            Assert.That(Player.equipment.SensorPosition.HasValue, Is.False, "Hunter ngoài 15 m không được hiện.");
            Line($"[{Clock()}] Hunter ra xa {Flat(Hunter.transform.position - Feet):0} m: mất tín hiệu sau lần cập nhật kế tiếp.");
            yield return Wait(Player.tuning.sensorDuration, () => !Player.equipment.SkillActive);
            Assert.That(Player.equipment.SkillSeconds, Is.EqualTo(Player.tuning.sensorCooldown).Within(0.5f));
            Line($"[{Clock()}] Hết {Player.tuning.sensorDuration:0} s hiệu lực, hồi chiêu {Player.equipment.SkillSeconds:0} s. HUD: “{Hud("skill")}”.");
            yield return Tap(Key.E);
            Assert.That(Player.equipment.SkillActive, Is.False);
            Line($"[{Clock()}] Bấm E khi đang hồi: không kích hoạt. Thông báo: “{Game.Status}”.");
            StringAssert.Contains("đang hồi", Game.Status);
        }
    }
}
