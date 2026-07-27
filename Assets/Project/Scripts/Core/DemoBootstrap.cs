using NanFishing.Data;
using NanFishing.Fishing;
using NanFishing.Input;
using NanFishing.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace NanFishing.Core
{
    public sealed class DemoBootstrap : MonoBehaviour
    {
        [Header("Game Data")]
        [SerializeField] private GameBalanceConfig gameSetting;
        [SerializeField] private FishDefinition[] fishCatalog;

        private void Awake()
        {
            if (Object.FindAnyObjectByType<GameFlowController>() != null)
            {
                return;
            }

            if (gameSetting == null)
            {
                Debug.LogError("GameSetting is not assigned to DemoBootstrap.", this);
                enabled = false;
                return;
            }

            if (fishCatalog == null || fishCatalog.Length == 0)
            {
                Debug.LogError("Fish Catalog is empty on DemoBootstrap.", this);
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            PrepareCamera();
            FindSceneReferences(out var bobber, out var fishRoot);
            EnsureEventSystem(transform);

            var input = gameObject.AddComponent<MotionInputService>();
            input.Initialize(gameSetting);
            var fishing = gameObject.AddComponent<FishingController>();
            fishing.Initialize(input, gameSetting, fishCatalog, bobber, fishRoot);
            var hud = Object.FindAnyObjectByType<FishingHUD>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogError("FishingHUD is missing from the scene. Run Tools/NAN Fishing/Build Scene UI.");
                return;
            }
            var flow = gameObject.AddComponent<GameFlowController>();
            flow.Initialize(input, fishing, hud, gameSetting, fishCatalog);
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

    }
}
