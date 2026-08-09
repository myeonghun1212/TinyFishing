using UnityEngine;

namespace TinyFishing.Fishing
{
    // Temporarily takes over a spawned fish visual that's living inside a
    // FishSpawnVolume, breaking it off from FishWanderAgent's idle roaming so it
    // can swim to a point beneath the bob and hold there. Added/removed by
    // FishSpawnVolume.BeginBiteApproach / CancelBiteApproach / ResolveBiteCaught -
    // not meant to be added by hand.
    public sealed class FishBiteAgent : MonoBehaviour
    {
        [Tooltip("How far below the bob's world Y this fish holds while waiting beneath it.")]
        [SerializeField] private float holdDepthBelowTarget = 0.3f;
        [Tooltip("Distance at which the fish is considered to have arrived beneath the bob.")]
        [SerializeField] private float arriveDistance = 0.2f;
        [SerializeField] private float turnSpeed = 6f;

        private Transform target;
        private float moveSpeed = 2f;

        // True once this fish has closed the distance to its hold point beneath the
        // bob. The game manager polls this to know when to trigger the bob's dip.
        public bool HasArrived { get; private set; }

        public void Begin(Transform bobTarget, float speed)
        {
            target = bobTarget;
            moveSpeed = Mathf.Max(0.05f, speed);
            HasArrived = false;
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            var destination = target.position + Vector3.down * holdDepthBelowTarget;
            var toTarget = destination - transform.position;
            var distance = toTarget.magnitude;

            if (distance <= arriveDistance)
            {
                HasArrived = true;
                // Keep gently tracking the hold point while waiting so it doesn't look
                // frozen if the bob drifts a little on the water surface.
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                return;
            }

            var direction = toTarget / distance;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction.sqrMagnitude > 0.0001f)
            {
                var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
            }
        }
    }
}
