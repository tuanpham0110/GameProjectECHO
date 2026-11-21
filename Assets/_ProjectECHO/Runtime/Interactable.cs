using UnityEngine;

namespace ProjectEcho
{
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual Vector3 Point => transform.position;
        public virtual void ResetRun() { }
        public abstract void Interact(PlayerController player);
    }
}
