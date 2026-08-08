using TinyFishing.Data;
using UnityEngine;

namespace TinyFishing.Fishing
{
    // Plain-C# model of the hooked fish's position inside the aim gauge (-1..1).
    // Wanders to a new random target direction every so often, similar to a
    // fish tugging the line side to side while it fights. the line side to side while it fights.
    /// &lt;/summary&gt;
    public sealed class FishDriftDriver
    {
        private readonly TinyFishingConfig config;
        private float targetDirection;
        private float changeTimer;
        private float? moveSpeedOverride;
        private Vector2? directionRangeOverride;

        public float Direction { get; private set; }

        public FishDriftDriver(TinyFishingConfig fishingConfig)
        {
            config = fishingConfig;
        }

        // Overrides the generic config difficulty with a specific fish's own
        // MoveSpeed/DirectionChangeInterval stats for this round. Call before Reset().
        public void SetFishOverride(float moveSpeed, float directionChangeInterval)
        {
            moveSpeedOverride = moveSpeed;
            directionRangeOverride = new Vector2(directionChangeInterval * 0.75f, directionChangeInterval * 1.25f);
        }

        public void ClearFishOverride()
        {
            moveSpeedOverride = null;
            directionRangeOverride = null;
        }

        public void Reset()
        {
            Direction = Random.Range(-0.6f, 0.6f);
            PickNewTarget();
        }

        public void Tick(float deltaTime)
        {
            changeTimer -= deltaTime;
            if (changeTimer <= 0f)
            {
                PickNewTarget();
            }

            var moveSpeed = moveSpeedOverride ?? config.fallbackFishMoveSpeed;
            Direction = Mathf.MoveTowards(Direction, targetDirection, moveSpeed * deltaTime);
        }

        private void PickNewTarget()
        {
            var range = directionRangeOverride ?? config.fallbackDirectionChangeIntervalRange;
            targetDirection = Random.Range(-1f, 1f);
            changeTimer = Random.Range(range.x, range.y);
        }
    }
}
