using UnityEngine;

namespace NanFishing.Data
{
    public enum FishRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2
    }

    [CreateAssetMenu(menuName = "NAN Fishing/Fish Definition", fileName = "FishDefinition")]
    public sealed class FishDefinition : ScriptableObject
    {
        [SerializeField] private string fishId = "fish";
        [SerializeField] private string displayName = "Fish";
        [SerializeField] private FishRarity rarity;
        [SerializeField, Min(1)] private int baseScore = 100;
        [SerializeField, Range(0.2f, 2f)] private float resistance = 1f;
        [SerializeField, Range(0.1f, 2f)] private float moveSpeed = 0.7f;
        [SerializeField, Range(0.2f, 3f)] private float directionChangeInterval = 1.2f;
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private GameObject prefab;

        public string Id => fishId;
        public string DisplayName => displayName;
        public FishRarity Rarity => rarity;
        public int BaseScore => baseScore;
        public float Resistance => resistance;
        public float MoveSpeed => moveSpeed;
        public float DirectionChangeInterval => directionChangeInterval;
        public Color Color => color;
        public GameObject Prefab => prefab;

        public void ConfigureRuntime(string id, string label, FishRarity fishRarity, int score,
            float fishResistance, float speed, float turnInterval, Color fishColor)
        {
            fishId = id;
            displayName = label;
            rarity = fishRarity;
            baseScore = score;
            resistance = fishResistance;
            moveSpeed = speed;
            directionChangeInterval = turnInterval;
            color = fishColor;
        }
    }
}
