namespace ProjectEcho
{
    public sealed class SupportPickup : Interactable
    {
        public SupportItem item;
        public override string Prompt => item switch { SupportItem.Emp => "EMP Mine", SupportItem.Stun => "Stun Grenade", _ => "Giáp" };
        public override void Interact(PlayerController player)
        {
            player.Level.game.Inventory.Add(item);
            player.Level.game.Notify($"Đã nhặt {Prompt}.");
            gameObject.SetActive(false);
        }
        public override void ResetRun() => gameObject.SetActive(true);
    }
}
