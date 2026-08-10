using TinyFishing.Audio;
using TinyFishing.Core;
using TinyFishing.Input;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TinyFishing.UI
{
    /// <summary>
    /// Presenter for the hand-authored start-menu UI.
    /// StartUI owns the initial full-screen click; this component owns the menu shown afterwards.
    /// </summary>
    public sealed class TinyFishingStartMenu : MonoBehaviour
    {
        [Header("Scenes")]
        [FormerlySerializedAs("infiniteGameplaySceneName")]
        [SerializeField] private string gameplaySceneName = "Pond Casual";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject howToPanel;

        [Header("Main Menu")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button infiniteModeButton;
        [SerializeField] private Button timeLimitedModeButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Image infiniteModeButtonImage;
        [SerializeField] private Image timeLimitedModeButtonImage;

        [Header("Input Settings")]
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button gyroButton;
        [SerializeField] private Button touchButton;
        [SerializeField] private Button recalibrateButton;
        [SerializeField] private Image gyroButtonImage;
        [SerializeField] private Image touchButtonImage;

        [Header("How To")]
        [SerializeField] private Button closeHowToButton;

        [Header("Audio Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Selection Colors")]
        [SerializeField] private Color selectedTint = Color.white;
        [SerializeField] private Color unselectedTint = new(0.78f, 0.78f, 0.78f, 1f);

        private TinyFishingInputState selectedInputState;
        private TinyFishingGameMode selectedGameMode;
        private bool isLoading;

        private void Awake()
        {
            settingsButton?.onClick.AddListener(OpenSettings);
            howToButton?.onClick.AddListener(OpenHowTo);
            closeSettingsButton?.onClick.AddListener(CloseSettings);
            closeHowToButton?.onClick.AddListener(CloseHowTo);
            infiniteModeButton?.onClick.AddListener(SelectInfiniteMode);
            timeLimitedModeButton?.onClick.AddListener(SelectTimeLimitedMode);
            gyroButton?.onClick.AddListener(SelectGyro);
            touchButton?.onClick.AddListener(SelectTouchScreen);
            startButton?.onClick.AddListener(StartSelectedMode);
            recalibrateButton?.onClick.AddListener(RecalibrateGyro);

            InitializeAudioSettings();
            ApplyInputSelection(TinyFishingInputPreferences.Load());
            ApplyGameModeSelection(TinyFishingGameModePreferences.Load());
        }

        private void OnDestroy()
        {
            settingsButton?.onClick.RemoveListener(OpenSettings);
            howToButton?.onClick.RemoveListener(OpenHowTo);
            closeSettingsButton?.onClick.RemoveListener(CloseSettings);
            closeHowToButton?.onClick.RemoveListener(CloseHowTo);
            infiniteModeButton?.onClick.RemoveListener(SelectInfiniteMode);
            timeLimitedModeButton?.onClick.RemoveListener(SelectTimeLimitedMode);
            gyroButton?.onClick.RemoveListener(SelectGyro);
            touchButton?.onClick.RemoveListener(SelectTouchScreen);
            startButton?.onClick.RemoveListener(StartSelectedMode);
            recalibrateButton?.onClick.RemoveListener(RecalibrateGyro);
            bgmVolumeSlider?.onValueChanged.RemoveListener(SetBgmVolume);
            sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);
            TinyFishingAudioPreferences.Flush();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                TinyFishingAudioPreferences.Flush();
            }
        }

        public void OpenSettings()
        {
            mainPanel?.SetActive(false);
            howToPanel?.SetActive(false);
            settingsPanel?.SetActive(true);
        }

        public void CloseSettings()
        {
            TinyFishingAudioPreferences.Flush();
            if (selectedInputState == TinyFishingInputState.Gyro)
            {
                RecalibrateGyro();
            }

            ShowMainPanel();
        }

        public void OpenHowTo()
        {
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            howToPanel?.SetActive(true);
        }

        public void CloseHowTo()
        {
            ShowMainPanel();
        }

        public void SelectInfiniteMode()
        {
            ApplyGameModeSelection(TinyFishingGameMode.Infinite);
        }

        public void SelectTimeLimitedMode()
        {
            ApplyGameModeSelection(TinyFishingGameMode.TimeLimited);
        }

        public void SelectGyro()
        {
            ApplyInputSelection(TinyFishingInputState.Gyro);
        }

        public void SelectTouchScreen()
        {
            ApplyInputSelection(TinyFishingInputState.TouchScreen);
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

        public void StartSelectedMode()
        {
            if (isLoading)
            {
                return;
            }

            TinyFishingInputPreferences.Save(selectedInputState);
            TinyFishingGameModePreferences.Save(selectedGameMode);
            TinyFishingAudioPreferences.Flush();

            isLoading = true;
            SceneManager.LoadScene(gameplaySceneName);
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

        private void ApplyGameModeSelection(TinyFishingGameMode mode)
        {
            selectedGameMode = mode;
            TinyFishingGameModePreferences.Save(mode);

            var infiniteSelected = mode == TinyFishingGameMode.Infinite;
            if (infiniteModeButtonImage != null)
            {
                infiniteModeButtonImage.color = infiniteSelected ? selectedTint : unselectedTint;
            }

            if (timeLimitedModeButtonImage != null)
            {
                timeLimitedModeButtonImage.color = infiniteSelected ? unselectedTint : selectedTint;
            }
        }

        private void ApplyInputSelection(TinyFishingInputState state)
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

            if (gyroSelected)
            {
                RecalibrateGyro();
            }
        }

        private void ShowMainPanel()
        {
            settingsPanel?.SetActive(false);
            howToPanel?.SetActive(false);
            mainPanel?.SetActive(true);
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

#if UNITY_EDITOR
        public void Configure(
            GameObject main,
            GameObject settings,
            GameObject howTo,
            Button openSettings,
            Button openHowTo,
            Button closeSettings,
            Button closeHowTo,
            Button infinite,
            Button timeLimited,
            Button start,
            Button gyro,
            Button touch,
            Button recalibrate,
            Slider bgmSlider,
            Slider sfxSlider,
            AudioMixer mixer)
        {
            mainPanel = main;
            settingsPanel = settings;
            howToPanel = howTo;
            settingsButton = openSettings;
            howToButton = openHowTo;
            closeSettingsButton = closeSettings;
            closeHowToButton = closeHowTo;
            infiniteModeButton = infinite;
            timeLimitedModeButton = timeLimited;
            startButton = start;
            infiniteModeButtonImage = infinite != null ? infinite.image : null;
            timeLimitedModeButtonImage = timeLimited != null ? timeLimited.image : null;
            gyroButton = gyro;
            touchButton = touch;
            recalibrateButton = recalibrate;
            gyroButtonImage = gyro != null ? gyro.image : null;
            touchButtonImage = touch != null ? touch.image : null;
            bgmVolumeSlider = bgmSlider;
            sfxVolumeSlider = sfxSlider;
            audioMixer = mixer;
        }
#endif
    }
}
