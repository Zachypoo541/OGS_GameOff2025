using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Audio;
using Cursor = UnityEngine.Cursor;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;
    
    [Header("Mixer Parameter Names")]
    [Tooltip("The exposed parameter name in your Audio Mixer for master volume")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    
    [Tooltip("The exposed parameter name in your Audio Mixer for music volume")]
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    
    [Tooltip("The exposed parameter name in your Audio Mixer for SFX volume")]
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";
    
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

    // Controls Panel
    private Button controlsButton;
    private VisualElement controlsPanel;
    private Button controlsBackButton;

    // Volume sliders
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;

    // Waveform buttons
    private Button sineButton;
    private Button sawButton;
    private Button squareButton;
    private Button triangleButton;

    // Waveform panels
    private VisualElement sinePanel;
    private VisualElement sawPanel;
    private VisualElement squarePanel;
    private VisualElement trianglePanel;

    // Waveform back buttons
    private Button sineBackButton;
    private Button sawBackButton;
    private Button squareBackButton;
    private Button triangleBackButton;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Initialize UI elements
        InitializeUI();
    }

    private void OnEnable()
    {
        // Enable input
        inputActions.Enable();
        inputActions.Gameplay.Pause.performed += OnPauseInput;
    }

    private void InitializeUI()
    {
        // Get the root visual element
        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;

            // Query all UI elements by name
            resumeButton = root.Q<Button>("ResumeButton");
            settingsButton = root.Q<Button>("SettingsButton");
            controlsButton = root.Q<Button>("ControlsButton");
            mainMenuButton = root.Q<Button>("MainMenuButton");
            backButton = root.Q<Button>("BackButton");
            controlsBackButton = root.Q<Button>("ControlsBackButton");

            // Query waveform buttons - NEW
            sineButton = root.Q<Button>("SineButton");
            sawButton = root.Q<Button>("SawButton");
            squareButton = root.Q<Button>("SquareButton");
            triangleButton = root.Q<Button>("TriangleButton");

            // Query panels
            mainPausePanel = root.Q<VisualElement>("MainPausePanel");
            settingsPanel = root.Q<VisualElement>("SettingsPanel");
            controlsPanel = root.Q<VisualElement>("ControlsPanel");

            // Query waveform panels - NEW
            sinePanel = root.Q<VisualElement>("SinePanel");
            sawPanel = root.Q<VisualElement>("SawPanel");
            squarePanel = root.Q<VisualElement>("SquarePanel");
            trianglePanel = root.Q<VisualElement>("TrianglePanel");

            // Query waveform back buttons - NEW
            sineBackButton = root.Q<Button>("SineBackButton");
            sawBackButton = root.Q<Button>("SawBackButton");
            squareBackButton = root.Q<Button>("SquareBackButton");
            triangleBackButton = root.Q<Button>("TriangleBackButton");

            // Query volume sliders
            masterVolumeSlider = root.Q<Slider>("master-volume-slider");
            musicVolumeSlider = root.Q<Slider>("music-volume-slider");
            sfxVolumeSlider = root.Q<Slider>("sfx-volume-slider");

            // Register button click events
            if (resumeButton != null)
                resumeButton.RegisterCallback<ClickEvent>(evt => Resume());
            else
                Debug.LogWarning("PauseManager: ResumeButton not found!");

            if (settingsButton != null)
                settingsButton.RegisterCallback<ClickEvent>(evt => OpenSettings());
            else
                Debug.LogWarning("PauseManager: SettingsButton not found!");

            if (controlsButton != null)
                controlsButton.RegisterCallback<ClickEvent>(evt => OpenControls());
            else
                Debug.LogWarning("PauseManager: ControlsButton not found!");

            if (mainMenuButton != null)
                mainMenuButton.RegisterCallback<ClickEvent>(evt => LoadMainMenu());
            else
                Debug.LogWarning("PauseManager: MainMenuButton not found!");

            if (backButton != null)
                backButton.RegisterCallback<ClickEvent>(evt => CloseSettings());
            else
                Debug.LogWarning("PauseManager: BackButton not found!");

            if (controlsBackButton != null)
                controlsBackButton.RegisterCallback<ClickEvent>(evt => CloseControls());
            else
                Debug.LogWarning("PauseManager: ControlsBackButton not found!");

            // Register waveform button events - NEW
            if (sineButton != null)
                sineButton.RegisterCallback<ClickEvent>(evt => OpenWaveformPanel("Sine"));
            else
                Debug.LogWarning("PauseManager: SineButton not found!");

            if (sawButton != null)
                sawButton.RegisterCallback<ClickEvent>(evt => OpenWaveformPanel("Saw"));
            else
                Debug.LogWarning("PauseManager: SawButton not found!");

            if (squareButton != null)
                squareButton.RegisterCallback<ClickEvent>(evt => OpenWaveformPanel("Square"));
            else
                Debug.LogWarning("PauseManager: SquareButton not found!");

            if (triangleButton != null)
                triangleButton.RegisterCallback<ClickEvent>(evt => OpenWaveformPanel("Triangle"));
            else
                Debug.LogWarning("PauseManager: TriangleButton not found!");

            // Register waveform back button events - NEW
            if (sineBackButton != null)
                sineBackButton.RegisterCallback<ClickEvent>(evt => CloseWaveformPanel());
            else
                Debug.LogWarning("PauseManager: SineBackButton not found!");

            if (sawBackButton != null)
                sawBackButton.RegisterCallback<ClickEvent>(evt => CloseWaveformPanel());
            else
                Debug.LogWarning("PauseManager: SawBackButton not found!");

            if (squareBackButton != null)
                squareBackButton.RegisterCallback<ClickEvent>(evt => CloseWaveformPanel());
            else
                Debug.LogWarning("PauseManager: SquareBackButton not found!");

            if (triangleBackButton != null)
                triangleBackButton.RegisterCallback<ClickEvent>(evt => CloseWaveformPanel());
            else
                Debug.LogWarning("PauseManager: TriangleBackButton not found!");

            // Setup volume sliders
            SetupVolumeSliders();

            // Hide the pause menu initially
            HidePauseMenu();
        }
        else
        {
            Debug.LogError("PauseManager: UIDocument is not assigned!");
        }
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Pause.performed -= OnPauseInput;
        inputActions.Disable();
    }

    private void SetupVolumeSliders()
    {
        // Load saved volumes and setup sliders
        if (masterVolumeSlider != null)
        {
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
            masterVolumeSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
            SetMixerVolume(masterVolumeParameter, masterVolume);
        }
        else
        {
            Debug.LogWarning("PauseManager: 'master-volume-slider' not found in UIDocument");
        }
        
        if (musicVolumeSlider != null)
        {
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);
            musicVolumeSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
            SetMixerVolume(musicVolumeParameter, musicVolume);
        }
        else
        {
            Debug.LogWarning("PauseManager: 'music-volume-slider' not found in UIDocument");
        }
        
        if (sfxVolumeSlider != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
            sfxVolumeSlider.RegisterValueChangedCallback(OnSFXVolumeChanged);
            SetMixerVolume(sfxVolumeParameter, sfxVolume);
        }
        else
        {
            Debug.LogWarning("PauseManager: 'sfx-volume-slider' not found in UIDocument");
        }
    }

    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        SetMixerVolume(masterVolumeParameter, evt.newValue);
        PlayerPrefs.SetFloat("MasterVolume", evt.newValue);
        PlayerPrefs.Save();
    }

    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        SetMixerVolume(musicVolumeParameter, evt.newValue);
        PlayerPrefs.SetFloat("MusicVolume", evt.newValue);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(ChangeEvent<float> evt)
    {
        SetMixerVolume(sfxVolumeParameter, evt.newValue);
        PlayerPrefs.SetFloat("SFXVolume", evt.newValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Convert linear slider value (0-1) to decibel value for Audio Mixer
    /// </summary>
    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        if (mainMixer == null)
        {
            Debug.LogError("PauseManager: No Audio Mixer assigned!");
            return;
        }

        // Convert from linear (0-1) to decibels (-80 to 0)
        // Using logarithmic scale for proper volume perception
        float volume = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
        
        mainMixer.SetFloat(parameterName, volume);
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

        // Show main pause panel (not settings or controls)
        if (mainPausePanel != null)
            mainPausePanel.style.display = DisplayStyle.Flex;

        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.None;

        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.None;

        // Hide waveform panels - NEW
        if (sinePanel != null)
            sinePanel.style.display = DisplayStyle.None;
        if (sawPanel != null)
            sawPanel.style.display = DisplayStyle.None;
        if (squarePanel != null)
            squarePanel.style.display = DisplayStyle.None;
        if (trianglePanel != null)
            trianglePanel.style.display = DisplayStyle.None;

        // Pause player
        if (Player.Instance != null)
            Player.Instance.SetPaused(true);

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioListener.pause = false;
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

        AudioListener.pause = false;
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
    private void OpenControls()
    {
        if (mainPausePanel != null)
            mainPausePanel.style.display = DisplayStyle.None;

        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseControls()
    {
        if (mainPausePanel != null)
            mainPausePanel.style.display = DisplayStyle.Flex;

        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.None;
    }

    private void OpenWaveformPanel(string waveformType)
    {
        // Hide controls panel
        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.None;

        // Show the appropriate waveform panel
        switch (waveformType)
        {
            case "Sine":
                if (sinePanel != null)
                    sinePanel.style.display = DisplayStyle.Flex;
                break;
            case "Saw":
                if (sawPanel != null)
                    sawPanel.style.display = DisplayStyle.Flex;
                break;
            case "Square":
                if (squarePanel != null)
                    squarePanel.style.display = DisplayStyle.Flex;
                break;
            case "Triangle":
                if (trianglePanel != null)
                    trianglePanel.style.display = DisplayStyle.Flex;
                break;
        }
    }

    private void CloseWaveformPanel()
    {
        // Hide all waveform panels
        if (sinePanel != null)
            sinePanel.style.display = DisplayStyle.None;
        if (sawPanel != null)
            sawPanel.style.display = DisplayStyle.None;
        if (squarePanel != null)
            squarePanel.style.display = DisplayStyle.None;
        if (trianglePanel != null)
            trianglePanel.style.display = DisplayStyle.None;

        // Show controls panel again
        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.Flex;
    }
}
