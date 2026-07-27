using System;
using NanFishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NanFishing.UI
{
    public sealed class FishingHUD : MonoBehaviour
    {
        public event Action RecalibrateRequested;
        public event Action RestartRequested;

        [Header("State Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Start")]
        [SerializeField] private Text startInstruction;

        [Header("Gameplay")]
        [SerializeField] private Text instruction;
        [SerializeField] private Text timer;
        [SerializeField] private Text score;
        [SerializeField] private Text combo;
        [SerializeField] private Text feedback;
        [SerializeField] private Image tensionFill;
        [SerializeField] private Text tensionStatus;
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform fishMarker;
        [SerializeField] private RectTransform playerTiltMarker;
        [SerializeField] private RectTransform phoneTiltVisual;
        [SerializeField] private Button recalibrateButton;

        [Header("Result")]
        [SerializeField] private Text result;
        [SerializeField] private Button restartButton;

        private bool listenersBound;

        public void ConfigureScene(GameObject start, GameObject gameplay, GameObject resultState,
            Text calibrationText, Text gameplayInstruction, Text timerText, Text scoreText,
            Text comboText, Text feedbackText, Image tension, Text tensionText, Image progress,
            RectTransform fishDirection, RectTransform playerDirection, RectTransform phoneVisual,
            Button recalibrate, Text resultText, Button restart)
        {
            startPanel = start;
            gameplayPanel = gameplay;
            resultPanel = resultState;
            startInstruction = calibrationText;
            instruction = gameplayInstruction;
            timer = timerText;
            score = scoreText;
            combo = comboText;
            feedback = feedbackText;
            tensionFill = tension;
            tensionStatus = tensionText;
            progressFill = progress;
            fishMarker = fishDirection;
            playerTiltMarker = playerDirection;
            phoneTiltVisual = phoneVisual;
            recalibrateButton = recalibrate;
            result = resultText;
            restartButton = restart;
            BindButtons();
        }

        private void Awake()
        {
            BindButtons();
        }

        public void ShowStart(bool hasMotion)
        {
            SetPanel(startPanel);
            startInstruction.text = hasMotion ? "Hold still to calibrate" : "Swipe up to cast";
            timer.text = "60";
            score.text = "SCORE 0";
            combo.text = string.Empty;
            SetBar(tensionFill, 0f);
            SetBar(progressFill, 0f);
        }

        public void SetCalibration(float progress, bool calibrated)
        {
            startInstruction.text = calibrated
                ? "Swing back, then cast forward!\n(Editor: press ENTER)"
                : $"Hold still... {Mathf.RoundToInt(progress * 100f)}%";
        }

        public void ShowGameplay()
        {
            SetPanel(gameplayPanel);
            instruction.text = "Casting...";
        }

        public void SetState(GameState state)
        {
            switch (state)
            {
                case GameState.Casting:
                    instruction.text = "CAST!";
                    feedback.text = string.Empty;
                    break;
                case GameState.WaitingForBite:
                    instruction.text = "Wait for it...";
                    break;
                case GameState.Reeling:
                    instruction.text = "HOLD to reel  •  TILT to match the fish";
                    feedback.text = "BITE!";
                    break;
                case GameState.SessionResult:
                    instruction.text = string.Empty;
                    break;
            }
        }

        public void SetSession(float timeRemaining, int currentScore, int catches, int currentCombo)
        {
            timer.text = Mathf.CeilToInt(timeRemaining).ToString("00");
            score.text = $"SCORE {currentScore}\nFISH {catches}";
            combo.text = currentCombo > 1 ? $"x{currentCombo} COMBO" : string.Empty;
        }

        public void SetReeling(float tension, float progress, float fishDirection,
            float playerDirection, bool isReeling)
        {
            SetBar(tensionFill, tension);
            tensionFill.color = Color.Lerp(new Color(0.25f, 0.9f, 0.45f),
                new Color(1f, 0.12f, 0.08f), tension);
            SetBar(progressFill, progress);

            fishMarker.anchoredPosition = new Vector2(fishDirection * 260f, 0f);
            playerTiltMarker.anchoredPosition = new Vector2(playerDirection * 260f, 0f);
            phoneTiltVisual.localRotation = Quaternion.Euler(0f, 0f, -playerDirection * 25f);
            phoneTiltVisual.localScale = isReeling ? Vector3.one * 1.08f : Vector3.one;

            var percent = Mathf.RoundToInt(tension * 100f);
            tensionStatus.text = tension switch
            {
                >= 0.82f => $"DANGER!  {percent}%",
                >= 0.62f => $"HIGH  {percent}%  •  RELEASE",
                >= 0.35f => $"PULLING  {percent}%",
                _ => $"SAFE  {percent}%"
            };
        }

        public void ShowCatch(string fishName, int awarded, Color color)
        {
            feedback.color = color;
            feedback.text = $"{fishName.ToUpperInvariant()}\n+{awarded}";
        }

        public void ShowFailure()
        {
            feedback.color = new Color(1f, 0.35f, 0.25f);
            feedback.text = "LINE BROKE!";
        }

        public void ShowResult(int finalScore, int bestScore, int catches, int maxCombo,
            int rarity, int discovered, int totalFish)
        {
            SetPanel(resultPanel);
            var rarityName = rarity switch { 2 => "RARE", 1 => "UNCOMMON", _ => "COMMON" };
            result.text = $"TIME!\n\nSCORE  {finalScore}\nBEST  {bestScore}\n\n" +
                          $"FISH  {catches}\nMAX COMBO  x{maxCombo}\nBEST CATCH  {rarityName}\n" +
                          $"COLLECTION  {discovered}/{totalFish}";
        }

        private void SetPanel(GameObject activePanel)
        {
            startPanel.SetActive(activePanel == startPanel);
            gameplayPanel.SetActive(activePanel == gameplayPanel);
            resultPanel.SetActive(activePanel == resultPanel);
        }

        private void BindButtons()
        {
            if (listenersBound || recalibrateButton == null || restartButton == null)
            {
                return;
            }
            recalibrateButton.onClick.AddListener(() => RecalibrateRequested?.Invoke());
            restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            listenersBound = true;
        }

        private static void SetBar(Image image, float value)
        {
            image.fillAmount = Mathf.Clamp01(value);
        }
    }
}
