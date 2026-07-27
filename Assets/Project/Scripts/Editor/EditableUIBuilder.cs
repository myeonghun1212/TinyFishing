using NanFishing.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NanFishing.Editor
{
    public static class EditableUIBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/SampleScene.unity";

        [MenuItem("Tools/NAN Fishing/Build Scene UI")]
        public static void BuildSceneUI()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var oldHud = GameObject.Find("FishingHUD");
            if (oldHud != null)
            {
                Object.DestroyImmediate(oldHud);
            }

            var canvasObject = new GameObject("FishingHUD", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(FishingHUD));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var startPanel = Panel("StartPanel", canvasObject.transform,
                new Color(0.02f, 0.12f, 0.18f, 0.42f));
            Label("Title", startPanel.transform, "TINY FISHING", 92, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.9f));
            var startInstruction = Label("StartInstruction", startPanel.transform,
                "Hold still to calibrate", 44, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.7f));
            Label("StartHint", startPanel.transform, "SWING BACK  >  CAST FORWARD", 30,
                TextAnchor.MiddleCenter, new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.5f));

            var gameplayPanel = Panel("GameplayPanel", canvasObject.transform, Color.clear);
            var instruction = Label("Instruction", gameplayPanel.transform, "CAST!", 42,
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.79f));
            var timer = Label("Timer", gameplayPanel.transform, "60", 72, TextAnchor.UpperCenter,
                new Vector2(0.38f, 0.9f), new Vector2(0.62f, 0.99f));
            var score = Label("Score", gameplayPanel.transform, "SCORE 0", 34,
                TextAnchor.UpperLeft, new Vector2(0.04f, 0.88f), new Vector2(0.38f, 0.98f));
            var combo = Label("Combo", gameplayPanel.transform, string.Empty, 40,
                TextAnchor.UpperRight, new Vector2(0.64f, 0.89f), new Vector2(0.96f, 0.98f));
            var feedback = Label("Feedback", gameplayPanel.transform, string.Empty, 64,
                TextAnchor.MiddleCenter, new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.68f));

            Label("TiltTitle", gameplayPanel.transform, "MATCH YOUR TILT TO THE FISH", 26,
                TextAnchor.MiddleCenter, new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.43f));
            var phone = Bar("PhoneTiltVisual", gameplayPanel.transform,
                new Vector2(0.43f, 0.29f), new Vector2(0.57f, 0.38f),
                new Color(0.08f, 0.12f, 0.17f, 0.92f)).rectTransform;
            Label("PhoneLabel", phone, "YOU", 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one).color = new Color(0.25f, 0.9f, 1f);

            var directionTrack = Bar("TiltTrack", gameplayPanel.transform,
                new Vector2(0.18f, 0.21f), new Vector2(0.82f, 0.265f),
                new Color(0f, 0f, 0f, 0.42f));
            var fishMarker = Marker("FishTiltMarker", directionTrack.transform,
                new Color(1f, 0.78f, 0.08f), "FISH", 36f);
            var playerMarker = Marker("PlayerTiltMarker", directionTrack.transform,
                new Color(0.2f, 0.85f, 1f), "YOU", -36f);

            var tensionBackground = Bar("LineTension", gameplayPanel.transform,
                new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.155f),
                new Color(0f, 0f, 0f, 0.5f));
            var tensionFill = Fill("TensionFill", tensionBackground.transform,
                new Color(0.25f, 0.9f, 0.45f));
            var tensionStatus = Label("TensionStatus", gameplayPanel.transform,
                "SAFE  0%", 27, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.155f), new Vector2(0.9f, 0.195f));

            var progressBackground = Bar("CatchProgress", gameplayPanel.transform,
                new Vector2(0.1f, 0.065f), new Vector2(0.9f, 0.1f),
                new Color(0f, 0f, 0f, 0.5f));
            var progressFill = Fill("ProgressFill", progressBackground.transform,
                new Color(0.2f, 0.75f, 1f));
            Label("ProgressLabel", gameplayPanel.transform, "CATCH PROGRESS", 22,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.098f), new Vector2(0.9f, 0.125f));
            var recalibrate = Button("RecalibrateButton", gameplayPanel.transform, "RECALIBRATE",
                new Vector2(0.69f, 0.012f), new Vector2(0.97f, 0.052f));

            var resultPanel = Panel("ResultPanel", canvasObject.transform,
                new Color(0.025f, 0.08f, 0.12f, 0.94f));
            var resultText = Label("ResultText", resultPanel.transform, "RESULT", 48,
                TextAnchor.MiddleCenter, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.88f));
            var restart = Button("RestartButton", resultPanel.transform, "PLAY AGAIN",
                new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.17f));

            canvasObject.GetComponent<FishingHUD>().ConfigureScene(startPanel, gameplayPanel,
                resultPanel, startInstruction, instruction, timer, score, combo, feedback,
                tensionFill, tensionStatus, progressFill, fishMarker, playerMarker, phone,
                recalibrate, resultText, restart);

            startPanel.SetActive(true);
            gameplayPanel.SetActive(false);
            resultPanel.SetActive(false);
            EnsureEventSystem();
            Selection.activeGameObject = canvasObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Scene UI built and saved: {ScenePath}");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            {
                return;
            }
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text Label(string name, Transform parent, string value, int size,
            TextAnchor alignment, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Image Bar(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image Fill(string name, Transform parent, Color color)
        {
            var image = Bar(name, parent, Vector2.zero, Vector2.one, color);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0f;
            return image;
        }

        private static RectTransform Marker(string name, Transform parent, Color color,
            string caption, float y)
        {
            var image = Bar(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), color);
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(54f, 54f);
            rect.anchoredPosition = new Vector2(0f, y);
            Label("Label", rect, caption, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return rect;
        }

        private static Button Button(string name, Transform parent, string caption,
            Vector2 min, Vector2 max)
        {
            var image = Bar(name, parent, min, max, new Color(0.1f, 0.55f, 0.8f, 0.96f));
            var button = image.gameObject.AddComponent<Button>();
            var label = Label("Label", image.transform, caption, 32, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);
            label.raycastTarget = false;
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
