using TinyFishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyFishing.UI
{
    /// <summary>
    /// Start-menu presenter for choosing an input route and entering the fishing scene.
    /// Gyro mode starts on a physical swing; touch mode exposes a regular start button.
    /// </summary>
    public sealed class TinyFishingStartMenu : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Pond FPV";

        [Header("Controls")]
        [SerializeField] private Button gyroButton;
        [SerializeField] private Button touchButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button recalibrateButton;

        [Header("Presentation")]
        [SerializeField] private GameObject gyroPanel;
        [SerializeField] private Text selectedModeText;
        [SerializeField] private Text sensorStatusText;
        [SerializeField] private Image gyroButtonImage;
        [SerializeField] private Image touchButtonImage;
        [SerializeField] private Color selectedColor = new(0.16f, 0.62f, 0.82f, 1f);
        [SerializeField] private Color unselectedColor = new(0.16f, 0.21f, 0.27f, 1f);

        [Header("Text (editable)")]
        [Tooltip("Shown in sensorStatusText once the gyro is calibrated and armed.")]
        [SerializeField] private string gyroReadyMessage = "자이로 준비 완료 - 흔들어서 시작하세요";
        [Tooltip("Shown in sensorStatusText when motion sensors aren't available on this device.")]
        [SerializeField] private string sensorUnavailableMessage = "동작 센서를 사용할 수 없습니다\n(에디터: Enter 키를 눌러 테스트)";
        [Tooltip("Shown in selectedModeText when Gyro mode is selected.")]
        [SerializeField] private string gyroModeLabel = "자이로";
        [Tooltip("Shown in selectedModeText when Touch Screen mode is selected.")]
        [SerializeField] private string touchModeLabel = "터치스크린";

        [Header("Swing Detection")]
        [SerializeField, Min(0.1f)] private float swingThreshold = 4.72f;
        [SerializeField, Range(0.01f, 1f)] private float sensorSmoothing = 0.25f;
        [SerializeField, Min(0f)] private float armingDelay = 0.75f;

        private TinyFishingInputState selectedState;
        private Vector3 filteredAcceleration = Vector3.up;
        private Vector3 previousFilteredAcceleration = Vector3.up;
        private float armedAt;
        private bool isLoading;
        private bool sensorsReady;

        private void Awake()
        {
            gyroButton?.onClick.AddListener(SelectGyro);
            touchButton?.onClick.AddListener(SelectTouchScreen);
            startButton?.onClick.AddListener(StartGame);
            recalibrateButton?.onClick.AddListener(RecalibrateGyro);

            ApplySelection(TinyFishingInputPreferences.Load());
        }

        private void OnDestroy()
        {
            gyroButton?.onClick.RemoveListener(SelectGyro);
            touchButton?.onClick.RemoveListener(SelectTouchScreen);
            startButton?.onClick.RemoveListener(StartGame);
            recalibrateButton?.onClick.RemoveListener(RecalibrateGyro);
        }

        private void Update()
        {
            if (isLoading || selectedState != TinyFishingInputState.Gyro)
            {
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartGame();
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
                StartGame();
            }
        }

        public void SelectGyro()
        {
            ApplySelection(TinyFishingInputState.Gyro);
        }

        public void SelectTouchScreen()
        {
            ApplySelection(TinyFishingInputState.TouchScreen);
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
                    ? gyroReadyMessage
                    : sensorUnavailableMessage;
            }
        }

        public void StartGame()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            TinyFishingInputPreferences.Save(selectedState);
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void ApplySelection(TinyFishingInputState state)
        {
            selectedState = state;
            TinyFishingInputPreferences.Save(state);

            var gyroSelected = state == TinyFishingInputState.Gyro;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(!gyroSelected);
            }
            gyroPanel?.SetActive(gyroSelected);

            if (selectedModeText != null)
            {
                selectedModeText.text = gyroSelected ? gyroModeLabel : touchModeLabel;
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
            Button gyro,
            Button touch,
            Button start,
            Button recalibrate,
            GameObject gyroInstructions,
            Text modeLabel,
            Text statusLabel)
        {
            gyroButton = gyro;
            touchButton = touch;
            startButton = start;
            recalibrateButton = recalibrate;
            gyroPanel = gyroInstructions;
            selectedModeText = modeLabel;
            sensorStatusText = statusLabel;
            gyroButtonImage = gyro != null ? gyro.image : null;
            touchButtonImage = touch != null ? touch.image : null;
        }
#endif
    }
}
