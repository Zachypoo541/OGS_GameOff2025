using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Spawns WaveformUnlock prefabs at specific locations when waves complete.
/// Attach to a GameObject in your scene and configure which waves trigger spawns.
/// </summary>
public class WaveformUnlockSpawner : MonoBehaviour
{
    [System.Serializable]
    public class WaveUnlockSpawn
    {
        [Tooltip("Which wave index triggers this spawn (0 = first wave, 1 = second wave, etc.)")]
        public int triggerWaveIndex;

        [Tooltip("The WaveformUnlock prefab to spawn")]
        public GameObject waveformUnlockPrefab;

        [Tooltip("Where to spawn the unlock")]
        public Transform spawnLocation;

        [Tooltip("Delay after wave completes before spawning (in seconds)")]
        public float spawnDelay = 1f;
    }

    [Header("Configuration")]
    [Tooltip("Reference to the SpawnController (will auto-find if not assigned)")]
    public SpawnController spawnController;

    [Header("Unlock Spawns")]
    [Tooltip("Configure which waves spawn which waveform unlocks")]
    public WaveUnlockSpawn[] unlockSpawns;

    [Header("Events")]
    [Tooltip("Called when a waveform unlock is spawned")]
    public UnityEvent<GameObject> OnUnlockSpawned;

    // Track which waves have already spawned unlocks
    private HashSet<int> spawnedWaves = new HashSet<int>();

    private void Start()
    {
        // Auto-find SpawnController if not assigned
        if (spawnController == null)
        {
            GameObject managers = GameObject.Find("Managers");
            if (managers != null)
            {
                Transform waveManager = managers.transform.Find("WaveManager");
                if (waveManager != null)
                {
                    spawnController = waveManager.GetComponent<SpawnController>();
                }
            }
        }

        if (spawnController == null)
        {
            Debug.LogError("WaveformUnlockSpawner: Could not find SpawnController!");
            return;
        }

        // Subscribe to wave completion event
        spawnController.OnWaveCompleted.AddListener(OnWaveComplete);
        Debug.Log("WaveformUnlockSpawner: Subscribed to OnWaveCompleted event");
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (spawnController != null)
        {
            spawnController.OnWaveCompleted.RemoveListener(OnWaveComplete);
        }
    }

    /// <summary>
    /// Call this method when a wave completes to check if an unlock should spawn
    /// </summary>
    public void OnWaveComplete(int completedWaveIndex)
    {
        Debug.Log($"WaveformUnlockSpawner: OnWaveComplete called for wave {completedWaveIndex}");

        // Check if we've already spawned for this wave
        if (spawnedWaves.Contains(completedWaveIndex))
        {
            Debug.Log($"WaveformUnlockSpawner: Wave {completedWaveIndex} unlock already spawned, skipping");
            return;
        }

        foreach (var unlockSpawn in unlockSpawns)
        {
            if (unlockSpawn.triggerWaveIndex == completedWaveIndex)
            {
                Debug.Log($"WaveformUnlockSpawner: Found matching unlock spawn for wave {completedWaveIndex}");
                spawnedWaves.Add(completedWaveIndex);
                StartCoroutine(SpawnUnlockAfterDelay(unlockSpawn));
                return;
            }
        }

        Debug.Log($"WaveformUnlockSpawner: No unlock spawn configured for wave {completedWaveIndex}");
    }

    private System.Collections.IEnumerator SpawnUnlockAfterDelay(WaveUnlockSpawn unlockSpawn)
    {
        if (unlockSpawn.spawnDelay > 0)
        {
            yield return new WaitForSeconds(unlockSpawn.spawnDelay);
        }

        if (unlockSpawn.waveformUnlockPrefab == null)
        {
            Debug.LogError("WaveformUnlockSpawner: No prefab assigned!");
            yield break;
        }

        if (unlockSpawn.spawnLocation == null)
        {
            Debug.LogError("WaveformUnlockSpawner: No spawn location assigned!");
            yield break;
        }

        GameObject spawnedUnlock = Instantiate(
            unlockSpawn.waveformUnlockPrefab,
            unlockSpawn.spawnLocation.position,
            unlockSpawn.spawnLocation.rotation
        );

        Debug.Log($"WaveformUnlockSpawner: Spawned unlock at {unlockSpawn.spawnLocation.position}");

        OnUnlockSpawned?.Invoke(spawnedUnlock);
    }
}