using NanFishing.Data;
using UnityEngine;

namespace NanFishing.Fishing
{
    public sealed class CatchZoneModel
    {
        private readonly GameBalanceConfig config;
        private float nextMilestone;

        public float Progress { get; private set; }
        public float ZoneCenter { get; private set; }
        public float ZoneHalfWidth { get; private set; }
        public bool IsCaught => Progress >= 1f;

        public CatchZoneModel(GameBalanceConfig balance)
        {
            config = balance;
            Reset();
        }

        public void Reset()
        {
            Progress = 0f;
            nextMilestone = config.catchZoneMilestone;
            RandomizeZone();
        }

        public bool Tick(float deltaTime, bool actionActive, float fishDirection)
        {
            var isInsideZone = Mathf.Abs(fishDirection - ZoneCenter) <= ZoneHalfWidth;
            if (actionActive && isInsideZone)
            {
                Progress = Mathf.Clamp01(Progress + config.catchProgressPerSecond *
                    Mathf.Max(0f, deltaTime));
            }

            if (Progress >= nextMilestone && nextMilestone < 1f)
            {
                RandomizeZone();
                nextMilestone += config.catchZoneMilestone;
            }
            return isInsideZone;
        }

        private void RandomizeZone()
        {
            var minimum = Mathf.Min(config.catchZoneHalfWidthMin, config.catchZoneHalfWidthMax);
            var maximum = Mathf.Max(config.catchZoneHalfWidthMin, config.catchZoneHalfWidthMax);
            ZoneHalfWidth = Random.Range(minimum, maximum);
            ZoneCenter = Random.Range(-1f + ZoneHalfWidth, 1f - ZoneHalfWidth);
        }
    }
}
