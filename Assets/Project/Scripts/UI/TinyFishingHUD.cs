using TinyFishing.Core;
using TinyFishing.Fishing;
using TinyFishing.Input;
using NanFishing.Data;
using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFishing.UI
{
    // Binds the TinyFishing Demo2 gameplay Canvas to a TinyFishingGameManager.
    // Gauge is drawn as a straight horizontal bar (rounded-cap sprite) rather than
    // the curved arc from the concept sketch - swap gaugeTrack for a curved sprite
    // and this still works, since markers are positioned purely via anchoredPosition.x.
    public sealed class TinyFishingHUD : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TinyFishingGameManager gameManager;

        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI fishCountText;
        [SerializeField] private TextMeshProUGUI bestText;

        [Header("Gauge Visibility")]
        [Tooltip("CanvasGroup wrapping the gauge track/progress bar. Hidden (alpha 0) until a bite is successfully hooked, then fades in as Reeling starts.")]
        [SerializeField] private CanvasGroup gaugeCanvasGroup;
        [SerializeField] private float gaugeFadeDuration = 0.25f;

        [Header("Gauge")]
        [SerializeField] private RectTransform gaugeTrack;
        [SerializeField] private RectTransform fishMarker;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private Image playerMarkerImage;
        [SerializeField, Range(0f, 1f)] private float playerMarkerReelingDarken = 0.55f;
        [SerializeField] private Image fishMarkerImage;
        [SerializeField] private Sprite fishSafeSprite;
        [SerializeField] private Sprite fishDangerSprite;
        [SerializeField] private Color alignedColor = new Color(0.30f, 0.85f, 0.35f);
        [SerializeField] private Color misalignedColor = new Color(0.92f, 0.20f, 0.20f);
        [SerializeField] private AudioSource splashAudioSource;
        [SerializeField] private AudioClip splashClip;

        [Header("Progress")]
        [SerializeField] private Image progressFill;
        [SerializeField] private Color progressEscapeColor = new Color(0.92f, 0.20f, 0.20f);

        [Header("Rod (3D)")]
        [SerializeField] private FishingRodController rodController;
        
        [Header("Rope (3D)")]
        [Tooltip("Renderer on the 3D rope/line mesh. Its material is tinted from normal toward ropeEscapeColor while the fish sits at 0% progress during the escape grace period.")]
        [SerializeField] private Renderer[] ropeRenderers;
        [SerializeField] private Color ropeEscapeColor = new Color(0.92f, 0.20f, 0.20f);
[SerializeField] private MonoBehaviour inputBehaviour; // must implement ITinyFishingInput; used to recalibrate on round reset

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private TextMeshProUGUI caughtFishNameText;
        [SerializeField] private Transform caughtFishModelAnchor;

        private float halfTrackWidth;
        private GameObject caughtFishModelInstance;
        private ITinyFishingInput rodInput;
        private float lastFishMarkerX;
        private bool fishMarkerFacingRight = true;
        private bool lastAligned = true;
        private bool alignedInitialized;
        private float fishMarkerBaseScaleX = 1f;
        private float fishMarkerBaseScaleY = 1f;
        private Coroutine directionFlipRoutine;
        private Coroutine stateFlipRoutine;
        [SerializeField] private float flipDuration = 0.15f;
        private Color playerMarkerBaseColor = Color.white;
        private Coroutine gaugeFadeRoutine;
                private Color[] ropeNormalColors;
        private float ropeEscapeTimer;
private Color progressFillNormalColor = Color.white;


private void Awake()
        {
            if (gaugeTrack != null)
            {
                halfTrackWidth = gaugeTrack.rect.width * 0.5f;
            }

            if (fishMarker != null)
            {
                fishMarkerBaseScaleX = Mathf.Abs(fishMarker.localScale.x);
                fishMarkerBaseScaleY = Mathf.Abs(fishMarker.localScale.y);
            }

            if (playerMarkerImage != null)
            {
                playerMarkerBaseColor = playerMarkerImage.color;
            }

            if (progressFill != null)
            {
                

            if (ropeRenderers != null && ropeRenderers.Length > 0)
            {
                ropeNormalColors = new Color[ropeRenderers.Length];
                for (int i = 0; i < ropeRenderers.Length; i++)
                {
                    ropeNormalColors[i] = ropeRenderers[i] != null ? ropeRenderers[i].material.color : Color.white;
                }
            }
progressFillNormalColor = progressFill.color;
            }

            rodInput = inputBehaviour as ITinyFishingInput;

            if (gaugeCanvasGroup != null)
            {
                gaugeCanvasGroup.alpha = 0f;
            }
        }


        private void OnEnable()
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.StateChanged += HandleStateChanged;
            gameManager.ScoreChanged += HandleScoreChanged;
            gameManager.ReelingUpdated += HandleReelingUpdated;
            gameManager.RoundResolved += HandleRoundResolved;
        }

        private void OnDisable()
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.StateChanged -= HandleStateChanged;
            gameManager.ScoreChanged -= HandleScoreChanged;
            gameManager.ReelingUpdated -= HandleReelingUpdated;
            gameManager.RoundResolved -= HandleRoundResolved;
        }

        // Called by the HUD recalibration button. The current phone attitude becomes the
        // new neutral pose and the rendered rod immediately returns to its rest rotation.
        public void RecalibrateGyro()
        {
            rodInput?.Recalibrate();
            rodController?.ResetRod();
        }

