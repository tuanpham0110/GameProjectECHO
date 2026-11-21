using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectEcho
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class GameplayHud : MonoBehaviour
    {
        public GameManager game;
        public EventSystem eventSystem;
        private Font font;
        private RectTransform hud;
        private RectTransform menu;
        private RectTransform relayFill;
        private Text time;
        private Text progress;
        private Text warning;
        private Text items;
        private Text skill;
        private Text prompt;
        private Text toast;
        private Text relayLabel;
        private Text heading;
        private Text subheading;
        private Text briefingSelection;
        private RectTransform radar;
        private RectTransform radarBlip;
        private Button firstButton;
        private Button startButton;
        private Button confirmButton;
        private readonly Button[] skillButtons = new Button[3];
        private GameState visibleState = (GameState)(-1);
        private int lastThreat = 1;
        private float warningSeconds;
        private readonly Color ink = new(0.89f, 0.94f, 0.97f);
        private readonly Color muted = new(0.58f, 0.71f, 0.76f);
        private readonly Color teal = new(0.08f, 0.89f, 0.82f);
        private readonly Color panel = new(0.034f, 0.066f, 0.085f, 0.97f);
        private static readonly Skill[] Available = { Skill.BurstSprint, Skill.Dash, Skill.HunterSensor };

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            hud = Rect("HUD", transform as RectTransform, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Stretch(hud);
            BuildHud();
            menu = Rect("Menu", transform as RectTransform, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            Stretch(menu);
        }

        private void Update()
        {
            if (visibleState != game.State.Current)
            {
                visibleState = game.State.Current;
                RebuildMenu();
            }
            if (visibleState == GameState.Briefing) UpdateBriefing();
            if (!game.IsRunning) return;

            var mission = game.Mission;
            int seconds = Mathf.CeilToInt(game.Timer.Remaining);
            time.text = $"{seconds / 60:00}:{seconds % 60:00}";
            time.color = seconds <= 30 ? new Color(1, 0.48f, 0.4f) : ink;
            progress.text = $"DỮ LIỆU   {mission.ValidData}/{mission.RequiredData}";
            if (lastThreat != mission.Threat)
            {
                lastThreat = mission.Threat;
                warningSeconds = 2.5f;
            }
            warningSeconds = Mathf.Max(0, warningSeconds - Time.deltaTime);
            warning.color = mission.Threat >= 3 ? new Color(1, 0.48f, 0.4f) : teal;
            warning.text = $"HUNTER {mission.Threat} · {TierLabel(mission.Threat)}" + (warningSeconds > 0 ? "  ▲" : "");
            var hunter = game.level.hunter;
            if (hunter.Current == HunterState.Chase)
            {
                warning.text = "⚠ BỊ PHÁT HIỆN  ·  " + warning.text;
                warning.color = new Color(1, 0.3f, 0.24f);
            }
            else if (hunter.Current is HunterState.Stunned or HunterState.Recovery)
                warning.text = "HUNTER BỊ CHOÁNG  ·  " + warning.text;
            var player = game.level.player;
            var equipment = player.equipment;
            string emp = equipment.SelectedItem == SupportItem.Emp ? "› EMP" : "EMP";
            string stun = equipment.SelectedItem == SupportItem.Stun ? "› Stun" : "Stun";
            items.text = $"1  {emp} ×{game.Inventory.Count(SupportItem.Emp)}      2  {stun} ×{game.Inventory.Count(SupportItem.Stun)}      G  Dùng";
            if (game.Inventory.Count(SupportItem.Armor) > 0) items.text += $"    Giáp ×{game.Inventory.Count(SupportItem.Armor)}";
            string activity = equipment.SkillSeconds > 0
                ? $"  ·  {(equipment.SkillActive ? "Hiệu lực" : "Hồi chiêu")} {Mathf.CeilToInt(equipment.SkillSeconds)}s" : "";
            skill.text = $"E  {SkillLabel(game.Equipped)}{activity}      T  Scanner {(equipment.ScannerOn ? "bật" : "tắt")}";
            radar.gameObject.SetActive(equipment.SkillActive && game.Equipped == Skill.HunterSensor);
            radarBlip.gameObject.SetActive(equipment.SensorPosition.HasValue);
            if (equipment.SensorPosition is Vector3 position)
            {
                Vector3 relative = position - player.transform.position;
                Vector3 local = Quaternion.Inverse(player.view.Heading) * relative;
                radarBlip.anchoredPosition = new Vector2(local.x, local.z) * (58 / player.tuning.sensorRange);
                skill.text += $"      Hunter {relative.magnitude:0.0} m · {Direction(local)}";
            }
            var target = player.interaction.Target;
            string interaction = target != null && target.isActiveAndEnabled ? $"F  {target.Prompt}" : "";
            if (player.interaction.NearbyHide != null)
                interaction += (interaction.Length > 0 ? "     " : "") + $"C  {player.interaction.NearbyHide.Prompt}";
            prompt.text = visibleState == GameState.Captured ? "BỊ BẮT" : player.HidePrompt ?? interaction;
            toast.text = game.Status ?? "";
            relayLabel.transform.parent.gameObject.SetActive(mission.Relay != RelayState.Idle);
            if (mission.Relay != RelayState.Idle)
            {
                Vector3 exitOffset = game.level.exit.transform.position - player.transform.position;
                string exitDirection = Direction(Quaternion.Inverse(player.view.Heading) * exitOffset);
                relayLabel.text = mission.CanExit ? $"CỔNG THOÁT  {exitOffset.magnitude:0} m · {exitDirection}"
                    : $"DATA RELAY  {Mathf.FloorToInt(mission.RelayProgress * 100)}%" + (game.level.IsInRelayRange ? "" : "   ·   TẠM DỪNG");
                relayFill.anchorMax = new Vector2(mission.RelayProgress, 1);
            }
        }

        private void BuildHud()
        {
            Vector2 topLeft = new(0, 1);
            var stats = Rect("Status", hud, topLeft, new Vector2(230, -102), new Vector2(390, 144));
            Image(stats, panel);
            time = Label(stats, topLeft, new Vector2(100, -42), new Vector2(150, 52), "", 42, ink);
            progress = Label(stats, topLeft, new Vector2(190, -93), new Vector2(330, 36), "", 22, ink);
            warning = Label(hud, Vector2.up + Vector2.right, new Vector2(-244, -67), new Vector2(450, 50), "", 26, teal, TextAnchor.MiddleRight);
            var tools = Rect("Equipment", hud, Vector2.zero, new Vector2(485, 71), new Vector2(900, 85));
            Image(tools, panel);
            items = Label(tools, topLeft, new Vector2(448, -23), new Vector2(865, 32), "", 21, ink);
            skill = Label(tools, topLeft, new Vector2(448, -58), new Vector2(865, 30), "", 19, muted);
            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0);
            RectTransform dot = Rect("Aim", hud, center, Vector2.zero, Vector2.one * 5);
            Image(dot, new Color(0.8f, 0.99f, 1, 0.88f));
            prompt = Label(hud, center, new Vector2(0, -85), new Vector2(720, 54), "", 28, ink, TextAnchor.MiddleCenter);
            toast = Label(hud, bottom, new Vector2(0, 184), new Vector2(980, 40), "", 26, muted, TextAnchor.MiddleCenter);
            var relay = Rect("Relay", hud, bottom, new Vector2(0, 260), new Vector2(520, 60));
            Image(relay, panel);
            relayLabel = Label(relay, center, new Vector2(0, 10), new Vector2(500, 30), "", 20, ink, TextAnchor.MiddleCenter);
            var track = Rect("Track", relay, center, new Vector2(0, -15), new Vector2(474, 7));
            Image(track, new Color(0.18f, 0.28f, 0.31f));
            relayFill = Rect("Fill", track, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(relayFill);
            Image(relayFill, teal);
            radar = Rect("Hunter Sensor", hud, Vector2.one, new Vector2(-106, -184), new Vector2(140, 140));
            Image(radar, panel);
            Image(Rect("East-west", radar, center, Vector2.zero, new Vector2(116, 1)), muted);
            Image(Rect("North-south", radar, center, Vector2.zero, new Vector2(1, 116)), muted);
            Image(Rect("TAY", radar, center, Vector2.zero, Vector2.one * 7), teal);
            radarBlip = Rect("Hunter", radar, center, Vector2.zero, Vector2.one * 9);
            Image(radarBlip, new Color(1, 0.28f, 0.19f));
        }

        private void RebuildMenu()
        {
            bool playing = game.IsRunning;
            hud.gameObject.SetActive(playing);
            menu.gameObject.SetActive(!playing);
            if (playing) return;

            foreach (Transform child in menu)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            var center = Vector2.one * 0.5f;
            var shade = Rect("Shade", menu, center, Vector2.zero, Vector2.zero);
            Stretch(shade);
            Image(shade, new Color(0.014f, 0.035f, 0.048f, 0.73f));
            float height = visibleState == GameState.Briefing ? 650 : 405;
            var card = Rect("Card", menu, center, Vector2.zero, new Vector2(780, height));
            Image(card, panel);
            Image(Rect("Accent", card, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(780, 4)), teal);
            var top = new Vector2(0.5f, 1);
            heading = Label(card, top, new Vector2(0, -62), new Vector2(680, 60), "", 42, ink);
            subheading = Label(card, top, new Vector2(0, -109), new Vector2(680, 42), "", 21, muted);
            firstButton = null;
            switch (visibleState)
            {
                case GameState.MainMenu:
                    heading.text = "PROJECT ECHO";
                    subheading.text = "Office · Level 01";
                    AddButton(card, -190, "Chuẩn bị nhiệm vụ", game.OpenBriefing);
                    AddButton(card, -254, "Thoát", game.Quit);
                    break;
                case GameState.Briefing:
                    heading.text = "Chuẩn bị nhiệm vụ";
                    subheading.text = "Thu thập dữ liệu, truyền tại Relay và tìm cổng thoát.";
                    for (int i = 0; i < Available.Length; i++)
                    {
                        Skill choice = Available[i];
                        skillButtons[i] = AddButton(card, -181 - i * 62, SkillLabel(choice), () => game.Select(choice));
                    }
                    briefingSelection = Label(card, top, new Vector2(0, -365), new Vector2(680, 52), "", 21, muted);
                    confirmButton = AddButton(card, -429, "Xác nhận kỹ năng", game.Confirm);
                    startButton = AddButton(card, -491, "Bắt đầu nhiệm vụ", game.StartRun);
                    AddButton(card, -553, "Quay lại", game.OpenMainMenu);
                    break;
                case GameState.Paused:
                    heading.text = "Tạm dừng";
                    subheading.text = "WASD di chuyển  ·  Shift chạy  ·  Ctrl cúi  ·  Space nhảy";
                    AddButton(card, -180, "Tiếp tục", game.TogglePause);
                    AddButton(card, -244, "Chuẩn bị lượt mới", game.OpenBriefing);
                    AddButton(card, -308, "Về menu chính", game.OpenMainMenu);
                    break;
                case GameState.Complete:
                case GameState.Failed:
                    bool won = visibleState == GameState.Complete;
                    heading.text = won ? "Nhiệm vụ hoàn tất" : "Nhiệm vụ thất bại";
                    subheading.text = won ? $"Đã thoát với {Mathf.CeilToInt(game.Timer.Remaining)} giây còn lại." : "Hết thời gian trước khi thoát.";
                    AddButton(card, -186, "Chuẩn bị lượt mới", game.OpenBriefing);
                    AddButton(card, -250, "Về menu chính", game.OpenMainMenu);
                    break;
            }
            if (firstButton != null) eventSystem.SetSelectedGameObject(firstButton.gameObject);
        }

        private void UpdateBriefing()
        {
            for (int i = 0; i < Available.Length; i++)
            {
                Skill value = Available[i];
                bool unlocked = game.IsUnlocked(value);
                skillButtons[i].interactable = unlocked;
                var text = skillButtons[i].GetComponentInChildren<Text>();
                text.text = SkillLabel(value) + "    ·    " + SkillDescription(value)
                    + (unlocked ? (game.Confirmed == value ? "   ✓" : game.Selected == value ? "   ›" : "") : "    [Khóa]");
            }
            briefingSelection.text = game.Confirmed == Skill.None ? "Chưa trang bị kỹ năng."
                : $"Đã trang bị {SkillLabel(game.Confirmed)}.";
            if (game.Selected != Skill.None && game.Selected != game.Confirmed)
                briefingSelection.text = $"Đang chọn {SkillLabel(game.Selected)}. Xác nhận để trang bị.";
            confirmButton.interactable = game.Selected != Skill.None && game.Selected != game.Confirmed;
            startButton.interactable = game.CanStart;
            if (eventSystem.currentSelectedGameObject == null ||
                (eventSystem.currentSelectedGameObject.TryGetComponent<Button>(out var selected) && !selected.interactable))
            {
                Button available = Array.Find(skillButtons, button => button.interactable);
                eventSystem.SetSelectedGameObject((game.CanStart ? startButton : available ?? confirmButton).gameObject);
            }
        }

        private Button AddButton(RectTransform card, float y, string text, Action action)
        {
            var rect = Rect(text, card, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(680, 50));
            var image = Image(rect, Color.white);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.077f, 0.17f, 0.20f),
                highlightedColor = new Color(0.13f, 0.32f, 0.35f),
                selectedColor = new Color(0.10f, 0.28f, 0.32f),
                pressedColor = new Color(0.14f, 0.36f, 0.37f),
                disabledColor = new Color(0.065f, 0.092f, 0.11f),
                colorMultiplier = 1,
                fadeDuration = 0.12f
            };
            Label(rect, Vector2.one * 0.5f, Vector2.zero, new Vector2(634, 46), text, 23, ink);
            button.onClick.AddListener(() => action());
            if (firstButton == null) firstButton = button;
            return button;
        }

        private Text Label(RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, string value,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = Rect("Label", parent, anchor, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image Image(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static string TierLabel(int tier) => tier switch { 4 => "LOCKDOWN", 3 => "HUNT", 2 => "SEARCH", _ => "PATROL" };
        private static string Direction(Vector3 relative)
        {
            float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
            return Mathf.Abs(angle) > 135 ? "sau" : angle > 30 ? "phải" : angle < -30 ? "trái" : "trước";
        }
        private static string SkillLabel(Skill value) => value switch
        {
            Skill.BurstSprint => "Burst Sprint", Skill.Dash => "Dash", Skill.HunterSensor => "Hunter Sensor", _ => "Không có kỹ năng"
        };

        private static string SkillDescription(Skill value) => value switch
        {
            Skill.BurstSprint => "Tăng tốc trong 4 giây",
            Skill.Dash => "Lướt qua khoảng trống hợp lệ",
            _ => "Quét Hunter trong phạm vi 15 m"
        };
    }
}
