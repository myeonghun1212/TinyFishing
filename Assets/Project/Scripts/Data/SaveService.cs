using System;
using System.Collections.Generic;
using UnityEngine;

namespace NanFishing.Data
{
    [Serializable]
    public sealed class SaveData
    {
        public int bestScore;
        public List<string> discoveredFishIds = new();
    }

    public sealed class SaveService
    {
        private const string SaveKey = "nan_fishing_save_v1";
        public SaveData Data { get; private set; }

        public SaveService()
        {
            Load();
        }

        public void RecordCatch(string fishId)
        {
            if (!string.IsNullOrWhiteSpace(fishId) && !Data.discoveredFishIds.Contains(fishId))
            {
                Data.discoveredFishIds.Add(fishId);
            }
        }

        public void RecordScore(int score)
        {
            Data.bestScore = Mathf.Max(Data.bestScore, score);
        }

        public void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            Data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
            Data ??= new SaveData();
            Data.discoveredFishIds ??= new List<string>();
        }
    }
}
