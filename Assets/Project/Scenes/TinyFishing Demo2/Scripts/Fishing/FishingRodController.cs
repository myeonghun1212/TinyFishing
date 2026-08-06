using TinyFishing.Input;
using UnityEngine;

namespace TinyFishing.Fishing
{
    // Bends the physical fishing rod model to follow the player's aim input (see
    // ITinyFishingInput.Direction / VerticalDirection). Pulled out of TinyFishingHUD so the
    // rod's own tuning (swing range, reference camera) lives with the rod, not the UI.
    //
    // The rod's rest pose can sit at an arbitrary world rotation depending on how the model
    // was authored, so rotating it around its own local X/Z axes doesn't reliably match what
    // the camera sees as "up/down" vs "left/right" - that mismatch is what made vertical input
    // swing the rod sideways. Instead we rotate around the reference camera's world-space
    // right/up axes and apply that on top of the rod's original rest rotation, so the rod
    // always bends the way it looks on screen regardless of its rest orientation.
    public sealed class FishingRodController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private MonoBehaviour inputBehaviour; // must implement ITinyFishingInput
        [SerializeField] private Camera referenceCamera; // defaults to Camera.main if left empty

        [Header("Swing Range")]
        [SerializeField] private float maxYawDegrees = 28f;
        [SerializeField] private float maxPitchDegrees = 18f;
        [SerializeField] private float rotationSmoothing = 8f;

        private ITinyFishingInput input;
        private Quaternion restRotation = Quaternion.identity;

        private void Awake()
        {
            input = inputBehaviour as ITinyFishingInput;
            if (input == null)
            {
                Debug.LogError("FishingRodController: assigned inputBehaviour does not implement ITinyFishingInput.");
            }

            if (referenceCamera == null)
            {
                referenceCamera = Camera.main;
            }

            restRotation = transform.rotation;
        }

        // Continuously drives the rod's yaw/pitch from live input, independent of game state,
        // so it visibly responds to tilt/drag as soon as the player touches it - not just while
        // actively reeling.
        private void LateUpdate()
        {
            if (input == null || referenceCamera == null)
            {
                return;
            }

            var camTransform = referenceCamera.transform;
            var yaw = Quaternion.AngleAxis(input.Direction * maxYawDegrees, camTransform.up);
            var pitch = Quaternion.AngleAxis(-input.VerticalDirection * maxPitchDegrees, camTransform.right);
            var target = yaw * pitch * restRotation;

            transform.rotation = rotationSmoothing > 0f
                ? Quaternion.Slerp(transform.rotation, target, Time.deltaTime * rotationSmoothing)
                : target;
        }

        // Snaps the rod back to its resting angle, e.g. when a round resets.
        public void ResetRod()
        {
            transform.rotation = restRotation;
        }
    }
}
