using System;
using TinyFishing.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyFishing.Input
{
    // Cast = shake the phone. Reel = discrete tap.
    // Aim (both left/right and up/down) is driven by phone tilt when sensors are present,
    // and by mouse/touch drag otherwise (or in addition, on-device, if the player prefers
    // to drag rather than physically tilt). A press only counts as a drag once it moves past
    // a small threshold; short, mostly-still presses are treated as reel taps instead.
    // Falls back to keyboard so the demo is fully playable in the Editor without a device attached.
    public sealed class TinyFishingInputService : MonoBehaviour, ITinyFishingInput
    {
        public event Action CastPerformed;
        public event Action ReelTapPerformed;

        [SerializeField] private TinyFishingConfig config;

        [Header("Invert Axis")]
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;


        private bool hasMotionSensors;
        private Quaternion baselineAttitude = Quaternion.identity;
        private Vector3 filteredAcceleration = Vector3.up;
        private Vector3 previousFilteredAcceleration = Vector3.up;
        private float castCooldownTimer;
        private float keyboardDirection;
        private float keyboardVertical;

        private bool pointerDown;
        private bool isDragging;
        private Vector2 pointerDownPosition;
        private float pointerDownTime;
        private float dragHorizontal;
        private float dragVertical;
        private float directionAtPressStart;
        private float verticalAtPressStart;
        private bool dragCastTriggered;

        public float Direction { get; private set; }
        public float VerticalDirection { get; private set; }
        public bool IsReady { get; private set; }

        public void Configure(TinyFishingConfig fishingConfig)
        {
            config = fishingConfig;
        }

        public void Recalibrate()
        {
            if (hasMotionSensors && AttitudeSensor.current != null)
            {
                baselineAttitude = AttitudeSensor.current.attitude.ReadValue();
            }
            keyboardDirection = 0f;
            keyboardVertical = 0f;
            isDragging = false;
            pointerDown = false;
            Direction = 0f;
            VerticalDirection = 0f;
            IsReady = true;
        }

        private void OnEnable()
        {
            hasMotionSensors = AttitudeSensor.current != null && Accelerometer.current != null;
            if (hasMotionSensors)
            {
                InputSystem.EnableDevice(AttitudeSensor.current);
                InputSystem.EnableDevice(Accelerometer.current);
            }
            Recalibrate();
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            castCooldownTimer = Mathf.Max(0f, castCooldownTimer - Time.unscaledDeltaTime);

            UpdatePointerDrag();
            UpdateTilt();
            UpdateShake();
            UpdateEditorFallback();
        }

        // Drag takes priority over tilt/keyboard while active, so a player can always grab
        // the rod directly with a finger or the mouse regardless of what device they're on.
private void UpdatePointerDrag()
        {
            if (!TryGetPointerPosition(out var currentPosition, out var pressed))
            {
                return;
            }

            if (pressed && !pointerDown)
            {
                // Press started. Anchor the drag to whatever Direction/VerticalDirection the
                // rod is already holding, so a fresh click doesn't snap the rod back toward
                // zero the instant the drag threshold is crossed - it just continues from
                // wherever it currently is.
                pointerDown = true;
                isDragging = false;
                pointerDownPosition = currentPosition;
                pointerDownTime = Time.unscaledTime;
                directionAtPressStart = Direction;
                verticalAtPressStart = VerticalDirection;
                dragCastTriggered = false;
            }
            else if (pressed && pointerDown)
            {
                var offset = currentPosition - pointerDownPosition;
                if (!isDragging && offset.magnitude >= config.dragStartThreshold)
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    var range = Mathf.Max(1f, config.dragRange);
                    dragHorizontal = Mathf.Clamp(directionAtPressStart + offset.x / range, -1f, 1f);
                    dragVertical = Mathf.Clamp(verticalAtPressStart + offset.y / range, -1f, 1f);
                    ApplyAim(dragHorizontal, dragVertical);

                    // A big enough vertical flick also casts, same as shaking the phone.
                    if (!dragCastTriggered && castCooldownTimer <= 0f
                        && Mathf.Abs(offset.y) >= config.dragCastThreshold)
                    {
                        dragCastTriggered = true;
                        TriggerCast();
                    }
                }
            }
            else if (!pressed && pointerDown)
            {
                // Release: a short, mostly-still press counts as a reel tap.
                var heldFor = Time.unscaledTime - pointerDownTime;
                if (!isDragging && heldFor <= config.tapMaxDuration)
                {
                    ReelTapPerformed?.Invoke();
                }

                pointerDown = false;
                isDragging = false;
            }
        }

        private static bool TryGetPointerPosition(out Vector2 position, out bool pressed)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = true;
                return true;
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed;
                return true;
            }

            position = Vector2.zero;
            pressed = false;
            return false;
        }

        // Drag sets Direction/VerticalDirection directly while active (see UpdatePointerDrag).
        // Once released, we deliberately do NOT reset those values back to zero here - the rod
        // should hold whatever angle the player last dragged it to, like letting go of a real
        // rod and having it stay put. Only an active tilt sensor or an active keyboard press
        // should override the held value; with neither, we simply leave Direction/VerticalDirection
        // untouched so they keep reading the last drag position.
