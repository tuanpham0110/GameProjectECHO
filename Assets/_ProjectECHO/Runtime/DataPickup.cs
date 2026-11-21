using UnityEngine;

namespace ProjectEcho
{
    public sealed class DataPickup : Interactable
    {
        public string dataId;
        public string displayName = "Gói dữ liệu";
        private Renderer[] renderers;
        private MaterialPropertyBlock highlight;
        private bool scanned;
        public override string Prompt => displayName;
        public override void Interact(PlayerController player) => player.Level.game.Collect(dataId);
        public void SetCollected(bool value)
        {
            if (value) SetScanned(false);
            gameObject.SetActive(!value);
        }

        public void SetScanned(bool value)
        {
            if (value == scanned) return;
            scanned = value;
            renderers ??= GetComponentsInChildren<Renderer>(true);
            highlight ??= new MaterialPropertyBlock();
            highlight.SetColor("_EmissionColor", new Color(0.02f, 1.8f, 1.56f));
            foreach (var renderer in renderers) renderer.SetPropertyBlock(value ? highlight : null);
        }
    }
}
