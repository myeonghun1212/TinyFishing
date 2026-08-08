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

        private readonly List<GameObject> spawned = new List<GameObject>();

        private Vector3 Center => transform.position;
        private Vector3 HalfExtents => size * 0.5f;

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

            spawned.Add(holder);
        }

        private void ClearSpawned()
        {
            foreach (var go in spawned)
            {
                if (go == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
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