private void SetGaugeVisible(bool visible, bool instant)
        {
            if (gaugeCanvasGroup == null)
            {
                return;
            }

            var target = visible ? 1f : 0f;

            if (gaugeFadeRoutine != null)
            {
                StopCoroutine(gaugeFadeRoutine);
                gaugeFadeRoutine = null;
            }

            if (instant || gaugeFadeDuration <= 0f)
            {
                gaugeCanvasGroup.alpha = target;
                return;
            }

            gaugeFadeRoutine = StartCoroutine(FadeGaugeRoutine(target));
        }

        private IEnumerator FadeGaugeRoutine(float target)
        {
            var start = gaugeCanvasGroup.alpha;
            var duration = Mathf.Max(0.01f, gaugeFadeDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                gaugeCanvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            gaugeCanvasGroup.alpha = target;
            gaugeFadeRoutine = null;
        }


private void HandleStateChanged(TinyFishingState state)
        {
            switch (state)
            {
                case TinyFishingState.ReadyToCast:
                    instructionText.text = "Shake your phone to cast!";
                    SetGaugeVisible(false, instant: true);
                    feedbackText.text = string.Empty;
                    if (caughtFishNameText != null)
                    {
                        caughtFishNameText.text = string.Empty;
                    }
                    if (caughtFishModelInstance != null)
                    {
                        Destroy(caughtFishModelInstance);
                        caughtFishModelInstance = null;
                    }
                    SetProgress(0f, false);
                    ResetRig();
                    break;
                case TinyFishingState.WaitingForBite:
                    instructionText.text = "Waiting for a bite...";
                    break;
                case TinyFishingState.Approaching:
                    instructionText.text = "Something's circling the bait...";
                    break;
                case TinyFishingState.Biting:
                    instructionText.text = "Tap now!";
                    break;
                case TinyFishingState.Reeling:
                    instructionText.text = "Tilt to aim, tap when green!";
                    SetGaugeVisible(true, instant: false);
                    break;
                case TinyFishingState.RoundResult:
                    instructionText.text = string.Empty;
                    break;
            }
        }

        private void Update()
        {
            if (rodInput != null)
            {
                SetPlayerMarkerDarkened(rodInput.IsPressed);
            }
        }

        private void SetPlayerMarkerDarkened(bool darkened)
        {
            if (playerMarkerImage == null)
            {
                return;
            }

            var factor = darkened ? playerMarkerReelingDarken : 1f;
            playerMarkerImage.color = new Color(
                playerMarkerBaseColor.r * factor,
                playerMarkerBaseColor.g * factor,
                playerMarkerBaseColor.b * factor,
                playerMarkerBaseColor.a);
        }

        private void HandleScoreChanged(int score, int fishCaught, int bestScore)
        {
            scoreText.text = $"SCORE\n{score}";
            fishCountText.text = $"x{fishCaught}";
            bestText.text = $"BEST: {bestScore}";
        }

private void HandleReelingUpdated(float playerDirection, float fishDirection, bool aligned, float progress)
        {
            float newFishX = fishDirection * halfTrackWidth;
            fishMarker.anchoredPosition = new Vector2(newFishX, fishMarker.anchoredPosition.y);
            playerMarker.anchoredPosition = new Vector2(playerDirection * halfTrackWidth, playerMarker.anchoredPosition.y);

            // Flip the fish marker across the vertical axis (mirror left/right) whenever
            // its horizontal travel direction reverses, so it visually faces the way it's moving.
            float deltaX = newFishX - lastFishMarkerX;
            const float directionEpsilon = 0.01f;
            if (Mathf.Abs(deltaX) > directionEpsilon)
            {
                bool movingRight = deltaX > 0f;
                if (movingRight != fishMarkerFacingRight)
                {
                    fishMarkerFacingRight = movingRight;
                    StartFlip(ref directionFlipRoutine, xAxis: true, targetSign: fishMarkerFacingRight ? 1f : -1f);
                }
            }
            lastFishMarkerX = newFishX;

            // Flip the fish marker across the horizontal axis (mirror top/bottom) whenever
            // its safe/danger alignment state changes.
            if (!alignedInitialized)
            {
                alignedInitialized = true;
                lastAligned = aligned;
            }
            else if (aligned != lastAligned)
            {
                lastAligned = aligned;
                float currentYSign = Mathf.Sign(fishMarker.localScale.y == 0f ? 1f : fishMarker.localScale.y);
                StartFlip(ref stateFlipRoutine, xAxis: false, targetSign: -currentYSign);
                PlaySplashSfx();
            }

            if (fishMarkerImage != null)
            {
                fishMarkerImage.sprite = aligned ? fishSafeSprite : fishDangerSprite;
            }

            SetProgress(progress, progress <= 0f);
        }

        private void PlaySplashSfx()
        {
            if (splashAudioSource == null || splashClip == null)
            {
                return;
            }

            // Stop-and-restart so rapid alignment flips retrigger cleanly instead of
            // layering multiple overlapping splashes on top of each other.
            splashAudioSource.Stop();
            splashAudioSource.clip = splashClip;
            splashAudioSource.Play();
        }

        private void StartFlip(ref Coroutine routine, bool xAxis, float targetSign)
        {
            if (fishMarker == null)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(FlipAxis(xAxis, targetSign));
        }

        private IEnumerator FlipAxis(bool xAxis, float targetSign)
        {
            float magnitude = xAxis ? fishMarkerBaseScaleX : fishMarkerBaseScaleY;
            float targetVal = magnitude * targetSign;
            float startVal = xAxis ? fishMarker.localScale.x : fishMarker.localScale.y;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, flipDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float val = Mathf.Lerp(startVal, targetVal, t);
                Vector3 scale = fishMarker.localScale;
                if (xAxis)
                {
                    scale.x = val;
                }
                else
                {
                    scale.y = val;
                }
                fishMarker.localScale = scale;
                yield return null;
            }

            Vector3 finalScale = fishMarker.localScale;
            if (xAxis)
            {
                finalScale.x = targetVal;
            }
            else
            {
                finalScale.y = targetVal;
            }
            fishMarker.localScale = finalScale;
        }

