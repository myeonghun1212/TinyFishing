using UnityEngine;

namespace TinyFishing.Core
{
    /// <summary>
    /// Stores the selected game mode independently from the shared input preference.
    /// </summary>
    public static class TinyFishingGameModePreferences
    {
        private const string GameModeKey = "TinyFishing.GameMode";

        public static TinyFishingGameMode Load(
            TinyFishingGameMode fallback = TinyFishingGameMode.Infinite)
        {
            var storedValue = PlayerPrefs.GetInt(GameModeKey, (int)fallback);
            return System.Enum.IsDefined(typeof(TinyFishingGameMode), storedValue)
                ? (TinyFishingGameMode)storedValue
                : fallback;
        }

        public static void Save(TinyFishingGameMode mode)
        {
            PlayerPrefs.SetInt(GameModeKey, (int)mode);
            PlayerPrefs.Save();
        }
    }
}
