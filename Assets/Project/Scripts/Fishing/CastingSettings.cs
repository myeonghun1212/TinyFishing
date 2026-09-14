using UnityEngine;

namespace Nan.Fishing
{
    [CreateAssetMenu(menuName = "Nan/Fishing/Casting Settings")]
    public sealed class CastingSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _duration = 0.85f;
        [SerializeField, Min(0f)] private float _arcHeight = 2.5f;
        [SerializeField, Range(0f, 90f)] private float _rodSwingAngle = 55f;

        public float Duration => Mathf.Max(0.01f, _duration);
        public float ArcHeight => Mathf.Max(0f, _arcHeight);
        public float RodSwingAngle => _rodSwingAngle;
    }
}
