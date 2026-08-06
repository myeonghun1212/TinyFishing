using System.Collections.Generic;
using UnityEngine;
using NanFishing.Data;
using UnityEngine;

/// <summary>
/// Holds every FishData asset in the game and picks one at random, weighted
/// by each fish's spawnWeight (rarer fish should use lower weights).
/// Create via Assets > Create > Fishing > Fish Database, then drag all your
/// FishData assets into the list.
/// </summary>
[CreateAssetMenu(fileName = "FishDatabase", menuName = "Fishing/Fish Database", order = 0)]
public class FishDatabase : ScriptableObject
{
    public List<FishData> allFish = new List<FishData>();

    /// <summary>
    /// Weighted-random pick from every fish the given player level can encounter.
    /// Pass int.MaxValue (default) to ignore level gating.
    /// </summary>
    public FishData GetRandomFish(int playerLevel = int.MaxValue)
    {
        List<FishData> eligible = allFish.FindAll(f => f != null && f.minPlayerLevel <= playerLevel);
        if (eligible.Count == 0) return null;

        float totalWeight = 0f;
        foreach (FishData f in eligible)
            totalWeight += Mathf.Max(0f, f.spawnWeight);

        if (totalWeight <= 0f)
            return eligible[Random.Range(0, eligible.Count)];

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (FishData f in eligible)
        {
            cumulative += Mathf.Max(0f, f.spawnWeight);
            if (roll <= cumulative) return f;
        }
        return eligible[eligible.Count - 1];
    }

    public List<FishData> GetFishByRarity(FishRarity rarity)
    {
        return allFish.FindAll(f => f != null && f.rarity == rarity);
    }
}
