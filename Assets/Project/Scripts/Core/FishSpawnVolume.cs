using System.Collections;
using System.Collections.Generic;
using NanFishing.Data;
using TinyFishing.Data;
using TinyFishing.Fishing;
using UnityEngine;

namespace TinyFishing.Core
{
    // Populates a world-space box volume with roaming fish visuals driven by a
    // FishPool asset: which fish types spawn, how many of each (entry.spawnCount),
    // and - via the same asset's weight/session-cap fields - how likely each one
    // is to be the fish that actually bites during reeling. Edit the FishPool
    // asset to change what's in the pool; edit this component to change where
    // and how fish roam.
    //
    // Also brokers the bite handoff: TinyFishingGameManager calls BeginBiteApproach
    // to pull a currently-wandering fish out of the pool and send it toward the bob
    // (via FishBiteAgent), then either CancelBiteApproach (missed/escaped - fish
    // resumes wandering) or ResolveBiteCaught (fish is removed and a same-type
    // replacement respawns after a delay).
    public sealed class FishSpawnVolume : MonoBehaviour
    {
        [Tooltip("Which fish types populate this volume, how many of each, and their catch odds. Edit this asset to change the pool.")]
        [SerializeField] private FishPool pool;

        [Header("Volume")]
        [Tooltip("Size of the roam volume, centered on this GameObject's position (world space, unaffected by rotation/scale).")]
        [SerializeField] private Vector3 size = new Vector3(6f, 1.5f, 6f);

        [Header("Wander Behaviour")]
        [Tooltip("Multiplier applied to each fish's own MoveSpeed (from its FishDefinition) when roaming here.")]
        [Min(0.01f)] public float speedMultiplier = 0.6f;
        [Min(0.1f)] public float turnSpeed = 4f;
        [Tooltip("Min/max seconds a fish pauses after reaching a wander point before picking a new one.")]
        public Vector2 pauseSecondsRange = new Vector2(0.4f, 1.8f);

        [Header("Bite & Respawn")]
        [Tooltip("Seconds after a fish is caught before a same-type replacement respawns into this volume's wander pool.")]
        [Min(0f)] public float caughtRespawnDelay = 4f;

        private sealed class SpawnedFish
        {
            public GameObject holder;
            public FishDefinition fish;
            public FishWanderAgent wanderAgent;
            public FishBiteAgent biteAgent;
        }

        private readonly List<SpawnedFish> spawned = new List<SpawnedFish>();

        public Vector3 Center => transform.position;
        public Vector3 HalfExtents => size * 0.5f;

        private void Start()
        {
            Respawn();
        }

        /// <summary>
        /// Clears any fish this volume previously spawned and spawns a fresh set
        /// from the current state of the assigned FishPool. Safe to call at
        /// runtime after changing pool entries (e.g. from a debug/settings UI).
        /// </summary>
        [ContextMenu("Respawn Fish")]
        public void Respawn()
        {
            ClearSpawned();

            if (pool == null)
            {
                return;
            }

            foreach (var entry in pool.Entries)
            {
                if (entry == null || !entry.enabled || entry.fish == null || entry.fish.Prefab == null)
                {
                    continue;
                }

                for (var i = 0; i < entry.spawnCount; i++)
                {
                    SpawnOne(entry.fish);
                }
            }
        }

        /// <summary>
        /// Hands a currently-wandering fish (preferably one matching <paramref name="fishType"/>,
        /// falling back to any free fish if none match or fishType is null) over to a FishBiteAgent
        /// that swims it to a hold point beneath <paramref name="bobTarget"/>. Returns the agent the
        /// caller should poll (HasArrived) and later resolve via CancelBiteApproach/ResolveBiteCaught,
        /// or null if this volume has no free fish to send (e.g. pool empty or everything already biting).
        /// </summary>
        public FishBiteAgent BeginBiteApproach(FishDefinition fishType, Transform bobTarget, float speed)
        {
            if (bobTarget == null)
            {
                return null;
            }

            var candidate = FindFreeFish(fishType) ?? FindFreeFish(null);
            if (candidate == null)
            {
                return null;
            }

            candidate.wanderAgent.enabled = false;

            var biteAgent = candidate.holder.GetComponent<FishBiteAgent>();
            if (biteAgent == null)
            {
                biteAgent = candidate.holder.AddComponent<FishBiteAgent>();
            }
            biteAgent.enabled = true;
            biteAgent.Begin(bobTarget, speed);
            candidate.biteAgent = biteAgent;

            return biteAgent;
        }

