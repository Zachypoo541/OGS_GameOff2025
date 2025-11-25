using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes hand video rainbow effects react to music by analyzing separate audio stems.
/// Works with MusicStemManager to analyze bass, mid, and high stems independently.
/// Can control multiple hand players (left and right).
/// </summary>
[RequireComponent(typeof(MusicStemManager))]
public class AudioReactiveHandEffect : MonoBehaviour
{
    [Header("Hand References")]
    [Tooltip("Reference to the Raw Image component displaying the right hand video")]
    public RawImage rightHandVideoImage;

    [Tooltip("Reference to the Raw Image component displaying the left hand video")]
    public RawImage leftHandVideoImage;

    [Header("Audio Settings")]
    [Range(64, 8192)]
    [Tooltip("Number of samples to analyze (higher = more precise, lower = better performance)")]
    public int sampleSize = 1024;

    [Header("Bass Stem - Rainbow Spread")]
    [Tooltip("Enable bass stem reactivity")]
    public bool useBassReactivity = true;

    [Range(0.5f, 20f)]
    public float bassMinSpread = 1.0f;

    [Range(0.5f, 100f)]
    public float bassMaxSpread = 3.0f;

    [Range(0f, 10000f)]
    [Tooltip("Sensitivity for bass stem (start with 50-200)")]
    public float bassSensitivity = 100f;

    [Header("Mid Stem - Rainbow Intensity")]
    [Tooltip("Enable mid stem reactivity")]
    public bool useMidReactivity = true;

    [Range(0.5f, 10f)]
    public float midMinIntensity = 0.8f;

    [Range(0.5f, 10f)]
    public float midMaxIntensity = 1.5f;

    [Range(0f, 10000f)]
    [Tooltip("Sensitivity for mid stem (start with 50-200)")]
    public float midSensitivity = 100f;

    [Header("High Stem - Line Sharpness")]
    [Tooltip("Enable high stem reactivity")]
    public bool useHighReactivity = true;

    [Range(0.1f, 5f)]
    public float highMinSharpness = 1.0f;

    [Range(0.1f, 5f)]
    public float highMaxSharpness = 3.0f;

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

    [Header("Debug")]
    [Tooltip("Show volume values in console")]
    public bool showDebugInfo = false;

    // Private variables
    private MusicStemManager stemManager;
    private Material rightHandMaterial;
    private Material leftHandMaterial;

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
    private float currentSpread = 1.5f;
    private float currentIntensity = 1.0f;
    private float currentSharpness = 2.0f;

    // Shader property IDs (cached for performance)
    private int spreadPropertyID;
    private int intensityPropertyID;
    private int sharpnessPropertyID;

    private void Start()
    {
        // Get stem manager
        stemManager = GetComponent<MusicStemManager>();
        if (stemManager == null)
        {
            Debug.LogError("AudioReactiveHandEffect: No MusicStemManager found! Add MusicStemManager component to this GameObject.");
            enabled = false;
            return;
        }

        // Check if at least one hand is assigned
        if (rightHandVideoImage == null && leftHandVideoImage == null)
        {
            Debug.LogError("AudioReactiveHandEffect: No RawImage assigned! Please assign at least one hand video RawImage in the Inspector.");
            enabled = false;
            return;
        }

        // Get materials from the Raw Images
        if (rightHandVideoImage != null)
        {
            rightHandMaterial = rightHandVideoImage.material;
            if (rightHandMaterial == null)
            {
                Debug.LogWarning("AudioReactiveHandEffect: Right hand RawImage has no material assigned!");
            }
            else
            {
                Debug.Log("AudioReactiveHandEffect: Right hand material initialized");
            }
        }

        if (leftHandVideoImage != null)
        {
            leftHandMaterial = leftHandVideoImage.material;
            if (leftHandMaterial == null)
            {
                Debug.LogWarning("AudioReactiveHandEffect: Left hand RawImage has no material assigned!");
            }
            else
            {
                Debug.Log("AudioReactiveHandEffect: Left hand material initialized");
            }
        }

        // If no valid materials, disable
        if (rightHandMaterial == null && leftHandMaterial == null)
        {
            Debug.LogError("AudioReactiveHandEffect: No valid materials found on any hand!");
            enabled = false;
            return;
        }

        // Cache shader property IDs
        spreadPropertyID = Shader.PropertyToID("_RainbowSpread");
        intensityPropertyID = Shader.PropertyToID("_RainbowIntensity");
        sharpnessPropertyID = Shader.PropertyToID("_LineSharpness");

        // Initialize spectrum data arrays
        bassSpectrumData = new float[sampleSize];
        midSpectrumData = new float[sampleSize];
        highSpectrumData = new float[sampleSize];

        // Initialize current values from first available material
        Material referenceMaterial = rightHandMaterial != null ? rightHandMaterial : leftHandMaterial;
        if (referenceMaterial.HasProperty(spreadPropertyID))
            currentSpread = referenceMaterial.GetFloat(spreadPropertyID);
        if (referenceMaterial.HasProperty(intensityPropertyID))
            currentIntensity = referenceMaterial.GetFloat(intensityPropertyID);
        if (referenceMaterial.HasProperty(sharpnessPropertyID))
            currentSharpness = referenceMaterial.GetFloat(sharpnessPropertyID);

        Debug.Log("AudioReactiveHandEffect: Initialized with stem-based audio analysis for hand videos");
    }

