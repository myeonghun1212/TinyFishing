using UnityEngine;

namespace NanFishing.Core
{
    public static class RuntimeVisualFactory
    {
        public static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", 0.18f);
            return material;
        }

        public static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Color color)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().material = CreateMaterial(color);
            return instance;
        }
    }
}
