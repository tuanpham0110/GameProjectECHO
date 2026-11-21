namespace ProjectEcho
{
    public sealed class DataRelay : Interactable
    {
        public override string Prompt => "Data Relay";
        public override void Interact(PlayerController player) => player.Level.game.BeginRelay();
    }
}
