using NanFishing.Data;
using NanFishing.Fishing;
using NanFishing.Input;
using NanFishing.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace NanFishing.Core
{
    public static class DemoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<GameFlowController>() != null)
            {
                return;
            }

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            var root = new GameObject("NAN_Fishing_Demo");
            var balance = GameBalanceConfig.CreateRuntime();
            var fishCatalog = CreateFishCatalog();
            PrepareCamera();
            FindSceneReferences(out var bobber, out var fishRoot);
            EnsureEventSystem(root.transform);

            var input = root.AddComponent<MotionInputService>();
            input.Initialize(balance);
            var fishing = root.AddComponent<FishingController>();
            fishing.Initialize(input, balance, fishCatalog, bobber, fishRoot);
            var hud = Object.FindAnyObjectByType<FishingHUD>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogError("FishingHUD is missing from the scene. Run Tools/NAN Fishing/Build Scene UI.");
                return;
            }
            var flow = root.AddComponent<GameFlowController>();
            flow.Initialize(input, fishing, hud, balance, fishCatalog);
        }

        private static Camera PrepareCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }
            camera.transform.position = new Vector3(0f, 4.8f, -8.5f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.8f, 4f) -
                                                                 camera.transform.position);
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.78f, 0.92f);
            return camera;
        }

        private static void FindSceneReferences(out Transform bobber, out Transform fishRoot)
        {
            var bobberObject = GameObject.Find("BobberAnchor");
            var fishPoolObject = GameObject.Find("FishPool");
            if (bobberObject == null || fishPoolObject == null)
            {
                Debug.LogError("Fishing scene references are missing. Run Tools/NAN Fishing/Build Editable Scene.");
                var fallbackRoot = new GameObject("MissingSceneReferences");
                bobberObject = bobberObject != null
                    ? bobberObject
                    : new GameObject("BobberAnchor");
                fishPoolObject = fishPoolObject != null
                    ? fishPoolObject
                    : new GameObject("FishPool");
                bobberObject.transform.SetParent(fallbackRoot.transform);
                fishPoolObject.transform.SetParent(fallbackRoot.transform);
            }

            bobber = bobberObject.transform;
            fishRoot = fishPoolObject.transform;
        }

        private static void EnsureEventSystem(Transform root)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root);
        }

        private static FishDefinition[] CreateFishCatalog()
        {
            return new[]
            {
                Fish("bluegill", "Bluegill", FishRarity.Common, 100, 0.75f, 0.65f, 1.35f,
                    new Color(0.25f, 0.72f, 0.85f)),
                Fish("carp", "Carp", FishRarity.Common, 120, 0.9f, 0.55f, 1.5f,
                    new Color(0.8f, 0.62f, 0.23f)),
                Fish("salmon", "Salmon", FishRarity.Uncommon, 190, 1.15f, 0.8f, 1.05f,
                    new Color(0.95f, 0.42f, 0.35f)),
                Fish("tuna", "Tuna", FishRarity.Uncommon, 230, 1.3f, 0.95f, 0.85f,
                    new Color(0.18f, 0.38f, 0.62f)),
                Fish("golden", "Golden Fish", FishRarity.Rare, 450, 1.55f, 1.15f, 0.65f,
                    new Color(1f, 0.78f, 0.08f))
            };
        }

        private static FishDefinition Fish(string id, string label, FishRarity rarity, int score,
            float resistance, float speed, float turnInterval, Color color)
        {
            var fish = ScriptableObject.CreateInstance<FishDefinition>();
            fish.ConfigureRuntime(id, label, rarity, score, resistance, speed, turnInterval, color);
            return fish;
        }
    }
}
