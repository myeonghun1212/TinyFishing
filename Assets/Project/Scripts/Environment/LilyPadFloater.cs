using UnityEngine;

namespace TinyFishing.Environment
{
    // Makes a lily pad gently drift around a "home" point on the water surface using
    // smooth Perlin-noise wandering, plus a slow rotation. Each instance uses its own
    // random noise offset so pads don't move in sync with each other. Stays flat on the
    // water surface (no vertical bob) so drift radius can be tuned to avoid overlap
    // between neighboring pads.
    public sealed class LilyPadFloater : MonoBehaviour
    {
        [Header("Drift (horizontal wander around home position)")]
        [SerializeField] private float driftRadius = 0.6f;
        [SerializeField] private float driftSpeed = 0.15f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeedDegreesPerSecond = 4f;

        private Vector3 homePosition;
        private float noiseOffsetX;
        private float noiseOffsetZ;
        private float rotationDirection;

        private void Awake()
        {
            homePosition = transform.position;
            noiseOffsetX = Random.Range(0f, 1000f);
            noiseOffsetZ = Random.Range(0f, 1000f);
            rotationDirection = Random.value < 0.5f ? -1f : 1f;
        }

        private void Update()
        {
            float t = Time.time;

            float dx = (Mathf.PerlinNoise(noiseOffsetX, t * driftSpeed) - 0.5f) * 2f * driftRadius;
            float dz = (Mathf.PerlinNoise(noiseOffsetZ, t * driftSpeed) - 0.5f) * 2f * driftRadius;

            transform.position = homePosition + new Vector3(dx, 0f, dz);
            transform.Rotate(Vector3.up, rotationDirection * rotationSpeedDegreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
