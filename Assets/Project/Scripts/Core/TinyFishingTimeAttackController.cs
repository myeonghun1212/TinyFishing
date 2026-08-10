using System;
using System.Collections;
using System.Collections.Generic;
using NanFishing.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyFishing.Core
{
    /// <summary>
    /// Owns only the time-attack rules. The fishing loop remains in
    /// <see cref="TinyFishingGameManager"/> and is shared with infinite mode.
    /// </summary>
    public sealed class TinyFishingTimeAttackController : MonoBehaviour
    {
        public event Action<float> TimeChanged;
        public event Action<int, int, Dictionary<FishDefinition, int>> Finished; // final score, best score, session catch counts

        [SerializeField] private TinyFishingGameManager gameManager;
        [SerializeField, Min(1f)] private float durationSeconds = 60f;
        [SerializeField, Min(0f)] private float resultDisplaySeconds = 30f;
        [SerializeField] private AudioSource timeUpAudioSource;
        [SerializeField] private AudioClip timeUpClip;

        private float remainingSeconds;
        private bool running;

        public bool IsTimeAttack => gameManager != null &&
                                    gameManager.GameMode == TinyFishingGameMode.TimeLimited;
        public float RemainingSeconds => remainingSeconds;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<TinyFishingGameManager>();
            }
        }

        private void Start()
        {
            if (!IsTimeAttack)
            {
                enabled = false;
                return;
            }

            remainingSeconds = durationSeconds;
            running = true;
            TimeChanged?.Invoke(remainingSeconds);
        }

        private void Update()
        {
            if (!running || gameManager == null || gameManager.IsPaused)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.unscaledDeltaTime);
            TimeChanged?.Invoke(remainingSeconds);

            if (remainingSeconds <= 0f)
            {
                Finish();
            }
        }

        private void Finish()
        {
            running = false;
            gameManager.EndSession();
            PlayTimeUpSfx();
            Finished?.Invoke(gameManager.Score, gameManager.BestScore, gameManager.SessionCatchCounts);
            StartCoroutine(ReturnToStartRoutine());
        }

        private void PlayTimeUpSfx()
        {
            if (timeUpAudioSource == null || timeUpClip == null)
            {
                return;
            }

            timeUpAudioSource.Stop();
            timeUpAudioSource.clip = timeUpClip;
            timeUpAudioSource.Play();
        }
        private IEnumerator ReturnToStartRoutine()
        {
            if (resultDisplaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(resultDisplaySeconds);
            }

            SceneManager.LoadScene(gameManager.StartSceneName);
        }
    }
}
