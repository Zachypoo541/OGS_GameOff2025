using UnityEngine;

/// <summary>
/// Defines a single spawn entry: what enemy, where, and how many.
/// </summary>
[System.Serializable]
public class SpawnEntry
{
    [Header("Enemy Configuration")]
    [Tooltip("The enemy prefab to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("How many of this enemy to spawn")]
    [Min(1)]
    public int count = 1;

    [Header("Location")]
    [Tooltip("The ID of the spawn point where this enemy will appear (must match a SpawnPoint's spawnPointID in the scene)")]
    public string spawnPointID;

    [Header("Spawn Behavior")]
    [Tooltip("Spread spawned enemies randomly around the spawn point")]
    public bool useRandomSpread = false;

    [Tooltip("Maximum distance to spread enemies from spawn point")]
    [Min(0f)]
    public float spreadRadius = 2f;

    [Tooltip("Time between spawning each individual enemy in this group")]
    [Min(0f)]
    public float spawnInterval = 0.2f;

    [Header("Drop Override (Optional)")]
    [Tooltip("Override the enemy's default drop prefab (leave null to use enemy's default)")]
    public GameObject overrideDropPrefab;

    [Tooltip("Override the drop chance (only used if overrideDropPrefab is set)")]
    [Range(0f, 1f)]
    public float overrideDropChance = 0.5f;

    [Tooltip("Apply drop override to this spawn entry")]
    public bool useDropOverride = false;
}