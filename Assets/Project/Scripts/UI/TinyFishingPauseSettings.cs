using TinyFishing.Audio;
using TinyFishing.Core;
using TinyFishing.Input;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TinyFishing.UI
{
    /// <summary>
    /// In-game pause/settings panel. Reuses the same persisted preferences as the
    /// start-menu settings (TinyFishingAudioPreferences / TinyFishingInputPreferences),
    /// but additionally pauses/resumes the active TinyFishingGameManager session.
    /// </summary>
    public sealed class TinyFishingPauseSettings : MonoBehaviour
    {
        [Header("Game")]
        [SerializeField] private TinyFishingGameManager gameManager;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("Input Settings")]
        [SerializeField] private Button gyroButton;
        [SerializeField] private Button touchButton;
        [SerializeField] private Button recalibrateButton;
        [SerializeField] private Image gyroButtonImage;
        [SerializeField] private Image touchButtonImage;

        [Header("Audio Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Selection Colors")]
        [SerializeField] private Color selectedTint = Color.white;
        [SerializeField] private Color unselectedTint = new(0.78f, 0.78f, 0.78f, 1f);

        private TinyFishingInputState selectedInputState;

        private void Awake()
        {
            closeButton?.onClick.AddListener(ClosePanel);
            gyroButton?.onClick.AddListener(SelectGyro);
            touchButton?.onClick.AddListener(SelectTouchScreen);
            recalibrateButton?.onClick.AddListener(RecalibrateGyro);

            InitializeAudioSettings();
            ApplyInputSelection(TinyFishingInputPreferences.Load(), recalibrate: false);
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(ClosePanel);
            gyroButton?.onClick.RemoveListener(SelectGyro);
            touchButton?.onClick.RemoveListener(SelectTouchScreen);
            recalibrateButton?.onClick.RemoveListener(RecalibrateGyro);
            bgmVolumeSlider?.onValueChanged.RemoveListener(SetBgmVolume);
            sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);
            TinyFishingAudioPreferences.Flush();
        }

        public void OpenPanel()
        {
            gameManager?.PauseGame();
            (panelRoot != null ? panelRoot : gameObject).SetActive(true);
        }

        public void ClosePanel()
        {
            TinyFishingAudioPreferences.Flush();
            if (selectedInputState == TinyFishingInputState.Gyro)
            {
                RecalibrateGyro();
            }

            (panelRoot != null ? panelRoot : gameObject).SetActive(false);
            gameManager?.ResumeGame();
        }

        public void SelectGyro()
        {
            ApplyInputSelection(TinyFishingInputState.Gyro, recalibrate: true);
        }

        public void SelectTouchScreen()
        {
            ApplyInputSelection(TinyFishingInputState.TouchScreen, recalibrate: false);
        }

        public void RecalibrateGyro()
        {
            EnableMotionSensors();
        }

        public void SetBgmVolume(float normalizedVolume)
        {
            TinyFishingAudioPreferences.SetBgmVolume(audioMixer, normalizedVolume);
        }

        public void SetSfxVolume(float normalizedVolume)
        {
            TinyFishingAudioPreferences.SetSfxVolume(audioMixer, normalizedVolume);
        }

        private void InitializeAudioSettings()
        {
            var bgmVolume = TinyFishingAudioPreferences.LoadBgmVolume();
            var sfxVolume = TinyFishingAudioPreferences.LoadSfxVolume();

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);
                bgmVolumeSlider.onValueChanged.AddListener(SetBgmVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
                sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
            }

            TinyFishingAudioPreferences.Apply(audioMixer, bgmVolume, sfxVolume);
        }

        private void ApplyInputSelection(TinyFishingInputState state, bool recalibrate)
        {
            selectedInputState = state;
            TinyFishingInputPreferences.Save(state);

            var gyroSelected = state == TinyFishingInputState.Gyro;
            if (gyroButtonImage != null)
            {
                gyroButtonImage.color = gyroSelected ? selectedTint : unselectedTint;
            }

            if (touchButtonImage != null)
            {
                touchButtonImage.color = gyroSelected ? unselectedTint : selectedTint;
            }

            if (gyroSelected && recalibrate)
            {
                RecalibrateGyro();
            }
        }

        private static bool EnableMotionSensors()
        {
            var attitude = AttitudeSensor.current;
            var accelerometer = Accelerometer.current;
            if (attitude == null || accelerometer == null)
            {
                return false;
            }

            if (!attitude.enabled)
            {
                InputSystem.EnableDevice(attitude);
            }

            if (!accelerometer.enabled)
            {
                InputSystem.EnableDevice(accelerometer);
            }

            return true;
        }
    }
}
