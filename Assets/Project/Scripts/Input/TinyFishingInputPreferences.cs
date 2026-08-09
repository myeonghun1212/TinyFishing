using UnityEngine;

namespace TinyFishing.Input
{
    /// <summary>
    /// Persists the input route selected on the start menu between scene loads.
    /// </summary>
    public static class TinyFishingInputPreferences
    {
        private const string InputStateKey = "TinyFishing.InputState";

        public static TinyFishingInputState Load(
            TinyFishingInputState fallback = TinyFishingInputState.Gyro)
        {
            var storedValue = PlayerPrefs.GetInt(InputStateKey, (int)fallback);
            return System.Enum.IsDefined(typeof(TinyFishingInputState), storedValue)
                ? (TinyFishingInputState)storedValue
                : fallback;
        }

        public static void Save(TinyFishingInputState state)
        {
            PlayerPrefs.SetInt(InputStateKey, (int)state);
            PlayerPrefs.Save();
        }
    }
}
