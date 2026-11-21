using UnityEngine;

namespace ProjectEcho
{
    public sealed class GameManager : MonoBehaviour
    {
        public LevelManager level;
        public GameStateMachine State { get; } = new();
        public MissionManager Mission { get; private set; }
        public InventorySystem Inventory { get; private set; }
        public TimerSystem Timer { get; } = new();
        public Skill Selected { get; private set; }
        public Skill Confirmed { get; private set; }
        public Skill Equipped { get; private set; }
        public bool IsRunning => State.Current is GameState.Playing or GameState.Captured;
        public bool CanAct => State.Current == GameState.Playing;
        public bool CanStart => State.Current == GameState.Briefing &&
            (Confirmed != Skill.None ? Confirmed == Selected && IsUnlocked(Confirmed) : !HasUnlockedSkill());
        public bool CanBeCaptured => CanAct && spawnProtection <= 0 && !level.player.IsSafelyHidden;
        public string Status { get; private set; }
        private SaveSystem save;
        private SaveProfile profile;
        private float captureRemaining;
        private float spawnProtection;
        private float statusRemaining;

        private void Awake()
        {
            LoadProfile(new SaveSystem(Application.persistentDataPath));
            Mission = new MissionManager(level.rules);
            Inventory = new InventorySystem();
            SyncCursor();
        }

        internal void LoadProfile(SaveSystem persistence)
        {
            save = persistence;
            profile = save.Load();
        }

        private void Update()
        {
            if (level.player.input.Pause.WasPressedThisFrame()) TogglePause();
            if (!IsRunning) return;
            Step(Time.deltaTime);
        }

        public void Step(float delta)
        {
            if (!IsRunning || delta <= 0) return;
            Timer.Tick(delta);
            if (Timer.Expired)
            {
                State.MoveTo(GameState.Failed);
                SyncCursor();
                return;
            }
            statusRemaining -= delta;
            if (statusRemaining <= 0) Status = null;
            spawnProtection = Mathf.Max(0, spawnProtection - delta);
            if (State.Current == GameState.Captured)
            {
                captureRemaining -= delta;
                if (captureRemaining <= 0)
                {
                    level.RespawnPlayer();
                    State.MoveTo(GameState.Playing);
                    spawnProtection = level.rules.spawnProtection;
                }
                return;
            }
            if (Mission.TickRelay(delta, level.IsInRelayRange))
            {
                level.SyncWorld();
                Notify("Truyền hoàn tất. Đến cổng thoát.");
            }
            if (Mission.CanExit && level.exit.HasEntered(level.player.transform.position)) TryExit();
        }

        public void OpenBriefing()
        {
            if (!State.MoveTo(GameState.Briefing)) return;
            Confirmed = Skill.None;
            Selected = IsUnlocked(profile.lastSkill) ? profile.lastSkill : Skill.None;
            SyncCursor();
        }

        public void OpenMainMenu()
        {
            if (State.MoveTo(GameState.MainMenu)) SyncCursor();
        }

        public bool IsUnlocked(Skill skill) => skill != Skill.None &&
            (profile.unlocked.Contains(skill) || System.Array.IndexOf(level.rules.playtestSkills, skill) >= 0);

        private bool HasUnlockedSkill() =>
            IsUnlocked(Skill.BurstSprint) || IsUnlocked(Skill.Dash) || IsUnlocked(Skill.HunterSensor);

        public void Select(Skill skill)
        {
            if (State.Current == GameState.Briefing && IsUnlocked(skill)) Selected = skill;
        }

        public void Confirm()
        {
            if (State.Current == GameState.Briefing && IsUnlocked(Selected)) Confirmed = Selected;
        }

        public void StartRun()
        {
            if (!CanStart || !State.MoveTo(GameState.Playing)) return;
            Equipped = Confirmed;
            profile.lastSkill = Equipped;
            Mission = new MissionManager(level.rules);
            Inventory = new InventorySystem();
            Timer.Start(level.rules.timeLimit);
            Status = null;
            spawnProtection = level.rules.spawnProtection;
            level.ResetRun();
            SyncCursor();
        }

        public void TogglePause()
        {
            if (State.Current == GameState.Paused) State.Resume();
            else if (IsRunning) State.MoveTo(GameState.Paused);
            SyncCursor();
        }

        public bool Collect(string id)
        {
            if (!CanAct || !Mission.Collect(id)) return false;
            level.SyncWorld();
            return true;
        }

        public void BeginRelay()
        {
            if (!CanAct || !level.IsInRelayRange) return;
            if (Mission.BeginRelay())
            {
                level.EmitNoise(level.relay.transform.position, 15);
                Notify("Đang truyền. Ở trong vùng Data Relay để tiếp tục.");
            }
            else if (Mission.Relay == RelayState.Idle)
                Notify($"Cần thêm {Mission.RequiredData - Mission.ValidData} gói dữ liệu.");
        }

        public bool TryCapture()
        {
            if (!CanBeCaptured || !State.MoveTo(GameState.Captured)) return false;
            bool armored = Inventory.Use(SupportItem.Armor);
            int lost = Mission.ApplyCapture(armored, count => Random.Range(0, count)).Count;
            captureRemaining = level.rules.captureSeconds;
            level.player.OnCaptured();
            level.SyncWorld();
            Notify(armored ? "Giáp đã bảo vệ dữ liệu." : $"Bị bắt. Mất {lost} gói dữ liệu.", captureRemaining + 3);
            return true;
        }

        public bool TryExit()
        {
            if (!CanAct || Timer.Expired || !Mission.CanExit || !level.IsAtExit)
            {
                if (CanAct && !Mission.CanExit) Notify("Cổng khóa. Hoàn tất truyền dữ liệu trước.");
                return false;
            }
            State.MoveTo(GameState.Complete);
            var record = profile.completed.Find(entry => entry != null && entry.id == level.rules.levelId);
            if (record == null)
            {
                record = new LevelRecord { id = level.rules.levelId };
                profile.completed.Add(record);
            }
            record.bestRemaining = Mathf.Max(record.bestRemaining, Timer.Remaining);
            var unlock = level.rules.completionUnlock;
            if (unlock != Skill.None && !profile.unlocked.Contains(unlock)) profile.unlocked.Add(unlock);
            save.Save(profile);
            SyncCursor();
            return true;
        }

        public void Notify(string message, float seconds = 3)
        {
            Status = message;
            statusRemaining = seconds;
        }

        private void SyncCursor()
        {
            Cursor.lockState = IsRunning ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !IsRunning;
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
