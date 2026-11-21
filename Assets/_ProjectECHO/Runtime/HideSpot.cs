using UnityEngine;

namespace ProjectEcho
{
    public sealed class HideSpot : Interactable
    {
        public Transform inside;
        public Transform outside;
        public bool absolute;
        [Min(0)] public float entryDelay = 1;
        [Min(0)] public float exitDelay = 0.8f;
        public override string Prompt => absolute ? "Chỗ nấp kín" : "Chỗ nấp sau vật cản";
        public override Vector3 Point => outside != null ? outside.position + Vector3.up * 0.5f : base.Point;
        public override void Interact(PlayerController player) => player.RequestHide(this);
    }
}
