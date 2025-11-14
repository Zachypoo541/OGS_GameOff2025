using UnityEngine;

/// <summary>
/// Makes the Edge Detection effect react to music by analyzing audio spectrum data.
/// Attach this to a GameObject with an AudioSource component.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioReactiveEdgeDetection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the EdgeDetectionController in your scene")]
    public EdgeDetectionController edgeController;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource component playing your music (must have volume > 0)")]
    public AudioSource audioSource;

    [Range(64, 8192)]
    [Tooltip("Number of samples to analyze (higher = more precise, lower = better performance)")]
    public int sampleSize = 1024;

    [Header("Volume Reactivity")]
    [Tooltip("Enable overall volume reactivity (adjusts one main parameter)")]
    public bool useVolumeReactivity = true;

    [Tooltip("Which parameter should react to the overall music volume")]
    public VolumeReactiveParameter volumeParameter = VolumeReactiveParameter.ChromaticIntensity;

    [Range(0f, 100f)]
    [Tooltip("Minimum value when audio is silent")]
    public float minIntensity = 3f;

    [Range(0f, 100f)]
    [Tooltip("Maximum value when audio is at peak volume")]
    public float maxIntensity = 10f;

    [Range(0f, 10000f)]
    [Tooltip("Multiplier for audio sensitivity (higher = more reactive) - Start with 100-500")]
    public float sensitivity = 200f;

    [Range(0f, 1f)]
    [Tooltip("Smoothing factor (0 = instant, 1 = very smooth)")]
    public float smoothing = 0.85f;

    [Range(0f, 1f)]
    [Tooltip("How quickly parameters interpolate to new values (0 = instant, 1 = very smooth)")]
    public float parameterSmoothing = 0.3f;

    [Header("Frequency Band Reactivity")]
    [Tooltip("Enable different parameters to react to different frequency bands")]
    public bool useFrequencyBands = true;

    [Tooltip("React to bass frequencies (0-250 Hz) - Drums, bass guitar")]
    public FrequencyBandReaction bassReaction = new FrequencyBandReaction
    {
        enabled = true,
        parameter = VolumeReactiveParameter.ChromaticSpread,
        minValue = 3f,
        maxValue = 6f,
        sensitivity = 200f
    };

    [Tooltip("React to mid frequencies (250-2000 Hz) - Vocals, guitars")]
    public FrequencyBandReaction midReaction = new FrequencyBandReaction
    {
        enabled = true,
        parameter = VolumeReactiveParameter.ChromaticIntensity,
        minValue = 3f,
        maxValue = 10f,
        sensitivity = 200f
    };

    [Tooltip("React to high frequencies (2000+ Hz) - Cymbals, high notes")]
    public FrequencyBandReaction highReaction = new FrequencyBandReaction
    {
        enabled = true,
        parameter = VolumeReactiveParameter.ChromaticFalloff,
        minValue = 3f,
        maxValue = 10f,
        sensitivity = 200f
    };

    [Header("Rainbow Mode")]
    [Tooltip("Auto-enable rainbow mode when music plays")]
    public bool enableRainbowWithMusic = true;

    [Tooltip("Automatically cycle rainbow samples based on beat")]
    public bool animateRainbowSamples = false;

    [Range(0.1f, 5f)]
    public float rainbowAnimationSpeed = 1f;

    [Header("Debug")]
    [Tooltip("Show volume values in console")]
    public bool showDebugInfo = false;

    // Private variables
    private float[] spectrumData;
    private float currentVolume = 0f;
    private float smoothedVolume = 0f;

    private float smoothedBass = 0f;
    private float smoothedMid = 0f;
    private float smoothedHigh = 0f;

    // Interpolated parameter values for smooth transitions
    private float currentChromaticIntensity = 3f;
    private float currentChromaticSpread = 3f;
    private float currentChromaticFalloff = 5f;
    private float currentEdgeThickness = 1f;
    private float currentDepthSensitivity = 10f;
    private float currentNormalSensitivity = 1f;

    public enum VolumeReactiveParameter
    {
        ChromaticIntensity,
        ChromaticSpread,
        ChromaticFalloff,
        EdgeThickness,
        DepthSensitivity,
        NormalSensitivity
    }

    [System.Serializable]
    public class FrequencyBandReaction
    {
        public bool enabled = false;
        public VolumeReactiveParameter parameter = VolumeReactiveParameter.ChromaticSpread;
        [Range(0f, 100f)] public float minValue = 3f;
        [Range(0f, 100f)] public float maxValue = 10f;
        [Range(0f, 10000f)] public float sensitivity = 200f;
    }

    private void Start()
    {
        // Get AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("AudioReactiveEdgeDetection: No AudioSource found! Please add an AudioSource component.");
            enabled = false;
            return;
        }

        if (edgeController == null)
        {
            Debug.LogError("AudioReactiveEdgeDetection: No EdgeDetectionController assigned! Please assign it in the Inspector.");
            enabled = false;
            return;
        }

        // Initialize spectrum data array
        spectrumData = new float[sampleSize];

        // Enable rainbow mode if requested
        if (enableRainbowWithMusic && audioSource.isPlaying)
        {
            edgeController.SetRainbowMode(true);
        }

        Debug.Log("AudioReactiveEdgeDetection: Initialized successfully");
        Debug.Log("NOTE: AudioSource volume must be > 0 for spectrum analysis. Use an Audio Mixer to control playback volume separately.");
    }

    private void Update()
    {
        if (audioSource == null || edgeController == null || !audioSource.isPlaying)
        {
            return;
        }

        // Get spectrum data from audio source
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        // Calculate overall volume
        currentVolume = CalculateAverageVolume();

        // Apply sensitivity and smooth the volume
        float amplifiedVolume = currentVolume * sensitivity;
        smoothedVolume = Mathf.Lerp(smoothedVolume, amplifiedVolume, 1f - smoothing);

        // Debug output
        if (showDebugInfo)
        {
            Debug.Log($"Raw: {currentVolume:F6} | Amp: {amplifiedVolume:F2} | Smooth: {smoothedVolume:F2}");
        }

        // Apply volume-based reactivity if enabled
        if (useVolumeReactivity)
        {
            ApplyVolumeReactivity(smoothedVolume);
        }

        // Apply frequency band reactivity if enabled
        if (useFrequencyBands)
        {
            ApplyFrequencyBandReactivity();
        }

        // Animate rainbow samples if enabled
        if (animateRainbowSamples && edgeController.useRainbowGradient)
        {
            edgeController.AnimateRainbowSamples(rainbowAnimationSpeed);
        }
    }

    /// <summary>
    /// Calculate average volume from spectrum data
    /// </summary>
    private float CalculateAverageVolume()
    {
        float sum = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            sum += spectrumData[i];
        }
        return sum / spectrumData.Length;
    }

    /// <summary>
    /// Apply the main volume-based parameter change
    /// </summary>
    private void ApplyVolumeReactivity(float volume)
    {
        // Map the volume (0-1+ range after sensitivity) to min/max intensity
        float clampedVolume = Mathf.Clamp01(volume);
        float mappedValue = Mathf.Lerp(minIntensity, maxIntensity, clampedVolume);

        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>Volume [{volumeParameter}]: {mappedValue:F2}</color>");
        }

        ApplyParameterValue(volumeParameter, mappedValue);
    }

    /// <summary>
    /// Apply frequency band-specific reactivity for more complex audio visualization
    /// </summary>
    private void ApplyFrequencyBandReactivity()
    {
        // Calculate frequency bands
        // Bass: 0-250 Hz (approximately first 1/32 of spectrum)
        // Mid: 250-2000 Hz (approximately next 1/8 of spectrum)
        // High: 2000+ Hz (remaining spectrum)
        float bass = CalculateFrequencyBand(0, sampleSize / 32);
        float mid = CalculateFrequencyBand(sampleSize / 32, sampleSize / 4);
        float high = CalculateFrequencyBand(sampleSize / 4, sampleSize);

        // Apply sensitivity and smooth the bands
        float amplifiedBass = bass * bassReaction.sensitivity;
        float amplifiedMid = mid * midReaction.sensitivity;
        float amplifiedHigh = high * highReaction.sensitivity;

        smoothedBass = Mathf.Lerp(smoothedBass, amplifiedBass, 1f - smoothing);
        smoothedMid = Mathf.Lerp(smoothedMid, amplifiedMid, 1f - smoothing);
        smoothedHigh = Mathf.Lerp(smoothedHigh, amplifiedHigh, 1f - smoothing);

        if (showDebugInfo)
        {
            Debug.Log($"<color=yellow>Bass: {smoothedBass:F2} | Mid: {smoothedMid:F2} | High: {smoothedHigh:F2}</color>");
        }

        // Apply bass reaction
        if (bassReaction.enabled)
        {
            float value = Mathf.Lerp(bassReaction.minValue, bassReaction.maxValue, Mathf.Clamp01(smoothedBass));
            if (showDebugInfo)
            {
                Debug.Log($"<color=red>Bass [{bassReaction.parameter}]: {value:F2}</color>");
            }
            ApplyParameterValue(bassReaction.parameter, value);
        }

        // Apply mid reaction
        if (midReaction.enabled)
        {
            float value = Mathf.Lerp(midReaction.minValue, midReaction.maxValue, Mathf.Clamp01(smoothedMid));
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>Mid [{midReaction.parameter}]: {value:F2}</color>");
            }
            ApplyParameterValue(midReaction.parameter, value);
        }

        // Apply high reaction
        if (highReaction.enabled)
        {
            float value = Mathf.Lerp(highReaction.minValue, highReaction.maxValue, Mathf.Clamp01(smoothedHigh));
            if (showDebugInfo)
            {
                Debug.Log($"<color=blue>High [{highReaction.parameter}]: {value:F2}</color>");
            }
            ApplyParameterValue(highReaction.parameter, value);
        }
    }

    /// <summary>
    /// Calculate average intensity for a specific frequency band
    /// </summary>
    private float CalculateFrequencyBand(int startIndex, int endIndex)
    {
        float sum = 0f;
        int count = endIndex - startIndex;

        for (int i = startIndex; i < endIndex && i < spectrumData.Length; i++)
        {
            sum += spectrumData[i];
        }

        return count > 0 ? sum / count : 0f;
    }

    /// <summary>
    /// Helper method to apply a parameter value with proper scaling and interpolation
    /// </summary>
    private void ApplyParameterValue(VolumeReactiveParameter param, float targetValue)
    {
        switch (param)
        {
            case VolumeReactiveParameter.ChromaticIntensity:
                currentChromaticIntensity = Mathf.Lerp(currentChromaticIntensity, targetValue, 1f - parameterSmoothing);
                edgeController.SetChromaticIntensity(currentChromaticIntensity);
                break;
            case VolumeReactiveParameter.ChromaticSpread:
                currentChromaticSpread = Mathf.Lerp(currentChromaticSpread, targetValue, 1f - parameterSmoothing);
                edgeController.SetChromaticSpread(currentChromaticSpread);
                break;
            case VolumeReactiveParameter.ChromaticFalloff:
                currentChromaticFalloff = Mathf.Lerp(currentChromaticFalloff, targetValue, 1f - parameterSmoothing);
                edgeController.SetChromaticFalloff(currentChromaticFalloff);
                break;
            case VolumeReactiveParameter.EdgeThickness:
                // EdgeThickness has 0-5 range, scale from 0-100 to 0-5
                float targetThickness = targetValue * 0.05f;
                currentEdgeThickness = Mathf.Lerp(currentEdgeThickness, targetThickness, 1f - parameterSmoothing);
                edgeController.SetEdgeThickness(currentEdgeThickness);
                break;
            case VolumeReactiveParameter.DepthSensitivity:
                currentDepthSensitivity = Mathf.Lerp(currentDepthSensitivity, targetValue, 1f - parameterSmoothing);
                edgeController.SetDepthSensitivity(currentDepthSensitivity);
                break;
            case VolumeReactiveParameter.NormalSensitivity:
                currentNormalSensitivity = Mathf.Lerp(currentNormalSensitivity, targetValue, 1f - parameterSmoothing);
                edgeController.SetNormalSensitivity(currentNormalSensitivity);
                break;
        }
    }

    /// <summary>
    /// Get current smoothed volume (useful for debugging)
    /// </summary>
    public float GetCurrentVolume()
    {
        return smoothedVolume;
    }

    /// <summary>
    /// Get frequency band values (useful for debugging)
    /// </summary>
    public Vector3 GetFrequencyBands()
    {
        return new Vector3(smoothedBass, smoothedMid, smoothedHigh);
    }
}