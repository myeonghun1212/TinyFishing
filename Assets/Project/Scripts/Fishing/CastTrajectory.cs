using UnityEngine;

namespace Nan.Fishing
{
    public static class CastTrajectory
    {
        public static Vector3 Evaluate(Vector3 start, Vector3 end, float height, float progress)
        {
            float t = Mathf.Clamp01(progress);
            return Vector3.Lerp(start, end, t) + Vector3.up * (4f * Mathf.Max(0f, height) * t * (1f - t));
        }
    }
}
