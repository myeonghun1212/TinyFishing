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

        [Header("Progress")]
        [SerializeField] private Image progressFill;

        [Header("Rod (3D)")]
        [SerializeField] private FishingRodController rodController;
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

            rodInput = inputBehaviour as ITinyFishingInput;
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

private void HandleStateChanged(TinyFishingState state)
        {
            switch (state)
            {
                case TinyFishingState.ReadyToCast:
                    instructionText.text = "Shake your phone to cast!";
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
                    SetProgress(0f);
                    ResetRig();
                    SetPlayerMarkerDarkened(false);
                    break;
                case TinyFishingState.WaitingForBite:
                    instructionText.text = "Waiting for a bite...";
                    SetPlayerMarkerDarkened(false);
                    break;
                case TinyFishingState.Reeling:
                    instructionText.text = "Tilt to aim, tap when green!";
                    SetPlayerMarkerDarkened(true);
                    break;
                case TinyFishingState.RoundResult:
                    instructionText.text = string.Empty;
                    SetPlayerMarkerDarkened(false);
                    break;
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
            }

            if (fishMarkerImage != null)
            {
                fishMarkerImage.sprite = aligned ? fishSafeSprite : fishDangerSprite;
            }

            SetProgress(progress);
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

        
private void SetProgress(float value)
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
        }
    }
}
