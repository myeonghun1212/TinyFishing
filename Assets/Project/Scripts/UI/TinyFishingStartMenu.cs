using TinyFishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyFishing.UI
{
    /// <summary>
    /// Start-menu presenter. Game mode is selected on the main panel, while the shared
    /// Gyro/Touch input preference is edited from a separate settings panel.
    /// </summary>
    public sealed class TinyFishingStartMenu : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string infiniteGameplaySceneName = "Pond Casual";
        [SerializeField] private string timeLimitedGameplaySceneName = "Pond Timed";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Menu")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button infiniteModeButton;
        [SerializeField] private Button timeLimitedModeButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Text selectedGameModeText;
        [SerializeField] private Text gameModeStatusText;
        [SerializeField] private Image infiniteModeButtonImage;
        [SerializeField] private Image timeLimitedModeButtonImage;

        [Header("Input Settings")]
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button gyroButton;
        [SerializeField] private Button touchButton;
        [SerializeField] private Button recalibrateButton;
        [SerializeField] private GameObject gyroPanel;
        [SerializeField] private Text selectedInputText;
        [SerializeField] private Text sensorStatusText;
        [SerializeField] private Image gyroButtonImage;
        [SerializeField] private Image touchButtonImage;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = new(0.16f, 0.62f, 0.82f, 1f);
        [SerializeField] private Color unselectedColor = new(0.16f, 0.21f, 0.27f, 1f);
        [SerializeField] private Color comingSoonColor = new(0.39f, 0.34f, 0.18f, 1f);

        [Header("Swing Detection")]
        [SerializeField, Min(0.1f)] private float swingThreshold = 4.72f;
        [SerializeField, Range(0.01f, 1f)] private float sensorSmoothing = 0.25f;
        [SerializeField, Min(0f)] private float armingDelay = 0.75f;

        private TinyFishingInputState selectedInputState;
        private TinyFishingGameMode selectedGameMode;
        private Vector3 filteredAcceleration = Vector3.up;
        private Vector3 previousFilteredAcceleration = Vector3.up;
        private float armedAt;
        private bool isLoading;
        private bool settingsOpen;
        private bool sensorsReady;

        private void Awake()
        {
            settingsButton?.onClick.AddListener(OpenSettings);
            closeSettingsButton?.onClick.AddListener(CloseSettings);
            infiniteModeButton?.onClick.AddListener(SelectInfiniteMode);
            timeLimitedModeButton?.onClick.AddListener(SelectTimeLimitedMode);
            gyroButton?.onClick.AddListener(SelectGyro);
            touchButton?.onClick.AddListener(SelectTouchScreen);
            startButton?.onClick.AddListener(StartSelectedMode);
            recalibrateButton?.onClick.AddListener(RecalibrateGyro);

            ApplyInputSelection(TinyFishingInputPreferences.Load());
            ApplyGameModeSelection(TinyFishingGameModePreferences.Load());
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            settingsButton?.onClick.RemoveListener(OpenSettings);
            closeSettingsButton?.onClick.RemoveListener(CloseSettings);
            infiniteModeButton?.onClick.RemoveListener(SelectInfiniteMode);
            timeLimitedModeButton?.onClick.RemoveListener(SelectTimeLimitedMode);
            gyroButton?.onClick.RemoveListener(SelectGyro);
            touchButton?.onClick.RemoveListener(SelectTouchScreen);
            startButton?.onClick.RemoveListener(StartSelectedMode);
            recalibrateButton?.onClick.RemoveListener(RecalibrateGyro);
        }

        private void Update()
        {
            if (isLoading || settingsOpen || selectedInputState != TinyFishingInputState.Gyro)
            {
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartSelectedMode();
                return;
            }
#endif

            if (!sensorsReady || Time.unscaledTime < armedAt || Accelerometer.current == null)
            {
                return;
            }

            previousFilteredAcceleration = filteredAcceleration;
            filteredAcceleration = Vector3.Lerp(
                filteredAcceleration,
                Accelerometer.current.acceleration.ReadValue(),
                sensorSmoothing);

            var deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            var jerk = (filteredAcceleration - previousFilteredAcceleration).magnitude / deltaTime;
            if (jerk >= swingThreshold)
            {
                StartSelectedMode();
            }
        }

        public void OpenSettings()
        {
            settingsOpen = true;
            mainPanel?.SetActive(false);
            settingsPanel?.SetActive(true);
        }

        public void CloseSettings()
        {
            ShowMainPanel();
            if (selectedInputState == TinyFishingInputState.Gyro)
            {
                RecalibrateGyro();
            }
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
            sensorsReady = EnableMotionSensors();
            filteredAcceleration = Accelerometer.current != null
                ? Accelerometer.current.acceleration.ReadValue()
                : Vector3.up;
            previousFilteredAcceleration = filteredAcceleration;
            armedAt = Time.unscaledTime + armingDelay;

            if (sensorStatusText != null)
            {
                sensorStatusText.text = sensorsReady
                    ? "Gyro ready - swing the phone to start"
                    : "Motion sensor unavailable\n(Editor: press Enter to test)";
            }
        }

        public void StartSelectedMode()
        {
            if (isLoading)
            {
                return;
            }

            TinyFishingInputPreferences.Save(selectedInputState);
            TinyFishingGameModePreferences.Save(selectedGameMode);

            if (selectedGameMode == TinyFishingGameMode.TimeLimited)
            {
                if (gameModeStatusText != null)
                {
                    gameModeStatusText.text = "TIME LIMITED mode saved - coming soon";
                }

                Debug.Log("Time Limited mode was selected and saved. Scene loading is not implemented yet.");

                // TODO: Enable this after the time-limited gameplay scene is implemented.
                // SceneManager.LoadScene(timeLimitedGameplaySceneName);
                return;
            }

            isLoading = true;
            SceneManager.LoadScene(infiniteGameplaySceneName);
        }

        private void ApplyGameModeSelection(TinyFishingGameMode mode)
        {
            selectedGameMode = mode;
            TinyFishingGameModePreferences.Save(mode);

            var infiniteSelected = mode == TinyFishingGameMode.Infinite;
            if (selectedGameModeText != null)
            {
                selectedGameModeText.text = infiniteSelected ? "INFINITE MODE" : "TIME LIMITED MODE";
            }
            if (gameModeStatusText != null)
            {
                gameModeStatusText.text = infiniteSelected
                    ? "Play without a time limit"
                    : "Coming soon - your selection is saved";
            }
            if (infiniteModeButtonImage != null)
            {
                infiniteModeButtonImage.color = infiniteSelected ? selectedColor : unselectedColor;
            }
            if (timeLimitedModeButtonImage != null)
            {
                timeLimitedModeButtonImage.color = infiniteSelected ? comingSoonColor : selectedColor;
            }
        }

        private void ApplyInputSelection(TinyFishingInputState state)
        {
            selectedInputState = state;
            TinyFishingInputPreferences.Save(state);

            var gyroSelected = state == TinyFishingInputState.Gyro;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(!gyroSelected);
            }
            gyroPanel?.SetActive(gyroSelected);

            if (selectedInputText != null)
            {
                selectedInputText.text = gyroSelected ? "GYRO" : "TOUCH SCREEN";
            }
            if (gyroButtonImage != null)
            {
                gyroButtonImage.color = gyroSelected ? selectedColor : unselectedColor;
            }
            if (touchButtonImage != null)
            {
                touchButtonImage.color = gyroSelected ? unselectedColor : selectedColor;
            }

            if (gyroSelected)
            {
                RecalibrateGyro();
            }
            else
            {
                sensorsReady = false;
            }
        }

        private void ShowMainPanel()
        {
            settingsOpen = false;
            settingsPanel?.SetActive(false);
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
            Button openSettings,
            Button closeSettings,
            Button infinite,
            Button timeLimited,
            Button start,
            Text selectedGameMode,
            Text gameModeStatus,
            Button gyro,
            Button touch,
            Button recalibrate,
            GameObject gyroInstructions,
            Text selectedInput,
            Text sensorStatus)
        {
            mainPanel = main;
            settingsPanel = settings;
            settingsButton = openSettings;
            closeSettingsButton = closeSettings;
            infiniteModeButton = infinite;
            timeLimitedModeButton = timeLimited;
            startButton = start;
            selectedGameModeText = selectedGameMode;
            gameModeStatusText = gameModeStatus;
            infiniteModeButtonImage = infinite != null ? infinite.image : null;
            timeLimitedModeButtonImage = timeLimited != null ? timeLimited.image : null;
            gyroButton = gyro;
            touchButton = touch;
            recalibrateButton = recalibrate;
            gyroPanel = gyroInstructions;
            selectedInputText = selectedInput;
            sensorStatusText = sensorStatus;
            gyroButtonImage = gyro != null ? gyro.image : null;
            touchButtonImage = touch != null ? touch.image : null;
        }
#endif
    }
}
