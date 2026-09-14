using UnityEngine;

namespace Nan.Fishing
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class FishingLineView : MonoBehaviour
    {
        [SerializeField] private Transform _rodTip;
        [SerializeField] private Transform _bobber;
        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
        }

        private void LateUpdate()
        {
            if (_rodTip == null || _bobber == null)
                return;
            _line.SetPosition(0, _rodTip.position);
            _line.SetPosition(1, _bobber.position);
        }
    }
}
