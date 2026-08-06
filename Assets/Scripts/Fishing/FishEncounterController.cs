using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs a single fish-fighting encounter using a FishData's difficulty stats.
/// Wire the UnityEvents below to your existing tension-gauge UI / rod visuals
/// instead of rewriting your gauge code here.
///
/// Usage: call BeginEncounter(fish) when a fish bites. Call SetReeling(true/false)
/// from your reel input (e.g. touch held down). Poll IsInSafeZone from your
/// gauge script, or replace the internal marker simulation below with your own.
/// </summary>
public class FishEncounterController : MonoBehaviour
{
    [Header("Setup")]
    public FishDatabase fishDatabase;
    [Tooltip("Where the caught fish's 3D model is spawned/attached (e.g. hook point)")]
    public Transform modelAttachPoint;

    [Header("Live State (read-only)")]
    public FishData currentFish;
    [Range(0f, 100f)] public float catchProgress;
    [Range(0f, 100f)] public float lineTension;
    public bool fishIsFighting;
    public bool isReeling;

    [Header("Events - hook these to your existing UI/gauge")]
    public UnityEvent<FishData> onFishHooked;
    public UnityEvent<float> onProgressChanged;   // 0-100
    public UnityEvent<float> onTensionChanged;    // 0-100
    public UnityEvent<bool> onFightStateChanged;  // true = fighting/red, false = calm/green
    public UnityEvent<FishData> onFishCaught;     // fires with score already on the fish
    public UnityEvent onLineSnapped;              // tension exceeded max -> fish escapes

    private GameObject _spawnedModel;
    private float _nextStateChangeTime;

    /// <summary>Call when a fish bites. Picks a random fish if none is passed.</summary>
    public void BeginEncounter(FishData fish = null)
    {
        currentFish = fish != null ? fish : fishDatabase.GetRandomFish();
        if (currentFish == null)
        {
            Debug.LogWarning("FishEncounterController: no fish available in database.");
            return;
        }

        catchProgress = 0f;
        lineTension = 0f;
        fishIsFighting = false;
        ScheduleNextStateChange();

        if (_spawnedModel != null) Destroy(_spawnedModel);
        if (currentFish.modelPrefab != null && modelAttachPoint != null)
            _spawnedModel = Instantiate(currentFish.modelPrefab, modelAttachPoint.position, modelAttachPoint.rotation, modelAttachPoint);

        onFishHooked?.Invoke(currentFish);
        onProgressChanged?.Invoke(catchProgress);
        onTensionChanged?.Invoke(lineTension);
        onFightStateChanged?.Invoke(fishIsFighting);
    }

    /// <summary>Call from your reel input (e.g. hold-to-reel button/touch).</summary>
    public void SetReeling(bool reeling)
    {
        isReeling = reeling;
    }

    private void Update()
    {
        if (currentFish == null) return;

        UpdateFishState();
        UpdateProgressAndTension();
    }

    private void UpdateFishState()
    {
        if (Time.time < _nextStateChangeTime) return;

        bool wasFighting = fishIsFighting;
        fishIsFighting = Random.value < currentFish.fightChance;
        ScheduleNextStateChange();

        if (fishIsFighting != wasFighting)
            onFightStateChanged?.Invoke(fishIsFighting);
    }

    private void ScheduleNextStateChange()
    {
        float variance = Random.Range(-currentFish.stateChangeIntervalVariance, currentFish.stateChangeIntervalVariance);
        _nextStateChangeTime = Time.time + Mathf.Max(0.1f, currentFish.stateChangeInterval + variance);
    }

    private void UpdateProgressAndTension()
    {
        float dt = Time.deltaTime;

        if (isReeling)
        {
            if (fishIsFighting)
            {
                catchProgress -= currentFish.progressLossRate * dt;
                lineTension += currentFish.tensionBuildRate * dt;
            }
            else
            {
                catchProgress += currentFish.progressGainRate * dt;
                lineTension = Mathf.Max(0f, lineTension - currentFish.tensionDecayRate * 0.5f * dt);
            }
        }
        else
        {
            lineTension = Mathf.Max(0f, lineTension - currentFish.tensionDecayRate * dt);
        }

        catchProgress = Mathf.Clamp(catchProgress, 0f, 100f);
        lineTension = Mathf.Clamp(lineTension, 0f, currentFish.lineTensionMax);

        onProgressChanged?.Invoke(catchProgress);
        onTensionChanged?.Invoke(lineTension);

        if (lineTension >= currentFish.lineTensionMax)
        {
            onLineSnapped?.Invoke();
            EndEncounter();
        }
        else if (catchProgress >= 100f)
        {
            onFishCaught?.Invoke(currentFish);
            EndEncounter();
        }
    }

    private void EndEncounter()
    {
        currentFish = null;
        isReeling = false;
    }
}
