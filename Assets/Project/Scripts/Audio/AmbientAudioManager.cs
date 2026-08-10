using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace TinyFishing.Audio
{
    // Scene-level ambience + background music.
    // - Pond ambience loops continuously on its own AudioSource.
    // - Background music picks a random clip from bgmPlaylist and plays it after a
    //   random wait, then repeats, so the scene doesn't loop a single BGM track back
    //   to back and instead has quiet gaps between plays.
    public sealed class AmbientAudioManager : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Pond Ambience (loops)")]
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioClip pondAmbienceClip;

        [Header("Background Music (random playlist)")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip[] bgmPlaylist;
        [Tooltip("Random delay range (seconds) between the end of one BGM track and the start of the next.")]
        [SerializeField] private Vector2 bgmIntervalRange = new Vector2(10f, 30f);
        [Tooltip("If true, also waits a random interval before the very first BGM track plays.")]
        [SerializeField] private bool randomDelayBeforeFirstTrack = true;

        private Coroutine bgmRoutine;

        private void Awake()
        {
            TinyFishingAudioPreferences.ApplySaved(audioMixer);
        }

        private void OnEnable()
        {
            StartAmbience();
            bgmRoutine = StartCoroutine(BgmPlaylistRoutine());
        }

        private void OnDisable()
        {
            if (bgmRoutine != null)
            {
                StopCoroutine(bgmRoutine);
                bgmRoutine = null;
            }
        }

        private void StartAmbience()
        {
            if (ambienceSource == null || pondAmbienceClip == null)
            {
                return;
            }

            ambienceSource.clip = pondAmbienceClip;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        private IEnumerator BgmPlaylistRoutine()
        {
            if (bgmSource == null || bgmPlaylist == null || bgmPlaylist.Length == 0)
            {
                yield break;
            }

            if (randomDelayBeforeFirstTrack)
            {
                yield return new WaitForSeconds(RandomInterval());
            }

            while (true)
            {
                Random.InitState(System.DateTime.Now.Millisecond);
                var clip = bgmPlaylist[Random.Range(0, bgmPlaylist.Length)];
                if (clip != null)
                {
                    bgmSource.clip = clip;
                    bgmSource.loop = false;
                    bgmSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }

                yield return new WaitForSeconds(RandomInterval());
            }
        }

        private float RandomInterval()
        {
            return Random.Range(bgmIntervalRange.x, bgmIntervalRange.y);
        }
    }
}
