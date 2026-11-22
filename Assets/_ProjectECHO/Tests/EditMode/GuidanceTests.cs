using NUnit.Framework;
using UnityEngine;

namespace ProjectEcho.Tests
{
    /// <summary>Hướng dẫn ba chặng phải khớp trạng thái nhiệm vụ, kể cả khi bị bắt giữa lúc truyền.</summary>
    public sealed class GuidanceTests
    {
        private LevelRules rules;
        [SetUp] public void CreateRules() => rules = ScriptableObject.CreateInstance<LevelRules>();
        [TearDown] public void DestroyRules() => Object.DestroyImmediate(rules);

        [Test]
        public void StageFollowsCollectRelayTransmitExit()
        {
            var mission = new MissionManager(rules);
            Assert.That(MissionGuide.StageOf(mission), Is.EqualTo(MissionStage.Collect));
            StringAssert.Contains("Scanner", MissionGuide.Objective(mission, rules, false, null, null));
            StringAssert.Contains("~12 m", MissionGuide.Objective(mission, rules, false, null, "~12 m · bên trái"));
            for (int i = 0; i < rules.requiredData; i++) mission.Collect(i.ToString());
            Assert.That(MissionGuide.StageOf(mission), Is.EqualTo(MissionStage.Relay));
            string relay = MissionGuide.Objective(mission, rules, false, "38 m · bên phải", null);
            StringAssert.Contains(rules.relayLocation, relay);
            StringAssert.Contains("38 m", relay);
            Assert.That(mission.BeginRelay(), Is.True);
            Assert.That(MissionGuide.StageOf(mission), Is.EqualTo(MissionStage.Transmit));
            StringAssert.Contains("tiến độ được giữ", MissionGuide.Objective(mission, rules, true, null, null));
            StringAssert.Contains("Quay lại", MissionGuide.Objective(mission, rules, false, "4 m · phía sau", null));
            mission.TickRelay(rules.relaySeconds, true);
            Assert.That(MissionGuide.StageOf(mission), Is.EqualTo(MissionStage.Exit));
            StringAssert.Contains(rules.exitLocation, MissionGuide.Objective(mission, rules, false, "44 m · phía trước", null));
        }

        [Test]
        public void CaptureDuringTransmissionReturnsToCollect()
        {
            var mission = new MissionManager(rules);
            for (int i = 0; i < rules.requiredData; i++) mission.Collect(i.ToString());
            mission.BeginRelay();
            mission.ApplyCapture(false, count => 0);
            Assert.That(MissionGuide.StageOf(mission), Is.EqualTo(MissionStage.Collect));
        }

        [Test]
        public void BriefingUsesLevelRules()
        {
            rules.requiredData = 6;
            rules.timeLimit = 720;
            rules.relaySeconds = 20;
            rules.captureLoss = 2;
            string text = MissionGuide.Briefing(rules);
            StringAssert.Contains("6 gói", text);
            StringAssert.Contains("12:00", text);
            StringAssert.Contains("20 giây", text);
            StringAssert.Contains("mất 2 gói", text);
            Assert.That(MissionGuide.Direction(new Vector3(0, 0, 1)), Is.EqualTo("phía trước"));
            Assert.That(MissionGuide.Direction(new Vector3(1, 0, 0)), Is.EqualTo("bên phải"));
            Assert.That(MissionGuide.Direction(new Vector3(0, 0, -1)), Is.EqualTo("phía sau"));
        }
    }
}
