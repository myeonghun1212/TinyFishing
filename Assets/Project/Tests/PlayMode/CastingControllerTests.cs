using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Nan.Core;
using Nan.Fishing;
using Nan.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nan.Tests
{
    public sealed class CastingControllerTests
    {
        private GameObject _root;
        private CastingController _controller;
        private CastingEventChannel _castingEvent;
        private FishingStateEventChannel _stateEvent;
        private CastingSettings _settings;
        private Transform _rod;
        private Transform _bobber;
        private Transform _target;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private List<FishingState> _states;
        private int _startedCount;
        private int _landedCount;
        private float _previousCaptureDeltaTime;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousCaptureDeltaTime = Time.captureDeltaTime;
            _previousTimeScale = Time.timeScale;
            Time.captureDeltaTime = 1f / 60f;
            Time.timeScale = 1f;
            _startedCount = 0;
            _landedCount = 0;
            _states = new List<FishingState>();
            _castingEvent = ScriptableObject.CreateInstance<CastingEventChannel>();
            _stateEvent = ScriptableObject.CreateInstance<FishingStateEventChannel>();
            _stateEvent.Raised += _states.Add;
            _settings = ScriptableObject.CreateInstance<CastingSettings>();
            SetField(_settings, "_duration", 0.2f);

            // Wire serialized dependencies before OnEnable, as a loaded scene does.
            _root = new GameObject("Casting test fixture");
            _root.SetActive(false);
            _root.transform.position = new Vector3(3f, 2f, -4f);
            _rod = CreateChild("Rod", new Vector3(0f, 1f, 0f));
            _rod.localRotation = Quaternion.Euler(10f, 20f, 5f);
            _bobber = CreateChild("Bobber", new Vector3(0f, 2f, 1f));
            _target = CreateChild("Landing target", new Vector3(0f, -2f, 8f));
            _restPosition = _bobber.localPosition;
            _restRotation = _rod.localRotation;
            _controller = _root.AddComponent<CastingController>();
            SetField(_controller, "_castingEvent", _castingEvent);
            SetField(_controller, "_stateEvent", _stateEvent);
            SetField(_controller, "_settings", _settings);
            SetField(_controller, "_rodPivot", _rod);
            SetField(_controller, "_bobber", _bobber);
            SetField(_controller, "_landingTarget", _target);
            _controller.CastStarted.AddListener(() => _startedCount++);
            _controller.Landed.AddListener(() => _landedCount++);
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_castingEvent);
            Object.DestroyImmediate(_stateEvent);
            Object.DestroyImmediate(_settings);
            Time.captureDeltaTime = _previousCaptureDeltaTime;
            Time.timeScale = _previousTimeScale;
        }

        [UnityTest]
        public IEnumerator CastingEvent_AnimatesThenLandsOnceAndRejectsDuplicateInput()
        {
            Vector3 start = _bobber.position;
            Vector3 end = _target.position;
            _castingEvent.Raise();
            _castingEvent.Raise();

            Assert.That(_controller.State, Is.EqualTo(FishingState.Casting));
            Assert.That(_startedCount, Is.EqualTo(1));
            Assert.That(_landedCount, Is.Zero);
            yield return null;
            yield return null;

            Assert.That(_controller.State, Is.EqualTo(FishingState.Casting));
            Assert.That(Vector3.Distance(_bobber.position, start), Is.GreaterThan(0.01f));
            Assert.That(Vector3.Distance(_bobber.position, end), Is.GreaterThan(0.01f));
            Assert.That(Quaternion.Angle(_rod.localRotation, _restRotation), Is.GreaterThan(1f));
            _castingEvent.Raise();
            yield return WaitForLanding();

            Assert.That(Vector3.Distance(_bobber.position, end), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(_rod.localRotation, _restRotation), Is.LessThan(0.001f));
            Assert.That(_landedCount, Is.EqualTo(1));
            _castingEvent.Raise();
            yield return null;
            Assert.That(_startedCount, Is.EqualTo(1));
            Assert.That(_landedCount, Is.EqualTo(1));
            Assert.That(_controller.State, Is.EqualTo(FishingState.Waiting));
            CollectionAssert.AreEqual(new[] { FishingState.Ready, FishingState.Casting, FishingState.Waiting }, _states);
        }

        [UnityTest]
        public IEnumerator ResetDuringFlight_CancelsLandingRestoresPoseAndAllowsNewCast()
        {
            _castingEvent.Raise();
            yield return null;
            yield return null;

            _controller.ResetCast();
            AssertRestored();
            for (int frame = 0; frame < 20; frame++)
                yield return null;
            Assert.That(_landedCount, Is.Zero);

            _castingEvent.Raise();
            yield return WaitForLanding();
            Assert.That(_startedCount, Is.EqualTo(2));
            Assert.That(_landedCount, Is.EqualTo(1));

            _controller.ResetCast();
            AssertRestored();
            _castingEvent.Raise();
            yield return WaitForLanding();
            Assert.That(_startedCount, Is.EqualTo(3));
            Assert.That(_landedCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DisableDuringFlight_CancelsAndResubscribesOnceWhenEnabled()
        {
            _castingEvent.Raise();
            yield return null;
            yield return null;

            _controller.enabled = false;
            AssertRestored();
            _castingEvent.Raise();
            _controller.Cast();
            for (int frame = 0; frame < 20; frame++)
                yield return null;
            Assert.That(_startedCount, Is.EqualTo(1));
            Assert.That(_landedCount, Is.Zero);

            _controller.enabled = true;
            _castingEvent.Raise();
            yield return WaitForLanding();
            Assert.That(_startedCount, Is.EqualTo(2));
            Assert.That(_landedCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(_bobber.position, _target.position), Is.LessThan(0.0001f));
        }

        private IEnumerator WaitForLanding()
        {
            for (int frame = 0; frame < 30 && _controller.State == FishingState.Casting; frame++)
                yield return null;
            Assert.That(_controller.State, Is.EqualTo(FishingState.Waiting), "The cast must complete within its configured duration.");
        }

        private void AssertRestored()
        {
            Assert.That(_controller.State, Is.EqualTo(FishingState.Ready));
            Assert.That(Vector3.Distance(_bobber.localPosition, _restPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(_rod.localRotation, _restRotation), Is.LessThan(0.001f));
        }

        private Transform CreateChild(string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(_root.transform, false);
            child.localPosition = localPosition;
            return child;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field {name} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
