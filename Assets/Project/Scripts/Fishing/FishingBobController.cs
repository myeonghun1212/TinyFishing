using System.Collections;
using UnityEngine;

namespace TinyFishing.Fishing
{
    // Drives the physical bait/bob object: sits frozen at its resting spot on the
    // rod between casts, and launches on a physics arc when the player casts, aimed
    // and powered by the tunable direction/strength/angle below. Buoyancy (on the
    // same object) takes over once it lands in water.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FishingBobController : MonoBehaviour
    {
        [Header("Cast Tuning")]
        [Tooltip("World-space horizontal direction the bait is cast toward. Only the X/Z components matter - it's normalized automatically.")]
        [SerializeField] private Vector3 castDirection = new Vector3(-0.43f, 0f, 0.9f);
        [Tooltip("Initial launch speed of the bait (units/sec). Higher = casts further.")]
        [SerializeField] private float castStrength = 8f;
        [Tooltip("Initial launch speed used for a minimum-power (0) cast.")]
        [SerializeField] private float minCastStrength = 6f;
        [Tooltip("Initial launch speed used for a full-power (1) cast.")]
        [SerializeField] private float maxCastStrength = 10f;
        [Tooltip("Launch angle above horizontal, in degrees. 0 = flat/straight out, 90 = straight up.")]
        [SerializeField, Range(0f, 90f)] private float castAngle = 35f;

        [Header("Audio")]
        [SerializeField] private Collider waterSurface;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private AudioClip splashClip;
        [Tooltip("Played the instant the fish takes the bait and the bob starts its dip (BeginDip).")]
        [SerializeField] private AudioClip biteSplashClip;

        public float CastStrength { get => castStrength; set => castStrength = Mathf.Max(0f, value); }

        private Rigidbody body;
        private bool hasSplashed;

        // True once the bob's OnTriggerEnter has detected it touching the water surface
        // for this cast; false while it's still in the air on its cast arc. Reeling is
        // gated on this so an early tap before splashdown is simply ignored instead of
        // spooking a fish that hasn't even started approaching yet.
        public bool IsInWater { get; private set; }
        private Vector3 restPosition;
        private Quaternion restRotation;

        private Coroutine dipRoutine;
        private Vector3 dipRestPosition;
        private bool isDipping;

        [Header("Reel Tension")]
        [Tooltip("Extra depth (world units) the bob is pulled down while the fish marker is misaligned (red) during reeling.")]
        [SerializeField] private float reelTensionPullDepth = 0.12f;
        [Tooltip("How quickly (units/sec) the bob eases toward its tension target each frame.")]
        [SerializeField] private float reelTensionSpeed = 4f;

        private Vector3 reelHeldPosition;
        private bool reelHoldActive;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            restPosition = transform.position;
            restRotation = transform.rotation;
        }

        // Snaps the bait back to its resting spot on the rod and freezes it there
        // until the next cast, so it doesn't drift or keep falling between rounds.
        public void ResetToRest()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(restPosition, restRotation);
            body.isKinematic = true;
        }

        // Launches the bait from wherever it currently sits. The normalized input strength
        // selects an initial speed between minCastStrength and maxCastStrength; direction,
        // angle, and gravity determine the resulting arc and landing point.
        public void Launch(float normalizedCastStrength)
        {
            var minimumStrength = Mathf.Min(minCastStrength, maxCastStrength);
            var maximumStrength = Mathf.Max(minCastStrength, maxCastStrength);
            CastStrength = Mathf.Lerp(minimumStrength, maximumStrength, Mathf.Clamp01(normalizedCastStrength));

            body.isKinematic = false;
            hasSplashed = false;
            IsInWater = false;
            PlaySfx(throwClip);

            var flatDirection = new Vector3(castDirection.x, 0f, castDirection.z);
            var direction = flatDirection.sqrMagnitude > 0.0001f ? flatDirection.normalized : Vector3.forward;

            var angleRad = castAngle * Mathf.Deg2Rad;
            var velocity = direction * (Mathf.Cos(angleRad) * castStrength);
            velocity.y = Mathf.Sin(angleRad) * castStrength;

            body.linearVelocity = velocity;
        }

        // Starts the fish-bite dip: animates the bob down by `depth` over `downTime`
        // seconds and freezes it there (kinematic, bypassing Buoyancy) until EndDip
        // resolves whether the hook window was caught in time.
        public void BeginDip(float depth, float downTime)
        {
            if (dipRoutine != null)
            {
                StopCoroutine(dipRoutine);
            }

            PlaySfx(biteSplashClip);

            dipRestPosition = transform.position;
            isDipping = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            dipRoutine = StartCoroutine(DipDownRoutine(depth, Mathf.Max(0.01f, downTime)));
        }

        private IEnumerator DipDownRoutine(float depth, float duration)
        {
            var from = transform.position;
            var to = from + Vector3.down * depth;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            transform.position = to;
            dipRoutine = null;
        }

        // Resolves an in-progress dip. On a successful hook, the bob stays held under
        // the surface (kinematic) for the reel minigame - ResetToRest() restores normal
        // physics on the next cast. On a missed hook window, it springs back up to
        // where it was floating before the dip and hands control back to Buoyancy.
        public void EndDip(bool caught, float returnTime)
        {
            if (dipRoutine != null)
            {
                StopCoroutine(dipRoutine);
                dipRoutine = null;
            }

            if (caught)
            {
                isDipping = false;
                return;
            }

            dipRoutine = StartCoroutine(DipReturnRoutine(Mathf.Max(0.01f, returnTime)));
        }

// Called once the fish is hooked and Reeling begins - captures the bob's current
        // (dipped, underwater, kinematic) position as the baseline that reel tension pulls
        // away from and eases back toward.
        public void BeginReelHold()
        {
            // Baseline is the floating position from BEFORE the bite dip (dipRestPosition),
            // not the bob's current position - it's still sitting at the dipped/submerged
            // spot when this is called (right after EndDip(true, ...)), so using the current
            // position here would keep it stuck underwater for the whole Reeling phase.
            reelHeldPosition = dipRestPosition;
            reelHoldActive = true;
        }

        // Called once the round resolves (catch lands) - stops UpdateReelTension from moving
        // the bob any further. ResetToRest() (on the next cast) takes over from here.
        public void EndReelHold()
        {
            reelHoldActive = false;
        }

        // Eases the bob toward (pulled) or back to (!pulled) its held position each frame,
        // driven by TinyFishingGameManager from the live fish-marker alignment during Reeling -
        // misaligned (marker red) pulls the bob under; aligned (marker safe) lets it settle back.
        public void UpdateReelTension(bool pulled)
        {
            if (!reelHoldActive)
            {
                return;
            }

            var target = reelHeldPosition + (pulled ? Vector3.down * reelTensionPullDepth : Vector3.zero);
            transform.position = Vector3.MoveTowards(transform.position, target, reelTensionSpeed * Time.deltaTime);
        }


        private IEnumerator DipReturnRoutine(float duration)
        {
            var from = transform.position;
            var to = dipRestPosition;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            transform.position = to;
            isDipping = false;
            body.isKinematic = false;
            dipRoutine = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasSplashed || waterSurface == null || other != waterSurface)
            {
                return;
            }

            hasSplashed = true;
            IsInWater = true;
            PlaySfx(splashClip);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            // Stop-and-restart so a throw/splash retrigger never overlaps itself.
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
