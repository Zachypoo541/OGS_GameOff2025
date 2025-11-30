using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Input")]
    private PlayerInputActions inputActions;
    
    private VisualElement root;
    private bool isPaused = false;
    
    // Cursor state tracking
    private bool wasCursorVisible;
    private CursorLockMode previousLockState;
    
    // UI Elements - we'll query these once
    private Button resumeButton;
    private Button settingsButton;
    private Button mainMenuButton;
    private Button backButton;
    private VisualElement mainPausePanel;
    private VisualElement settingsPanel;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Enable input
        inputActions.Enable();
        inputActions.Gameplay.Pause.performed += OnPauseInput;
        
        // Get the root visual element
        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;
            
            // Query all UI elements by name
            resumeButton = root.Q<Button>("ResumeButton");
            settingsButton = root.Q<Button>("SettingsButton");
            mainMenuButton = root.Q<Button>("MainMenuButton");
            backButton = root.Q<Button>("BackButton");
            mainPausePanel = root.Q<VisualElement>("MainPausePanel");
            settingsPanel = root.Q<VisualElement>("SettingsPanel");
            
            // Register button click events
            if (resumeButton != null)
                resumeButton.RegisterCallback<ClickEvent>(evt => Resume());
            
            if (settingsButton != null)
                settingsButton.RegisterCallback<ClickEvent>(evt => OpenSettings());
            
            if (mainMenuButton != null)
                mainMenuButton.RegisterCallback<ClickEvent>(evt => LoadMainMenu());
            
            if (backButton != null)
                backButton.RegisterCallback<ClickEvent>(evt => CloseSettings());
            
            // Hide the pause menu initially
            HidePauseMenu();
        }
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Pause.performed -= OnPauseInput;
        inputActions.Disable();
        
        // Unregister callbacks
        if (resumeButton != null)
            resumeButton.UnregisterCallback<ClickEvent>(evt => Resume());
        
        if (settingsButton != null)
            settingsButton.UnregisterCallback<ClickEvent>(evt => OpenSettings());
        
        if (mainMenuButton != null)
            mainMenuButton.UnregisterCallback<ClickEvent>(evt => LoadMainMenu());
        
        if (backButton != null)
            backButton.UnregisterCallback<ClickEvent>(evt => CloseSettings());
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        // Save cursor state
        wasCursorVisible = Cursor.visible;
        previousLockState = Cursor.lockState;
        
        // Pause game
        Time.timeScale = 0f;
        isPaused = true;
        
        // Show pause menu
        ShowPauseMenu();
        
        // Show main pause panel (not settings)
        if (mainPausePanel != null)
            mainPausePanel.style.display = DisplayStyle.Flex;
        
        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.None;
        
        // Pause player
        if (Player.Instance != null)
            Player.Instance.SetPaused(true);
        
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Resume()
    {
        // Hide pause menu
        HidePauseMenu();
        
        // Resume game
        Time.timeScale = 1f;
        isPaused = false;
        
        // Unpause player
        if (Player.Instance != null)
            Player.Instance.SetPaused(false);
        
        // Restore cursor state
        Cursor.visible = wasCursorVisible;
        Cursor.lockState = previousLockState;
    }

    private void OpenSettings()
    {
        if (mainPausePanel != null)
            mainPausePanel.style.display = DisplayStyle.None;
        
        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseSettings()
    {
        if (mainPausePanel != null)
            mainPausePanel.style.display = DisplayStyle.Flex;
        
        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.None;
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void ShowPauseMenu()
    {
        if (root != null)
            root.style.display = DisplayStyle.Flex;
    }

    private void HidePauseMenu()
    {
        if (root != null)
            root.style.display = DisplayStyle.None;
    }
}