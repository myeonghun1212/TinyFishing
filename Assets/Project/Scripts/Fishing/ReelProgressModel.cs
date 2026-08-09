using TinyFishing.Data;
using UnityEngine;

namespace TinyFishing.Fishing
{
    // Tracks catch progress (0..1) for the discrete-tap reeling mechanic:
    // tapping while aligned with the fish gains progress, tapping while
    // misaligned costs progress. Progress also passively decays over time
    // (rate depends on the hooked fish), and if it sits at 0% for too long
    // the fish breaks free - see Tick() and HasEscaped.
    public sealed class ReelProgressModel
    {
        private readonly TinyFishingConfig config;

        public float Progress { get; private set; }
        public bool IsCaught { get { return Progress >= 1f; } }
        public bool HasEscaped { get; private set; }
        public bool LastTapWasGood { get; private set; }

        // Seconds Progress has continuously sat at 0%. Reset the instant Progress rises above 0.
        public float TimeAtZero { get; private set; }

        // Baseline a fish's MaxHealth is balanced against - a fish with MaxHealth
        // equal to this needs the same number of good taps as before this stat existed.
        private const float BaselineHealth = 100f;
        private float resistance = 1f;
        private float healthDivisor = 1f;
        private float decayRate;

        public ReelProgressModel(TinyFishingConfig fishingConfig)
        {
            config = fishingConfig;
        }

        public void Reset(float fishResistance = 1f, float fishMaxHealth = BaselineHealth, float fishProgressDecayRate = -1f)
        {
            Progress = Mathf.Clamp01(config.startingProgress);
            HasEscaped = false;
            LastTapWasGood = false;
            TimeAtZero = 0f;
            resistance = Mathf.Max(0.01f, fishResistance);
            healthDivisor = Mathf.Max(0.01f, fishMaxHealth) / BaselineHealth;
            // A negative sentinel means "use the config fallback" - lets callers omit a
            // per-fish rate (e.g. when no FishDefinition is available for the round).
            decayRate = Mathf.Max(0f, fishProgressDecayRate >= 0f ? fishProgressDecayRate : config.fallbackProgressDecayRate);
        }

        public bool IsAligned(float playerDirection, float fishDirection)
        {
            return Mathf.Abs(playerDirection - fishDirection) <= config.alignDeadZone;
        }

        // Advance passive progress decay and the 0%-escape timer. Call once per frame
        // while Reeling. No-ops once the fish has been caught or has already escaped.
        public void Tick(float deltaTime)
        {
            if (IsCaught || HasEscaped)
            {
                return;
            }

            if (decayRate > 0f)
            {
                Progress = Mathf.Clamp01(Progress - decayRate * deltaTime);
            }

            if (Progress <= 0f)
            {
                TimeAtZero += deltaTime;
                if (TimeAtZero >= config.escapeGraceSeconds)
                {
                    HasEscaped = true;
                }
            }
            else
            {
                TimeAtZero = 0f;
            }
        }

        // Apply one discrete reel tap. Returns true if the tap was a "good" (aligned) tap.
        public bool ApplyTap(float playerDirection, float fishDirection, float verticalDirection = 0f)
        {
            var aligned = IsAligned(playerDirection, fishDirection);
            var progressBefore = Progress;

            // Reconstruct the actual upward tilt angle from the normalized VerticalDirection
            // (which is itself linear in pitch, see TinyFishingInputService), then re-normalize
            // against its own dedicated angle range so the tap bonus/penalty curve can be tuned
            // independently of maximumPitchAngle, which governs aim sensitivity instead.
            var tiltAngleDegrees = Mathf.Max(0f, verticalDirection) * config.maximumPitchAngle;
            var tiltAmount = Mathf.Clamp01(tiltAngleDegrees / Mathf.Max(0.01f, config.tiltBonusAngleRange));

            if (aligned)
            {
                // Tilting the rod/device upward boosts how much progress a good tap earns.
                var tiltBonus = 1f + tiltAmount * config.tiltUpTapBonusMultiplier;
                // Higher resistance and higher MaxHealth both mean more good taps are needed to land the fish.
                var gain = config.progressPerGoodTap * tiltBonus / (resistance * healthDivisor);
                Progress += gain;
                Debug.Log($"[ReelTap] GOOD tap: +{gain:F4} pts (base={config.progressPerGoodTap:F4}, " +
                    $"tiltAngle={tiltAngleDegrees:F1}deg, tiltBonus={tiltBonus:F2}x, resistance={resistance:F2}, " +
                    $"healthDivisor={healthDivisor:F2}) progress {progressBefore:F4} -> {Mathf.Clamp01(Progress):F4}");
            }
            else
            {
                // Tilting the rod/device upward also amplifies how much a bad tap costs,
                // via its own separate multiplier so the two can be tuned independently.
                var tiltPenalty = 1f + tiltAmount * config.tiltUpTapPenaltyMultiplier;
                // Higher resistance also punishes a bad tap harder, mirroring how tougher
                // fish resist being reeled in when you fall out of alignment.
                var loss = config.progressLossPerBadTap * resistance * tiltPenalty;
                Progress -= loss;
                Debug.Log($"[ReelTap] BAD tap: -{loss:F4} pts (base={config.progressLossPerBadTap:F4}, " +
                    $"tiltAngle={tiltAngleDegrees:F1}deg, tiltPenalty={tiltPenalty:F2}x, resistance={resistance:F2}) " +
                    $"progress {progressBefore:F4} -> {Mathf.Clamp01(Progress):F4}");
            }

            Progress = Mathf.Clamp01(Progress);
            LastTapWasGood = aligned;
            return aligned;
        }
    }
}
