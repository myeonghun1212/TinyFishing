using UnityEngine;

namespace TinyFishing.Audio
{
    // Persistent, scene-independent player for UI click SFX.
    // Lives on its own GameObject (separate from any button) so the sound
    // keeps playing even if the button that triggered it - or a panel it
    // opens/closes - gets disabled in the very same click.
    public sealed class UiSfxManager : MonoBehaviour
    {
        public static UiSfxManager Instance { get; private set; }

        [SerializeField] private AudioClip defaultClickClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        public void PlayClick()
        {
            PlayClip(defaultClickClip);
        }

        public void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, volume);
        }
    }
}
