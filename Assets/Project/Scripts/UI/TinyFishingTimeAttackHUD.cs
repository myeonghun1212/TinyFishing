using System.Collections.Generic;
using NanFishing.Data;
using TinyFishing.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFishing.UI
{
    /// <summary>
    /// Time-attack-only presentation. It builds a small optional overlay at runtime so
    /// the existing Pond Casual HUD stays reusable and unchanged for infinite mode.
    /// </summary>
    public sealed class TinyFishingTimeAttackHUD : MonoBehaviour
    {
        [SerializeField] private TinyFishingTimeAttackController controller;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject BestScoreAlert;

        private TextMeshProUGUI timerText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI sessionCatchText;
        private TMP_FontAsset presentationFont;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<TinyFishingTimeAttackController>();
            }
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                return;
            }

            controller.TimeChanged += HandleTimeChanged;
            controller.Finished += HandleFinished;
        }

        private void OnDisable()
        {
            if (controller == null)
            {
                return;
            }

            controller.TimeChanged -= HandleTimeChanged;
            controller.Finished -= HandleFinished;
        }

        private void Start()
        {
            if (!controller.IsTimeAttack)
            {
                enabled = false;
                return;
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas == null)
            {
                Debug.LogError("TinyFishingTimeAttackHUD: no Canvas was found.");
                enabled = false;
                return;
            }

            BuildPresentation();
            HandleTimeChanged(controller.RemainingSeconds);
        }

        private void HandleTimeChanged(float seconds)
        {
            if (timerText == null)
            {
                return;
            }

            var wholeSeconds = Mathf.CeilToInt(seconds);
            timerText.text = $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
            timerText.color = wholeSeconds <= 10
                ? new Color(1f, 0.3f, 0.25f)
                : Color.white;
        }

        private void HandleFinished(int score, int bestScore, Dictionary<FishDefinition, int> sessionCatchCounts)
        {
            if (timerText != null)
            {
                timerText.text = "00:00";
            }

            if(score >= bestScore)
            {
                BestScoreAlert.SetActive(true);
            }

            foreach (var kvp in sessionCatchCounts)
            {
                var fish = kvp.Key;
                var count = kvp.Value;
                if(fish == null || count <= 0) continue;
                sessionCatchText.text += $"{fish.DisplayName} : {count}x\n";
            }

            finalScoreText.text = $"점수 : {score}";

            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        private void BuildPresentation()
        {
            var existingText = targetCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
            presentationFont = existingText != null ? existingText.font : null;

            timerText = CreateText("TimeAttackTimer", targetCanvas.transform, 52f, FontStyles.Bold);
            ConfigureRect(timerText.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(260f, 70f));
        }

        private TextMeshProUGUI CreateText(
            string objectName, Transform parent, float fontSize, FontStyles fontStyle)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            if (presentationFont != null)
            {
                text.font = presentationFont;
            }
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureRect(
            RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
