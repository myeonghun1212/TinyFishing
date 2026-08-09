using System;
using System.Collections;
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
        public event Action<int, int> Finished; // final score, best score

        [SerializeField] private TinyFishingGameManager gameManager;
        [SerializeField, Min(1f)] private float durationSeconds = 60f;
        [SerializeField, Min(0f)] private float resultDisplaySeconds = 4f;
        [SerializeField] private string startSceneName = "Pond Start Menu";

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
            if (!running)
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
            Finished?.Invoke(gameManager.Score, gameManager.BestScore);
            StartCoroutine(ReturnToStartRoutine());
        }

        private IEnumerator ReturnToStartRoutine()
        {
            if (resultDisplaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(resultDisplaySeconds);
            }

            SceneManager.LoadScene(startSceneName);
        }
    }
}
