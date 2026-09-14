using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nan.Input
{
    /// <summary>Small casting-only adapter. Other input systems may raise the same channel directly.</summary>
    public sealed class CastingInput : MonoBehaviour
    {
        [SerializeField] private CastingEventChannel _castingEvent;
        [SerializeField, Range(0.01f, 1f)] private float _minimumDragScreenFraction = 0.12f;
        private Vector2 _dragStart;
        private bool _dragging;

        private void OnDisable()
        {
            _dragging = false;
        }

        // EventSystem.Update must process this frame's pointers before the UI hit check.
        private void LateUpdate()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                RequestCast();

            if (Touchscreen.current != null &&
                (Touchscreen.current.primaryTouch.press.isPressed ||
                 Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _dragging = false;
                    return;
                }
                ReadDrag(touch.position.ReadValue(), touch.press.wasPressedThisFrame,
                    touch.press.wasReleasedThisFrame, touch.touchId.ReadValue());
            }
            else if (Mouse.current != null)
            {
                ReadDrag(Mouse.current.position.ReadValue(), Mouse.current.leftButton.wasPressedThisFrame,
                    Mouse.current.leftButton.wasReleasedThisFrame, -1);
            }
        }

        private void ReadDrag(Vector2 position, bool pressed, bool released, int pointerId)
        {
            if (pressed)
            {
                _dragging = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(pointerId);
                _dragStart = position;
            }
            if (!released || !_dragging)
                return;

            _dragging = false;
            Vector2 delta = position - _dragStart;
            if (delta.y >= Screen.height * _minimumDragScreenFraction && delta.y > Mathf.Abs(delta.x))
                RequestCast();
        }

        public void RequestCast()
        {
            if (isActiveAndEnabled && _castingEvent != null)
                _castingEvent.Raise();
        }
    }
}
