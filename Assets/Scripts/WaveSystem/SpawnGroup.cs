using UnityEngine;

/// <summary>
/// A group of spawns that occur together (after a specified delay).
/// Multiple spawn groups within a wave allow for staged enemy appearances.
/// </summary>
[System.Serializable]
public class SpawnGroup
{
    [Header("Timing")]
    [Tooltip("Delay (in seconds) before this spawn group activates")]
    [Min(0f)]
    public float delayBeforeSpawn = 0f;

    [Header("Spawns")]
    [Tooltip("All enemies that spawn as part of this group")]
    public SpawnEntry[] spawnEntries;

    [Header("Optional Settings")]
    [Tooltip("Display name for this group (helps with organization)")]
    public string groupName = "Spawn Group";

    [Tooltip("Play a sound effect when this group spawns")]
    public AudioClip spawnSound;

    /// <summary>
    /// Get the total number of enemies in this spawn group
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int total = 0;
        if (spawnEntries != null)
        {
            foreach (var entry in spawnEntries)
            {
                if (entry != null)
                {
                    total += entry.count;
                }
            }
        }
        return total;
    }
}