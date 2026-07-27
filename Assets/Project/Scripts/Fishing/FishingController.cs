using System;
using System.Collections;
using NanFishing.Core;
using NanFishing.Data;
using NanFishing.Input;
using UnityEngine;

namespace NanFishing.Fishing
{
    public sealed class FishingController : MonoBehaviour
    {
        public event Action<GameState> StateChanged;
        public event Action<FishDefinition, bool, float> RoundResolved;
        public event Action<float, float, float, float, bool> ReelingUpdated;

        private IPlayerInput playerInput;
        private GameBalanceConfig config;
        private FishDefinition[] catalog;
        private Transform bobber;
        private Transform fishRoot;
        private FishController activeFish;
        private LineTensionModel tensionModel;
        private Transform rodVisual;
        private Quaternion rodRestRotation;
        private float reelDuration;
        private Coroutine activeRoutine;

        public GameState State { get; private set; } = GameState.Start;
        public FishDefinition ActiveFish => activeFish != null ? activeFish.Definition : null;

        public void Initialize(IPlayerInput input, GameBalanceConfig balance,
            FishDefinition[] fishCatalog, Transform bobberTransform, Transform fishParent)
        {
            playerInput = input;
            config = balance;
            catalog = fishCatalog;
            bobber = bobberTransform;
            fishRoot = fishParent;
            tensionModel = new LineTensionModel(config);
            var rodObject = GameObject.Find("FishingRod");
            if (rodObject != null)
            {
                rodVisual = rodObject.transform;
                rodRestRotation = rodVisual.rotation;
            }
            playerInput.CastPerformed += HandleCast;
            SetState(GameState.Casting);
        }

        public void StopFishing()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            ClearFish();
            SetState(GameState.SessionResult);
        }

        private void OnDestroy()
        {
            if (playerInput != null)
            {
                playerInput.CastPerformed -= HandleCast;
            }
        }

        private void Update()
        {
            if (State != GameState.Reeling || activeFish == null)
            {
                return;
            }

            reelDuration += Time.deltaTime;
            activeFish.Simulate(Time.deltaTime);
            tensionModel.Tick(Time.deltaTime, playerInput.IsReeling, playerInput.Direction,
                activeFish.Direction, activeFish.Definition.Resistance);
            ReelingUpdated?.Invoke(tensionModel.Tension, tensionModel.Progress, activeFish.Direction,
                playerInput.Direction, playerInput.IsReeling);
            if (rodVisual != null)
            {
                var tiltRotation = Quaternion.AngleAxis(-playerInput.Direction * 18f, Vector3.forward);
                rodVisual.rotation = Quaternion.Slerp(rodVisual.rotation,
                    tiltRotation * rodRestRotation, Time.deltaTime * 8f);
            }

            if (tensionModel.IsCaught)
            {
                ResolveRound(true);
            }
            else if (tensionModel.IsBroken)
            {
                ResolveRound(false);
            }
        }

        private void HandleCast(CastStrength strength)
        {
            if (State != GameState.Casting)
            {
                return;
            }

            activeRoutine = StartCoroutine(CastAndBiteRoutine(strength));
        }

        private IEnumerator CastAndBiteRoutine(CastStrength strength)
        {
            var distance = 3.5f + (int)strength * 1.2f;
            var start = new Vector3(0.65f, 2.4f, 0.4f);
            var end = new Vector3(0f, 0.2f, distance);
            var duration = 0.55f;
            var elapsed = 0f;
            SetState(GameState.WaitingForBite);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var arc = Mathf.Sin(t * Mathf.PI) * 2.2f;
                bobber.position = Vector3.Lerp(start, end, t) + Vector3.up * arc;
                yield return null;
            }

            bobber.position = end;
            yield return new WaitForSeconds(UnityEngine.Random.Range(
                config.biteDelayRange.x, config.biteDelayRange.y));
            SpawnFish(end);
            tensionModel.Reset();
            reelDuration = 0f;
            SetState(GameState.Reeling);
            activeRoutine = null;
        }

        private void SpawnFish(Vector3 bobberPosition)
        {
            ClearFish();
            var definition = SelectFish();
            var fishObject = definition.Prefab != null
                ? Instantiate(definition.Prefab, fishRoot)
                : CreateProceduralFish(definition, fishRoot);
            fishObject.transform.position = bobberPosition + new Vector3(0f, -0.45f, 0f);
            activeFish = fishObject.GetComponent<FishController>() ?? fishObject.AddComponent<FishController>();
            activeFish.Initialize(definition);
        }

        private FishDefinition SelectFish()
        {
            var roll = UnityEngine.Random.value;
            var desiredRarity = roll < 0.1f ? FishRarity.Rare :
                roll < 0.4f ? FishRarity.Uncommon : FishRarity.Common;
            var matches = Array.FindAll(catalog, fish => fish.Rarity == desiredRarity);
            return matches.Length > 0 ? matches[UnityEngine.Random.Range(0, matches.Length)] :
                catalog[UnityEngine.Random.Range(0, catalog.Length)];
        }

        private void ResolveRound(bool caught)
        {
            var fish = activeFish.Definition;
            SetState(GameState.CatchResult);
            RoundResolved?.Invoke(fish, caught, reelDuration);
            ClearFish();
            activeRoutine = StartCoroutine(ReturnToCastingRoutine());
        }

        private IEnumerator ReturnToCastingRoutine()
        {
            yield return new WaitForSeconds(config.catchResultDuration);
            bobber.position = new Vector3(0.65f, 2.4f, 0.4f);
            SetState(GameState.Casting);
            activeRoutine = null;
        }

        private void ClearFish()
        {
            if (activeFish != null)
            {
                Destroy(activeFish.gameObject);
                activeFish = null;
            }
        }

        private void SetState(GameState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }

        private static GameObject CreateProceduralFish(FishDefinition definition, Transform parent)
        {
            var root = new GameObject($"Fish_{definition.Id}");
            root.transform.SetParent(parent);

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(1.3f, 0.65f, 0.55f);
            body.GetComponent<Renderer>().material = RuntimeVisualFactory.CreateMaterial(definition.Color);
            Destroy(body.GetComponent<Collider>());

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform);
            tail.transform.localPosition = new Vector3(-0.85f, 0f, 0f);
            tail.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            tail.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            tail.GetComponent<Renderer>().material = RuntimeVisualFactory.CreateMaterial(
                Color.Lerp(definition.Color, Color.white, 0.15f));
            Destroy(tail.GetComponent<Collider>());
            return root;
        }
    }
}
