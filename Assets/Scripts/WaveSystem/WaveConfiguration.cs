using UnityEngine;

/// <summary>
/// Defines a single wave of enemies within an arena.
/// Create via: Assets > Create > Wave System > Wave Configuration
/// </summary>
[CreateAssetMenu(fileName = "New Wave", menuName = "Wave System/Wave Configuration", order = 1)]
public class WaveConfiguration : ScriptableObject
{
    [Header("Wave Identity")]
    [Tooltip("Identifying number for this wave")]
    public int waveNumber = 1;

    [Tooltip("Optional description for designers")]
    [TextArea(2, 4)]
    public string waveDescription;

    [Header("Player Spawn")]
    [Tooltip("Spawn Point ID where the player spawns when starting/restarting this wave")]
    public string playerWaveStartID;

    [Header("Spawn Groups")]
    [Tooltip("All spawn groups in this wave (executed in order with delays)")]
    public SpawnGroup[] spawnGroups;

    [Header("Wave Completion")]
    [Tooltip("Automatically start next wave after delay (uncheck for manual triggers like waveform unlocks)")]
    public bool autoStartNextWave = true;

    [Tooltip("Delay before next wave starts (0 = immediate)")]
    [Min(0f)]
    public float delayBeforeNextWave = 1.5f;

    [Header("Optional Audio")]
    [Tooltip("Music or audio that plays when this wave starts")]
    public AudioClip waveMusic;

    [Tooltip("Announcement sound (e.g., 'Wave 2!')")]
    public AudioClip waveAnnouncementSound;

    /// <summary>
    /// Get the total number of enemies across all spawn groups in this wave
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int total = 0;
        if (spawnGroups != null)
        {
            foreach (var group in spawnGroups)
            {
                if (group != null)
                {
                    total += group.GetTotalEnemyCount();
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Get the total duration of all spawn group delays
    /// </summary>
    public float GetTotalSpawnDuration()
    {
        float duration = 0f;
        if (spawnGroups != null)
        {
            foreach (var group in spawnGroups)
            {
                if (group != null)
                {
                    duration += group.delayBeforeSpawn;
                }
            }
        }
        return duration;
    }
}