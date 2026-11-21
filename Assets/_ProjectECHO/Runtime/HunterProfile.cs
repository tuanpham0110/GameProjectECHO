using System;
using UnityEngine;

namespace ProjectEcho
{
    [Serializable]
    public struct HunterTier
    {
        [Min(0.1f)] public float speedRatio;
        [Min(0.1f)] public float sight;
        [Range(1, 360)] public float angle;
        [Min(0)] public float hearing;
        [Min(0)] public float reaction;
        [Min(0.1f)] public float searchSeconds;
        public HunterTier(float speedRatio, float angle, float hearing, float reaction, float searchSeconds)
        {
            this.speedRatio = speedRatio;
            sight = 8;
            this.angle = angle;
            this.hearing = hearing;
            this.reaction = reaction;
            this.searchSeconds = searchSeconds;
        }
    }

    [CreateAssetMenu(menuName = "Project ECHO/Hunter Profile")]
    public sealed class HunterProfile : ScriptableObject
    {
        public HunterTier patrol = new(0.8f, 90, 0, 0.35f, 3);
        public HunterTier search = new(1, 110, 0, 0.25f, 5);
        public HunterTier hunt = new(1.1f, 130, 15, 0.16f, 7);
        public HunterTier lockdown = new(1.2f, 150, 15, 0.08f, 9);
        [Min(0.1f)] public float captureRange = 1.5f;
        [Min(0.1f)] public float eyeHeight = 1.55f;
        [Min(0.1f)] public float investigateSeconds = 4;
        [Min(0.1f)] public float repathSeconds = 0.22f;
        [Min(0.1f)] public float stuckSeconds = 1.6f;
        [Min(0)] public float recoverySeconds = 0.45f;
        [Min(0)] public float signalUncertainty = 1.25f;
        [Range(0, 1)] public float occludedHearing = 0.55f;
        [Tooltip("Khoảng cách Hunter nhận diện và xử lý cửa đóng trên tuyến đường.")] [Min(0.5f)] public float doorSearchRadius = 2.2f;
        [Tooltip("Gameplay Specification tr. 35: nấp tương đối quá 30 giây có nguy cơ bị kiểm tra (tr. 19 ghi 20 giây cho nằm/ngồi).")]
        [Min(1)] public float relativeHideCheckSeconds = 30;
        public LayerMask obstructionMask = ~0;

        public HunterTier ForLevel(int level) => level switch
        {
            4 => lockdown, 3 => hunt, 2 => search, _ => patrol
        };
    }
}
