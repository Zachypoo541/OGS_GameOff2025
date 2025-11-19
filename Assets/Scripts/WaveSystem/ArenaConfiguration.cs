using UnityEngine;

/// <summary>
/// Defines an entire arena with all its waves.
/// Create via: Assets > Create > Wave System > Arena Configuration
/// </summary>
[CreateAssetMenu(fileName = "New Arena", menuName = "Wave System/Arena Configuration", order = 0)]
public class ArenaConfiguration : ScriptableObject
{
    [Header("Arena Identity")]
    [Tooltip("Arena number/identifier")]
    public int arenaNumber = 1;

    [Tooltip("Display name for the arena")]
    public string arenaName = "Arena 1";

    [Tooltip("Description of this arena")]
    [TextArea(3, 5)]
    public string arenaDescription;

    [Header("Waves")]
    [Tooltip("All waves in this arena (played sequentially)")]
    public WaveConfiguration[] waves;

    [Header("Arena Settings")]
    [Tooltip("Scene to load for this arena (leave empty if using same scene)")]
    public string arenaSceneName;

    [Tooltip("Spawn point group identifier for this arena")]
    public string spawnPointGroupTag = "ArenaSpawns";

    [Header("Completion")]
    [Tooltip("Arena to unlock after completing this one")]
    public ArenaConfiguration nextArena;

    [Tooltip("Rewards given upon arena completion")]
    public string[] completionRewards;

    /// <summary>
    /// Get the total number of waves in this arena
    /// </summary>
    public int GetWaveCount()
    {
        return waves != null ? waves.Length : 0;
    }

    /// <summary>
    /// Get a specific wave by index
    /// </summary>
    public WaveConfiguration GetWave(int index)
    {
        if (waves != null && index >= 0 && index < waves.Length)
        {
            return waves[index];
        }
        return null;
    }

    /// <summary>
    /// Get the total number of enemies across all waves
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int total = 0;
        foreach (var wave in waves)
        {
            total += wave.GetTotalEnemyCount();
        }
        return total;
    }
}