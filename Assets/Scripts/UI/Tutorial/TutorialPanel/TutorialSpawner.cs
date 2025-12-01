using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class TutorialSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject tutorialPanelPrefab;
    [SerializeField] private GameObject advancedTutorialPanelPrefab; // Info-only panel
    [SerializeField] private Canvas playerCanvas;

    [Header("Movement Tutorial")]
    [SerializeField] private List<TutorialTask> movementTasks;
    [SerializeField] private float movementTutorialDelay = 1f;

    [Header("Combat Tutorial")]
    [SerializeField] private List<TutorialTask> combatTasks;

    [Header("Waveform Switching Tutorial")]
    [SerializeField] private List<TutorialTask> waveformSwitchingTasks;

    [Header("UI Positioning")]
    [SerializeField] private Vector2 basicTutorialPosition = new Vector2(-500, -20);

    public UnityEvent OnCombatTutorialCompleted;
    public UnityEvent OnWaveformSwitchingTutorialCompleted;

    private TutorialPanel currentMovementTutorial;
    private TutorialPanel currentCombatTutorial;
    private bool movementTutorialCompleted = false;
    private bool combatTutorialCompleted = false;
    private bool waveformTutorialCompleted = false;
    private bool combatTutorialQueued = false;

    private void Start()
    {
        // Only spawn movement tutorial in the Forest scene
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Forest")
        {
            StartCoroutine(SpawnMovementTutorialDelayed());
        }
        else
        {
            // Mark movement tutorial as completed so it doesn't interfere with other tutorials
            movementTutorialCompleted = true;
        }
    }

    private IEnumerator SpawnMovementTutorialDelayed()
    {
        yield return new WaitForSeconds(movementTutorialDelay);
        currentMovementTutorial = SpawnBasicTutorial(movementTasks);

        if (currentMovementTutorial != null)
        {
            currentMovementTutorial.OnTutorialCompleted.AddListener(OnMovementTutorialCompleted);
        }
    }

    private void OnMovementTutorialCompleted()
    {
        movementTutorialCompleted = true;
        currentMovementTutorial = null;

        if (combatTutorialQueued && !combatTutorialCompleted)
        {
            combatTutorialQueued = false;
            SpawnCombatTutorial();
        }
    }

    public void OnWaveformUnlockInteraction()
    {
        if (combatTutorialCompleted)
            return;

        if (!movementTutorialCompleted && currentMovementTutorial != null)
        {
            combatTutorialQueued = true;
        }
        else
        {
            SpawnCombatTutorial();
        }
    }

    private void SpawnCombatTutorial()
    {
        if (combatTutorialCompleted)
            return;

        currentCombatTutorial = SpawnBasicTutorial(combatTasks);

        if (currentCombatTutorial != null)
        {
            currentCombatTutorial.OnTutorialCompleted.AddListener(OnCombatTutorialCompleted_Internal);
        }
    }

    private void OnCombatTutorialCompleted_Internal()
    {
        combatTutorialCompleted = true;
        currentCombatTutorial = null;
        OnCombatTutorialCompleted?.Invoke();
    }

    public void SpawnWaveformSwitchingTutorial()
    {
        if (waveformTutorialCompleted)
            return;

        SpawnInfoPanel();
    }

    private void SpawnInfoPanel()
    {
        if (advancedTutorialPanelPrefab == null || playerCanvas == null)
        {
            return;
        }

        GameObject panel = Instantiate(advancedTutorialPanelPrefab, playerCanvas.transform);

        // Position in center of screen
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // Initialize info-only panel (no tasks)
        TutorialPanel tutorialPanel = panel.GetComponent<TutorialPanel>();
        if (tutorialPanel == null)
        {
            return;
        }

        tutorialPanel.Initialize(null, true); // null tasks = info only, true = pause

        // When info panel closes, spawn the actual task tutorial
        tutorialPanel.OnTutorialCompleted.AddListener(OnInfoPanelClosed);
    }

    private void OnInfoPanelClosed()
    {
        StartCoroutine(SpawnTaskTutorialAfterDelay());
    }

    private IEnumerator SpawnTaskTutorialAfterDelay()
    {
        // Wait to ensure info panel is fully destroyed
        yield return new WaitForSeconds(0.5f);

        TutorialPanel taskPanel = SpawnBasicTutorial(waveformSwitchingTasks);

        if (taskPanel != null)
        {
            taskPanel.OnTutorialCompleted.AddListener(OnWaveformTutorialCompleted_Internal);
        }
    }

    private void OnWaveformTutorialCompleted_Internal()
    {
        waveformTutorialCompleted = true;
        OnWaveformSwitchingTutorialCompleted?.Invoke();
    }

    private TutorialPanel SpawnBasicTutorial(List<TutorialTask> tasks)
    {
        if (tutorialPanelPrefab == null || playerCanvas == null)
        {
            Debug.LogError("Tutorial prefab or canvas not assigned!");
            return null;
        }

        GameObject panel = Instantiate(tutorialPanelPrefab, playerCanvas.transform);

        // Position in top right corner
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = basicTutorialPosition;

        // Initialize with tasks
        TutorialPanel tutorialPanel = panel.GetComponent<TutorialPanel>();
        tutorialPanel.Initialize(tasks, false); // false = no pause

        return tutorialPanel;
    }
}