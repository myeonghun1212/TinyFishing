using UnityEngine;

namespace NanFishing.Data
{
    [CreateAssetMenu(menuName = "NAN Fishing/Game Balance", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Session")]
        [Min(10f)] public float sessionDuration = 60f;
        public Vector2 biteDelayRange = new(0.3f, 0.8f);
        [Min(0.1f)] public float catchResultDuration = 1f;

        [Header("Motion")]
        [Min(0.1f)] public float stableDuration = 0.5f;
        [Min(0.01f)] public float stableAccelerationTolerance = 0.18f;
        [Min(1f)] public float backswingAngle = 12f;
        [Min(0.1f)] public float forwardSwingAcceleration = 1.2f;
        [Min(0f)] public float castCooldown = 0.75f;
        [Range(0.01f, 1f)] public float sensorSmoothing = 0.16f;
        [Min(1f)] public float maximumTiltAngle = 32f;

        [Header("Reeling")]
        [Range(0f, 1f)] public float startingTension = 0.25f;
        [Min(0.01f)] public float reelProgressPerSecond = 0.23f;
        [Min(0f)] public float escapeProgressPerSecond = 0.055f;
        [Min(0f)] public float tensionGainPerSecond = 0.38f;
        [Min(0f)] public float tensionRecoveryPerSecond = 0.32f;
        [Range(0f, 1f)] public float directionDeadZone = 0.2f;
        [Range(0f, 1f)] public float dangerTension = 0.82f;
        [Min(0.1f)] public float breakGraceDuration = 0.7f;

        [Header("Score")]
        [Min(0)] public int quickCatchBonus = 75;
        [Min(0)] public int comboStepBonus = 25;

        public static GameBalanceConfig CreateRuntime()
        {
            return CreateInstance<GameBalanceConfig>();
        }
    }
}
