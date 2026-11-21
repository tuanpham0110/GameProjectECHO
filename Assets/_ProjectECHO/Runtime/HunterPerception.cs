using UnityEngine;

namespace ProjectEcho
{
    public static class HunterPerception
    {
        public static bool HasSight(Vector3 origin, Vector3 forward, Vector3 target, float range, float angle, int mask)
        {
            Vector3 offset = target - origin;
            if (offset.sqrMagnitude > range * range) return false;
            Vector3 planar = Vector3.ProjectOnPlane(offset, Vector3.up);
            if (planar.sqrMagnitude > 0.0001f && Vector3.Angle(forward, planar) > angle * 0.5f) return false;
            return IsClear(origin, target, mask);
        }

        public static bool IsClear(Vector3 origin, Vector3 target, int mask) =>
            !Physics.Linecast(origin, target, mask, QueryTriggerInteraction.Ignore);
    }
}
