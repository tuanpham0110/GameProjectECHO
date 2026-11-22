using UnityEngine;

namespace ProjectEcho
{
    public sealed class ExitGate : Interactable
    {
        [Min(0.1f)] public float activationRadius = 1.75f;
        public GameObject barrier;
        public GameObject openIndicator;
        public Transform passage;
        public bool IsOpen { get; private set; }
        public override string Prompt => IsOpen ? "Thoát khỏi khu vực" : "Cổng thoát khóa · cần truyền Relay xong";
        public bool HasEntered(Vector3 feet) => passage != null && (feet - passage.position).sqrMagnitude < 0.7f * 0.7f;
        public override void Interact(PlayerController player) => player.Level.game.TryExit();
        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (barrier != null) barrier.SetActive(!open);
            if (openIndicator != null) openIndicator.SetActive(open);
        }
    }
}