        /// <summary>
        /// Abandons an in-progress bite approach (missed hook window, or the player reeled too
        /// early) and hands the fish back to FishWanderAgent so it resumes roaming from wherever
        /// it currently is.
        /// </summary>
        public void CancelBiteApproach(FishBiteAgent agent)
        {
            var entry = FindByBiteAgent(agent);
            if (entry == null)
            {
                return;
            }

            if (entry.biteAgent != null)
            {
                entry.biteAgent.enabled = false;
            }
            entry.biteAgent = null;

            if (entry.wanderAgent != null)
            {
                entry.wanderAgent.enabled = true;
            }
        }

        /// <summary>
        /// Resolves a bite approach as a successful catch: removes the fish from this volume's
        /// pool (it's on the line now) and, after caughtRespawnDelay seconds, spawns a fresh
        /// same-type replacement back into the wander pool.
        /// </summary>
        public void ResolveBiteCaught(FishBiteAgent agent)
        {
            var entry = FindByBiteAgent(agent);
            if (entry == null)
            {
                return;
            }

            spawned.Remove(entry);
            var fishToRespawn = entry.fish;
            if (entry.holder != null)
            {
                Destroy(entry.holder);
            }

            if (fishToRespawn != null)
            {
                StartCoroutine(RespawnAfterDelay(fishToRespawn, caughtRespawnDelay));
            }
        }

        private IEnumerator RespawnAfterDelay(FishDefinition fish, float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
            if (fish != null && fish.Prefab != null)
            {
                SpawnOne(fish);
            }
        }

        private SpawnedFish FindFreeFish(FishDefinition fishType)
        {
            foreach (var entry in spawned)
            {
                if (entry.holder == null || entry.wanderAgent == null || !entry.wanderAgent.enabled)
                {
                    continue; // destroyed, or already committed to a bite
                }

                if (fishType != null && entry.fish != fishType)
                {
                    continue;
                }

                return entry;
            }
            return null;
        }

        private SpawnedFish FindByBiteAgent(FishBiteAgent agent)
        {
            if (agent == null)
            {
                return null;
            }

            foreach (var entry in spawned)
            {
                if (entry.biteAgent == agent)
                {
                    return entry;
                }
            }
            return null;
        }

        private void SpawnOne(FishDefinition fish)
        {
            var spawnPoint = Center + new Vector3(
                Random.Range(-HalfExtents.x, HalfExtents.x),
                Random.Range(-HalfExtents.y, HalfExtents.y),
                Random.Range(-HalfExtents.z, HalfExtents.z));

            // The fish prefabs bake a corrective local rotation/scale (to align the
            // imported model and mirror it correctly) into their root transform.
            // A wander-driven holder carries world position/facing; the prefab is
            // instantiated as its child with that baked local rotation/scale left
            // untouched, so wandering never fights the model's built-in correction.
            var holder = new GameObject(fish.DisplayName);
            holder.transform.SetParent(transform, false);
            holder.transform.position = spawnPoint;
            holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var visual = Instantiate(fish.Prefab, holder.transform);
            visual.transform.localPosition = Vector3.zero;

            var agent = holder.AddComponent<FishWanderAgent>();
            agent.Configure(Center, HalfExtents, Mathf.Max(0.05f, fish.MoveSpeed * speedMultiplier), turnSpeed, pauseSecondsRange);

            spawned.Add(new SpawnedFish { holder = holder, fish = fish, wanderAgent = agent, biteAgent = null });
        }

        private void ClearSpawned()
        {
            foreach (var entry in spawned)
            {
                if (entry.holder == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(entry.holder);
                }
                else
                {
                    DestroyImmediate(entry.holder);
                }
            }
            spawned.Clear();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.18f);
            Gizmos.DrawCube(Center, size);
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireCube(Center, size);
        }
    }
}