private void UpdateTilt()
        {
            if (isDragging)
            {
                return; // active drag owns both axes until release
            }

            if (hasMotionSensors && AttitudeSensor.current != null)
            {
                var attitude = AttitudeSensor.current.attitude.ReadValue();
                var relative = Quaternion.Inverse(baselineAttitude) * attitude;
                var euler = relative.eulerAngles;
                var roll = NormalizeAngle(euler.y);
                var pitch = NormalizeAngle(euler.x);

                var tiltDirection = Mathf.Clamp(roll / Mathf.Max(1f, config.maximumTiltAngle), -1f, 1f);
                var tiltVertical = Mathf.Clamp(-pitch / Mathf.Max(1f, config.maximumPitchAngle), -1f, 1f);

                ApplyAim(tiltDirection + keyboardDirection, tiltVertical + keyboardVertical);
                return;
            }

            if (Mathf.Abs(keyboardDirection) > 0.0001f || Mathf.Abs(keyboardVertical) > 0.0001f)
            {
                ApplyAim(keyboardDirection, keyboardVertical);
            }
            // else: no sensors, no keyboard input, not dragging - hold the last position.
        }

// Single choke point for writing Direction/VerticalDirection so the invert toggles
        // above apply consistently no matter which input source (drag, tilt, keyboard) is active.
        private void ApplyAim(float rawHorizontal, float rawVertical)
        {
            Direction = Mathf.Clamp(invertHorizontal ? -rawHorizontal : rawHorizontal, -1f, 1f);
            VerticalDirection = Mathf.Clamp(invertVertical ? -rawVertical : rawVertical, -1f, 1f);
        }


        private void UpdateShake()
        {
            if (!hasMotionSensors || Accelerometer.current == null)
            {
                return;
            }

            var acceleration = Accelerometer.current.acceleration.ReadValue();
            previousFilteredAcceleration = filteredAcceleration;
            filteredAcceleration = Vector3.Lerp(filteredAcceleration, acceleration, config.sensorSmoothing);

            var jerk = (filteredAcceleration - previousFilteredAcceleration).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            if (jerk >= config.shakeThreshold && castCooldownTimer <= 0f)
            {
                TriggerCast();
            }
        }

private void UpdateEditorFallback()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Keyboard.current == null)
            {
                return;
            }

            var direction = 0f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            {
                direction -= 1f;
            }
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                direction += 1f;
            }
            keyboardDirection = direction;

            var vertical = 0f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
            {
                vertical -= 1f;
            }
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
            {
                vertical += 1f;
            }
            keyboardVertical = vertical;

            if (Keyboard.current.enterKey.wasPressedThisFrame && castCooldownTimer <= 0f)
            {
                TriggerCast();
            }

            // Testing shortcut: Space reels in, same as a tap/click.
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ReelTapPerformed?.Invoke();
            }
#endif
        }

        private void TriggerCast()
        {
            castCooldownTimer = config.castCooldown;
            CastPerformed?.Invoke();
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
