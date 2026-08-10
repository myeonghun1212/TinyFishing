using System;
using System.Collections;
using System.Collections.Generic;
using TinyFishing.Data;
using TinyFishing.Fishing;
using TinyFishing.Input;
using UnityEngine;
using NanFishing.Data;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace TinyFishing.Core
{
    // Orchestrates the TinyFishing Demo2 game loop:
    // ReadyToCast -> (shake to cast) -> WaitingForBite -> Approaching -> Biting -> Reeling -> RoundResult -> ReadyToCast
    //   WaitingForBite: bob is out; after a random delay a fish is chosen and starts swimming in.
    //   Approaching: FishSpawnVolume hands the chosen fish to a FishBiteAgent that swims it beneath the bob.
    //     A reel tap here is premature - it spooks the fish back to the pool and forces a rethrow.
    //   Biting: the fish has arrived, the bob dips, and a short hook window is open. A reel tap inside
    //     the window hooks the fish and starts Reeling; too late (or no tap) lets it escape.
    //   Reeling / RoundResult: unchanged tap-to-align minigame, then back to ReadyToCast.
    public sealed class TinyFishingGameManager : MonoBehaviour
    {
        public event Action<TinyFishingState> StateChanged;
        public event Action<int, int, int> ScoreChanged; // score, fishCaught, bestScore
        public event Action<float, float, bool, float> ReelingUpdated; // playerDir, fishDir, aligned, progress
        public event Action<bool, FishDefinition> RoundResolved; // caught?, the fish that was hooked (null if none)
        public event Action<bool> PauseChanged;

        [Header("Game Mode Configs")]
        [FormerlySerializedAs("config")]
        [SerializeField] private TinyFishingConfig infiniteConfig;
        [SerializeField] private TinyFishingConfig timeLimitedConfig;
        [SerializeField] private MonoBehaviour inputBehaviour; // must implement ITinyFishingInput
        [SerializeField] private FishDefinition[] fishCatalog;
        [Tooltip("Optional: drives which fish types are in rotation and each one's relative catch chance. When assigned, this takes priority over fishCatalog's built-in rarity odds.")]
        [SerializeField] private FishPool fishPool;
        [SerializeField] private FishingBobController bobController;
        [Tooltip("The volume of roaming fish visuals the chosen fish swims in from during Approaching. Optional - if unassigned (or it has no free fish), the round skips straight to the bite/hook window with no swim-in visual.")]
        [SerializeField] private FishSpawnVolume fishSpawnVolume;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip goodReelClip;
        [SerializeField] private AudioClip badReelClip;
        [SerializeField] private AudioSource catchAudioSource;
        [SerializeField] private AudioClip successfulCatchClip;
        [SerializeField] private AudioClip unsuccessfulCatchClip;
        [Tooltip("Looping AudioSource dedicated to the rope-tighten sfx - separate from the other sources so it can loop for the whole Reeling state without being stopped by tap/catch sfx.")]
        [SerializeField] private AudioSource ropeAudioSource;
        [SerializeField] private AudioClip ropeTightenClip;

        private ITinyFishingInput input;
        private FishDriftDriver fish;
        private ReelProgressModel reelModel;
        [SerializeField] private FishDefinition currentFish;
        private readonly Dictionary<FishDefinition, int> sessionCatchCounts = new Dictionary<FishDefinition, int>();

        private Coroutine activeRoutine;
        private FishBiteAgent biteAgent;
        private bool hookWindowOpen;
        private bool bobDipped;

        private int score;
        private int fishCaught;
        private int bestScore;
        private bool sessionActive = true;
        private bool isPaused;
        private bool ownsGlobalPause;
        private float timeScaleBeforePause = 1f;
        private bool audioPauseBeforePause;
        private TinyFishingConfig config;
        [SerializeField] private string startSceneName;

        public TinyFishingState State { get; private set; } = TinyFishingState.ReadyToCast;
        public TinyFishingConfig Config => config;
        public FishDefinition CurrentFish => currentFish;
        public TinyFishingGameMode GameMode { get; private set; }
        public int Score => score;
        public int FishCaught => fishCaught;
        public int BestScore => bestScore;
        public bool IsSessionActive => sessionActive;
        public bool IsPaused => isPaused;
        public Dictionary<FishDefinition, int> SessionCatchCounts => sessionCatchCounts;

        public string StartSceneName => startSceneName;

        private void Awake()
        {
            input = inputBehaviour as ITinyFishingInput;
            if (input == null)
            {
                Debug.LogError("TinyFishingGameManager: assigned inputBehaviour does not implement ITinyFishingInput.");
                enabled = false;
                return;
            }

            GameMode = TinyFishingGameModePreferences.Load();
            config = GameMode == TinyFishingGameMode.TimeLimited
                ? timeLimitedConfig
                : infiniteConfig;

            if (config == null)
            {
                config = infiniteConfig != null ? infiniteConfig : timeLimitedConfig;
                Debug.LogWarning($"TinyFishingGameManager: no config is assigned for {GameMode}; using the other mode's config as a fallback.");
            }

            if (config == null)
            {
                Debug.LogError("TinyFishingGameManager: both game mode configs are missing.");
                enabled = false;
                return;
            }

            input.Configure(config);
            fish = new FishDriftDriver(config);
            reelModel = new ReelProgressModel(config);
            bestScore = PlayerPrefs.GetInt(TinyFishingScoreKeys.For(GameMode), 0);
        }

        private void OnEnable()
        {
            if (input == null)
            {
                return;
            }
            input.CastPerformed += HandleCastPerformed;
            input.ReelTapPerformed += HandleReelTap;
            input.HookAttemptPerformed += HandleHookAttempt;
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.CastPerformed -= HandleCastPerformed;
                input.ReelTapPerformed -= HandleReelTap;
                input.HookAttemptPerformed -= HandleHookAttempt;
            }

            // Time.timeScale and AudioListener.pause are global. Never leave them altered
            // when this manager is disabled during a scene transition or domain reload.
            RestoreGlobalPauseState();
            isPaused = false;
        }

        private void Start()
        {
            input.Recalibrate();
            SetState(TinyFishingState.ReadyToCast);
            ScoreChanged?.Invoke(score, fishCaught, bestScore);
        }

        private void Update()
        {
            if (!sessionActive || isPaused || State != TinyFishingState.Reeling)
            {
                return;
            }

            fish.Tick(Time.deltaTime);
            reelModel.Tick(Time.deltaTime);
            var aligned = reelModel.IsAligned(input.Direction, fish.Direction);
            ReelingUpdated?.Invoke(input.Direction, fish.Direction, aligned, reelModel.Progress);

            // Fish marker red (misaligned) pulls the bob under; safe (aligned) lets it ease back.
            bobController?.UpdateReelTension(!aligned);

            if (reelModel.HasEscaped)
            {
                ResolveRound(false);
                return;
            }

            if (reelModel.IsCaught)
            {
                ResolveRound(true);
            }
        }

        private void HandleCastPerformed(float castStrength)
        {
            if (!sessionActive || isPaused || State != TinyFishingState.ReadyToCast)
            {
                return;
            }

            activeRoutine = StartCoroutine(CastAndBiteRoutine(Mathf.Clamp01(castStrength)));
        }

        private IEnumerator CastAndBiteRoutine(float castStrength)
        {
            SetState(TinyFishingState.WaitingForBite);
            if (bobController != null)
            {
                bobController.Launch(castStrength);
            }
            var delay = UnityEngine.Random.Range(config.biteDelayRange.x, config.biteDelayRange.y);
            yield return new WaitForSeconds(delay);

            currentFish = SelectFish();
            if (currentFish != null)
            {
                fish.SetFishOverride(currentFish.MoveSpeed, currentFish.DirectionChangeInterval / currentFish.Resistance);
            }
            else
            {
                fish.ClearFishOverride();
            }

            // Send the chosen fish swimming in toward the bob. If no spawn volume is
            // assigned, or it has no free fish to give up, skip straight to the bite -
            // the round stays playable, it just won't have a swim-in visual this time.
            SetState(TinyFishingState.Approaching);
            biteAgent = fishSpawnVolume != null && bobController != null
                ? fishSpawnVolume.BeginBiteApproach(currentFish, bobController.transform, config.fishBiteApproachSpeed)
                : null;

            if (biteAgent != null)
            {
                while (!biteAgent.HasArrived)
                {
                    yield return null;
                }
            }

            // Bob dips and the hook window opens. A hook attempt while State is Biting and
            // hookWindowOpen is true (handled in HandleHookAttempt) hooks the fish and starts
            // Reeling; if the window elapses first, the loop below falls through to a miss.
            SetState(TinyFishingState.Biting);
            if (bobController != null)
            {
                bobController.BeginDip(config.bobDipDepth, config.bobDipDownTime);
            }
            bobDipped = true;
            hookWindowOpen = true;

            var elapsed = 0f;
            while (elapsed < config.biteHookWindowSeconds && State == TinyFishingState.Biting)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Still Biting after the loop means nobody hooked it in time - a successful
            // hook (HandleHookAttempt) already moved State to Reeling and exited the loop above.
            if (State == TinyFishingState.Biting)
            {
                hookWindowOpen = false;
                FailBite();
            }

            activeRoutine = null;
        }

        private void HandleReelTap()
        {
            if (!sessionActive || isPaused)
            {
                return;
            }

            switch (State)
            {
                case TinyFishingState.WaitingForBite:
                case TinyFishingState.Approaching:
                    // Taps before the bob has even splashed down are ignored outright - nothing
                    // is happening yet, so there's no fish to spook. Once it's in the water,
                    // an early tap (before the bite dip) still spooks the fish off early.
                    if (bobController != null && !bobController.IsInWater)
                    {
                        return;
                    }
                    AbortBite();
                    break;

                case TinyFishingState.Reeling:
                    var goodTap = reelModel.ApplyTap(input.Direction, fish.Direction, input.VerticalDirection);
                    PlayReelTapSfx(goodTap);
                    ReelingUpdated?.Invoke(input.Direction, fish.Direction,
                        reelModel.IsAligned(input.Direction, fish.Direction), reelModel.Progress);
                    break;
            }
        }

        private void HandleHookAttempt()
        {
            if (!sessionActive || isPaused || State != TinyFishingState.Biting || !hookWindowOpen)
            {
                return;
            }

            hookWindowOpen = false;
            BeginReelingAfterHook();
        }

        // Cancels the in-flight cast/bite coroutine (if any) and resolves the round as a
        // miss - used when the player reels too early (before the bob dips) or too late
        // (the hook window elapses with no tap).
        private void AbortBite()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            FailBite();
        }

        private void FailBite()
        {
            hookWindowOpen = false;

            if (biteAgent != null)
            {
                fishSpawnVolume?.CancelBiteApproach(biteAgent);
                biteAgent = null;
            }

            if (bobDipped && bobController != null)
            {
                bobController.EndDip(false, config.bobDipReturnTime);
            }
            bobDipped = false;

            SetState(TinyFishingState.RoundResult);
            PlayMissSfx();
            RoundResolved?.Invoke(false, null);
            StartCoroutine(ReturnToCastingRoutine());
        }

        // Called from HandleHookAttempt when an input lands inside the Biting hook window -
        // the fish is hooked, so hand it off to the real reeling minigame.
        private void BeginReelingAfterHook()
        {
            if (bobController != null)
            {
                bobController.EndDip(true, config.bobDipReturnTime);
                // Fish stays hooked under the bob (FishBiteAgent keeps tracking it) all the
                // way through Reeling - it's only removed from the pool once the round
                // actually resolves as a catch (see ResolveRound).
                bobController.BeginReelHold();
            }
            bobDipped = false;

            fish.Reset();
            reelModel.Reset(currentFish != null ? currentFish.MaxHealth : 100f,
                currentFish != null ? currentFish.ProgressDecayRate : config.fallbackProgressDecayRate);
            SetState(TinyFishingState.Reeling);
            PlayRopeLoop();
        }

        private void PlayRopeLoop()
        {
            if (ropeAudioSource == null || ropeTightenClip == null)
            {
                return;
            }

            ropeAudioSource.clip = ropeTightenClip;
            ropeAudioSource.loop = true;
            ropeAudioSource.Play();
        }

        private void StopRopeLoop()
        {
            if (ropeAudioSource == null)
            {
                return;
            }

            ropeAudioSource.Stop();
        }

        private void PlayReelTapSfx(bool goodTap)
        {
            if (audioSource == null)
            {
                return;
            }

            var clip = goodTap ? goodReelClip : badReelClip;
            if (clip == null)
            {
                return;
            }

            // Stop-and-restart on a single AudioSource so good/bad tap sfx never overlap
            // each other or themselves on rapid taps - only the latest tap's sound plays.
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }

        private void PlayCatchSfx()
        {
            if (catchAudioSource == null || successfulCatchClip == null)
            {
                return;
            }

            // Own AudioSource, separate from the tap source, so the catch sound isn't
            // cut off by (or cuts off) the good/bad tap sfx stop-and-restart logic.
            catchAudioSource.Stop();
            catchAudioSource.clip = successfulCatchClip;
            catchAudioSource.Play();
        }

        private void PlayMissSfx()
        {
            if (catchAudioSource == null || unsuccessfulCatchClip == null)
            {
                return;
            }

            // Same AudioSource as the successful-catch sfx - a round can only resolve one
            // way, so there's never a need for the two to play at once.
            catchAudioSource.Stop();
            catchAudioSource.clip = unsuccessfulCatchClip;
            catchAudioSource.Play();
        }

        private void ResolveRound(bool caught)
        {
            SetState(TinyFishingState.RoundResult);
            StopRopeLoop();
            bobController?.EndReelHold();

            if (biteAgent != null)
            {
                if (caught)
                {
                    fishSpawnVolume?.ResolveBiteCaught(biteAgent);
                }
                else
                {
                    fishSpawnVolume?.CancelBiteApproach(biteAgent);
                }
                biteAgent = null;
            }

            if (caught)
            {
                PlayCatchSfx();
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
                    PlayerPrefs.SetInt(TinyFishingScoreKeys.For(GameMode), bestScore);
                    PlayerPrefs.Save();
                }
                ScoreChanged?.Invoke(score, fishCaught, bestScore);
            }
            else
            {
                PlayMissSfx();
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
            if (sessionActive)
            {
                SetState(TinyFishingState.ReadyToCast);
            }
        }

        /// <summary>
        /// Stops the shared fishing loop without changing scenes. Mode-specific controllers
        /// can then present results and decide where the player goes next.
        /// </summary>
        public void EndSession()
        {
            if (!sessionActive)
            {
                return;
            }

            if (isPaused)
            {
                ExitPause(resumeInput: false);
            }

            sessionActive = false;
            input.SetInputEnabled(false);
            StopAllCoroutines();
            activeRoutine = null;
            hookWindowOpen = false;

            if (biteAgent != null)
            {
                fishSpawnVolume?.CancelBiteApproach(biteAgent);
                biteAgent = null;
            }

            bobDipped = false;
            StopRopeLoop();
            bobController?.EndReelHold();
            bobController?.ResetToRest();
            SetState(TinyFishingState.GameOver);
        }

        public void PauseGame()
        {
            if (isPaused || !sessionActive)
            {
                return;
            }

            isPaused = true;
            timeScaleBeforePause = Time.timeScale;
            audioPauseBeforePause = AudioListener.pause;
            ownsGlobalPause = true;

            input.SetInputEnabled(false);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            PauseChanged?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (!isPaused)
            {
                return;
            }

            ExitPause(resumeInput: sessionActive);
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        private void ExitPause(bool resumeInput)
        {
            isPaused = false;
            RestoreGlobalPauseState();

            if (resumeInput)
            {
                input.SetInputEnabled(true);
            }

            PauseChanged?.Invoke(false);
        }

        private void RestoreGlobalPauseState()
        {
            if (!ownsGlobalPause)
            {
                return;
            }

            Time.timeScale = timeScaleBeforePause;
            AudioListener.pause = audioPauseBeforePause;
            ownsGlobalPause = false;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PauseGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                PauseGame();
            }
        }

        private void SetState(TinyFishingState next)
        {
            State = next;
            if (next == TinyFishingState.ReadyToCast && bobController != null)
            {
                bobController.ResetToRest();
            }
            StateChanged?.Invoke(next);
        }

        public void RestartGame()
        {
            PrepareForSceneChange();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToStart()
        {
            PrepareForSceneChange();
            SceneManager.LoadScene(startSceneName);
        }

        private void PrepareForSceneChange()
        {
            if (isPaused)
            {
                ExitPause(resumeInput: false);
            }
            input?.SetInputEnabled(false);
        }

    }
}