private void HandleRoundResolved(bool caught, FishDefinition fish)
        {
            feedbackText.color = caught ? alignedColor : misalignedColor;
            feedbackText.text = caught ? "CAUGHT!" : "IT GOT AWAY";

            if (caughtFishModelInstance != null)
            {
                Destroy(caughtFishModelInstance);
                caughtFishModelInstance = null;
            }

            if (caught && fish != null)
            {
                if (caughtFishNameText != null)
                {
                    caughtFishNameText.text = fish.DisplayName;
                }

                if (fish.Prefab != null && caughtFishModelAnchor != null)
                {
                    caughtFishModelInstance = Instantiate(fish.Prefab, caughtFishModelAnchor.position,
                        caughtFishModelAnchor.rotation, caughtFishModelAnchor);
                }
            }
            else if (caughtFishNameText != null)
            {
                caughtFishNameText.text = string.Empty;
            }
        }

private void ResetRig()
        {
            // The input service now holds the player's last drag position indefinitely
            // (see TinyFishingInputService) instead of snapping back to zero on release.
            // Recalibrating here clears that held value so a fresh round starts with the
            // rod upright instead of the snap below being immediately fought by LateUpdate
            // pulling it back toward a stale drag angle.
            rodInput?.Recalibrate();
            rodController?.ResetRod();

            if (directionFlipRoutine != null)
            {
                StopCoroutine(directionFlipRoutine);
                directionFlipRoutine = null;
            }
            if (stateFlipRoutine != null)
            {
                StopCoroutine(stateFlipRoutine);
                stateFlipRoutine = null;
            }

            if (fishMarker != null)
            {
                fishMarker.anchoredPosition = new Vector2(0f, fishMarker.anchoredPosition.y);
                fishMarker.localScale = new Vector3(fishMarkerBaseScaleX, fishMarkerBaseScaleY, fishMarker.localScale.z);
                fishMarkerFacingRight = true;
                lastFishMarkerX = 0f;
                alignedInitialized = false;
            }

            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = new Vector2(0f, playerMarker.anchoredPosition.y);
            }

            if (fishMarkerImage != null)
            {
                fishMarkerImage.sprite = fishSafeSprite;
            }
        }

        
