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

        private TextMeshProUGUI timerText;
        private GameObject resultPanel;
        private TextMeshProUGUI finalScoreText;
        private TextMeshProUGUI bestScoreText;
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

        private void HandleFinished(int score, int bestScore)
        {
            if (timerText != null)
            {
                timerText.text = "00:00";
            }

            finalScoreText.text = $"게임 기록  {score}";
            bestScoreText.text = $"최고 점수  {bestScore}";
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

            resultPanel = new GameObject("TimeAttackResult", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(targetCanvas.transform, false);
            var panelRect = (RectTransform)resultPanel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = resultPanel.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.05f, 0.08f, 0.9f);
            panelImage.raycastTarget = true;

            var title = CreateText("Title", panelRect, 64f, FontStyles.Bold);
            title.text = "타임 오버!";
            ConfigureRect(title.rectTransform,
                new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f),
                Vector2.zero, new Vector2(600f, 90f));

            finalScoreText = CreateText("FinalScore", panelRect, 44f, FontStyles.Bold);
            ConfigureRect(finalScoreText.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 70f));

            bestScoreText = CreateText("BestScore", panelRect, 36f, FontStyles.Normal);
            bestScoreText.color = new Color(1f, 0.82f, 0.25f);
            ConfigureRect(bestScoreText.rectTransform,
                new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
                Vector2.zero, new Vector2(700f, 60f));

            var returnText = CreateText("ReturnNotice", panelRect, 25f, FontStyles.Normal);
            returnText.text = "잠시 후 시작 화면으로 돌아갑니다";
            returnText.color = new Color(0.8f, 0.85f, 0.9f);
            ConfigureRect(returnText.rectTransform,
                new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f),
                Vector2.zero, new Vector2(700f, 50f));

            resultPanel.SetActive(false);
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
