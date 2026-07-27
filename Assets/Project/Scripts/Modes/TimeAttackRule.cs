using NanFishing.Data;
using UnityEngine;

namespace NanFishing.Modes
{
    public sealed class TimeAttackRule : IGameModeRule
    {
        private readonly GameBalanceConfig config;

        public float Duration => config.sessionDuration;
        public float TimeRemaining { get; private set; }
        public bool IsFinished => TimeRemaining <= 0f;
        public int Score { get; private set; }
        public int CatchCount { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        public TimeAttackRule(GameBalanceConfig balance)
        {
            config = balance;
        }

        public void Begin()
        {
            TimeRemaining = Duration;
            Score = 0;
            CatchCount = 0;
            Combo = 0;
            MaxCombo = 0;
        }

        public void Tick(float deltaTime)
        {
            TimeRemaining = Mathf.Max(0f, TimeRemaining - Mathf.Max(0f, deltaTime));
        }

        public int RegisterCatch(int baseScore, int rarity, float catchDuration)
        {
            Combo++;
            CatchCount++;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            var rarityMultiplier = 1f + rarity * 0.5f;
            var quickBonus = catchDuration <= 4f ? config.quickCatchBonus : 0;
            var awarded = Mathf.RoundToInt(baseScore * rarityMultiplier)
                          + quickBonus
                          + Mathf.Max(0, Combo - 1) * config.comboStepBonus;
            Score += awarded;
            return awarded;
        }

        public void RegisterFailure()
        {
            Combo = 0;
        }
    }
}