private void SetProgress(float value, bool aboutToEscape)
        {
            if (progressFill == null)
            {
                return;
            }

            // Stretchy-fill technique instead of Image.fillAmount: more reliably clips
            // a spriteless Image than the fill masking path, which was rendering as a
            // solid bar regardless of fillAmount here.
            var fillRect = progressFill.rectTransform;
            var anchorMax = fillRect.anchorMax;
            anchorMax.y = Mathf.Clamp01(value);
            fillRect.anchorMax = anchorMax;

            // The bar itself always stays its normal color now - the rope tints red instead.
            progressFill.color = progressFillNormalColor;
            UpdateRopeEscapeTint(aboutToEscape);
        }

        // Gradually tints the 3D rope material toward ropeEscapeColor while the fish sits
        // at 0% progress and the escape grace countdown (TinyFishingConfig.escapeGraceSeconds)
        // is running, resetting back to normal the moment the escape threat clears.
        private void UpdateRopeEscapeTint(bool aboutToEscape)
        {
            if (ropeRenderers == null || ropeRenderers.Length == 0)
            {
                return;
            }

            float graceSeconds = gameManager != null && gameManager.Config != null
                ? Mathf.Max(0.01f, gameManager.Config.escapeGraceSeconds)
                : 1f;

            ropeEscapeTimer = aboutToEscape ? ropeEscapeTimer + Time.deltaTime : 0f;

            float t = Mathf.Clamp01(ropeEscapeTimer / graceSeconds);
            for (int i = 0; i < ropeRenderers.Length; i++)
            {
                if (ropeRenderers[i] == null)
                {
                    continue;
                }

                Color baseColor = ropeNormalColors != null && i < ropeNormalColors.Length ? ropeNormalColors[i] : Color.white;
                ropeRenderers[i].material.color = Color.Lerp(baseColor, ropeEscapeColor, t);
            }
        }
    }
}
