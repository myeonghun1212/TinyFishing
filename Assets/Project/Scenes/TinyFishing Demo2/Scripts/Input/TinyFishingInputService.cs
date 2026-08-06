using System;
using TinyFishing.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyFishing.Input
{
    // Cast = shake the phone. Aim (both left/right and up/down) is driven by phone tilt when
    // sensors are present, and by mouse/touch drag otherwise (or in addition, on-device, if the
    // player prefers to drag rather than physically tilt). A press only counts as aiming once it
    // moves past a small threshold - a plain tap never repositions the rod.
    // Reel = a second finger touching down while the first is held (multitouch), or, with only
    // a mouse available (Editor/desktop), a short click that isn't a drag.
    // Falls back to keyboard so the demo is fully playable in the Editor without a device attached.
    public sealed class TinyFishingInputService : MonoBehaviour, ITinyFishingInput
    {
        public event Action CastPerformed;
        public event Action ReelTapPerformed;

        [SerializeField] private TinyFishingConfig config;

        [Header("Invert Axis")]
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;

        [Tooltip("On some devices the attitude sensor's roll/pitch come back on swapped axes, so tilting left/right ends up moving the rod up/down instead of side to side. Enable this to swap them back; use Invert Horizontal/Vertical above if a direction still comes out reversed after swapping.")]
        [SerializeField] private bool swapTiltAxes = true;


        private bool hasMotionSensors;
        private Quaternion baselineAttitude = Quaternion.identity;
        private Vector3 filteredAcceleration = Vector3.up;
        private Vector3 previousFilteredAcceleration = Vector3.up;
        private float castCooldownTimer;
        private float keyboardDirection;
        private float keyboardVertical;

        private bool pointerDown;
        private bool isDragging;
        private int primaryTouchId = -1;
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
        public bool IsPressed => pointerDown;


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
        // Touch and mouse are handled separately: touch supports true multitouch (a second
        // finger reels while the first is held), which the single-pointer mouse path can't do,
        // so mouse keeps the old short-click-to-reel behaviour for Editor/desktop testing.
private void UpdatePointerDrag()
        {
            // Only route to the touch path when a touch is actually in play - many PCs/laptops
            // enumerate a Touchscreen device even when it's never used, and routing there
            // unconditionally would silently swallow all mouse input on those machines.
            var touchActive = Touchscreen.current != null && (primaryTouchId >= 0 || AnyTouchPressed());
            if (touchActive)
            {
                UpdateTouchInput();
                return;
            }

            UpdateMouseInput();
        }

        private static bool AnyTouchPressed()
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateTouchInput()
        {
            var touches = Touchscreen.current.touches;

            if (pointerDown)
            {
                var primaryStillDown = false;
                foreach (var touch in touches)
                {
                    if (touch.touchId.ReadValue() == primaryTouchId && touch.press.isPressed)
                    {
                        UpdateDrag(touch.position.ReadValue());
                        primaryStillDown = true;
                        break;
                    }
                }

                if (!primaryStillDown)
                {
                    // Primary finger lifted. Touch reeling is handled entirely by the second-finger
                    // gesture below, not by tap-on-release, so there's nothing else to do here.
                    EndPress(allowTapReel: false);
                    primaryTouchId = -1;
                    return;
                }

                // A second finger touching down while the first is held reels the fish in,
                // regardless of whether the first finger is aiming (dragging) or just resting.
                foreach (var touch in touches)
                {
                    if (touch.touchId.ReadValue() != primaryTouchId && touch.press.wasPressedThisFrame)
                    {
                        ReelTapPerformed?.Invoke();
                        break;
                    }
                }

                return;
            }

            foreach (var touch in touches)
            {
                if (touch.press.isPressed)
                {
                    primaryTouchId = touch.touchId.ReadValue();
                    BeginPress(touch.position.ReadValue());
                    break;
                }
            }
        }

        private void UpdateMouseInput()
        {
            if (Mouse.current == null)
            {
                return;
            }

            var currentPosition = Mouse.current.position.ReadValue();
            var pressed = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed;

            if (pressed && !pointerDown)
            {
                BeginPress(currentPosition);
            }
            else if (pressed && pointerDown)
            {
                UpdateDrag(currentPosition);
            }
            else if (!pressed && pointerDown)
            {
                EndPress(allowTapReel: true);
            }
        }

        // Press started. Anchor the drag to whatever Direction/VerticalDirection the rod is
        // already holding, so a fresh press doesn't snap the rod back toward zero the instant
        // the drag threshold is crossed - it just continues from wherever it currently is.
        private void BeginPress(Vector2 position)
        {
            pointerDown = true;
            isDragging = false;
            pointerDownPosition = position;
            pointerDownTime = Time.unscaledTime;
            directionAtPressStart = Direction;
            verticalAtPressStart = VerticalDirection;
            dragCastTriggered = false;
        }

        // Only actually moves the rod once the press has moved past the drag threshold - a
        // press that never crosses it (a tap) never touches Direction/VerticalDirection.
        private void UpdateDrag(Vector2 currentPosition)
        {
            var offset = currentPosition - pointerDownPosition;
            if (!isDragging && offset.magnitude >= DpiScaledPixels(config.dragStartThreshold))
            {
                isDragging = true;
            }

            if (isDragging)
            {
                var range = Mathf.Max(1f, DpiScaledPixels(config.dragRange));
                dragHorizontal = Mathf.Clamp(directionAtPressStart + offset.x / range, -1f, 1f);
                dragVertical = Mathf.Clamp(verticalAtPressStart + offset.y / range, -1f, 1f);
                ApplyAim(dragHorizontal, dragVertical);

                // A big enough vertical flick also casts, same as shaking the phone.
                if (!dragCastTriggered && castCooldownTimer <= 0f
                    && Mathf.Abs(offset.y) >= DpiScaledPixels(config.dragCastThreshold))
                {
                    dragCastTriggered = true;
                    TriggerCast();
                }
            }
        }

        // allowTapReel is only true on the mouse path - touch reeling is handled by the
        // second-finger gesture instead, since a plain single-touch tap should never reel or
        // move the rod (that's what read as an accidental rod-position jump on real devices).
        private void EndPress(bool allowTapReel)
        {
            if (allowTapReel && !isDragging)
            {
                var heldFor = Time.unscaledTime - pointerDownTime;
                if (heldFor <= config.tapMaxDuration)
                {
                    ReelTapPerformed?.Invoke();
                }
            }

            pointerDown = false;
            isDragging = false;
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

                if (swapTiltAxes)
                {
                    (tiltDirection, tiltVertical) = (tiltVertical, tiltDirection);
                }

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

        // Config drag/tap distances (dragStartThreshold, dragRange, dragCastThreshold) are tuned
        // in "desktop" pixels around a ~160dpi reference. Real phone screens run at a much higher
        // pixel density (400dpi+), so a fixed pixel threshold is tiny relative to a finger's
        // natural jitter - a light tap easily exceeds it and gets misclassified as a drag, which
        // is why reel taps landed far less reliably than tilt on device. Scaling by dpi keeps the
        // physical (inches) distance consistent across devices.
        private static float DpiScaledPixels(float desktopPixels)
        {
            var dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return desktopPixels * (dpi / 160f);
        }
    }
}
