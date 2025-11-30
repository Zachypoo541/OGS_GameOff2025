using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MainMenuController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;
    
    private UIDocument _document;
    
    // Main Menu elements
    private VisualElement _mainMenuPanel;
    private Button _startButton;
    private Button _optionsButton;
    private Button _quitButton;
    
    // Settings Menu elements
    private VisualElement _settingsPanel;
    private Button _backButton;
    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    
    void Awake()
    {
        _document = GetComponent<UIDocument>();
    }
    
    void OnEnable()
    {
        // Get root element
        var root = _document.rootVisualElement;
        
        // Query main menu panel and buttons
        _mainMenuPanel = root.Q<VisualElement>("main-menu-panel");
        _startButton = root.Q<Button>("start-button");
        _optionsButton = root.Q<Button>("options-button");
        _quitButton = root.Q<Button>("quit-button");
        
        // Query settings panel and its elements
        _settingsPanel = root.Q<VisualElement>("settings-panel");
        _backButton = root.Q<Button>("back-button");
        _masterVolumeSlider = root.Q<Slider>("master-volume-slider");
        _musicVolumeSlider = root.Q<Slider>("music-volume-slider");
        _sfxVolumeSlider = root.Q<Slider>("sfx-volume-slider");
        
        // Subscribe to main menu button events
        _startButton.clicked += OnStartClicked;
        _optionsButton.clicked += OnOptionsClicked;
        _quitButton.clicked += OnQuitClicked;
        
        // Subscribe to settings button events
        _backButton.clicked += OnBackClicked;
        
        // Subscribe to slider events
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.RegisterValueChangedCallback(evt => SetMasterVolume(evt.newValue));
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.RegisterValueChangedCallback(evt => SetMusicVolume(evt.newValue));
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.RegisterValueChangedCallback(evt => SetSFXVolume(evt.newValue));
        
        // Load saved settings
        LoadSettings();
        
        // Show main menu by default
        ShowMainMenu();
    }
    
    void OnDisable()
    {
        // Unsubscribe from main menu events
        _startButton.clicked -= OnStartClicked;
        _optionsButton.clicked -= OnOptionsClicked;
        _quitButton.clicked -= OnQuitClicked;
        
        // Unsubscribe from settings events
        _backButton.clicked -= OnBackClicked;
    }
    
    private void ShowMainMenu()
    {
        _mainMenuPanel.style.display = DisplayStyle.Flex;
        _settingsPanel.style.display = DisplayStyle.None;
    }
    
    private void ShowSettings()
    {
        _mainMenuPanel.style.display = DisplayStyle.None;
        _settingsPanel.style.display = DisplayStyle.Flex;
    }
    
    private void OnStartClicked()
    {
        Debug.Log("Start Game clicked!");
        SceneManager.LoadScene("Forest");
    }
    
    private void OnOptionsClicked()
    {
        Debug.Log("Options clicked!");
        ShowSettings();
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("Quit clicked!");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void OnBackClicked()
    {
        Debug.Log("Back clicked!");
        ShowMainMenu();
    }
    
    // Audio Settings Methods
    private void SetMasterVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }
    }
    
    private void SetMusicVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
    }
    
    private void SetSFXVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }
    }
    
    private void LoadSettings()
    {
        // Load saved volumes or default to 1 (max volume)
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.value = masterVolume;
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.value = musicVolume;
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.value = sfxVolume;
    }
}
