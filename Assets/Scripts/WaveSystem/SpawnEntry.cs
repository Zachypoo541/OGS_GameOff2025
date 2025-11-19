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
    [Tooltip("The spawn point where this enemy will appear")]
    public SpawnPoint spawnLocation;

    [Header("Spawn Behavior")]
    [Tooltip("Spread spawned enemies randomly around the spawn point")]
    public bool useRandomSpread = false;

    [Tooltip("Maximum distance to spread enemies from spawn point")]
    [Min(0f)]
    public float spreadRadius = 2f;

    [Tooltip("Time between spawning each individual enemy in this group")]
    [Min(0f)]
    public float spawnInterval = 0.2f;
}