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
        private const string GameplayScenePath = "Assets/Project/Scenes/Pond Casual.unity";

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
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var shade = CreatePanel("BackgroundShade", canvasObject.transform,
                new Color(0.015f, 0.045f, 0.065f, 0.74f));
            Stretch(shade.rectTransform);

            var card = CreatePanel("MenuCard", shade.transform,
                new Color(0.035f, 0.09f, 0.12f, 0.96f));
            SetAnchoredRect(card.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(900f, 1240f), Vector2.zero);

            var mainPanel = CreateObject("MainPanel", card.transform);
            Stretch(mainPanel.GetComponent<RectTransform>());
            CreateText("Title", mainPanel.transform, "TINY FISHING", 68, FontStyle.Bold,
                new Vector2(0f, 470f), new Vector2(760f, 100f), new Color(0.87f, 0.97f, 1f));
            CreateText("Subtitle", mainPanel.transform, "CHOOSE A GAME MODE", 28, FontStyle.Normal,
                new Vector2(0f, 390f), new Vector2(760f, 55f), new Color(0.48f, 0.76f, 0.83f));

            var settings = CreateButton("SettingsButton", mainPanel.transform, "SETTINGS",
                new Vector2(282f, 525f), new Vector2(230f, 68f), 22);
            var infinite = CreateButton("InfiniteModeButton", mainPanel.transform,
                "INFINITE MODE\nNo time limit", new Vector2(0f, 235f), new Vector2(700f, 170f), 30);
            var timeLimited = CreateButton("TimeLimitedModeButton", mainPanel.transform,
                "TIME LIMITED\nCOMING SOON", new Vector2(0f, 25f), new Vector2(700f, 170f), 30);

            var selectedGameMode = CreateText("SelectedGameMode", mainPanel.transform,
                "INFINITE MODE", 34, FontStyle.Bold, new Vector2(0f, -125f),
                new Vector2(760f, 60f), Color.white);
            var gameModeStatus = CreateText("GameModeStatus", mainPanel.transform,
                "Play without a time limit", 23, FontStyle.Normal, new Vector2(0f, -185f),
                new Vector2(760f, 75f), new Color(0.56f, 0.77f, 0.82f));
            var start = CreateButton("StartButton", mainPanel.transform, "START GAME",
                new Vector2(0f, -320f), new Vector2(470f, 105f), 30);
            CreateText("MainFooter", mainPanel.transform,
                "Gyro: swing to start  |  Touch: tap Start Game", 20, FontStyle.Normal,
                new Vector2(0f, -505f), new Vector2(780f, 65f), new Color(0.45f, 0.61f, 0.65f));

            var settingsPanel = CreateObject("SettingsPanel", card.transform);
            Stretch(settingsPanel.GetComponent<RectTransform>());
            CreateText("SettingsTitle", settingsPanel.transform, "INPUT SETTINGS", 58, FontStyle.Bold,
                new Vector2(0f, 465f), new Vector2(760f, 90f), new Color(0.87f, 0.97f, 1f));
            CreateText("SettingsSubtitle", settingsPanel.transform,
                "This input setting is shared by every game mode", 23, FontStyle.Normal,
                new Vector2(0f, 390f), new Vector2(760f, 70f), new Color(0.48f, 0.76f, 0.83f));

            var gyro = CreateButton("GyroButton", settingsPanel.transform, "GYRO",
                new Vector2(-190f, 270f), new Vector2(340f, 100f), 27);
            var touch = CreateButton("TouchButton", settingsPanel.transform, "TOUCH SCREEN",
                new Vector2(190f, 270f), new Vector2(340f, 100f), 27);
            var selectedInput = CreateText("SelectedInput", settingsPanel.transform, "GYRO", 34,
                FontStyle.Bold, new Vector2(0f, 170f), new Vector2(760f, 60f), Color.white);

            var gyroPanel = CreateObject("GyroPanel", settingsPanel.transform);
            SetAnchoredRect(gyroPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(780f, 350f), new Vector2(0f, -35f));
            CreateText("SwingHint", gyroPanel.transform,
                "Hold the phone naturally, then initialize the gyro", 24, FontStyle.Normal,
                new Vector2(0f, 105f), new Vector2(740f, 70f), new Color(0.87f, 0.94f, 0.96f));
            var sensorStatus = CreateText("SensorStatus", gyroPanel.transform,
                "Preparing motion sensor...", 21, FontStyle.Normal,
                new Vector2(0f, 25f), new Vector2(740f, 75f), new Color(0.48f, 0.76f, 0.83f));
            var recalibrate = CreateButton("RecalibrateButton", gyroPanel.transform,
                "INITIALIZE GYRO", new Vector2(0f, -90f), new Vector2(410f, 90f), 25);

            var closeSettings = CreateButton("CloseSettingsButton", settingsPanel.transform,
                "BACK", new Vector2(0f, -480f), new Vector2(360f, 90f), 27);
            CreateText("SettingsFooter", settingsPanel.transform,
                "Your choice is saved automatically", 20, FontStyle.Normal,
                new Vector2(0f, -555f), new Vector2(760f, 50f), new Color(0.45f, 0.61f, 0.65f));

            controller.Configure(
                mainPanel,
                settingsPanel,
                settings,
                closeSettings,
                infinite,
                timeLimited,
                start,
                selectedGameMode,
                gameModeStatus,
                gyro,
                touch,
                recalibrate,
                gyroPanel,
                selectedInput,
                sensorStatus);
            settingsPanel.SetActive(false);

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
                .Where(scene => scene.path != MenuScenePath
                    && scene.path != GameplayScenePath
                    && scene.path != "Assets/Project/Scenes/Pond FPV.unity")
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
            Vector2 position, Vector2 dimensions, int fontSize)
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
            CreateText("Label", buttonObject.transform, label, fontSize, FontStyle.Bold,
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
