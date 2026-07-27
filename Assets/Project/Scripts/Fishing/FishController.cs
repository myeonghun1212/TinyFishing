using NanFishing.Data;
using UnityEngine;

namespace NanFishing.Fishing
{
    public sealed class FishController : MonoBehaviour
    {
        private FishDefinition definition;
        private float turnTimer;
        private float targetDirection;
        private Vector3 centerPosition;

        public float Direction { get; private set; }
        public FishDefinition Definition => definition;

        public void Initialize(FishDefinition fish)
        {
            definition = fish;
            targetDirection = Random.Range(-1f, 1f);
            Direction = targetDirection;
            turnTimer = fish.DirectionChangeInterval;
            centerPosition = transform.localPosition;
            transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.05f, fish.Resistance / 2f);
        }

        public void Simulate(float deltaTime)
        {
            if (definition == null)
            {
                return;
            }

            turnTimer -= deltaTime;
            if (turnTimer <= 0f)
            {
                targetDirection = Random.Range(-1f, 1f);
                turnTimer = definition.DirectionChangeInterval * Random.Range(0.75f, 1.25f);
            }

            Direction = Mathf.MoveTowards(Direction, targetDirection, definition.MoveSpeed * deltaTime);
            transform.localPosition = centerPosition +
                                      new Vector3(Direction * 2.4f,
                                          Mathf.Sin(Time.time * 3f) * 0.12f, 0f);
            transform.localRotation = Quaternion.Euler(0f, Direction >= 0f ? 90f : -90f, 0f);
        }
    }
}
