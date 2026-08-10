using UnityEngine;
using UnityEngine.Serialization;

namespace TinyFishing.Data
{
    [CreateAssetMenu(menuName = "TinyFishing/Config", fileName = "TinyFishingConfig")]
    public sealed class TinyFishingConfig : ScriptableObject
    {
        [Header("Casting")]
        [Tooltip("Minimum acceleration spike (m/s^2 change) required to register a cast shake.")]
        public float shakeThreshold = 2.2f;
        [Tooltip("Acceleration-change strength that maps to a full-power gyro cast. Values between Shake Threshold and this value map linearly to 0..1 cast strength.")]
        public float gyroFullStrength = 15f;
        [Tooltip("Seconds to wait before another shake can trigger a cast.")]
        public float castCooldown = 0.6f;
        [Tooltip("Vertical drag distance (pixels), on top of the shake gesture, that also triggers a cast.")]
        public float dragCastThreshold = 260f;
        [Tooltip("Seconds or less to reach the drag-cast threshold for a full-power touch cast.")]
        [Min(0f)] public float touchFastCastDuration = 0.15f;
        [Tooltip("Seconds or more to reach the drag-cast threshold for a minimum-power touch cast.")]
        [Min(0f)] public float touchSlowCastDuration = 1f;

        [Header("Bite Discovery")]
        [Tooltip("Min/max seconds after casting before a fish discovers the bait and breaks off from wandering to start swimming in toward the bob. A random value in this range is rolled on every cast - lower it to make bites come faster, raise it for a longer wait.")]
        public Vector2 biteDelayRange = new Vector2(5f, 10f);

        [Header("Bite & Approach")]
        [Tooltip("Speed (units/sec) the selected fish swims from wherever it's wandering to a point beneath the bob.")]
        public float fishBiteApproachSpeed = 2.2f;
        [Tooltip("Seconds the player has to reel, starting the instant the bob dips, before the fish loses interest and escapes back to the pool.")]
        [Min(0.05f)] public float biteHookWindowSeconds = 1f;
        [Tooltip("How far (world units) the bob dips down when the fish takes the bait.")]
        public float bobDipDepth = 0.18f;
        [Tooltip("Seconds for the bob to animate down into the dip once the fish arrives.")]
        public float bobDipDownTime = 0.12f;
        [Tooltip("Seconds for the bob to spring back up to its resting float height if the hook window is missed.")]
        public float bobDipReturnTime = 0.18f;

        [Header("Tilt / Direction")]
        [Tooltip("Phone roll angle (degrees) that maps to full -1..1 horizontal direction range.")]
        public float maximumTiltAngle = 28f;
        [Tooltip("Phone pitch angle (degrees) that maps to full -1..1 vertical direction range.")]
        public float maximumPitchAngle = 24f;
        [Tooltip("Mouse/touch drag distance (pixels) that maps to the full -1..1 direction range on either axis.")]
        public float dragRange = 240f;
        [Tooltip("Pointer movement (pixels) beyond which a press is treated as a drag instead of a reel tap.")]
        public float dragStartThreshold = 14f;
        [Tooltip("Max seconds a press can last and still count as a reel tap rather than a drag.")]
        public float tapMaxDuration = 0.35f;
        [Tooltip("Smoothing applied to raw accelerometer samples.")]
        [Range(0f, 1f)] public float sensorSmoothing = 0.25f;
        [Tooltip("How close player direction must be to fish direction to count as aligned.")]
        [Range(0f, 1f)] public float alignDeadZone = 0.16f;

        [Header("Fish Behaviour")]
        [Tooltip("Fallback fish direction-change interval range (seconds). Only used when no FishDefinition is available for the round (e.g. the fish pool has nothing eligible and the catalog is empty) - normally each fish's own DirectionChangeInterval is used instead.")]
        [FormerlySerializedAs("directionChangeIntervalRange")]
        public Vector2 fallbackDirectionChangeIntervalRange = new Vector2(0.6f, 1.6f);
        [Tooltip("Fallback fish drift speed. Only used when no FishDefinition is available for the round - normally each fish's own MoveSpeed is used instead.")]
        [FormerlySerializedAs("fishMoveSpeed")]
        public float fallbackFishMoveSpeed = 1.4f;

        [Header("Reeling (discrete tap)")]
        [Tooltip("Progress (0..1) gained per successful tap while aligned.")]
        [Range(0f, 1f)] public float progressPerGoodTap = 0.14f;
        [Tooltip("Progress (0..1) lost per tap while misaligned.")]
        [Range(0f, 1f)] public float progressLossPerBadTap = 0.10f;
        [Tooltip("Extra progress multiplier applied to a good tap's gain when tilting the rod/device upward. 0 = no bonus, 1 = up to double gain at full upward tilt.")]
        [Range(0f, 3f)] public float tiltUpTapBonusMultiplier = 1f;
        [Tooltip("Extra progress multiplier applied to a bad tap's loss when tilting the rod/device upward. 0 = no extra penalty, 1 = up to double loss at full upward tilt.")]
        [Range(0f, 3f)] public float tiltUpTapPenaltyMultiplier = 1f;
        [Tooltip("Degrees of upward tilt over which the tap tilt bonus/penalty scales linearly from 0 to full strength. Independent of maximumPitchAngle (which governs aim sensitivity) - e.g. 10 means full tap bonus/penalty is reached at just 10 degrees of upward tilt even if maximumPitchAngle is higher.")]
        [Min(0.01f)] public float tiltBonusAngleRange = 24f;
        [Tooltip("Progress (0..1) the reel bar starts at the instant a fish is hooked and reeling begins.")]
        [Range(0f, 1f)] public float startingProgress = 0.25f;
        [Tooltip("Fallback progress-decay rate (0..1 per second) used while reeling when no FishDefinition is available for the round - normally each fish's own ProgressDecayRate is used instead.")]
        [Range(0f, 1f)] public float fallbackProgressDecayRate = 0.05f;
        [Tooltip("Seconds the reel progress bar can sit at 0% before the fish breaks free and escapes.")]
        [Min(0.05f)] public float escapeGraceSeconds = 5f;

        [Header("Rod Visual (3D)")]
        [Tooltip("Max degrees the 3D rod swings left/right at full horizontal direction.")]
        public float rodMaxYawDegrees = 32f;
        [Tooltip("Max degrees the 3D rod tips forward/back at full vertical direction.")]
        public float rodMaxPitchDegrees = 26f;
        [Tooltip("Idle hook bob amplitude/speed for a little life while waiting.")]
        public float hookBobAmplitude = 0.05f;
        public float hookBobSpeed = 1.6f;

        [Header("Scoring")]
        [Tooltip("Fallback score awarded on a catch. Only used when no FishDefinition is available for the round - normally the caught fish's own BaseScore is used instead.")]
        [FormerlySerializedAs("scorePerCatch")]
        public int fallbackScorePerCatch = 50;
    }
}
