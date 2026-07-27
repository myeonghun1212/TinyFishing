using System.IO;
using NanFishing.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NanFishing.Editor
{
    public static class EditableSceneBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/SampleScene.unity";
        private const string MaterialFolder = "Assets/Project/Art/Materials";

        [MenuItem("Tools/NAN Fishing/Build Editable Scene")]
        public static void BuildEditableScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var oldEnvironment = GameObject.Find("Environment");
            if (oldEnvironment != null)
            {
                Object.DestroyImmediate(oldEnvironment);
            }

            EnsureFolder("Assets/Project/Art");
            EnsureFolder(MaterialFolder);

            var environment = new GameObject("Environment");
            var water = CreatePrimitive("Water", PrimitiveType.Cube, environment.transform,
                new Vector3(0f, -0.35f, 4f), new Vector3(18f, 0.5f, 18f),
                Material("M_Water", new Color(0.08f, 0.58f, 0.74f), 0.5f));
            var dock = new GameObject("Dock");
            dock.transform.SetParent(environment.transform);
            CreatePrimitive("DockBase", PrimitiveType.Cube, dock.transform,
                new Vector3(0f, 0.05f, -0.2f), new Vector3(4.2f, 0.3f, 4f),
                Material("M_Wood", new Color(0.63f, 0.4f, 0.19f), 0.08f));
            for (var index = -3; index <= 3; index++)
            {
                CreatePrimitive($"Plank_{index + 4:00}", PrimitiveType.Cube, dock.transform,
                    new Vector3(index * 0.58f, 0.23f, -0.2f), new Vector3(0.52f, 0.08f, 3.85f),
                    Material("M_WoodLight", new Color(0.72f, 0.49f, 0.25f), 0.06f));
            }

            var props = new GameObject("FishingProps");
            props.transform.SetParent(environment.transform);
            var rod = CreatePrimitive("FishingRod", PrimitiveType.Cylinder, props.transform,
                new Vector3(1.05f, 1.65f, 0f), new Vector3(0.07f, 1.8f, 0.07f),
                Material("M_Rod", new Color(0.12f, 0.15f, 0.16f), 0.25f));
            rod.transform.rotation = Quaternion.Euler(65f, 0f, -12f);

            var bobber = CreatePrimitive("BobberAnchor", PrimitiveType.Sphere, props.transform,
                new Vector3(0.65f, 2.4f, 0.4f), Vector3.one * 0.24f,
                Material("M_Bobber", new Color(1f, 0.2f, 0.12f), 0.3f));
            Object.DestroyImmediate(bobber.GetComponent<Collider>());

            var fishPool = new GameObject("FishPool");
            fishPool.transform.SetParent(environment.transform);

            var background = new GameObject("Background");
            background.transform.SetParent(environment.transform);
            CreateIsland(background.transform, new Vector3(-5.5f, 0f, 8f), 2.8f);
            CreateIsland(background.transform, new Vector3(5.8f, -0.1f, 10f), 3.6f);

            ConfigureCamera();
            ConfigureLight();
            Selection.activeGameObject = environment;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Editable fishing scene built and saved: {ScenePath}");
        }

        private static void CreateIsland(Transform parent, Vector3 position, float scale)
        {
            var island = new GameObject($"Island_{parent.childCount + 1:00}");
            island.transform.SetParent(parent);
            CreatePrimitive("Rock", PrimitiveType.Sphere, island.transform, position,
                new Vector3(scale, 0.7f, scale * 0.75f),
                Material("M_Sand", new Color(0.82f, 0.7f, 0.42f), 0.02f));
            for (var index = 0; index < 3; index++)
            {
                var treePosition = position + new Vector3((index - 1) * 0.75f, 1f, 0f);
                CreatePrimitive($"TreeTrunk_{index + 1}", PrimitiveType.Cylinder, island.transform,
                    treePosition, new Vector3(0.16f, 0.9f, 0.16f),
                    Material("M_Trunk", new Color(0.38f, 0.23f, 0.12f), 0.02f));
                CreatePrimitive($"TreeCrown_{index + 1}", PrimitiveType.Sphere, island.transform,
                    treePosition + Vector3.up * 1.35f, new Vector3(0.8f, 1f, 0.8f),
                    Material("M_Leaves", new Color(0.18f, 0.55f, 0.27f), 0.05f));
            }
        }

        private static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }
            camera.transform.position = new Vector3(0f, 4.8f, -8.5f);
            camera.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 0.8f, 4f) - camera.transform.position);
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.78f, 0.92f);
        }

        private static void ConfigureLight()
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light == null)
            {
                light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
            }
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            return instance;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
