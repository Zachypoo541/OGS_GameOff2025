using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Back Button")]
    [SerializeField] private Button backButton;

    private PauseMenuUI pauseMenuUI;

    private void Start()
    {
        pauseMenuUI = GetComponentInParent<PauseMenuUI>();
        Debug.Log("PauseMenuUI found: " + (pauseMenuUI != null));

        // Setup slider listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        // Setup back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            Debug.Log("Back button listener added");
        }
        else
        {
            Debug.LogError("Back button is not assigned!");
        }

        // Load saved settings or set defaults
        LoadSettings();
    }

    public void SetMasterVolume(float volume)
    {
        float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
        bool success = mainMixer.SetFloat("MasterVolume", dbValue);
        Debug.Log($"Setting MasterVolume: {dbValue}dB (success: {success})");
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
        mainMixer.SetFloat("MusicVolume", dbValue);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
        mainMixer.SetFloat("SFXVolume", dbValue);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    private void OnBackClicked()
    {
        Debug.Log("Back button clicked");
        if (pauseMenuUI != null)
        {
            pauseMenuUI.ShowMainPause();
        }
        else
        {
            Debug.LogError("PauseMenuUI is null!");
        }
    }

    private void LoadSettings()
    {
        // Load saved volumes or default to 1 (max volume)
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;
    }
}