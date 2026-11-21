using UnityEngine;

namespace ProjectEcho
{
    public sealed class InteractionSystem : MonoBehaviour
    {
        public PlayerController player;
        public Interactable Target { get; private set; }
        public HideSpot NearbyHide { get; private set; }

        public void Refresh()
        {
            Target = FindTarget(false);
            NearbyHide = FindTarget(true) as HideSpot;
            if (player.input.Interact.WasPressedThisFrame()) Target?.Interact(player);
            if (player.input.Hide.WasPressedThisFrame()) NearbyHide?.Interact(player);
        }

        public void Clear()
        {
            Target = null;
            NearbyHide = null;
        }

        private Interactable FindTarget(bool hideOnly)
        {
            Interactable best = null;
            float bestScore = float.PositiveInfinity;
            Vector3 eye = player.Eye;
            Vector3 facing = player.view.Heading * Vector3.forward;
            foreach (var target in player.Level.interactions)
            {
                if (target == null || !target.isActiveAndEnabled || (target is HideSpot) != hideOnly) continue;
                Vector3 offset = target.Point - eye;
                float distance = offset.magnitude;
                if (distance > player.tuning.interactionRange || distance < 0.01f) continue;
                if (Physics.Raycast(eye, offset / distance, out var hit, distance,
                        player.Level.worldMask, QueryTriggerInteraction.Ignore) &&
                    !hit.transform.IsChildOf(target.transform)) continue;
                float score = distance + (1 - Vector3.Dot(facing, offset.normalized)) * 0.45f;
                if (score >= bestScore) continue;
                bestScore = score;
                best = target;
            }
            return best;
        }
    }
}
