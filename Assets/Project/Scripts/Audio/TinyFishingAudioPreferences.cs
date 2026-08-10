using UnityEngine;
using UnityEngine.Audio;

namespace TinyFishing.Audio
{
    /// <summary>
    /// Persists normalized menu volume values and applies them to exposed mixer parameters.
    /// </summary>
    public static class TinyFishingAudioPreferences
    {
        public const string BgmParameter = "BGMVolume";
        public const string SfxParameter = "SFXVolume";

        private const string BgmKey = "TinyFishing.Audio.BgmVolume";
        private const string SfxKey = "TinyFishing.Audio.SfxVolume";
        private const float DefaultVolume = 1f;
        private const float MinimumDecibels = -80f;

        public static float LoadBgmVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, DefaultVolume));
        }

        public static float LoadSfxVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultVolume));
        }

        public static void ApplySaved(AudioMixer mixer)
        {
            Apply(mixer, LoadBgmVolume(), LoadSfxVolume());
        }

        public static void Apply(AudioMixer mixer, float bgmVolume, float sfxVolume)
        {
            ApplyParameter(mixer, BgmParameter, bgmVolume);
            ApplyParameter(mixer, SfxParameter, sfxVolume);
        }

        public static void SetBgmVolume(AudioMixer mixer, float normalizedVolume)
        {
            var value = Mathf.Clamp01(normalizedVolume);
            PlayerPrefs.SetFloat(BgmKey, value);
            ApplyParameter(mixer, BgmParameter, value);
        }

        public static void SetSfxVolume(AudioMixer mixer, float normalizedVolume)
        {
            var value = Mathf.Clamp01(normalizedVolume);
            PlayerPrefs.SetFloat(SfxKey, value);
            ApplyParameter(mixer, SfxParameter, value);
        }

        public static void Flush()
        {
            PlayerPrefs.Save();
        }

        public static float ToDecibels(float normalizedVolume)
        {
            var value = Mathf.Clamp01(normalizedVolume);
            return value <= 0.0001f
                ? MinimumDecibels
                : Mathf.Max(MinimumDecibels, 20f * Mathf.Log10(value));
        }

        private static void ApplyParameter(AudioMixer mixer, string parameter, float normalizedVolume)
        {
            if (mixer != null)
            {
                mixer.SetFloat(parameter, ToDecibels(normalizedVolume));
            }
        }
    }
}
