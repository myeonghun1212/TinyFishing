using NanFishing.Data;
using UnityEngine;

namespace NanFishing.Fishing
{
    public sealed class LineTensionModel
    {
        private readonly GameBalanceConfig config;
        private float dangerTime;

        public float Tension { get; private set; }
        public float Progress { get; private set; }
        public bool IsBroken => dangerTime >= config.breakGraceDuration;
        public bool IsCaught => Progress >= 1f;

        public LineTensionModel(GameBalanceConfig balance)
        {
            config = balance;
            Reset();
        }

        public void Reset()
        {
            Tension = config.startingTension;
            Progress = 0f;
            dangerTime = 0f;
        }

        public void Tick(float deltaTime, bool isReeling, float playerDirection,
            float fishDirection, float resistance)
        {
            var dt = Mathf.Max(0f, deltaTime);
            var directionError = Mathf.Abs(playerDirection - fishDirection) * 0.5f;

            if (isReeling)
            {
                Progress += config.reelProgressPerSecond * dt / Mathf.Max(0.25f, resistance);
                var strain = Mathf.Max(0f, directionError - config.directionDeadZone);
                var pullLoad = 0.16f + strain * 1.35f;
                Tension += config.tensionGainPerSecond * pullLoad * resistance * dt;
            }
            else
            {
                Progress -= config.escapeProgressPerSecond * resistance * dt;
                Tension -= config.tensionRecoveryPerSecond * dt;
            }

            Progress = Mathf.Clamp01(Progress);
            Tension = Mathf.Clamp01(Tension);
            dangerTime = Tension >= config.dangerTension
                ? dangerTime + dt
                : Mathf.Max(0f, dangerTime - dt * 2f);
        }
    }
}
