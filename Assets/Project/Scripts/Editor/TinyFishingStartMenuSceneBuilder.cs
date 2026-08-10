#if UNITY_EDITOR
using System;
using System.Linq;
using TinyFishing.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyFishing.Editor
{
    /// <summary>
    /// Wires the hand-authored start-menu hierarchy without rebuilding or repositioning UI.
    /// </summary>
    public static class TinyFishingStartMenuSceneBuilder
    {
        private const string MenuScenePath = "Assets/Project/Scenes/Pond Start Menu.unity";
        private const string AudioMixerPath = "Assets/Project/Audio/TinyFishingAudioMixer.mixer";
        private const string BlurMaterialPath = "Assets/KawaseBlur/UnlitBlur.mat";

        [MenuItem("Tools/Tiny Fishing/Wire Start Menu UI")]
        public static void Wire()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var menuRoot = RequireRoot(scene, "MenuUI");
            var startUi = RequireRoot(scene, "StartUI");
            var controller = RequireComponent<TinyFishingStartMenu>(menuRoot);

            var mainCanvas = RequireChild(menuRoot.transform, "MainCanvas");
            var main = RequireChild(mainCanvas, "Main");
            var modalCanvas = RequireChild(menuRoot.transform, "Setting&HowtoCanvas");
            var settings = RequireChild(modalCanvas, "Setting");
            var howTo = RequireChild(modalCanvas, "Howto");

            var infinite = RequireButton(RequireChild(main, "Casual"));
            var timeLimited = RequireButton(RequireChild(main, "Time"));
            var openHowTo = RequireButton(RequireChild(main, "HowtoPlayButton"));
            var openSettings = RequireButton(RequireChild(main, "SettingButton"));
            var start = RequireButton(RequireChild(main, "StartButton"));

            var gyroObject = RequireDescendant(settings, "Gyro");
            var touchObject = RequireDescendant(settings, "Touch");
            var gyro = RequireButton(gyroObject);
            var touch = RequireButton(touchObject);

            var recalibrationGroup = RequireDescendant(settings, "Recalibartion");
            var recalibrateObject = recalibrationGroup.Cast<Transform>()
                .FirstOrDefault(child => child.GetComponent<Image>() != null);
            if (recalibrateObject == null)
            {
                throw new InvalidOperationException("Recalibration image button was not found.");
            }

            var closeSettings = RequireButton(RequireChild(settings, "Button"));
            var closeHowTo = RequireButton(RequireChild(howTo, "BackButton"));
            var recalibrate = RequireButton(recalibrateObject);

            var sfxSlider = RequireDescendant(RequireChild(settings, "EffectVolume"), "Slider")
                .GetComponent<Slider>();
            var bgmGroup = settings.Cast<Transform>()
                .FirstOrDefault(child => child.name.Contains("BGA", StringComparison.OrdinalIgnoreCase));
            if (bgmGroup == null)
            {
                throw new InvalidOperationException("BGM volume group was not found.");
            }

            var bgmSlider = RequireDescendant(bgmGroup, "Slider").GetComponent<Slider>();
            if (bgmSlider == null || sfxSlider == null)
            {
                throw new InvalidOperationException("Both volume sliders must have Slider components.");
            }

            var blurMaterial = AssetDatabase.LoadAssetAtPath<Material>(BlurMaterialPath);
            if (blurMaterial == null)
            {
                throw new InvalidOperationException($"Blur material is missing: {BlurMaterialPath}");
            }

            settings.GetComponent<Image>().material = blurMaterial;
            howTo.GetComponent<Image>().material = blurMaterial;

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerPath);
            if (mixer == null)
            {
                throw new InvalidOperationException($"Audio mixer is missing: {AudioMixerPath}");
            }

            controller.Configure(
                main.gameObject,
                settings.gameObject,
                howTo.gameObject,
                openSettings,
                openHowTo,
                closeSettings,
                closeHowTo,
                infinite,
                timeLimited,
                start,
                gyro,
                touch,
                recalibrate,
                bgmSlider,
                sfxSlider,
                mixer);

            main.gameObject.SetActive(true);
            settings.gameObject.SetActive(false);
            howTo.gameObject.SetActive(false);
            startUi.SetActive(true);
            menuRoot.SetActive(false);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(settings.GetComponent<Image>());
            EditorUtility.SetDirty(howTo.GetComponent<Image>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Tiny Fishing start menu UI wiring completed without layout changes.");
        }

        [MenuItem("Tools/Tiny Fishing/Remove Legacy Background Shade")]
        public static void RemoveLegacyBackgroundShade()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var menuRoot = RequireRoot(scene, "MenuUI");
            var mainCanvas = RequireChild(menuRoot.transform, "MainCanvas");
            var backgroundShade = mainCanvas.Find("BackgroundShade");
            if (backgroundShade != null)
            {
                Undo.DestroyObjectImmediate(backgroundShade.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, MenuScenePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Legacy BackgroundShade removed.");
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            var value = scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
            return value != null
                ? value
                : throw new InvalidOperationException($"Root object '{name}' was not found.");
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            var value = parent.Cast<Transform>().FirstOrDefault(child => child.name == name);
            return value != null
                ? value
                : throw new InvalidOperationException($"Child '{name}' was not found below '{parent.name}'.");
        }

        private static Transform RequireDescendant(Transform parent, string name)
        {
            var value = parent.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child != parent && child.name == name);
            return value != null
                ? value
                : throw new InvalidOperationException($"Descendant '{name}' was not found below '{parent.name}'.");
        }

        private static T RequireComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null
                ? component
                : throw new InvalidOperationException($"{target.name} requires {typeof(T).Name}.");
        }

        private static Button RequireButton(Transform target)
        {
            var image = RequireComponent<Image>(target.gameObject);
            var button = target.GetComponent<Button>();
            if (button == null)
            {
                button = Undo.AddComponent<Button>(target.gameObject);
            }

            button.targetGraphic = image;
            EditorUtility.SetDirty(button);
            return button;
        }
    }
}
#endif
