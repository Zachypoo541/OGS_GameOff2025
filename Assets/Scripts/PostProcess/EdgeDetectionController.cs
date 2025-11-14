using UnityEngine;

/// <summary>
/// Controls the Edge Detection post-process effect parameters at runtime.
/// Attach this to any GameObject in your scene.
/// This version is compatible with AudioReactiveEdgeDetection - no Update() loop interference!
/// </summary>
public class EdgeDetectionController : MonoBehaviour
{
    [Header("Edge Detection Material")]
    [Tooltip("The material using the Fullscreen/EdgeDetection shader")]
    public Material edgeDetectionMaterial;

    [Header("Edge Settings")]
    [Range(0f, 5f)]
    public float edgeThickness = 1.0f;
    [Range(0f, 100f)]
    public float depthSensitivity = 10.0f;
    [Range(0f, 100f)]
    public float normalSensitivity = 1.0f;
    public Color edgeColor = Color.white;

    [Header("Chromatic Aberration")]
    public bool useRainbowGradient = false;
    [Range(0f, 100f)]
    public float chromaticSpread = 3.5f;
    [Range(0f, 100f)]
    public float chromaticIntensity = 100f;
    public float chromaticDistance = 10.0f;
    public float chromaticFalloff = 5.0f;
    [Range(3, 12)]
    [Tooltip("Number of color bands in rainbow mode (more = smoother gradient)")]
    public int rainbowSamples = 7;

    // Shader property IDs (cached for performance)
    private static readonly int EdgeThicknessID = Shader.PropertyToID("_EdgeThickness");
    private static readonly int DepthSensitivityID = Shader.PropertyToID("_DepthSensitivity");
    private static readonly int NormalSensitivityID = Shader.PropertyToID("_NormalSensitivity");
    private static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
    private static readonly int UseRainbowGradientID = Shader.PropertyToID("_UseRainbowGradient");
    private static readonly int ChromaticSpreadID = Shader.PropertyToID("_ChromaticSpread");
    private static readonly int ChromaticIntensityID = Shader.PropertyToID("_ChromaticIntensity");
    private static readonly int ChromaticDistanceID = Shader.PropertyToID("_ChromaticDistance");
    private static readonly int ChromaticFalloffID = Shader.PropertyToID("_ChromaticFalloff");
    private static readonly int RainbowSamplesID = Shader.PropertyToID("_RainbowSamples");

    private void Start()
    {
        if (edgeDetectionMaterial == null)
        {
            Debug.LogError("EdgeDetectionController: No material assigned! Please assign the edge detection material in the Inspector.");
            return;
        }

        // Sync with material defaults
        LoadFromMaterial();

        // Apply initial values to shader
        UpdateShaderProperties();

        Debug.Log("EdgeDetectionController: Initialized (audio-reactive compatible mode)");
    }

    // REMOVED Update() - it was overwriting audio-reactive values every frame!
    // The Set methods update the shader directly, so no Update loop is needed.

    /// <summary>
    /// Load all values from the material to sync the controller with material defaults
    /// </summary>
    public void LoadFromMaterial()
    {
        if (edgeDetectionMaterial == null) return;

        edgeThickness = edgeDetectionMaterial.GetFloat(EdgeThicknessID);
        depthSensitivity = edgeDetectionMaterial.GetFloat(DepthSensitivityID);
        normalSensitivity = edgeDetectionMaterial.GetFloat(NormalSensitivityID);
        edgeColor = edgeDetectionMaterial.GetColor(EdgeColorID);
        useRainbowGradient = edgeDetectionMaterial.GetFloat(UseRainbowGradientID) > 0.5f;
        chromaticSpread = edgeDetectionMaterial.GetFloat(ChromaticSpreadID);
        chromaticIntensity = edgeDetectionMaterial.GetFloat(ChromaticIntensityID);
        chromaticDistance = edgeDetectionMaterial.GetFloat(ChromaticDistanceID);
        chromaticFalloff = edgeDetectionMaterial.GetFloat(ChromaticFalloffID);
        rainbowSamples = Mathf.RoundToInt(edgeDetectionMaterial.GetFloat(RainbowSamplesID));

        Debug.Log("EdgeDetectionController: Synced with material defaults");
    }

