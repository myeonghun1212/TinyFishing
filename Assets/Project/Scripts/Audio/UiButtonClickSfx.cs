using UnityEngine;
using UnityEngine.UI;

namespace TinyFishing.Audio
{
    // Plays a click sound whenever this Button is pressed.
    // Attach directly to any UI Button; assign clickClip (defaults to the
    // shared UI click sound if left unset by the prefab/scene).
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class UiButtonClickSfx : MonoBehaviour
    {
        [SerializeField] private AudioClip clickClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        private Button button;
        private AudioSource audioSource;

        private void Awake()
        {
            button = GetComponent<Button>();
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(PlayClick);
        }

        private void PlayClick()
        {
            if (clickClip == null || audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(clickClip, volume);
        }
    }
}
