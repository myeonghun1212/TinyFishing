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

        [Header("Catch Zone")]
        [Min(0.01f)] public float catchProgressPerSecond = 0.34f;
        [Range(0.05f, 0.8f)] public float catchZoneHalfWidthMin = 0.16f;
        [Range(0.05f, 0.8f)] public float catchZoneHalfWidthMax = 0.3f;
        [Range(0.05f, 0.5f)] public float catchZoneMilestone = 0.2f;

        [Header("Rod Raise Gesture")]
        [Range(45f, 90f)] public float rodRaiseAngle = 70f;

        [Header("Score")]
        [Min(0)] public int quickCatchBonus = 75;
        [Min(0)] public int comboStepBonus = 25;

        public static GameBalanceConfig CreateRuntime()
        {
            return CreateInstance<GameBalanceConfig>();
        }
    }
}
