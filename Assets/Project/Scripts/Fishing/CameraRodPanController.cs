using TinyFishing.Input;
using UnityEngine;

namespace TinyFishing.Fishing
{
    // Applies a small rotational pan to the camera in the same direction the fishing rod
    // is being aimed (see FishingRodController), so the view subtly leans the way the player
    // is tilting/dragging instead of staying perfectly locked off. Reads the same
    // ITinyFishingInput driving the rod rather than the rod's transform directly, so it keeps
    // working even if the rod's own smoothing/rest-pose setup changes.
    public sealed class CameraRodPanController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private MonoBehaviour inputBehaviour; // must implement ITinyFishingInput

        [Header("Pan Range")]
        [Tooltip("Max yaw pan in degrees at full left/right aim.")]
        [SerializeField] private float maxYawDegrees = 6f;
        [Tooltip("Max pitch pan in degrees at full up/down aim.")]
        [SerializeField] private float maxPitchDegrees = 3f;
        [SerializeField] private float panSmoothing = 4f;

        private ITinyFishingInput input;
        private Quaternion restRotation = Quaternion.identity;

        private void Awake()
        {
            input = inputBehaviour as ITinyFishingInput;
            if (input == null)
            {
                Debug.LogError("CameraRodPanController: assigned inputBehaviour does not implement ITinyFishingInput.");
            }

            restRotation = transform.localRotation;
        }

        // Runs continuously off live input, independent of game state, the same way
        // FishingRodController does - both simply read the current input value each frame,
        // so exact execution order between the two doesn't matter.
        private void LateUpdate()
        {
            if (input == null || !input.IsInputEnabled)
            {
                return;
            }

            // Panning is expressed as a small offset around world-space up/right so it
            // composes cleanly on top of however the camera is already angled in the scene,
            // the same way the rod itself layers its own aim rotation on its rest pose.
            var yaw = Quaternion.AngleAxis(input.Direction * maxYawDegrees, Vector3.up);
            var pitch = Quaternion.AngleAxis(-input.VerticalDirection * maxPitchDegrees, Vector3.right);
            var target = yaw * pitch * restRotation;

            transform.localRotation = panSmoothing > 0f
                ? Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * panSmoothing)
                : target;
        }

        // Snaps the camera back to its resting angle, e.g. when a round resets.
        public void ResetPan()
        {
            transform.localRotation = restRotation;
        }
    }
}
