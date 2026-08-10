#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TinyFishing.Editor
{
    /// <summary>
    /// Provides development utilities for resetting locally persisted game data.
    /// </summary>
    public static class TinyFishingPlayerPrefsTools
    {
        private const string MenuPath = "Tools/Tiny Fishing/Clear Saved PlayerPrefs";

        [MenuItem(MenuPath)]
        public static void ClearSavedPlayerPrefs()
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Clear Saved PlayerPrefs",
                "Delete all locally saved Tiny Fishing data?\n\n" +
                "This includes best scores, game mode, input settings, and audio volumes.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("Tiny Fishing PlayerPrefs were cleared.");
            EditorUtility.DisplayDialog(
                "PlayerPrefs Cleared",
                "All locally saved Tiny Fishing data has been deleted.",
                "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateClearSavedPlayerPrefs()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
#endif
