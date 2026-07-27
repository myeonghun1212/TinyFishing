using System;

namespace NanFishing.Input
{
    public enum CastStrength
    {
        Short = 0,
        Medium = 1,
        Long = 2
    }

    public interface IPlayerInput
    {
        event Action<CastStrength> CastPerformed;
        float Direction { get; }
        bool IsReeling { get; }
        bool IsRodRaised { get; }
        bool IsCalibrated { get; }
        bool HasMotionSensors { get; }
        float CalibrationProgress { get; }
        void BeginCalibration();
        void Recalibrate();
    }
}
