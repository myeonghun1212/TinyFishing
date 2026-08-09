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
        [Tooltip("Launch angle above horizontal, in degrees. 0 = flat/straight out, 90 = straight up.")]
        [SerializeField, Range(0f, 90f)] private float castAngle = 35f;

        [Header("Audio")]
        [SerializeField] private Collider waterSurface;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private AudioClip splashClip;

        private Rigidbody body;
        private bool hasSplashed;
        private Vector3 restPosition;
        private Quaternion restRotation;

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

        // Launches the bait from wherever it currently sits, using castDirection,
        // castStrength and castAngle to build the initial velocity. Where it lands
        // is purely a result of that velocity plus gravity - no random target point.
        public void Launch()
        {
            body.isKinematic = false;
            hasSplashed = false;
            PlaySfx(throwClip);

            var flatDirection = new Vector3(castDirection.x, 0f, castDirection.z);
            var direction = flatDirection.sqrMagnitude > 0.0001f ? flatDirection.normalized : Vector3.forward;

            var angleRad = castAngle * Mathf.Deg2Rad;
            var velocity = direction * (Mathf.Cos(angleRad) * castStrength);
            velocity.y = Mathf.Sin(angleRad) * castStrength;

            body.linearVelocity = velocity;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasSplashed || waterSurface == null || other != waterSurface)
            {
                return;
            }

            hasSplashed = true;
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
