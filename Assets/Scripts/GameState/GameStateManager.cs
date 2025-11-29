using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages overall game state, including death, restarts, and wave progression
/// Singleton pattern for easy access throughout the game
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DeathUIManager deathUIManager;
    [SerializeField] private SpawnController spawnController;
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("Restart Settings")]
    [SerializeField] private float delayBeforeWaveStart = 2f;
    [Tooltip("Should the player be invulnerable briefly after respawn?")]
    [SerializeField] private bool giveRespawnInvulnerability = true;
    [SerializeField] private float respawnInvulnerabilityDuration = 2f;

    private WaveConfiguration currentWaveConfig;
    private bool isPlayerDead = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Auto-find DeathUIManager
        if (deathUIManager == null)
        {
            deathUIManager = FindFirstObjectByType<DeathUIManager>();
            if (deathUIManager == null)
            {
                Debug.LogError("GameStateManager: No DeathUIManager found in scene!");
            }
            else
            {
                Debug.Log("GameStateManager: Auto-found DeathUIManager");
            }
        }

        // Auto-find SpawnController
        if (spawnController == null)
        {
            spawnController = FindFirstObjectByType<SpawnController>();
            if (spawnController == null)
            {
                Debug.LogWarning("GameStateManager: No SpawnController found in scene. Wave restart may not work.");
            }
            else
            {
                Debug.Log("GameStateManager: Auto-found SpawnController");
            }
        }

        // Auto-find PlayerCharacter via Player.Instance
        if (playerCharacter == null)
        {
            if (Player.Instance != null)
            {
                playerCharacter = Player.Instance.GetPlayerCharacter();
                if (playerCharacter != null)
                {
                    Debug.Log("GameStateManager: Auto-found PlayerCharacter via Player.Instance");
                }
                else
                {
                    Debug.LogError("GameStateManager: Player.Instance exists but GetPlayerCharacter() returned null!");
                }
            }
            else
            {
                Debug.LogError("GameStateManager: Player.Instance is null! Cannot find PlayerCharacter.");
            }
        }
    }

    /// <summary>
    /// Called when the player dies. Triggers the death sequence.
    /// </summary>
    public void OnPlayerDeath()
    {
        if (isPlayerDead)
        {
            return; // Prevent multiple death triggers
        }

        isPlayerDead = true;

        Debug.Log("GameStateManager: Player died, triggering death sequence");

        if (deathUIManager != null)
        {
            deathUIManager.TriggerDeathSequence();
        }
        else
        {
            Debug.LogError("GameStateManager: Cannot trigger death sequence, DeathUIManager is null!");
        }
    }

    /// <summary>
    /// Restarts the current wave, respawning the player at the wave start position
    /// </summary>
    public void RestartCurrentWave()
    {
        if (spawnController == null)
        {
            Debug.LogError("GameStateManager: Cannot restart wave, SpawnController is null!");
            return;
        }

        currentWaveConfig = spawnController.currentArena.GetWave(spawnController.GetCurrentWaveIndex());

        if (currentWaveConfig == null)
        {
            Debug.LogError("GameStateManager: Current wave configuration is null!");
            return;
        }

        StartCoroutine(RestartWaveCoroutine());
    }

    private IEnumerator RestartWaveCoroutine()
    {
        Debug.Log($"GameStateManager: Restarting wave {currentWaveConfig.waveNumber}");

        // Reset player state
        ResetPlayer();

        // Find and move player to wave start position
        if (!string.IsNullOrEmpty(currentWaveConfig.playerWaveStartID))
        {
            // Find the SpawnPoint with the matching ID
            SpawnPoint[] allSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            SpawnPoint playerSpawnPoint = null;

            foreach (SpawnPoint sp in allSpawnPoints)
            {
                if (sp.spawnPointID == currentWaveConfig.playerWaveStartID)
                {
                    playerSpawnPoint = sp;
                    break;
                }
            }

            if (playerSpawnPoint != null)
            {
                playerCharacter.SetPosition(
                    playerSpawnPoint.transform.position,
                    killVelocity: true
                );

                // Also set rotation
                playerCharacter.GetMotorTransform().rotation = playerSpawnPoint.transform.rotation;

                Debug.Log($"GameStateManager: Player respawned at SpawnPoint '{currentWaveConfig.playerWaveStartID}'");
            }
            else
            {
                Debug.LogError($"GameStateManager: Could not find SpawnPoint with ID '{currentWaveConfig.playerWaveStartID}' in scene!");
            }
        }
        else
        {
            Debug.LogWarning($"GameStateManager: Wave {currentWaveConfig.waveNumber} has no playerWaveStartID set! Player position unchanged.");
        }

        // Apply respawn invulnerability if enabled
        if (giveRespawnInvulnerability && playerCharacter != null)
        {
            // You would need to add a method to PlayerCharacter or CombatEntity for this
            // For now, we'll just log it
            Debug.Log($"GameStateManager: Player invulnerable for {respawnInvulnerabilityDuration}s (not implemented in CombatEntity yet)");
            // TODO: Implement temporary invulnerability in CombatEntity
        }

        // Wait before spawning enemies
        yield return new WaitForSeconds(delayBeforeWaveStart);

        // Restart the wave in the spawn controller
        spawnController.RestartCurrentWave();
    }

    /// <summary>
    /// Restarts the entire level by reloading the current scene
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log("GameStateManager: Restarting level");

        // Reset death state
        isPlayerDead = false;

        // Cleanup before scene reload
        CleanupBeforeSceneChange();

        // Reload current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Loads the main menu scene
    /// </summary>
    public void LoadMainMenu()
    {
        Debug.Log("GameStateManager: Loading Main Menu");

        // Reset death state
        isPlayerDead = false;

        // Cleanup before scene change
        CleanupBeforeSceneChange();

        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Cleanup resources before changing scenes to prevent memory leaks
    /// </summary>
    private void CleanupBeforeSceneChange()
    {
        // Disable player input to prevent memory leak
        if (Player.Instance != null)
        {
            Player.Instance.CleanupInputActions();
        }
    }

    /// <summary>
    /// Resets the player's health, energy, and death state
    /// </summary>
    private void ResetPlayer()
    {
        if (playerCharacter == null)
        {
            Debug.LogError("GameStateManager: Cannot reset player, PlayerCharacter is null!");
            return;
        }

        // Reset health and energy to max
        playerCharacter.currentHealth = playerCharacter.maxHealth;
        playerCharacter.currentEnergy = playerCharacter.maxEnergy;

        // Reset death flag
        isPlayerDead = false;

        Debug.Log("GameStateManager: Player health and energy restored");
    }

    /// <summary>
    /// Updates the current wave configuration (called by WaveManager)
    /// </summary>
    public void SetCurrentWave(WaveConfiguration wave)
    {
        currentWaveConfig = wave;
    }

    /// <summary>
    /// Checks if the player is currently dead
    /// </summary>
    public bool IsPlayerDead()
    {
        return isPlayerDead;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}