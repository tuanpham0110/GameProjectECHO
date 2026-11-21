namespace ProjectEcho
{
    public enum GameState { MainMenu, Briefing, Playing, Captured, Paused, Complete, Failed }

    public sealed class GameStateMachine
    {
        private GameState beforePause;
        public GameState Current { get; private set; } = GameState.MainMenu;

        public bool MoveTo(GameState next)
        {
            bool allowed = Current switch
            {
                GameState.MainMenu => next == GameState.Briefing,
                GameState.Briefing => next is GameState.MainMenu or GameState.Playing,
                GameState.Playing => next is GameState.Captured or GameState.Paused or GameState.Complete or GameState.Failed,
                GameState.Captured => next is GameState.Playing or GameState.Paused or GameState.Failed,
                GameState.Paused => next == beforePause || next is GameState.Briefing or GameState.MainMenu,
                GameState.Complete or GameState.Failed => next is GameState.Briefing or GameState.MainMenu,
                _ => false
            };
            if (!allowed) return false;
            if (next == GameState.Paused) beforePause = Current;
            Current = next;
            return true;
        }

        public bool Resume() => Current == GameState.Paused && MoveTo(beforePause);
    }
}
