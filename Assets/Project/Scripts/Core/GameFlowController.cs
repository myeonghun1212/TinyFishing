using System;
using NanFishing.Data;
using NanFishing.Fishing;
using NanFishing.Input;
using NanFishing.Modes;
using NanFishing.UI;
using UnityEngine;

namespace NanFishing.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        private MotionInputService input;
        private FishingController fishing;
        private FishingHUD hud;
        private GameBalanceConfig config;
        private FishDefinition[] catalog;
        private IGameModeRule mode;
        private SaveService save;
        private bool sessionRunning;
        private int highestRarity;

        public void Initialize(MotionInputService motionInput, FishingController fishingController,
            FishingHUD gameHud, GameBalanceConfig balance, FishDefinition[] fishCatalog)
        {
            input = motionInput;
            fishing = fishingController;
            hud = gameHud;
            config = balance;
            catalog = fishCatalog;
            mode = new TimeAttackRule(config);
            save = new SaveService();

            input.CastPerformed += HandleFirstCast;
            fishing.StateChanged += HandleFishingState;
            fishing.RoundResolved += HandleRoundResolved;
            fishing.ReelingUpdated += hud.SetReeling;
            hud.CalibrationRestartRequested += input.BeginCalibration;
            hud.RecalibrateRequested += input.Recalibrate;
            hud.RestartRequested += Restart;

            hud.ShowStart(input.HasMotionSensors);
            input.BeginCalibration();
        }

        private void OnDestroy()
        {
            if (input != null) input.CastPerformed -= HandleFirstCast;
            if (fishing != null)
            {
                fishing.StateChanged -= HandleFishingState;
                fishing.RoundResolved -= HandleRoundResolved;
                fishing.ReelingUpdated -= hud.SetReeling;
            }
            if (hud != null)
            {
                hud.CalibrationRestartRequested -= input.BeginCalibration;
                hud.RecalibrateRequested -= input.Recalibrate;
                hud.RestartRequested -= Restart;
            }
        }

        private void Update()
        {
            if (!sessionRunning)
            {
                hud.SetCalibration(input.CalibrationProgress, input.IsCalibrated);
                return;
            }

            mode.Tick(Time.deltaTime);
            hud.SetSession(mode.TimeRemaining, mode.Score, mode.CatchCount, mode.Combo);
            if (mode.IsFinished)
            {
                FinishSession();
            }
        }

        private void HandleFirstCast(CastStrength strength)
        {
            if (sessionRunning || !input.IsCalibrated)
            {
                return;
            }

            sessionRunning = true;
            highestRarity = 0;
            mode.Begin();
            hud.ShowGameplay();
        }

        private void HandleFishingState(GameState state)
        {
            if (!sessionRunning && state == GameState.Casting)
            {
                return;
            }
            hud.SetState(state);
        }

        private void HandleRoundResolved(FishDefinition fish, bool caught, float catchDuration)
        {
            if (!sessionRunning)
            {
                return;
            }

            if (caught)
            {
                var awarded = mode.RegisterCatch(fish.BaseScore, (int)fish.Rarity, catchDuration);
                highestRarity = Mathf.Max(highestRarity, (int)fish.Rarity);
                save.RecordCatch(fish.Id);
                hud.ShowCatch(fish.DisplayName, awarded, fish.Color);
                Handheld.Vibrate();
            }
            else
            {
                mode.RegisterFailure();
                hud.ShowFailure();
            }
        }

        private void FinishSession()
        {
            sessionRunning = false;
            fishing.StopFishing();
            save.RecordScore(mode.Score);
            save.Save();
            hud.ShowResult(mode.Score, save.Data.bestScore, mode.CatchCount, mode.MaxCombo,
                highestRarity, save.Data.discoveredFishIds.Count, catalog.Length);
        }

        private void Restart()
        {
            sessionRunning = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