    private void Update()
    {
        if (stemManager == null || !stemManager.IsPlaying())
        {
            return;
        }

        // Check if we have any valid materials
        if (rightHandMaterial == null && leftHandMaterial == null)
        {
            return;
        }

        // Get audio sources from stem manager
        AudioSource bassSource = stemManager.GetBassAnalysisSource();
        AudioSource midSource = stemManager.GetMidAnalysisSource();
        AudioSource highSource = stemManager.GetHighAnalysisSource();

        // Analyze bass stem (controls Rainbow Spread)
        if (useBassReactivity && bassSource != null && bassSource.clip != null)
        {
            bassSource.GetSpectrumData(bassSpectrumData, 0, FFTWindow.BlackmanHarris);
            currentBassVolume = CalculateAverageVolume(bassSpectrumData);

            float amplifiedBass = currentBassVolume * bassSensitivity;
            smoothedBassVolume = Mathf.Lerp(smoothedBassVolume, amplifiedBass, 1f - smoothing);

            float targetSpread = Mathf.Lerp(bassMinSpread, bassMaxSpread, Mathf.Clamp01(smoothedBassVolume));
            currentSpread = Mathf.Lerp(currentSpread, targetSpread, 1f - parameterSmoothing);

            // Apply to both materials
            SetPropertyOnBothMaterials(spreadPropertyID, currentSpread);

            if (showDebugInfo)
            {
                Debug.Log($"<color=red>Bass: Raw={currentBassVolume:F6} Smooth={smoothedBassVolume:F2} Spread={currentSpread:F2}</color>");
            }
        }

        // Analyze mid stem (controls Rainbow Intensity)
        if (useMidReactivity && midSource != null && midSource.clip != null)
        {
            midSource.GetSpectrumData(midSpectrumData, 0, FFTWindow.BlackmanHarris);
            currentMidVolume = CalculateAverageVolume(midSpectrumData);

            float amplifiedMid = currentMidVolume * midSensitivity;
            smoothedMidVolume = Mathf.Lerp(smoothedMidVolume, amplifiedMid, 1f - smoothing);

            float targetIntensity = Mathf.Lerp(midMinIntensity, midMaxIntensity, Mathf.Clamp01(smoothedMidVolume));
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, 1f - parameterSmoothing);

            // Apply to both materials
            SetPropertyOnBothMaterials(intensityPropertyID, currentIntensity);

            if (showDebugInfo)
            {
                Debug.Log($"<color=green>Mid: Raw={currentMidVolume:F6} Smooth={smoothedMidVolume:F2} Intensity={currentIntensity:F2}</color>");
            }
        }

        // Analyze high stem (controls Line Sharpness)
        if (useHighReactivity && highSource != null && highSource.clip != null)
        {
            highSource.GetSpectrumData(highSpectrumData, 0, FFTWindow.BlackmanHarris);
            currentHighVolume = CalculateAverageVolume(highSpectrumData);

            float amplifiedHigh = currentHighVolume * highSensitivity;
            smoothedHighVolume = Mathf.Lerp(smoothedHighVolume, amplifiedHigh, 1f - smoothing);

            float targetSharpness = Mathf.Lerp(highMinSharpness, highMaxSharpness, Mathf.Clamp01(smoothedHighVolume));
            currentSharpness = Mathf.Lerp(currentSharpness, targetSharpness, 1f - parameterSmoothing);

            // Apply to both materials
            SetPropertyOnBothMaterials(sharpnessPropertyID, currentSharpness);

            if (showDebugInfo)
            {
                Debug.Log($"<color=blue>High: Raw={currentHighVolume:F6} Smooth={smoothedHighVolume:F2} Sharpness={currentSharpness:F2}</color>");
            }
        }
    }

    /// <summary>
    /// Helper method to set a shader property on both hand materials
    /// </summary>
    private void SetPropertyOnBothMaterials(int propertyID, float value)
    {
        if (rightHandMaterial != null && rightHandMaterial.HasProperty(propertyID))
        {
            rightHandMaterial.SetFloat(propertyID, value);
        }

        if (leftHandMaterial != null && leftHandMaterial.HasProperty(propertyID))
        {
            leftHandMaterial.SetFloat(propertyID, value);
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
    /// Get current smoothed volumes (useful for debugging)
    /// </summary>
    public Vector3 GetStemVolumes()
    {
        return new Vector3(smoothedBassVolume, smoothedMidVolume, smoothedHighVolume);
    }

    /// <summary>
    /// Manually set the spread value (useful for testing or overriding)
    /// </summary>
    public void SetSpread(float value)
    {
        currentSpread = value;
        SetPropertyOnBothMaterials(spreadPropertyID, value);
    }

    /// <summary>
    /// Manually set the intensity value
    /// </summary>
    public void SetIntensity(float value)
    {
        currentIntensity = value;
        SetPropertyOnBothMaterials(intensityPropertyID, value);
    }

    /// <summary>
    /// Manually set the sharpness value
    /// </summary>
    public void SetSharpness(float value)
    {
        currentSharpness = value;
        SetPropertyOnBothMaterials(sharpnessPropertyID, value);
    }
}