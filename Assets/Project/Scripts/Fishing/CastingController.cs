using Nan.Core;
using Nan.Input;
using UnityEngine;
using UnityEngine.Events;

namespace Nan.Fishing
{
    public sealed class CastingController : MonoBehaviour
    {
        [SerializeField] private CastingEventChannel _castingEvent;
        [SerializeField] private FishingStateEventChannel _stateEvent;
        [SerializeField] private CastingSettings _settings;
        [SerializeField] private Transform _rodPivot;
        [SerializeField] private Transform _bobber;
        [SerializeField] private Transform _landingTarget;
        [SerializeField] private UnityEvent _castStarted = new UnityEvent();
        [SerializeField] private UnityEvent _landed = new UnityEvent();

        private readonly FishingSession _session = new FishingSession();
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _castStart;
        private Vector3 _castEnd;
        private float _elapsed;
        private bool _initialized;

        public FishingState State => _session.State;
        public UnityEvent CastStarted => _castStarted;
        public UnityEvent Landed => _landed;

        private void OnEnable()
        {
            if (_castingEvent == null || _settings == null || _rodPivot == null ||
                _bobber == null || _landingTarget == null)
            {
                Debug.LogError("Casting requires an event, settings, rod pivot, bobber and landing target.", this);
                enabled = false;
                return;
            }

            _restPosition = _bobber.localPosition;
            _restRotation = _rodPivot.localRotation;
            _initialized = true;
            _session.StateChanged += OnStateChanged;
            _castingEvent.Raised += Cast;
            OnStateChanged(State);
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;

            _castingEvent.Raised -= Cast;
            ResetCast();
            _session.StateChanged -= OnStateChanged;
            _initialized = false;
        }

        public void Cast()
        {
            if (!isActiveAndEnabled || !_initialized || State != FishingState.Ready)
                return;

            _castStart = _bobber.position;
            _castEnd = _landingTarget.position;
            _elapsed = 0f;
            if (_session.TryBeginCast() && State == FishingState.Casting)
                _castStarted.Invoke();
        }

        private void Update()
        {
            if (State != FishingState.Casting)
                return;

            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _settings.Duration);
            _rodPivot.localRotation = _restRotation *
                Quaternion.Euler(-Mathf.Sin(progress * Mathf.PI) * _settings.RodSwingAngle, 0f, 0f);
            _bobber.position = CastTrajectory.Evaluate(_castStart, _castEnd, _settings.ArcHeight, progress);

            if (progress >= 1f)
            {
                _rodPivot.localRotation = _restRotation;
                if (_session.TryCompleteCast() && State == FishingState.Waiting)
                    _landed.Invoke();
            }
        }

        public void ResetCast()
        {
            if (!_initialized)
                return;

            if (_rodPivot != null)
                _rodPivot.localRotation = _restRotation;
            if (_bobber != null)
                _bobber.localPosition = _restPosition;
            _elapsed = 0f;
            _session.Reset();
        }

        private void OnStateChanged(FishingState state)
        {
            if (_stateEvent != null)
                _stateEvent.Raise(state);
        }
    }
}
