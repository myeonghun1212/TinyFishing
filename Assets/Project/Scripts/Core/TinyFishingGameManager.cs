using System;
using System.Collections;
using System.Collections.Generic;
using TinyFishing.Data;
using TinyFishing.Fishing;
using TinyFishing.Input;
using UnityEngine;
using NanFishing.Data;

namespace TinyFishing.Core
{
    // Orchestrates the TinyFishing Demo2 game loop:
    // ReadyToCast -> (shake to cast) -> WaitingForBite -> Reeling -> RoundResult -> ReadyToCast
    public sealed class TinyFishingGameManager : MonoBehaviour
    {
        private const string BestScoreKey = "TinyFishing.BestScore";

        public event Action<TinyFishingState> StateChanged;
        public event Action<int, int, int> ScoreChanged; // score, fishCaught, bestScore
        public event Action<float, float, bool, float> ReelingUpdated; // playerDir, fishDir, aligned, progress
        public event Action<bool, FishDefinition> RoundResolved; // caught?, the fish that was hooked (null if none)

        [SerializeField] private TinyFishingConfig config;
        [SerializeField] private MonoBehaviour inputBehaviour; // must implement ITinyFishingInput
        [SerializeField] private FishDefinition[] fishCatalog;
        [Tooltip("Optional: drives which fish types are in rotation and each one's relative catch chance. When assigned, this takes priority over fishCatalog's built-in rarity odds.")]
        [SerializeField] private FishPool fishPool;

        private ITinyFishingInput input;
        private FishDriftDriver fish;
        private ReelProgressModel reelModel;
        private FishDefinition currentFish;
        private readonly Dictionary<FishDefinition, int> sessionCatchCounts = new Dictionary<FishDefinition, int>();

        private int score;
        private int fishCaught;
        private int bestScore;

        public TinyFishingState State { get; private set; } = TinyFishingState.ReadyToCast;
        public TinyFishingConfig Config => config;
        public FishDefinition CurrentFish => currentFish;

        private void Awake()
        {
            input = inputBehaviour as ITinyFishingInput;
            if (input == null)
            {
                Debug.LogError("TinyFishingGameManager: assigned inputBehaviour does not implement ITinyFishingInput.");
                enabled = false;
                return;
            }

            fish = new FishDriftDriver(config);
            reelModel = new ReelProgressModel(config);
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        private void OnEnable()
        {
            if (input == null)
            {
                return;
            }
            input.CastPerformed += HandleCastPerformed;
            input.ReelTapPerformed += HandleReelTap;
        }

        private void OnDisable()
        {
            if (input == null)
            {
                return;
            }
            input.CastPerformed -= HandleCastPerformed;
            input.ReelTapPerformed -= HandleReelTap;
        }

        private void Start()
        {
            input.Recalibrate();
            SetState(TinyFishingState.ReadyToCast);
            ScoreChanged?.Invoke(score, fishCaught, bestScore);
        }

        private void Update()
        {
            if (State != TinyFishingState.Reeling)
            {
                return;
            }

            fish.Tick(Time.deltaTime);
            var aligned = reelModel.IsAligned(input.Direction, fish.Direction);
            ReelingUpdated?.Invoke(input.Direction, fish.Direction, aligned, reelModel.Progress);

            if (reelModel.IsCaught)
            {
                ResolveRound(true);
            }
        }

        private void HandleCastPerformed()
        {
            if (State != TinyFishingState.ReadyToCast)
            {
                return;
            }
            StartCoroutine(CastAndBiteRoutine());
        }

private IEnumerator CastAndBiteRoutine()
        {
            SetState(TinyFishingState.WaitingForBite);
            var delay = UnityEngine.Random.Range(config.biteDelayRange.x, config.biteDelayRange.y);
            yield return new WaitForSeconds(delay);

            currentFish = SelectFish();
            if (currentFish != null)
            {
                fish.SetFishOverride(currentFish.MoveSpeed, currentFish.DirectionChangeInterval);
            }
            else
            {
                fish.ClearFishOverride();
            }
            fish.Reset();
            reelModel.Reset(currentFish != null ? currentFish.Resistance : 1f,
                currentFish != null ? currentFish.MaxHealth : 100f);
            SetState(TinyFishingState.Reeling);
        }

private void HandleReelTap()
        {
            if (State != TinyFishingState.Reeling)
            {
                return;
            }

            reelModel.ApplyTap(input.Direction, fish.Direction, input.VerticalDirection);
            ReelingUpdated?.Invoke(input.Direction, fish.Direction,
                reelModel.IsAligned(input.Direction, fish.Direction), reelModel.Progress);
        }

private void ResolveRound(bool caught)
        {
            SetState(TinyFishingState.RoundResult);
            if (caught)
            {
                score += currentFish != null ? currentFish.BaseScore : config.fallbackScorePerCatch;
                fishCaught += 1;
                if (currentFish != null)
                {
                    sessionCatchCounts.TryGetValue(currentFish, out var caughtSoFar);
                    sessionCatchCounts[currentFish] = caughtSoFar + 1;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    PlayerPrefs.SetInt(BestScoreKey, bestScore);
                }
                ScoreChanged?.Invoke(score, fishCaught, bestScore);
            }

            RoundResolved?.Invoke(caught, caught ? currentFish : null);
            StartCoroutine(ReturnToCastingRoutine());
        }

private FishDefinition SelectFish()
        {
            if (fishPool != null)
            {
                var pooled = fishPool.GetRandomFish(sessionCatchCounts);
                if (pooled != null)
                {
                    return pooled;
                }
                // Pool assigned but nothing eligible right now (all disabled or session-capped) - fall back to catalog if present.
            }

            if (fishCatalog == null || fishCatalog.Length == 0)
            {
                return null;
            }

            var roll = UnityEngine.Random.value;
            var desiredRarity = roll < 0.02f ? FishRarity.Legendary :
                roll < 0.10f ? FishRarity.Epic :
                roll < 0.25f ? FishRarity.Rare :
                roll < 0.55f ? FishRarity.Uncommon : FishRarity.Common;
            var matches = Array.FindAll(fishCatalog, f => f != null && f.Rarity == desiredRarity);
            return matches.Length > 0 ? matches[UnityEngine.Random.Range(0, matches.Length)] :
                fishCatalog[UnityEngine.Random.Range(0, fishCatalog.Length)];
        }


        private IEnumerator ReturnToCastingRoutine()
        {
            yield return new WaitForSeconds(1.1f);
            SetState(TinyFishingState.ReadyToCast);
        }

        private void SetState(TinyFishingState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
