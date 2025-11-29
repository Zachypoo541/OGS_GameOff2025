using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    private MonoBehaviour pauseManagerScript;

    private void Start()
    {
        // Setup button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
            Debug.Log("Resume button listener added");
        }
        else
        {
            Debug.LogError("Resume button is not assigned!");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            Debug.Log("Settings button listener added");
        }
        else
        {
            Debug.LogError("Settings button is not assigned!");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            Debug.Log("Main Menu button listener added");
        }
        else
        {
            Debug.LogError("Main Menu button is not assigned!");
        }

        // Make sure settings panel is hidden at start
        ShowMainPause();
    }

    public void SetPauseManager(MonoBehaviour manager)
    {
        pauseManagerScript = manager;
        Debug.Log("PauseManager set: " + (pauseManagerScript != null));
    }

    private void OnResumeClicked()
    {
        Debug.Log("Resume clicked");
        if (pauseManagerScript != null)
        {
            pauseManagerScript.SendMessage("Resume");
        }
        else
        {
            Debug.LogError("PauseManager is null!");
        }
    }

    private void OnSettingsClicked()
    {
        Debug.Log("Settings clicked");
        ShowSettings();
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("Main Menu clicked");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ShowMainPause()
    {
        Debug.Log("Showing main pause panel");
        if (mainPausePanel != null)
        {
            mainPausePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Main Pause Panel is not assigned!");
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Settings Panel is not assigned!");
        }
    }

    public void ShowSettings()
    {
        Debug.Log("Showing settings panel");
        if (mainPausePanel != null)
        {
            mainPausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
}