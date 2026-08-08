using System;
using System.Collections.Generic;
using NanFishing.Data;
using UnityEngine;

namespace TinyFishing.Data
{
    // One configurable slot in a FishPool: which fish, whether it's currently
    // catchable, and its relative chance of being picked. "How many" of a fish
    // type is expressed here as a spawn weight rather than a literal inventory
    // count, since fish aren't consumed - weight controls how often it bites.
    [Serializable]
    public sealed class FishPoolEntry
    {
        public FishDefinition fish;
        [Tooltip("Whether this fish can currently be caught. Disable to remove it from rotation without deleting the entry.")]
        public bool enabled = true;
        [Tooltip("Relative chance of this fish being picked. Weights are compared against the sum of all enabled entries' weights, so absolute scale doesn't matter, only the ratio between fish.")]
        [Min(0f)] public float weight = 10f;
        [Tooltip("Optional cap on how many of this fish can be caught in a single play session. 0 = unlimited.")]
        [Min(0)] public int maxCatchesPerSession = 0;
        [Tooltip("How many of this fish should be actively roaming a FishSpawnVolume at once. Separate from weight, which only affects catch odds during reeling.")]
        [Min(0)] public int spawnCount = 3;
    }

    // A configurable catch pool: pick how many fish types are in rotation, which
    // ones, and each one's relative catch chance, all from one asset instead of
    // hardcoded rarity odds in code.
    [CreateAssetMenu(menuName = "TinyFishing/Fish Pool", fileName = "FishPool")]
    public sealed class FishPool : ScriptableObject
    {
        [SerializeField] private List<FishPoolEntry> entries = new List<FishPoolEntry>();

        public IReadOnlyList<FishPoolEntry> Entries => entries;

        /// <summary>
        /// Weighted-random pick among enabled entries whose per-session catch cap
        /// (if any) hasn't been reached yet. sessionCatchCounts is optional - pass
        /// null to ignore session caps entirely.
        /// </summary>
        public FishDefinition GetRandomFish(Dictionary<FishDefinition, int> sessionCatchCounts = null)
        {
            var eligible = new List<FishPoolEntry>();
            float totalWeight = 0f;

            foreach (var entry in entries)
            {
                if (entry == null || entry.fish == null || !entry.enabled || entry.weight <= 0f)
                {
                    continue;
                }

                if (entry.maxCatchesPerSession > 0 && sessionCatchCounts != null)
                {
                    sessionCatchCounts.TryGetValue(entry.fish, out var caughtSoFar);
                    if (caughtSoFar >= entry.maxCatchesPerSession)
                    {
                        continue;
                    }
                }

                eligible.Add(entry);
                totalWeight += entry.weight;
            }

            if (eligible.Count == 0)
            {
                return null;
            }

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            var cumulative = 0f;
            foreach (var entry in eligible)
            {
                cumulative += entry.weight;
                if (roll <= cumulative)
                {
                    return entry.fish;
                }
            }
            return eligible[eligible.Count - 1].fish;
        }
    }
}
