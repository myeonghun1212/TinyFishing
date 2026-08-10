using UnityEngine;
using UnityEngine.Audio;

namespace TinyFishing.Audio
{
    // Loops a single background music track for a menu/UI scene (e.g. the
    // start menu). Mirrors AmbientAudioManager's mixer-preferences pattern
    // used for gameplay scenes.
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusicPlayer : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Music (loops)")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip musicClip;

        private void Awake()
        {
            TinyFishingAudioPreferences.ApplySaved(audioMixer);

            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            PlayMusic();
        }

        private void PlayMusic()
        {
            if (musicSource == null || musicClip == null)
            {
                return;
            }

            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.Play();
        }
    }
}
