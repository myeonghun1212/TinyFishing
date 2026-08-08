using UnityEngine;
using UnityEngine.InputSystem;

public class UnityRemoteTest : MonoBehaviour
{
#if UNITY_EDITOR
    void Update()
    {
        var attitudeSensor = AttitudeSensor.current;
        var accelerometer = Accelerometer.current;

        // Unity Remote can register each sensor at a different time. Keep checking in the
        // Editor and enable every sensor as soon as it becomes available.
        if (attitudeSensor != null && !attitudeSensor.enabled)
        {
            InputSystem.EnableDevice(attitudeSensor);
        }

        if (accelerometer != null && !accelerometer.enabled)
        {
            InputSystem.EnableDevice(accelerometer);
        }
    }
#endif
}
