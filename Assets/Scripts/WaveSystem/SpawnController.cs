using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the spawning of enemies based on wave configurations.
/// Attach to a GameObject in your scene (one per arena).
/// </summary>
public class SpawnController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The arena configuration to use")]
    public ArenaConfiguration currentArena;

    [Header("Runtime Settings")]
    [Tooltip("Start the first wave automatically")]
    public bool autoStartFirstWave = true;

    [Tooltip("Parent transform for spawned enemies (keeps hierarchy clean)")]
    public Transform enemyContainer;

    [Header("Events")]
    [Tooltip("Called when a spawn group begins spawning")]
    public UnityEngine.Events.UnityEvent<int> OnSpawnGroupStart;

    [Tooltip("Called when all enemies in a wave have been spawned")]
    public UnityEngine.Events.UnityEvent<int> OnWaveSpawnComplete;

    [Tooltip("Called when all enemies in a wave are defeated")]
    public UnityEngine.Events.UnityEvent<int> OnWaveCompleted;

    // Private state
    private int currentWaveIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = false;
    private bool waveCompleting = false;  // NEW: Prevents multiple wave completions
    private Coroutine pendingWaveStartCoroutine = null;

    // Cache of spawn points by ID for fast lookup
    private Dictionary<string, SpawnPoint> spawnPointCache = new Dictionary<string, SpawnPoint>();

    private void Start()
    {
        // Create enemy container if not assigned
        if (enemyContainer == null)
        {
            GameObject container = new GameObject("Active Enemies");
            container.transform.SetParent(transform);
            enemyContainer = container.transform;
        }

        // Cache all spawn points in the scene
        CacheSpawnPoints();

        if (autoStartFirstWave && currentArena != null)
        {
            StartWave(0);
        }
    }

    /// <summary>
    /// Find and cache all spawn points in the scene by their ID
    /// </summary>
    private void CacheSpawnPoints()
    {
        spawnPointCache.Clear();
        SpawnPoint[] allSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (SpawnPoint sp in allSpawnPoints)
        {
            if (string.IsNullOrEmpty(sp.spawnPointID))
            {
                Debug.LogWarning($"SpawnController: SpawnPoint on {sp.gameObject.name} has no ID assigned!");
                continue;
            }

            if (spawnPointCache.ContainsKey(sp.spawnPointID))
            {
                Debug.LogWarning($"SpawnController: Duplicate spawn point ID '{sp.spawnPointID}' found! Using first occurrence.");
                continue;
            }

            spawnPointCache[sp.spawnPointID] = sp;
        }

        Debug.Log($"SpawnController: Cached {spawnPointCache.Count} spawn points");
    }

    /// <summary>
    /// Start a specific wave by index
    /// </summary>
    public void StartWave(int waveIndex)
    {
        if (currentArena == null)
        {
            Debug.LogError("SpawnController: No arena configuration assigned!");
            return;
        }

        if (waveIndex < 0 || waveIndex >= currentArena.GetWaveCount())
        {
            Debug.LogError($"SpawnController: Wave index {waveIndex} out of range!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("SpawnController: Already spawning a wave!");
            return;
        }

        // Cancel any pending auto-start from previous wave
        if (pendingWaveStartCoroutine != null)
        {
            StopCoroutine(pendingWaveStartCoroutine);
            pendingWaveStartCoroutine = null;
            Debug.Log("SpawnController: Cancelled pending wave auto-start");
        }

        currentWaveIndex = waveIndex;
        isSpawning = true;
        waveCompleting = false;  // NEW: Reset the flag when starting a new wave
        WaveConfiguration wave = currentArena.GetWave(waveIndex);
        StartCoroutine(SpawnWaveCoroutine(wave));
    }

    /// <summary>
    /// Start the next wave in sequence
    /// </summary>
    public void StartNextWave()
    {
        int nextWaveIndex = currentWaveIndex + 1;

        // Safety check to prevent starting waves that don't exist
        if (nextWaveIndex >= currentArena.GetWaveCount())
        {
            Debug.LogWarning($"SpawnController: Cannot start wave {nextWaveIndex + 1}, arena only has {currentArena.GetWaveCount()} waves. Arena should be complete.");
            return;
        }

        StartWave(nextWaveIndex);
    }

    /// <summary>
    /// Coroutine that handles spawning all groups in a wave
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(WaveConfiguration wave)
    {
        // Note: isSpawning is now set in StartWave() before this coroutine starts

        // Play wave announcement sound
        if (wave.waveAnnouncementSound != null)
        {
            AudioSource.PlayClipAtPoint(wave.waveAnnouncementSound, Camera.main.transform.position);
        }

        // Process each spawn group
        for (int groupIndex = 0; groupIndex < wave.spawnGroups.Length; groupIndex++)
        {
            SpawnGroup group = wave.spawnGroups[groupIndex];

            // Wait for the group's delay
            if (group.delayBeforeSpawn > 0)
            {
                yield return new WaitForSeconds(group.delayBeforeSpawn);
            }

            // Notify listeners
            OnSpawnGroupStart?.Invoke(groupIndex);

            // Play group spawn sound
            if (group.spawnSound != null)
            {
                AudioSource.PlayClipAtPoint(group.spawnSound, Camera.main.transform.position);
            }

            // Spawn all entries in this group
            yield return StartCoroutine(SpawnGroupCoroutine(group));
        }

        isSpawning = false;
        OnWaveSpawnComplete?.Invoke(currentWaveIndex);

        Debug.Log($"Wave {wave.waveNumber} spawn complete. Total enemies spawned: {wave.GetTotalEnemyCount()}");
    }

    /// <summary>
    /// Spawn all enemies in a spawn group
    /// </summary>
    private IEnumerator SpawnGroupCoroutine(SpawnGroup group)
    {
        foreach (SpawnEntry entry in group.spawnEntries)
        {
            if (entry.enemyPrefab == null)
            {
                Debug.LogWarning("SpawnController: Spawn entry has missing enemy prefab!");
                continue;
            }

            if (string.IsNullOrEmpty(entry.spawnPointID))
            {
                Debug.LogWarning("SpawnController: Spawn entry has no spawn point ID assigned!");
                continue;
            }

            if (!spawnPointCache.ContainsKey(entry.spawnPointID))
            {
                Debug.LogWarning($"SpawnController: Spawn point '{entry.spawnPointID}' not found in scene!");
                continue;
            }

            // Spawn each enemy in this entry
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry);

                // Wait between individual spawns if specified
                if (entry.spawnInterval > 0 && i < entry.count - 1)
                {
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }
        }
    }

    /// <summary>
    /// Spawn a single enemy from a spawn entry
    /// </summary>
    private void SpawnEnemy(SpawnEntry entry)
    {
        // Find the spawn point by ID
        if (!spawnPointCache.TryGetValue(entry.spawnPointID, out SpawnPoint spawnPoint))
        {
            Debug.LogError($"SpawnController: Could not find spawn point with ID '{entry.spawnPointID}'. Make sure it exists in the scene!");
            return;
        }

        if (entry.enemyPrefab == null)
        {
            Debug.LogWarning($"SpawnController: Spawn entry for '{entry.spawnPointID}' has no enemy prefab assigned!");
            return;
        }

        Vector3 spawnPosition = spawnPoint.transform.position;
        Quaternion spawnRotation = spawnPoint.transform.rotation;

        // Apply random spread if enabled
        if (entry.useRandomSpread && entry.spreadRadius > 0)
        {
            Vector2 randomOffset = Random.insideUnitCircle * entry.spreadRadius;
            spawnPosition += new Vector3(randomOffset.x, 0, randomOffset.y);
        }

        // Instantiate the enemy
        GameObject enemy = Instantiate(entry.enemyPrefab, spawnPosition, spawnRotation, enemyContainer);
        activeEnemies.Add(enemy);

        // Apply drop override if enabled
        if (entry.useDropOverride)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetDropOverride(entry.overrideDropPrefab, entry.overrideDropChance);
            }
            else
            {
                Debug.LogWarning($"SpawnController: Enemy {enemy.name} does not have EnemyAI component, cannot apply drop override!");
            }
        }

        // Subscribe to enemy death event
        CombatEntity combatEntity = enemy.GetComponent<CombatEntity>();
        if (combatEntity != null)
        {
            combatEntity.OnDeath += () => OnEnemyDeath(enemy);
        }
        else
        {
            Debug.LogWarning($"SpawnController: Enemy {enemy.name} does not have CombatEntity component!");
        }
    }

    /// <summary>
    /// Called when an enemy dies (optional, for tracking wave completion)
    /// </summary>
    private void OnEnemyDeath(GameObject enemy)
    {
        activeEnemies.Remove(enemy);

        // Check if wave is complete
        // NEW: Added waveCompleting check to prevent multiple completions
        if (activeEnemies.Count == 0 && !isSpawning && !waveCompleting)
        {
            waveCompleting = true;  // NEW: Set flag immediately to prevent re-entry
            OnWaveComplete();
        }
    }

    /// <summary>
    /// Called when all enemies in the current wave are defeated
    /// </summary>
    private void OnWaveComplete()
    {
        Debug.Log($"Wave {currentWaveIndex + 1} complete!");

        // Invoke event with the completed wave index
        OnWaveCompleted?.Invoke(currentWaveIndex);

        WaveConfiguration completedWave = currentArena.GetWave(currentWaveIndex);

        // Check if there's another wave
        if (currentWaveIndex + 1 < currentArena.GetWaveCount())
        {
            // Only auto-start if the completed wave has autoStartNextWave enabled
            if (completedWave.autoStartNextWave)
            {
                // Start next wave after delay and store coroutine reference
                pendingWaveStartCoroutine = StartCoroutine(StartNextWaveAfterDelay(completedWave.delayBeforeNextWave));
            }
            else
            {
                Debug.Log($"Wave {currentWaveIndex + 1} requires manual trigger to continue (autoStartNextWave is disabled)");
            }
        }
        else
        {
            // Arena complete
            OnArenaComplete();
        }
    }

    /// <summary>
    /// Wait before starting the next wave
    /// </summary>
    private IEnumerator StartNextWaveAfterDelay(float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        StartNextWave();
    }

    /// <summary>
    /// Called when all waves in the arena are complete
    /// </summary>
    private void OnArenaComplete()
    {
        Debug.Log($"Arena {currentArena.arenaNumber} complete!");

        // TODO: Integrate with your progression system when ready
        // For now, just log completion
        if (currentArena.nextArena != null)
        {
            Debug.Log($"Next arena unlocked: {currentArena.nextArena.arenaName}");
        }
        else
        {
            Debug.Log("Final arena completed!");
        }

        // You can add hooks here for:
        // - Unlocking next arena in save system
        // - Giving rewards (energy pickups, new waveforms, etc.)
        // - Transitioning to victory screen or next arena
        // - Saving player progress
    }

    /// <summary>
    /// Get the number of enemies currently alive
    /// </summary>
    public int GetActiveEnemyCount()
    {
        // Clean up any null references
        activeEnemies.RemoveAll(enemy => enemy == null);
        return activeEnemies.Count;
    }

    /// <summary>
    /// Clear all active enemies (useful for debugging or arena reset)
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }
}