namespace NanFishing.Modes
{
    public interface IGameModeRule
    {
        float Duration { get; }
        float TimeRemaining { get; }
        bool IsFinished { get; }
        int Score { get; }
        int CatchCount { get; }
        int Combo { get; }
        int MaxCombo { get; }
        void Begin();
        void Tick(float deltaTime);
        int RegisterCatch(int baseScore, int rarity, float catchDuration);
        void RegisterFailure();
    }
}
