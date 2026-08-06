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
        [Tooltip("Seconds to wait before another shake can trigger a cast.")]
        public float castCooldown = 0.6f;
        [Tooltip("Vertical drag distance (pixels), on top of the shake gesture, that also triggers a cast.")]
        public float dragCastThreshold = 260f;

        [Tooltip("Minimum seconds after a bite before a fish can be caught, purely for feel.")]
        public Vector2 biteDelayRange = new Vector2(0.4f, 1.4f);

        [Header("Tilt / Direction")]
        [Tooltip("Phone roll angle (degrees) that maps to full -1..1 horizontal direction range.")]
        public float maximumTiltAngle = 28f;
        [Tooltip("Phone pitch angle (degrees) that maps to full -1..1 vertical direction range.")]
        public float maximumPitchAngle = 24f;
        [Tooltip("Mouse/touch drag distance (pixels) that maps to the full -1..1 direction range on either axis.")]
        public float dragRange = 220f;
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
