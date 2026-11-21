using UnityEngine;

namespace ProjectEcho
{
    public sealed class TimerSystem
    {
        public float Remaining { get; private set; }
        public bool Expired => Remaining <= 0;

        public void Start(float seconds) => Remaining = Mathf.Max(0, seconds);
        public void Tick(float seconds) => Remaining = Mathf.Max(0, Remaining - Mathf.Max(0, seconds));
    }
}
