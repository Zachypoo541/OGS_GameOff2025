using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class FadeInText
{
    public TextMeshProUGUI textElement;
    public float delayAfterPanelAppears;
    public float fadeDuration = 0.5f;
}

public class TutorialPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform taskContainer;
    [SerializeField] private GameObject taskTextPrefab;
    [SerializeField] private Button continueButton;

    [Header("Sequential Text Fade (Advanced Panels Only)")]
    [SerializeField] private List<FadeInText> sequentialTexts = new List<FadeInText>();

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;

    [Header("Tutorial Configuration")]
    [SerializeField] private InputActionAsset inputActions;

    public UnityEvent OnTutorialCompleted;

    private List<TutorialTask> tasks;
    private Dictionary<string, bool> taskCompletion = new Dictionary<string, bool>();
    private Dictionary<string, TextMeshProUGUI> taskTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, System.Action<InputAction.CallbackContext>> actionCallbacks = new Dictionary<string, System.Action<InputAction.CallbackContext>>();
    private bool initialized = false;
    private bool shouldPauseGame = false;
    private bool allTasksCompleted = false;
    private bool isDestroyed = false;
    private bool isInfoOnly = false;
    private bool showTasksAfterContinue = false; // NEW: Flag for showing tasks after continue

    public void Initialize(List<TutorialTask> newTasks, bool pauseGame)
    {
        if (initialized) return;

        tasks = newTasks;
        shouldPauseGame = pauseGame;

        // Check if this is an info-only panel (no tasks or no task container)
        isInfoOnly = (newTasks == null || newTasks.Count == 0 || taskContainer == null);

        if (!isInfoOnly)
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset not assigned to TutorialPanel!");
                return;
            }

            inputActions.Enable();
            SetupTasks();
        }

        // Initialize sequential text fade-in
        InitializeSequentialTexts();

        canvasGroup.alpha = 0f;
        StartCoroutine(FadeIn());

        if (shouldPauseGame)
        {
            PauseGame();
            ShowCursor();

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                // For info-only panels, button is always enabled
                continueButton.interactable = isInfoOnly ? true : false;
            }
            else
            {
                Debug.LogError("TutorialPanel: Continue button is not assigned! This is required for paused tutorials.");
            }
        }
        else
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
        }

        initialized = true;
    }

    private void InitializeSequentialTexts()
    {
        // Start all sequential texts at alpha 0
        foreach (var fadeText in sequentialTexts)
        {
            if (fadeText.textElement != null)
            {
                Color color = fadeText.textElement.color;
                color.a = 0f;
                fadeText.textElement.color = color;
            }
        }
    }

    private void SetupTasks()
    {
        if (taskContainer == null)
        {
            Debug.LogWarning("TaskContainer is null, skipping task setup");
            return;
        }

        foreach (var task in tasks)
        {
            GameObject taskObject = Instantiate(taskTextPrefab, taskContainer);
            TextMeshProUGUI taskText = taskObject.GetComponent<TextMeshProUGUI>();
            taskText.text = task.displayText;
            taskText.color = defaultColor;

            taskTexts[task.actionName] = taskText;
            taskCompletion[task.actionName] = false;

            var actionMap = inputActions.FindActionMap(task.actionMapName);
            if (actionMap != null)
            {
                var action = actionMap.FindAction(task.actionName);
                if (action != null)
                {
                    string actionName = task.actionName;
                    System.Action<InputAction.CallbackContext> callback = (ctx) => OnTaskPerformed(actionName);
                    actionCallbacks[actionName] = callback;
                    action.performed += callback;
                }
                else
                {
                    Debug.LogWarning($"Action '{task.actionName}' not found in action map '{task.actionMapName}'");
                }
            }
            else
            {
                Debug.LogWarning($"Action map '{task.actionMapName}' not found in InputActionAsset");
            }
        }
    }

    private void OnTaskPerformed(string actionName)
    {
        if (isDestroyed) return;

        if (taskCompletion.ContainsKey(actionName) && !taskCompletion[actionName])
        {
            taskCompletion[actionName] = true;

            if (taskTexts.ContainsKey(actionName))
            {
                taskTexts[actionName].color = completedColor;
            }

            CheckAllComplete();
        }
    }

    private void CheckAllComplete()
    {
        if (isDestroyed) return;

        foreach (var completed in taskCompletion.Values)
        {
            if (!completed)
                return;
        }

        allTasksCompleted = true;

        if (shouldPauseGame)
        {
            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }
        else
        {
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    private void OnContinueButtonClicked()
    {
        if (isDestroyed) return;

        // If we need to show tasks after continue, do that instead of closing
        if (showTasksAfterContinue && !allTasksCompleted)
        {
            StartCoroutine(TransitionToTaskTracking());
            return;
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator TransitionToTaskTracking()
    {

        // Fade out info text
        foreach (var fadeText in sequentialTexts)
        {
            if (fadeText.textElement != null)
            {
                StartCoroutine(FadeOutText(fadeText.textElement, 0.3f));
            }
        }

        // Hide continue button
        if (continueButton != null)
        {
            continueButton.interactable = false;
            continueButton.gameObject.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // Now setup the tasks
        if (inputActions == null)
        {
            yield break;
        }

        inputActions.Enable();
        SetupTasks();
        isInfoOnly = false;
        showTasksAfterContinue = false; // We've shown them now

        // Resume game and hide cursor so player can perform tasks
        ResumeGame();
        HideCursor();

    }

    private IEnumerator FadeOutText(TextMeshProUGUI text, float duration)
    {
        float elapsed = 0f;
        Color startColor = text.color;
        Color targetColor = startColor;
        targetColor.a = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            text.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        text.color = targetColor;
    }

    private IEnumerator FadeIn()
    {
        // Fade in the panel itself
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }

        // Start sequential text fade-ins
        if (sequentialTexts.Count > 0)
        {
            StartCoroutine(FadeInSequentialTexts());
        }
    }

    private IEnumerator FadeInSequentialTexts()
    {
        // Start all fade-in coroutines
        foreach (var fadeText in sequentialTexts)
        {
            if (fadeText.textElement != null)
            {
                StartCoroutine(FadeInText(fadeText));
            }
        }

        yield return null;
    }

    private IEnumerator FadeInText(FadeInText fadeText)
    {
        // Wait for the specified delay
        yield return new WaitForSecondsRealtime(fadeText.delayAfterPanelAppears);

        // Fade in the text
        float elapsed = 0f;
        Color startColor = fadeText.textElement.color;
        Color targetColor = startColor;
        targetColor.a = 1f;

        while (elapsed < fadeText.fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeText.fadeDuration;
            fadeText.textElement.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        fadeText.textElement.color = targetColor;
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (isDestroyed) yield break;

        yield return new WaitForSecondsRealtime(0.5f);

        while (canvasGroup.alpha > 0f && !isDestroyed)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }

        if (shouldPauseGame)
        {
            ResumeGame();
            HideCursor();
        }

        OnTutorialCompleted?.Invoke();

        isDestroyed = true;
        UnsubscribeFromActions();
        Destroy(gameObject);
    }

    private void UnsubscribeFromActions()
    {
        if (inputActions == null || tasks == null) return;

        foreach (var task in tasks)
        {
            var actionMap = inputActions.FindActionMap(task.actionMapName);
            if (actionMap != null)
            {
                var action = actionMap.FindAction(task.actionName);
                if (action != null && actionCallbacks.ContainsKey(task.actionName))
                {
                    action.performed -= actionCallbacks[task.actionName];
                }
            }
        }

        actionCallbacks.Clear();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable player movement script
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.enabled = false;
            }
        }
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Re-enable player movement script
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.enabled = true;
            }
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;

        UnsubscribeFromActions();

        if (shouldPauseGame && Time.timeScale == 0f)
        {
             ResumeGame();
            HideCursor();
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
        }
    }
}