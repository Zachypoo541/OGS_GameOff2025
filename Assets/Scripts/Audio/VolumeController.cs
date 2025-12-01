using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class VolumeController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Document")]
    [SerializeField] private UIDocument menuDocument;

    [Header("Mixer Parameter Names")]
    [Tooltip("The exposed parameter name in your Audio Mixer for master volume")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    
    [Tooltip("The exposed parameter name in your Audio Mixer for music volume")]
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    
    [Tooltip("The exposed parameter name in your Audio Mixer for SFX volume")]
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;

    private void Start()
    {
        SetupUI();
        LoadVolumeSettings();
    }

    private void SetupUI()
    {
        if (menuDocument == null)
        {
            Debug.LogError("VolumeController: No UIDocument assigned!");
            return;
        }

        var root = menuDocument.rootVisualElement;

        // Get slider references
        masterVolumeSlider = root.Q<Slider>("master-volume-slider");
        musicVolumeSlider = root.Q<Slider>("music-volume-slider");
        sfxVolumeSlider = root.Q<Slider>("sfx-volume-slider");

        // Register callbacks
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeChanged(evt.newValue));
        }
        else
        {
            Debug.LogWarning("VolumeController: 'master-volume-slider' not found in UIDocument");
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.RegisterValueChangedCallback(evt => OnMusicVolumeChanged(evt.newValue));
        }
        else
        {
            Debug.LogWarning("VolumeController: 'music-volume-slider' not found in UIDocument");
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.RegisterValueChangedCallback(evt => OnSFXVolumeChanged(evt.newValue));
        }
        else
        {
            Debug.LogWarning("VolumeController: 'sfx-volume-slider' not found in UIDocument");
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        SetMixerVolume(masterVolumeParameter, value);
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void OnMusicVolumeChanged(float value)
    {
        SetMixerVolume(musicVolumeParameter, value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(float value)
    {
        SetMixerVolume(sfxVolumeParameter, value);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Convert linear slider value (0-1) to decibel value for Audio Mixer
    /// </summary>
    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        if (mainMixer == null)
        {
            Debug.LogError("VolumeController: No Audio Mixer assigned!");
            return;
        }

        // Convert from linear (0-1) to decibels (-80 to 0)
        // Using logarithmic scale for proper volume perception
        float volume = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
        
        mainMixer.SetFloat(parameterName, volume);
    }

    /// <summary>
    /// Load saved volume settings and apply them
    /// </summary>
    private void LoadVolumeSettings()
    {
        // Load master volume
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }
        SetMixerVolume(masterVolumeParameter, masterVolume);

        // Load music volume
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);
        }
        SetMixerVolume(musicVolumeParameter, musicVolume);

        // Load SFX volume
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
        }
        SetMixerVolume(sfxVolumeParameter, sfxVolume);
    }

    /// <summary>
    /// Reset all volumes to default (1.0)
    /// </summary>
    public void ResetVolumes()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
    }

    /// <summary>
    /// Get the current master volume (0-1)
    /// </summary>
    public float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat("MasterVolume", 1f);
    }

    /// <summary>
    /// Get the current music volume (0-1)
    /// </summary>
    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat("MusicVolume", 1f);
    }

    /// <summary>
    /// Get the current SFX volume (0-1)
    /// </summary>
    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat("SFXVolume", 1f);
    }
}
