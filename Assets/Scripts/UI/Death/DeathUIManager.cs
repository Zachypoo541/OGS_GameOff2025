using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the death screen fade effects and UI display
/// </summary>
public class DeathUIManager : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image blackFadeImage;
    [SerializeField] private float fadeToBlackDuration = 1.5f;
    [SerializeField] private float deathUIFadeInDuration = 1f;

    [Header("Death UI")]
    [SerializeField] private CanvasGroup deathUICanvasGroup;
    [SerializeField] private GameObject deathUIPanel;

    [Header("Buttons")]
    [SerializeField] private Button restartWaveButton;
    [SerializeField] private Button restartLevelButton;
    [SerializeField] private Button quitGameButton;

    private bool isDeathSequenceActive = false;

    private void Awake()
    {
        // Ensure fade image starts transparent
        if (blackFadeImage != null)
        {
            Color c = blackFadeImage.color;
            c.a = 0f;
            blackFadeImage.color = c;
            blackFadeImage.raycastTarget = false;
        }

        // Ensure death UI starts hidden
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        if (deathUICanvasGroup != null)
        {
            deathUICanvasGroup.alpha = 0f;
        }

        // Setup button listeners
        if (restartWaveButton != null)
        {
            restartWaveButton.onClick.AddListener(OnRestartWave);
        }

        if (restartLevelButton != null)
        {
            restartLevelButton.onClick.AddListener(OnRestartLevel);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(OnQuitGame);
        }
    }

    /// <summary>
    /// Triggers the full death sequence: fade to black, destroy enemies, show UI
    /// </summary>
    public void TriggerDeathSequence()
    {
        if (isDeathSequenceActive)
        {
            return;
        }

        isDeathSequenceActive = true;
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // Phase 1: Fade to black
        yield return StartCoroutine(FadeToBlack());

        // Phase 2: Destroy all enemies (screen is now fully black)
        DestroyAllEnemies();

        // Disable player input
        DisablePlayerInput();

        // Short pause
        yield return new WaitForSeconds(0.3f);

        // Phase 3: Fade in death UI
        yield return StartCoroutine(FadeInDeathUI());

        // Show cursor and unlock it
        ShowCursor();
    }

    private IEnumerator FadeToBlack()
    {
        if (blackFadeImage == null)
        {
            Debug.LogError("DeathUIManager: No black fade image assigned!");
            yield break;
        }

        blackFadeImage.raycastTarget = true;
        float elapsed = 0f;

        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeToBlackDuration);

            Color c = blackFadeImage.color;
            c.a = alpha;
            blackFadeImage.color = c;

            yield return null;
        }

        // Ensure fully black
        Color finalColor = blackFadeImage.color;
        finalColor.a = 1f;
        blackFadeImage.color = finalColor;
    }

    private IEnumerator FadeInDeathUI()
    {
        if (deathUIPanel == null || deathUICanvasGroup == null)
        {
            Debug.LogError("DeathUIManager: Death UI components not assigned!");
            yield break;
        }

        // Activate the UI panel
        deathUIPanel.SetActive(true);
        deathUICanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < deathUIFadeInDuration)
        {
            elapsed += Time.deltaTime;
            deathUICanvasGroup.alpha = Mathf.Clamp01(elapsed / deathUIFadeInDuration);
            yield return null;
        }

        // Ensure fully visible
        deathUICanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Fades out the death UI and fade image (for restart)
    /// </summary>
    public IEnumerator FadeOutDeathUI(float duration = 0.5f)
    {
        if (deathUICanvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = deathUICanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                deathUICanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }

            deathUICanvasGroup.alpha = 0f;
            deathUIPanel.SetActive(false);
        }

        // Fade out black screen
        if (blackFadeImage != null)
        {
            float elapsed = 0f;
            Color startColor = blackFadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);

                Color c = blackFadeImage.color;
                c.a = alpha;
                blackFadeImage.color = c;

                yield return null;
            }

            Color finalColor = blackFadeImage.color;
            finalColor.a = 0f;
            blackFadeImage.color = finalColor;
            blackFadeImage.raycastTarget = false;
        }

        // Hide cursor and lock it
        HideCursor();

        // Re-enable player input
        EnablePlayerInput();

        isDeathSequenceActive = false;
    }

    /// <summary>
    /// Show and unlock the cursor for UI interaction
    /// </summary>
    private void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("DeathUIManager: Cursor unlocked and visible");
    }

    /// <summary>
    /// Hide and lock the cursor for gameplay
    /// </summary>
    private void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("DeathUIManager: Cursor locked and hidden");
    }

    /// <summary>
    /// Disable player input during death screen
    /// </summary>
    private void DisablePlayerInput()
    {
        if (Player.Instance != null)
        {
            Player.Instance.SetInputEnabled(false);
            Debug.Log("DeathUIManager: Player input disabled");
        }
        else
        {
            Debug.LogWarning("DeathUIManager: Player.Instance is null, cannot disable input");
        }
    }

    /// <summary>
    /// Re-enable player input after respawn
    /// </summary>
    private void EnablePlayerInput()
    {
        if (Player.Instance != null)
        {
            Player.Instance.SetInputEnabled(true);
            Debug.Log("DeathUIManager: Player input enabled");
        }
        else
        {
            Debug.LogWarning("DeathUIManager: Player.Instance is null, cannot enable input");
        }
    }

    private void DestroyAllEnemies()
    {
        // Find all enemies in the scene
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        Debug.Log($"DeathUIManager: Destroying {enemies.Length} enemies");

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    private void OnRestartWave()
    {
        Debug.Log("Restart Wave clicked");

        if (GameStateManager.Instance != null)
        {
            StartCoroutine(RestartWaveSequence());
        }
        else
        {
            Debug.LogError("GameStateManager.Instance is null!");
        }
    }

    private IEnumerator RestartWaveSequence()
    {
        // Fade out UI
        yield return StartCoroutine(FadeOutDeathUI());

        // Tell game state manager to restart wave
        GameStateManager.Instance.RestartCurrentWave();
    }

    private void OnRestartLevel()
    {
        Debug.Log("Restart Level clicked");

        if (GameStateManager.Instance != null)
        {
            StartCoroutine(RestartLevelSequence());
        }
    }

    private IEnumerator RestartLevelSequence()
    {
        // Fade out UI
        yield return StartCoroutine(FadeOutDeathUI());

        // Tell game state manager to restart level
        GameStateManager.Instance.RestartLevel();
    }

    private void OnQuitGame()
    {
        Debug.Log("Quit to Main Menu clicked");

        if (GameStateManager.Instance != null)
        {
            StartCoroutine(QuitToMainMenuSequence());
        }
    }

    private IEnumerator QuitToMainMenuSequence()
    {
        // Fade out UI
        yield return StartCoroutine(FadeOutDeathUI());

        // Load main menu scene
        GameStateManager.Instance.LoadMainMenu();
    }

    /// <summary>
    /// Reset the death UI manager state (for restarts)
    /// </summary>
    public void ResetDeathUI()
    {
        isDeathSequenceActive = false;

        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        if (deathUICanvasGroup != null)
        {
            deathUICanvasGroup.alpha = 0f;
        }

        if (blackFadeImage != null)
        {
            Color c = blackFadeImage.color;
            c.a = 0f;
            blackFadeImage.color = c;
            blackFadeImage.raycastTarget = false;
        }

        // Restore gameplay state
        HideCursor();
        EnablePlayerInput();
    }
}