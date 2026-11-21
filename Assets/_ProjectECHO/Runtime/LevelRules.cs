using System;
using UnityEngine;

namespace ProjectEcho
{
    public enum EscalationPolicy { VersusValidData, HighestReached }
    public enum Skill { None, BurstSprint, Dash, HunterSensor }
    public enum SupportItem { Emp, Stun, Armor }

    [CreateAssetMenu(menuName = "Project ECHO/Level Rules")]
    public sealed class LevelRules : ScriptableObject
    {
        public string levelId = "office-01";
        [Min(1)] public int requiredData = 6;
        [Min(1)] public float timeLimit = 720;
        [Min(0.1f)] public float relaySeconds = 20;
        [Min(0.1f)] public float relayRadius = 3;
        public EscalationPolicy escalation = EscalationPolicy.VersusValidData;
        [Range(0, 0.99f)] public float searchThreshold = 0.30f;
        [Range(0, 0.99f)] public float huntThreshold = 0.70f;
        public bool invalidateCompletedRelay = true;
        [Min(0)] public int captureLoss = 2;
        [Min(0)] public float captureSeconds = 1.2f;
        [Min(0)] public float spawnProtection = 1.5f;
        public Skill[] playtestSkills = { Skill.BurstSprint };
        public Skill completionUnlock = Skill.BurstSprint;

        public int GetThreat(int validData)
        {
            if (validData >= requiredData) return 4;
            float ratio = (float)validData / requiredData;
            if (ratio > huntThreshold) return 3;
            return ratio > searchThreshold ? 2 : 1;
        }

        private void OnValidate()
        {
            huntThreshold = Mathf.Max(huntThreshold, searchThreshold);
        }
    }
}
