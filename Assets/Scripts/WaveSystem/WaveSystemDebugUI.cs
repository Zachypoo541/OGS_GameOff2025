using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug UI for testing wave spawning system.
/// Attach to a Canvas in your scene for quick testing controls.
/// </summary>
public class WaveSystemDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpawnController spawnController;

    [Header("UI Elements (Optional - will create if not assigned)")]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button nextWaveButton;
    [SerializeField] private Button clearEnemiesButton;
    [SerializeField] private Button restartArenaButton;

    [Header("Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode nextWaveKey = KeyCode.N;
    [SerializeField] private KeyCode clearEnemiesKey = KeyCode.K;

    private void Start()
    {
        if (spawnController == null)
        {
            spawnController = FindObjectOfType<SpawnController>();
        }

        // Wire up buttons if assigned
        if (nextWaveButton != null)
        {
            nextWaveButton.onClick.AddListener(OnNextWavePressed);
        }

        if (clearEnemiesButton != null)
        {
            clearEnemiesButton.onClick.AddListener(OnClearEnemiesPressed);
        }

        if (restartArenaButton != null)
        {
            restartArenaButton.onClick.AddListener(OnRestartArenaPressed);
        }
    }

    private void Update()
    {
        // Keyboard shortcuts
        if (Input.GetKeyDown(nextWaveKey))
        {
            OnNextWavePressed();
        }

        if (Input.GetKeyDown(clearEnemiesKey))
        {
            OnClearEnemiesPressed();
        }

        // Update debug info
        if (showDebugInfo && infoText != null && spawnController != null)
        {
            UpdateDebugInfo();
        }
    }

    private void UpdateDebugInfo()
    {
        int activeEnemies = spawnController.GetActiveEnemyCount();

        string info = $"<b>Wave System Debug</b>\n";
        info += $"Active Enemies: {activeEnemies}\n";
        info += $"\n<size=10>Press {nextWaveKey} - Next Wave\n";
        info += $"Press {clearEnemiesKey} - Clear Enemies</size>";

        infoText.text = info;
    }

    private void OnNextWavePressed()
    {
        if (spawnController != null)
        {
            spawnController.StartNextWave();
            Debug.Log("Debug: Started next wave");
        }
    }

    private void OnClearEnemiesPressed()
    {
        if (spawnController != null)
        {
            spawnController.ClearAllEnemies();
            Debug.Log("Debug: Cleared all enemies");
        }
    }

    private void OnRestartArenaPressed()
    {
        if (spawnController != null)
        {
            spawnController.ClearAllEnemies();
            spawnController.StartWave(0);
            Debug.Log("Debug: Restarted arena");
        }
    }
}