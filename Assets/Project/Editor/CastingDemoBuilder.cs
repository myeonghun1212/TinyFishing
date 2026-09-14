using System;
using Nan.Core;
using Nan.Fishing;
using Nan.Input;
using Nan.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nan.Editor
{
    /// <summary>Creates a standalone demo without changing any open scene or existing asset.</summary>
    public static class CastingDemoBuilder
    {
        private const string ScenePath = "Assets/Project/Scene/Casting Demo.unity";
        private const string DataFolder = "Assets/Project/Data/Casting Demo";
        private const string MaterialFolder = "Assets/Project/Art/Materials/Casting Demo";

        [MenuItem("Nan/Create Casting Demo")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Create the casting demo outside Play Mode.");
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath) != null || System.IO.File.Exists(ScenePath))
                throw new InvalidOperationException("Casting Demo.unity already exists. Open it to try casting.");

            EnsureFolder("Assets/Project/Scene");
            EnsureFolder(DataFolder);
            EnsureFolder(MaterialFolder);
            Scene previousScene = SceneManager.GetActiveScene();
            Scene demoScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(demoScene);
                BuildContents();
                if (!EditorSceneManager.SaveScene(demoScene, ScenePath))
                    throw new InvalidOperationException("Could not save the casting demo scene.");
                Debug.Log("Created " + ScenePath + ". Open the scene and press Play. Swipe upward or press Space to cast.");
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(demoScene, true);
            }
        }

        private static void BuildContents()
        {
            CastingSettings settings = CreateData<CastingSettings>("Casting Settings");
            CastingEventChannel castingEvent = CreateData<CastingEventChannel>("Casting Event");
            FishingStateEventChannel stateEvent = CreateData<FishingStateEventChannel>("Fishing State Event");
            Material water = CreateMaterial("Water", new Color(0.12f, 0.5f, 0.65f));
            Material wood = CreateMaterial("Dock", new Color(0.48f, 0.29f, 0.13f));
            Material rodMaterial = CreateMaterial("Rod", new Color(0.15f, 0.18f, 0.2f));
            Material bobberMaterial = CreateMaterial("Bobber", new Color(1f, 0.22f, 0.12f));
            Material lineMaterial = CreateMaterial("Line", new Color(0.95f, 0.96f, 0.88f));

            Primitive("Water", PrimitiveType.Plane, new Vector3(0f, 0f, 7f), new Vector3(4f, 1f, 4f), water);
            Primitive("Dock", PrimitiveType.Cube, new Vector3(0f, -0.12f, -2.5f), new Vector3(3f, 0.3f, 3f), wood);

            Transform pivot = new GameObject("Rod Pivot").transform;
            pivot.position = new Vector3(0f, 0.8f, -1.6f);
            pivot.rotation = Quaternion.Euler(35f, 0f, 0f);
            Transform rod = Primitive("Rod", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(0.035f, 1.5f, 0.035f), rodMaterial).transform;
            rod.SetParent(pivot, false);
            rod.localPosition = new Vector3(0f, 1.5f, 0f);
            Transform tip = new GameObject("Rod Tip").transform;
            tip.SetParent(pivot, false);
            tip.localPosition = new Vector3(0f, 3f, 0f);

            Transform bobber = Primitive("Bobber", PrimitiveType.Sphere, tip.position + Vector3.down * 0.5f,
                new Vector3(0.22f, 0.32f, 0.22f), bobberMaterial).transform;
            Transform target = new GameObject("Landing Target").transform;
            target.position = new Vector3(0f, 0.16f, 7f);

            GameObject fishing = new GameObject("Casting");
            CastingController controller = fishing.AddComponent<CastingController>();
            SetReference(controller, "_castingEvent", castingEvent);
            SetReference(controller, "_stateEvent", stateEvent);
            SetReference(controller, "_settings", settings);
            SetReference(controller, "_rodPivot", pivot);
            SetReference(controller, "_bobber", bobber);
            SetReference(controller, "_landingTarget", target);
            CastingInput input = fishing.AddComponent<CastingInput>();
            SetReference(input, "_castingEvent", castingEvent);
            CastingDemoView view = new GameObject("Casting Demo UI").AddComponent<CastingDemoView>();
            SetReference(view, "_casting", controller);

            GameObject lineObject = new GameObject("Fishing Line");
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.startWidth = 0.012f;
            line.endWidth = 0.008f;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, tip.position);
            line.SetPosition(1, bobber.position);
            FishingLineView lineView = lineObject.AddComponent<FishingLineView>();
            SetReference(lineView, "_rodTip", tip);
            SetReference(lineView, "_bobber", bobber);

            AudioSource throwSound = AddAudio(fishing, "Assets/Project/Audio/throw.mp3");
            AudioSource splashSound = AddAudio(fishing, "Assets/Project/Audio/Splash.mp3");
            UnityEventTools.AddPersistentListener(controller.CastStarted, throwSound.Play);
            UnityEventTools.AddPersistentListener(controller.Landed, splashSound.Play);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(6f, 5f, -8f);
            camera.transform.LookAt(new Vector3(0f, 1f, 3f));
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.63f, 0.82f, 0.91f);
            camera.gameObject.AddComponent<AudioListener>();

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.75f);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(instance.GetComponent<Collider>());
            return instance;
        }

        private static AudioSource AddAudio(GameObject owner, string path)
        {
            AudioSource source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            source.volume = 0.6f;
            return source;
        }

        private static T CreateData<T>(string name) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(DataFolder + "/" + name + ".asset"));
            return asset;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The casting demo requires the project's URP Lit shader.");
            Material material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(MaterialFolder + "/" + name + ".mat"));
            return material;
        }

        private static void SetReference(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException(owner.GetType().Name + " has no serialized field " + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
