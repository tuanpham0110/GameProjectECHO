using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ProjectEcho.Tests
{
    public sealed class MissionTests
    {
        private LevelRules rules;
        [SetUp] public void CreateRules() => rules = ScriptableObject.CreateInstance<LevelRules>();
        [TearDown] public void DestroyRules() => Object.DestroyImmediate(rules);

        [TestCase(0, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(7, 2)]
        [TestCase(8, 3)]
        [TestCase(10, 4)]
        public void ThreatUsesExactBoundaries(int collected, int expected)
        {
            rules.requiredData = 10;
            var mission = Collect(collected);
            Assert.That(mission.Threat, Is.EqualTo(expected));
        }

        [TestCase(EscalationPolicy.VersusValidData, 2)]
        [TestCase(EscalationPolicy.HighestReached, 4)]
        public void CaptureRemovesTwoUniqueRecoverableIds(EscalationPolicy policy, int expectedThreat)
        {
            rules.escalation = policy;
            var mission = Collect(6);
            Assert.That(mission.BeginRelay(), Is.True);
            var lost = mission.ApplyCapture(false, count => count - 1);
            CollectionAssert.AreEquivalent(new[] { "4", "5" }, lost);
            Assert.That(mission.Relay, Is.EqualTo(RelayState.Idle));
            Assert.That(mission.Threat, Is.EqualTo(expectedThreat));
            Assert.That(mission.Collect("4"), Is.True);
            Assert.That(mission.Collect("4"), Is.False);
            Assert.That(mission.Collect("5"), Is.True);
            Assert.That(mission.BeginRelay(), Is.True);
        }

        [Test]
        public void RelayRequiresAllDataAndRetainsProgressOutsideRange()
        {
            var mission = Collect(5);
            Assert.That(mission.BeginRelay(), Is.False);
            Assert.That(mission.CanExit, Is.False);
            mission.Collect("5");
            Assert.That(mission.BeginRelay(), Is.True);
            Assert.That(mission.BeginRelay(), Is.False);
            mission.TickRelay(rules.relaySeconds / 2, true);
            mission.TickRelay(rules.relaySeconds, false);
            Assert.That(mission.RelayProgress, Is.EqualTo(0.5f));
            Assert.That(mission.CanExit, Is.False);
            Assert.That(mission.TickRelay(rules.relaySeconds / 2, true), Is.True);
            Assert.That(mission.CanExit, Is.True);
        }

        [TestCase(true, false, false)]
        [TestCase(false, false, true)]
        [TestCase(true, true, true)]
        public void CompletionCaptureRespectsRuleAndArmor(bool invalidate, bool armor, bool expectedOpen)
        {
            rules.invalidateCompletedRelay = invalidate;
            var mission = Collect(6);
            mission.BeginRelay();
            mission.TickRelay(rules.relaySeconds, true);
            var lost = mission.ApplyCapture(armor, _ => 0);
            Assert.That(mission.CanExit, Is.EqualTo(expectedOpen));
            Assert.That(lost.Count, Is.EqualTo(expectedOpen ? 0 : 2));
        }

        [Test]
        public void ArmorProtectsDataButInterruptsActiveRelay()
        {
            var mission = Collect(6);
            mission.BeginRelay();
            mission.TickRelay(2, true);
            Assert.That(mission.ApplyCapture(true, _ => 0), Is.Empty);
            Assert.That(mission.ValidData, Is.EqualTo(6));
            Assert.That(mission.Relay, Is.EqualTo(RelayState.Idle));
            Assert.That(mission.RelayProgress, Is.Zero);
        }

        [Test]
        public void TimerClampsToZeroAndStateRejectsInvalidExit()
        {
            var timer = new TimerSystem();
            timer.Start(2);
            timer.Tick(3);
            Assert.That(timer.Remaining, Is.Zero);
            var state = new GameStateMachine();
            Assert.That(state.MoveTo(GameState.Complete), Is.False);
            Assert.That(state.MoveTo(GameState.Briefing), Is.True);
            Assert.That(state.MoveTo(GameState.Playing), Is.True);
            Assert.That(state.MoveTo(GameState.Captured), Is.True);
            Assert.That(state.MoveTo(GameState.Paused), Is.True);
            Assert.That(state.Resume(), Is.True);
            Assert.That(state.Current, Is.EqualTo(GameState.Captured));
        }

        [Test]
        public void SavePersistsUnlocksAndRecoversBackup()
        {
            string directory = Path.Combine(Path.GetTempPath(), "EchoSaveTest-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var persistence = new SaveSystem(directory);
                var profile = new SaveProfile { lastSkill = Skill.BurstSprint };
                profile.unlocked.Add(Skill.BurstSprint);
                Assert.That(persistence.Save(profile), Is.True);
                profile.unlocked.Add(Skill.Dash);
                Assert.That(persistence.Save(profile), Is.True);
                Assert.That(persistence.Load().unlocked, Does.Contain(Skill.Dash));
                File.WriteAllText(Path.Combine(directory, "echo-profile.json"), "CORRUPT");
                Assert.That(persistence.Load().unlocked, Is.EqualTo(new[] { Skill.BurstSprint }));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private MissionManager Collect(int count)
        {
            var mission = new MissionManager(rules);
            for (int i = 0; i < count; i++) Assert.That(mission.Collect(i.ToString()), Is.True);
            return mission;
        }
    }
}
