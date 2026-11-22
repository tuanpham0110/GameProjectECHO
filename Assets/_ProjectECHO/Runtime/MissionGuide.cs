using UnityEngine;

namespace ProjectEcho
{
    public enum MissionStage { Collect, Relay, Transmit, Exit }

    /// <summary>
    /// Câu chữ hướng dẫn ba chặng nhiệm vụ (thu thập, truyền Relay, tìm cổng). Tách khỏi HUD để briefing, menu tạm dừng,
    /// thông báo và EditMode test dùng cùng một nguồn.
    /// </summary>
    public static class MissionGuide
    {
        public static MissionStage StageOf(MissionManager mission)
        {
            if (mission.CanExit) return MissionStage.Exit;
            if (mission.Relay == RelayState.Transmitting) return MissionStage.Transmit;
            return mission.ValidData >= mission.RequiredData ? MissionStage.Relay : MissionStage.Collect;
        }

        /// <summary>Mục tiêu hiện tại. <paramref name="route"/> là "N m · hướng" tới mục tiêu cố định (Relay, cổng), có thể null.</summary>
        public static string Objective(MissionManager mission, LevelRules rules, bool inRelay, string route, string signal)
        {
            string tail = string.IsNullOrEmpty(route) ? "" : "  ·  " + route;
            return StageOf(mission) switch
            {
                MissionStage.Collect => signal != null
                    ? $"1/3  Tìm dữ liệu {mission.ValidData}/{mission.RequiredData}  ·  tín hiệu gần nhất {signal}"
                    : $"1/3  Tìm dữ liệu {mission.ValidData}/{mission.RequiredData}  ·  T bật Scanner để dò tín hiệu",
                MissionStage.Relay => $"2/3  Đến Data Relay ({rules.relayLocation}){tail}",
                MissionStage.Transmit => inRelay
                    ? "2/3  Giữ trong vùng Relay. Hunter tới thì rời vùng, tiến độ được giữ"
                    : $"2/3  Quay lại vùng Relay để truyền tiếp{tail}",
                _ => $"3/3  Về cổng thoát ({rules.exitLocation}){tail}"
            };
        }

        public static string Briefing(LevelRules rules) =>
            $"1. Tìm {rules.requiredData} gói dữ liệu, F để nhặt. T bật Scanner dò tín hiệu; bật lâu Hunter sẽ bắt sóng.\n" +
            $"2. Đủ dữ liệu, Hunter chuyển LOCKDOWN. Tới Data Relay ({rules.relayLocation}), F rồi ở trong vùng {rules.relaySeconds:0} giây. Rời vùng vẫn giữ tiến độ.\n" +
            $"3. Về cổng thoát ({rules.exitLocation}) trước khi hết {Clock(rules.timeLimit)}. Bị bắt mất {rules.captureLoss} gói và về điểm xuất phát.";

        public const string Controls = "WASD, chuột  ·  Shift chạy  ·  Ctrl cúi  ·  Space nhảy  ·  F tương tác  ·  C nấp  ·  E kỹ năng  ·  T Scanner  ·  1/2 + G vật phẩm  ·  Esc";

        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        /// <summary>Hướng tương đối so với camera, theo cách người chơi đọc nhanh.</summary>
        public static string Direction(Vector3 local)
        {
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            return Mathf.Abs(angle) > 135 ? "phía sau" : angle > 30 ? "bên phải" : angle < -30 ? "bên trái" : "phía trước";
        }
    }
}
