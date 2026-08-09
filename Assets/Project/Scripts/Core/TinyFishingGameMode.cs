namespace TinyFishing.Core
{
    public enum TinyFishingGameMode
    {
        Infinite,
        TimeLimited
    }

    public static class TinyFishingScoreKeys
    {
        // Keep the original key so existing Pond Casual records are preserved.
        public const string Infinite = "TinyFishing.BestScore";
        public const string TimeAttack60Seconds = "TinyFishing.BestScore.TimeAttack60";

        public static string For(TinyFishingGameMode mode)
        {
            return mode == TinyFishingGameMode.TimeLimited
                ? TimeAttack60Seconds
                : Infinite;
        }
    }
}
