#if UNITY_EDITOR
using System.Linq;
using TinyFishing.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyFishing.Editor
{
    public static class TinyFishingStartMenuSceneBuilder
    {
        private const string MenuScenePath = "Assets/Project/Scenes/Pond Start Menu.unity";
        private const string GameplayScenePath = "Assets/Project/Scenes/Pond FPV.unity";

        [MenuItem("Tools/Tiny Fishing/Build Start Menu")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find("StartMenuUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject("StartMenuUI");
            var controller = root.AddComponent<TinyFishingStartMenu>();

            var canvasObject = CreateObject("Canvas", root.transform);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var shade = CreatePanel("BackgroundShade", canvasObject.transform,
                new Color(0.015f, 0.045f, 0.065f, 0.74f));
            Stretch(shade.rectTransform);

            var card = CreatePanel("MenuCard", shade.transform,
                new Color(0.035f, 0.09f, 0.12f, 0.96f));
            SetAnchoredRect(card.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(820f, 780f), Vector2.zero);

            CreateText("Title", card.transform, "TINY FISHING", 64, FontStyle.Bold,
                new Vector2(0f, 276f), new Vector2(700f, 90f), new Color(0.87f, 0.97f, 1f));
            CreateText("Subtitle", card.transform, "CHOOSE YOUR CONTROL", 25, FontStyle.Normal,
                new Vector2(0f, 211f), new Vector2(700f, 50f), new Color(0.48f, 0.76f, 0.83f));

            var gyro = CreateButton("GyroButton", card.transform, "GYRO",
                new Vector2(-188f, 125f), new Vector2(330f, 92f));
            var touch = CreateButton("TouchButton", card.transform, "TOUCH SCREEN",
                new Vector2(188f, 125f), new Vector2(330f, 92f));

            var selectedMode = CreateText("SelectedMode", card.transform, "GYRO", 31,
                FontStyle.Bold, new Vector2(0f, 48f), new Vector2(700f, 55f), Color.white);

            var gyroPanel = CreateObject("GyroPanel", card.transform);
            var gyroPanelRect = gyroPanel.GetComponent<RectTransform>();
            SetAnchoredRect(gyroPanelRect, new Vector2(0.5f, 0.5f),
                new Vector2(700f, 245f), new Vector2(0f, -80f));
            CreateText("SwingHint", gyroPanel.transform,
                "Hold the phone naturally, then swing to start", 25, FontStyle.Normal,
                new Vector2(0f, 65f), new Vector2(660f, 55f), new Color(0.87f, 0.94f, 0.96f));
            var sensorStatus = CreateText("SensorStatus", gyroPanel.transform,
                "Preparing motion sensor...", 20, FontStyle.Normal,
                new Vector2(0f, 10f), new Vector2(660f, 65f), new Color(0.48f, 0.76f, 0.83f));
            var recalibrate = CreateButton("RecalibrateButton", gyroPanel.transform,
                "INITIALIZE GYRO", new Vector2(0f, -73f), new Vector2(360f, 78f));

            var start = CreateButton("StartButton", card.transform, "START GAME",
                new Vector2(0f, -172f), new Vector2(430f, 96f));
            CreateText("Footer", card.transform,
                "Gyro: swing  |  Touch: tap Start Game", 19, FontStyle.Normal,
                new Vector2(0f, -320f), new Vector2(700f, 45f), new Color(0.45f, 0.61f, 0.65f));

            controller.Configure(gyro, touch, start, recalibrate, gyroPanel,
                selectedMode, sensorStatus);

            EnsureEventSystem();
            UpdateBuildSettings();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Tiny Fishing start menu built successfully.");
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventObject = new GameObject("EventSystem");
                eventObject.AddComponent<EventSystem>();
                eventObject.AddComponent<InputSystemUIInputModule>();
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    Object.DestroyImmediate(oldModule);
                }
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void UpdateBuildSettings()
        {
            var remaining = EditorBuildSettings.scenes
                .Where(scene => scene.path != MenuScenePath && scene.path != GameplayScenePath)
                .ToList();
            remaining.Insert(0, new EditorBuildSettingsScene(GameplayScenePath, true));
            remaining.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            EditorBuildSettings.scenes = remaining.ToArray();
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var value = CreateObject(name, parent);
            var image = value.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size,
            FontStyle style, Vector2 position, Vector2 dimensions, Color color)
        {
            var textObject = CreateObject(name, parent);
            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetAnchoredRect(text.rectTransform, new Vector2(0.5f, 0.5f), dimensions, position);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label,
            Vector2 position, Vector2 dimensions)
        {
            var buttonObject = CreateObject(name, parent);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.21f, 0.27f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.20f, 0.69f, 0.88f, 1f);
            colors.pressedColor = new Color(0.10f, 0.48f, 0.66f, 1f);
            button.colors = colors;
            SetAnchoredRect(image.rectTransform, new Vector2(0.5f, 0.5f), dimensions, position);
            CreateText("Label", buttonObject.transform, label, 25, FontStyle.Bold,
                Vector2.zero, dimensions, Color.white);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor,
            Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
#endif
