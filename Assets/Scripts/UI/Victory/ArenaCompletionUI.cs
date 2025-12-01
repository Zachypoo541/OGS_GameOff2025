using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the Arena Completion UI screen with fade-in animation
/// Attach to the UI prefab root (Panel)
/// </summary>
public class ArenaCompletionUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Button for next arena/main menu")]
    public Button actionButton;

    [Tooltip("Text on the action button")]
    public TextMeshProUGUI buttonText; // Use 'Text' if not using TextMeshPro

    [Tooltip("Optional: Arena completion message text")]
    public TextMeshProUGUI completionText;

    [Header("Fade Settings")]
    [Tooltip("Duration of the fade-in animation")]
    public float fadeInDuration = 0.5f;

    private ArenaConfiguration currentArena;
    private ArenaTransitionManager transitionManager;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        transitionManager = FindFirstObjectByType<ArenaTransitionManager>();

        // Add CanvasGroup for fading if not present
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start fully transparent
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Initialize the UI with arena data
    /// </summary>
    public void Initialize(ArenaConfiguration completedArena)
    {
        currentArena = completedArena;

        // Update completion message
        if (completionText != null)
        {
            completionText.text = $"{completedArena.arenaName} Complete!";
        }

        // Configure button
        if (actionButton != null && buttonText != null)
        {
            if (completedArena.nextArena != null)
            {
                buttonText.text = "Next Arena";
                actionButton.onClick.AddListener(OnNextArenaClicked);
            }
            else
            {
                buttonText.text = "Main Menu";
                actionButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        // Start fade-in animation
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Fade in the UI elements
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f; // Ensure fully visible
    }

    /// <summary>
    /// Called when Next Arena button is clicked
    /// </summary>
    private void OnNextArenaClicked()
    {
        if (currentArena.nextArena != null && transitionManager != null)
        {
            string sceneName = currentArena.nextArena.arenaName;
            transitionManager.LoadNextArena(sceneName);
        }
    }

    /// <summary>
    /// Called when Main Menu button is clicked
    /// </summary>
    private void OnMainMenuClicked()
    {
        if (transitionManager != null)
        {
            transitionManager.LoadMainMenu();
        }
    }
}