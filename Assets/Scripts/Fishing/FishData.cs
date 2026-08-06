using UnityEngine;
using NanFishing.Data;

/// <summary>
/// Defines a single catchable fish species. Create one asset per fish via
/// Assets > Create > Fishing > Fish Data, assign a 3D model prefab, then tune
/// score/rarity/difficulty. Drop the asset into a FishDatabase to make it
/// spawnable.
/// </summary>
[CreateAssetMenu(fileName = "New Fish", menuName = "Fishing/Fish Data", order = 1)]
public class FishData : ScriptableObject
{
    [Header("Identity")]
    public string fishName = "New Fish";
    [TextArea] public string description;
    [Tooltip("3D model spawned on the line when this fish is hooked")]
    public GameObject modelPrefab;
    [Tooltip("Optional UI icon, e.g. for a catch log / dex")]
    public Sprite icon;

    [Header("Rewards")]
    public int scoreValue = 100;
    public FishRarity rarity = FishRarity.Common;
    [Tooltip("Relative weight used when randomly picking which fish bites. Higher = more common. Rarer fish should use lower weights.")]
    public float spawnWeight = 10f;

    [Header("Difficulty - Fish Marker Behavior")]
    [Tooltip("Average seconds between the fish changing state (calm <-> fighting)")]
    public float stateChangeInterval = 2f;
    [Tooltip("Random +/- range applied to stateChangeInterval so it isn't perfectly predictable")]
    public float stateChangeIntervalVariance = 0.5f;
    [Tooltip("How fast the fish marker moves inside the tension gauge")]
    public float markerSpeed = 50f;
    [Tooltip("Size of the safe (green) zone on the gauge, as a fraction 0-1 of the total gauge")]
    [Range(0.05f, 1f)] public float safeZoneSize = 0.4f;
    [Tooltip("Chance (0-1) the fish enters the fighting/red state on each state change")]
    [Range(0f, 1f)] public float fightChance = 0.3f;

    [Header("Difficulty - Progress & Tension")]
    [Tooltip("Catch progress gained per second while reeling with the marker in the safe zone")]
    public float progressGainRate = 20f;
    [Tooltip("Catch progress lost per second while reeling with the marker outside the safe zone")]
    public float progressLossRate = 15f;
    [Tooltip("Line tension added per second while reeling during a fight state")]
    public float tensionBuildRate = 25f;
    [Tooltip("Line tension recovered per second while not reeling or fish is calm")]
    public float tensionDecayRate = 15f;
    [Tooltip("Tension value at which the line snaps and the fish escapes")]
    public float lineTensionMax = 100f;

    [Header("Gating (optional)")]
    [Tooltip("Minimum player level/skill required before this fish can be encountered. Leave 0 if unused.")]
    public int minPlayerLevel = 0;
}