    /// <summary>
    /// Updates all shader properties based on current values
    /// Only call this manually when needed - not every frame!
    /// </summary>
    public void UpdateShaderProperties()
    {
        if (edgeDetectionMaterial == null) return;

        edgeDetectionMaterial.SetFloat(EdgeThicknessID, edgeThickness);
        edgeDetectionMaterial.SetFloat(DepthSensitivityID, depthSensitivity);
        edgeDetectionMaterial.SetFloat(NormalSensitivityID, normalSensitivity);
        edgeDetectionMaterial.SetColor(EdgeColorID, edgeColor);
        edgeDetectionMaterial.SetFloat(UseRainbowGradientID, useRainbowGradient ? 1f : 0f);
        edgeDetectionMaterial.SetFloat(ChromaticSpreadID, chromaticSpread);
        edgeDetectionMaterial.SetFloat(ChromaticIntensityID, chromaticIntensity);
        edgeDetectionMaterial.SetFloat(ChromaticDistanceID, chromaticDistance);
        edgeDetectionMaterial.SetFloat(ChromaticFalloffID, chromaticFalloff);
        edgeDetectionMaterial.SetFloat(RainbowSamplesID, rainbowSamples);
    }

    // --- Public methods to control parameters from other scripts ---

    public void SetEdgeThickness(float thickness)
    {
        edgeThickness = Mathf.Clamp(thickness, 0f, 5f);
        edgeDetectionMaterial?.SetFloat(EdgeThicknessID, edgeThickness);
    }

    public void SetDepthSensitivity(float sensitivity)
    {
        depthSensitivity = Mathf.Max(0f, sensitivity);
        edgeDetectionMaterial?.SetFloat(DepthSensitivityID, depthSensitivity);
    }

    public void SetNormalSensitivity(float sensitivity)
    {
        normalSensitivity = Mathf.Max(0f, sensitivity);
        edgeDetectionMaterial?.SetFloat(NormalSensitivityID, normalSensitivity);
    }

    public void SetEdgeColor(Color color)
    {
        edgeColor = color;
        edgeDetectionMaterial?.SetColor(EdgeColorID, edgeColor);
    }

    public void SetRainbowMode(bool enabled)
    {
        useRainbowGradient = enabled;
        edgeDetectionMaterial?.SetFloat(UseRainbowGradientID, enabled ? 1f : 0f);
    }

    public void ToggleRainbowMode()
    {
        SetRainbowMode(!useRainbowGradient);
    }

    public void SetRainbowSamples(int samples)
    {
        rainbowSamples = Mathf.Clamp(samples, 3, 12);
        edgeDetectionMaterial?.SetFloat(RainbowSamplesID, rainbowSamples);
    }

    public void SetChromaticSpread(float spread)
    {
        chromaticSpread = Mathf.Clamp(spread, 0f, 100f);
        edgeDetectionMaterial?.SetFloat(ChromaticSpreadID, chromaticSpread);
    }

    public void SetChromaticIntensity(float intensity)
    {
        // Accept 0-100 range (matches shader's Range(0, 100))
        chromaticIntensity = Mathf.Clamp(intensity, 0f, 100f);
        edgeDetectionMaterial?.SetFloat(ChromaticIntensityID, chromaticIntensity);
    }

    public void SetChromaticDistance(float distance)
    {
        chromaticDistance = Mathf.Max(0f, distance);
        edgeDetectionMaterial?.SetFloat(ChromaticDistanceID, chromaticDistance);
    }

    public void SetChromaticFalloff(float falloff)
    {
        chromaticFalloff = Mathf.Max(0.1f, falloff);
        edgeDetectionMaterial?.SetFloat(ChromaticFalloffID, chromaticFalloff);
    }

    // --- Example animation methods ---

    /// <summary>
    /// Example: Pulse the chromatic aberration intensity over time
    /// </summary>
    public void PulseChromaticIntensity(float speed = 1f, float min = 0f, float max = 100f)
    {
        float intensity = Mathf.Lerp(min, max, (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        SetChromaticIntensity(intensity);
    }

    /// <summary>
    /// Example: Fade chromatic aberration in/out over time
    /// </summary>
    public void FadeChromaticAberration(float targetIntensity, float duration)
    {
        StartCoroutine(FadeChromaticCoroutine(targetIntensity, duration));
    }

    private System.Collections.IEnumerator FadeChromaticCoroutine(float targetIntensity, float duration)
    {
        float startIntensity = chromaticIntensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            SetChromaticIntensity(Mathf.Lerp(startIntensity, targetIntensity, t));
            yield return null;
        }

        SetChromaticIntensity(targetIntensity);
    }

    /// <summary>
    /// Example: Animate through rainbow samples for a morphing effect
    /// </summary>
    public void AnimateRainbowSamples(float speed = 1f)
    {
        int samples = Mathf.RoundToInt(Mathf.Lerp(3, 12, (Mathf.Sin(Time.time * speed) + 1f) / 2f));
        SetRainbowSamples(samples);
    }
}