using UnityEngine;
using UnityEngine.UI;

namespace TinyFishing.Audio
{
    // Drop this on any pressable UI element tagged "UIClickable" and it will
    // play the shared UI click sound through UiSfxManager whenever pressed.
    // The sound is played by the persistent manager rather than this
    // object's own AudioSource, so it is never cut off by this button (or a
    // panel it opens/closes) being disabled as part of the same click.
    [RequireComponent(typeof(Button))]
    public sealed class UiClickSfxTag : MonoBehaviour
    {
        public const string ClickableTag = "UIClickable";

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();

            if (!CompareTag(ClickableTag))
            {
                Debug.LogWarning(
                    $"'{name}' has UiClickSfxTag but isn't tagged '{ClickableTag}'; " +
                    "tag it to keep the click-sound convention consistent.",
                    this);
            }
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
            if (UiSfxManager.Instance != null)
            {
                UiSfxManager.Instance.PlayClick();
            }
        }
    }
}
