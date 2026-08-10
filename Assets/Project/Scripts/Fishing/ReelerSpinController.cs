using TinyFishing.Core;
using TinyFishing.Input;
using UnityEngine;

namespace TinyFishing.Fishing
{
    // Spins the rod's reel handle mesh around its local Z axis for a short burst after each
    // reel tap (while TinyFishingGameManager.State == Reeling), then coasts to a stop if no
    // further taps come in. Holding the tap down does not extend the spin beyond spinHoldSeconds -
    // each tap simply refreshes the timer.
    public sealed class ReelerSpinController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TinyFishingGameManager gameManager;

        [Tooltip("MonoBehaviour that implements ITinyFishingInput - assign the same object used as the GameManager's Input Behaviour.")]
        [SerializeField] private MonoBehaviour inputBehaviour;

        [Header("Spin")]
        [Tooltip("How fast the reel spins, in degrees per second, while active.")]
        [SerializeField] private float spinSpeedDegreesPerSecond = 360f;

        [Tooltip("How long the reel keeps spinning after each tap before it stops (seconds).")]
        [SerializeField] private float spinHoldSeconds = 0.25f;

        private ITinyFishingInput input;
        private float spinTimeRemaining;

        private void Awake()
        {
            input = inputBehaviour as ITinyFishingInput;
            if (input == null)
            {
                Debug.LogError("ReelerSpinController: assigned inputBehaviour does not implement ITinyFishingInput.");
            }
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.ReelTapPerformed += HandleReelTap;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.ReelTapPerformed -= HandleReelTap;
            }
        }

        private void HandleReelTap()
        {
            if (gameManager == null || gameManager.State != TinyFishingState.Reeling)
            {
                return;
            }

            spinTimeRemaining = spinHoldSeconds;
        }

        private void Update()
        {
            if (spinTimeRemaining <= 0f)
            {
                return;
            }

            spinTimeRemaining -= Time.deltaTime;
            transform.Rotate(Vector3.forward, spinSpeedDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
