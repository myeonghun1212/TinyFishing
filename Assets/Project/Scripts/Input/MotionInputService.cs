using System;
using NanFishing.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NanFishing.Input
{
    public sealed class MotionInputService : MonoBehaviour, IPlayerInput
    {
        public event Action<CastStrength> CastPerformed;

        [SerializeField] private GameBalanceConfig config;

        private Quaternion baselineAttitude = Quaternion.identity;
        private Vector3 filteredAcceleration = Vector3.up;
        private float stableTime;
        private float cooldown;
        private bool backswingReady;
        private bool calibrationRequested;
        private Vector2 pointerStart;
        private bool pointerWasPressed;

        public float Direction { get; private set; }
        public bool IsReeling { get; private set; }
        public bool IsCalibrated { get; private set; }
        public bool HasMotionSensors { get; private set; }
        public float CalibrationProgress =>
            config == null ? 0f : Mathf.Clamp01(stableTime / Mathf.Max(0.01f, config.stableDuration));

        public void Initialize(GameBalanceConfig balance)
        {
            config = balance;
            TryEnableSensors();
        }

        public void BeginCalibration()
        {
            stableTime = 0f;
            calibrationRequested = true;
            IsCalibrated = false;
            backswingReady = false;
        }

        public void Recalibrate()
        {
            if (HasMotionSensors && AttitudeSensor.current != null)
            {
                baselineAttitude = AttitudeSensor.current.attitude.ReadValue();
                stableTime = config != null ? config.stableDuration : 0f;
                calibrationRequested = false;
                IsCalibrated = true;
                backswingReady = false;
                return;
            }
            BeginCalibration();
        }

        private void OnEnable()
        {
            TryEnableSensors();
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            cooldown = Mathf.Max(0f, cooldown - Time.unscaledDeltaTime);
            UpdatePointer();
            UpdateMotion();
            UpdateEditorInput();
        }

        private void TryEnableSensors()
        {
            HasMotionSensors = AttitudeSensor.current != null && Accelerometer.current != null;
            if (!HasMotionSensors)
            {
                return;
            }

            InputSystem.EnableDevice(AttitudeSensor.current);
            InputSystem.EnableDevice(Accelerometer.current);
            if (UnityEngine.InputSystem.Gyroscope.current != null)
            {
                InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
            }
        }

        private void UpdateMotion()
        {
            if (!HasMotionSensors)
            {
                if (calibrationRequested)
                {
                    stableTime = config.stableDuration;
                    calibrationRequested = false;
                    IsCalibrated = true;
                }
                return;
            }

            var acceleration = Accelerometer.current.acceleration.ReadValue();
            filteredAcceleration = Vector3.Lerp(filteredAcceleration, acceleration, config.sensorSmoothing);
            var attitude = AttitudeSensor.current.attitude.ReadValue();

            if (calibrationRequested)
            {
                var deviationFromGravity = Mathf.Abs(filteredAcceleration.magnitude - 1f);
                stableTime = deviationFromGravity <= config.stableAccelerationTolerance
                    ? stableTime + Time.unscaledDeltaTime
                    : 0f;

                if (stableTime >= config.stableDuration)
                {
                    baselineAttitude = attitude;
                    calibrationRequested = false;
                    IsCalibrated = true;
                }
                return;
            }

            if (!IsCalibrated)
            {
                return;
            }

            var relative = Quaternion.Inverse(baselineAttitude) * attitude;
            var relativeEuler = relative.eulerAngles;
            var pitch = NormalizeAngle(relativeEuler.x);
            var roll = NormalizeAngle(relativeEuler.y);
            Direction = Mathf.Clamp(roll / config.maximumTiltAngle, -1f, 1f);

            if (pitch <= -config.backswingAngle)
            {
                backswingReady = true;
            }

            var forwardImpulse = -filteredAcceleration.z;
            if (backswingReady && forwardImpulse >= config.forwardSwingAcceleration && cooldown <= 0f)
            {
                TriggerCast(GetStrength(forwardImpulse));
            }
        }

        private void UpdatePointer()
        {
            var pressed = false;
            var position = Vector2.zero;

            if (Touchscreen.current != null)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = Touchscreen.current.primaryTouch.press.isPressed;
            }
            if (!pressed && Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.isPressed;
            }

            IsReeling = pressed;
            if (pressed && !pointerWasPressed)
            {
                pointerStart = position;
            }
            else if (!pressed && pointerWasPressed)
            {
                var delta = position - pointerStart;
                if (delta.y > Screen.height * 0.12f && cooldown <= 0f)
                {
                    TriggerCast(GetStrength(delta.y / Mathf.Max(1f, Screen.height) * 5f));
                }
            }

            if (pressed && !HasMotionSensors)
            {
                Direction = Mathf.Clamp((position.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f, -1f, 1f);
            }

            pointerWasPressed = pressed;
        }

        private void UpdateEditorInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Keyboard.current == null)
            {
                return;
            }

            var keyboardDirection = 0f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            {
                keyboardDirection -= 0.05f;
            }
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                keyboardDirection += 0.05f;
            }
            if (Mathf.Abs(keyboardDirection) > 0f)
            {
                Direction += keyboardDirection;
                Direction = Mathf.Clamp(Direction, -1f, 1f);
            }
            IsReeling |= Keyboard.current.spaceKey.isPressed;
            if (Keyboard.current.enterKey.wasPressedThisFrame && cooldown <= 0f)
            {
                TriggerCast(CastStrength.Medium);
            }
#endif
        }

        private void TriggerCast(CastStrength strength)
        {
            if (!IsCalibrated)
            {
                return;
            }
            backswingReady = false;
            cooldown = config.castCooldown;
            CastPerformed?.Invoke(strength);
        }

        private static CastStrength GetStrength(float value)
        {
            if (value >= 2.2f) return CastStrength.Long;
            if (value >= 1.55f) return CastStrength.Medium;
            return CastStrength.Short;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
