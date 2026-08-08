using UnityEngine;

namespace TinyFishing.Fishing
{
    // Makes a spawned fish visual wander within a world-space box volume:
    // periodically picks a new random point inside the bounds, steers toward
    // it, and rotates to face its travel direction. Configured at spawn time
    // by FishSpawnVolume - not meant to be hand-tuned per instance.
    public sealed class FishWanderAgent : MonoBehaviour
    {
        private Vector3 volumeCenter;
        private Vector3 volumeHalfExtents;
        private float moveSpeed = 1f;
        private float turnSpeed = 4f;
        private Vector2 pauseSecondsRange = new Vector2(0.5f, 2f);

        private Vector3 targetPoint;
        private float pauseTimer;
        private bool paused;
        private bool configured;

        /// <summary>
        /// Sets the roam volume (world-space center + half extents) and movement
        /// tuning for this agent, then picks its first wander target.
        /// </summary>
        public void Configure(Vector3 center, Vector3 halfExtents, float speed, float turnRate, Vector2 pauseRange)
        {
            volumeCenter = center;
            volumeHalfExtents = halfExtents;
            moveSpeed = Mathf.Max(0.05f, speed);
            turnSpeed = Mathf.Max(0.1f, turnRate);
            pauseSecondsRange = pauseRange;
            configured = true;
            PickNewTarget();
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            if (paused)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f)
                {
                    paused = false;
                    PickNewTarget();
                }
                return;
            }

            var toTarget = targetPoint - transform.position;
            var distance = toTarget.magnitude;

            if (distance < 0.15f)
            {
                paused = true;
                pauseTimer = Random.Range(pauseSecondsRange.x, pauseSecondsRange.y);
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

        private void PickNewTarget()
        {
            targetPoint = volumeCenter + new Vector3(
                Random.Range(-volumeHalfExtents.x, volumeHalfExtents.x),
                Random.Range(-volumeHalfExtents.y, volumeHalfExtents.y),
                Random.Range(-volumeHalfExtents.z, volumeHalfExtents.z));
        }
    }
}
