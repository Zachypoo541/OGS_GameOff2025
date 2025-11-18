using UnityEngine;

/// <summary>
/// Makes the Edge Detection effect react to music by analyzing separate audio stems.
/// Works with MusicStemManager to analyze bass, mid, and high stems independently.
/// </summary>
[RequireComponent(typeof(MusicStemManager))]
public class AudioReactiveEdgeDetection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the EdgeDetectionController in your scene")]
    public EdgeDetectionController edgeController;

    [Header("Audio Settings")]
    [Range(64, 8192)]
    [Tooltip("Number of samples to analyze (higher = more precise, lower = better performance)")]
    public int sampleSize = 1024;

    [Header("Stem Reactivity")]
    [Tooltip("Enable bass stem reactivity")]
    public bool useBassReactivity = true;

    [Tooltip("Bass stem controls this parameter")]
    public VolumeReactiveParameter bassParameter = VolumeReactiveParameter.ChromaticSpread;

    [Range(0f, 100f)]
    public float bassMinValue = 3f;

    [Range(0f, 100f)]
    public float bassMaxValue = 6f;

    [Range(0f, 10000f)]
    [Tooltip("Sensitivity for bass stem (start with 50-200)")]
    public float bassSensitivity = 100f;

    [Header("Mid Stem Settings")]
    [Tooltip("Enable mid stem reactivity")]
    public bool useMidReactivity = true;

    [Tooltip("Mid stem controls this parameter")]
    public VolumeReactiveParameter midParameter = VolumeReactiveParameter.ChromaticIntensity;

    [Range(0f, 100f)]
    public float midMinValue = 3f;

    [Range(0f, 100f)]
    public float midMaxValue = 10f;

    [Range(0f, 10000f)]
    [Tooltip("Sensitivity for mid stem (start with 50-200)")]
    public float midSensitivity = 100f;

    [Header("High Stem Settings")]
    [Tooltip("Enable high stem reactivity")]
    public bool useHighReactivity = true;

    [Tooltip("High stem controls this parameter")]
    public VolumeReactiveParameter highParameter = VolumeReactiveParameter.ChromaticFalloff;

    [Range(0f, 100f)]
    public float highMinValue = 3f;

    [Range(0f, 100f)]
    public float highMaxValue = 10f;

    [Range(0f, 10000f)]
    [Tooltip("Sensitivity for high stem (start with 50-200)")]
    public float highSensitivity = 100f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    [Tooltip("Smoothing factor for audio data (0 = instant, 1 = very smooth)")]
    public float smoothing = 0.85f;

    [Range(0f, 1f)]
    [Tooltip("How quickly parameters interpolate to new values (0 = instant, 1 = very smooth)")]
    public float parameterSmoothing = 0.3f;

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
    private MusicStemManager stemManager;
    private float[] bassSpectrumData;
    private float[] midSpectrumData;
    private float[] highSpectrumData;

    private float currentBassVolume = 0f;
    private float currentMidVolume = 0f;
    private float currentHighVolume = 0f;

    private float smoothedBassVolume = 0f;
    private float smoothedMidVolume = 0f;
    private float smoothedHighVolume = 0f;

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

    private void Start()
    {
        // Get stem manager
        stemManager = GetComponent<MusicStemManager>();
        if (stemManager == null)
        {
            Debug.LogError("AudioReactiveEdgeDetection: No MusicStemManager found! Add MusicStemManager component to this GameObject.");
            enabled = false;
            return;
        }

        if (edgeController == null)
        {
            Debug.LogError("AudioReactiveEdgeDetection: No EdgeDetectionController assigned! Please assign it in the Inspector.");
            enabled = false;
            return;
        }

        // Initialize spectrum data arrays
        bassSpectrumData = new float[sampleSize];
        midSpectrumData = new float[sampleSize];
        highSpectrumData = new float[sampleSize];

        Debug.Log("AudioReactiveEdgeDetection: Initialized with stem-based audio analysis");
    }

    private void Update()
    {
        if (stemManager == null || edgeController == null || !stemManager.IsPlaying())
        {
            return;
        }

        // Get audio sources from stem manager
        AudioSource bassSource = stemManager.GetBassAnalysisSource();
        AudioSource midSource = stemManager.GetMidAnalysisSource();
        AudioSource highSource = stemManager.GetHighAnalysisSource();

        // Analyze bass stem
        if (useBassReactivity && bassSource != null && bassSource.clip != null)
        {
            bassSource.GetSpectrumData(bassSpectrumData, 0, FFTWindow.BlackmanHarris);
            currentBassVolume = CalculateAverageVolume(bassSpectrumData);

            float amplifiedBass = currentBassVolume * bassSensitivity;
            smoothedBassVolume = Mathf.Lerp(smoothedBassVolume, amplifiedBass, 1f - smoothing);

            float mappedBass = Mathf.Lerp(bassMinValue, bassMaxValue, Mathf.Clamp01(smoothedBassVolume));
            ApplyParameterValue(bassParameter, mappedBass);

            if (showDebugInfo)
            {
                Debug.Log($"<color=red>Bass: Raw={currentBassVolume:F6} Smooth={smoothedBassVolume:F2} [{bassParameter}]={mappedBass:F2}</color>");
            }
        }

        // Analyze mid stem
        if (useMidReactivity && midSource != null && midSource.clip != null)
        {
            midSource.GetSpectrumData(midSpectrumData, 0, FFTWindow.BlackmanHarris);
            currentMidVolume = CalculateAverageVolume(midSpectrumData);

            float amplifiedMid = currentMidVolume * midSensitivity;
            smoothedMidVolume = Mathf.Lerp(smoothedMidVolume, amplifiedMid, 1f - smoothing);

            float mappedMid = Mathf.Lerp(midMinValue, midMaxValue, Mathf.Clamp01(smoothedMidVolume));
            ApplyParameterValue(midParameter, mappedMid);

            if (showDebugInfo)
            {
                Debug.Log($"<color=green>Mid: Raw={currentMidVolume:F6} Smooth={smoothedMidVolume:F2} [{midParameter}]={mappedMid:F2}</color>");
            }
        }

        // Analyze high stem
        if (useHighReactivity && highSource != null && highSource.clip != null)
        {
            highSource.GetSpectrumData(highSpectrumData, 0, FFTWindow.BlackmanHarris);
            currentHighVolume = CalculateAverageVolume(highSpectrumData);

            float amplifiedHigh = currentHighVolume * highSensitivity;
            smoothedHighVolume = Mathf.Lerp(smoothedHighVolume, amplifiedHigh, 1f - smoothing);

            float mappedHigh = Mathf.Lerp(highMinValue, highMaxValue, Mathf.Clamp01(smoothedHighVolume));
            ApplyParameterValue(highParameter, mappedHigh);

            if (showDebugInfo)
            {
                Debug.Log($"<color=blue>High: Raw={currentHighVolume:F6} Smooth={smoothedHighVolume:F2} [{highParameter}]={mappedHigh:F2}</color>");
            }
        }

        // Enable rainbow mode if requested
        if (enableRainbowWithMusic && !edgeController.useRainbowGradient)
        {
            edgeController.SetRainbowMode(true);
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
    private float CalculateAverageVolume(float[] spectrumData)
    {
        float sum = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            sum += spectrumData[i];
        }
        return sum / spectrumData.Length;
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
    /// Get current smoothed volumes (useful for debugging)
    /// </summary>
    public Vector3 GetStemVolumes()
    {
        return new Vector3(smoothedBassVolume, smoothedMidVolume, smoothedHighVolume);
    }
}