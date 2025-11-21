using UnityEngine;

[RequireComponent(typeof(MusicStemManager))]
public class ChromaticScreenEdgeDriver : MonoBehaviour
{
    [Header("References")]
    public ChromaticScreenEdgeUI edgeUI;

    [Header("Audio Settings")]
    [Range(64, 8192)] public int sampleSize = 1024;

    [Header("Bass → Spread")]
    public bool useBassReactivity = true;
    public float bassMinSpread = 10f;
    public float bassMaxSpread = 80f;
    public float bassSensitivity = 200f;

    [Header("Mid → Base Intensity")]
    public bool useMidReactivity = true;
    public float midMinIntensity = 40f;
    public float midMaxIntensity = 100f;
    public float midSensitivity = 150f;

    [Header("High → Intensity Boost")]
    public bool useHighReactivity = true;
    public float highIntensityBoost = 30f;
    public float highSensitivity = 120f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.85f;
    [Range(0f, 1f)] public float parameterSmoothing = 0.3f;

    [Header("Auto Show")]
    public bool showWithMusic = true;

    private MusicStemManager stemManager;

    private float[] bassSpectrum;
    private float[] midSpectrum;
    private float[] highSpectrum;

    private float smoothBass = 0;
    private float smoothMid = 0;
    private float smoothHigh = 0;

    private float currentSpread = 0.05f;
    private float currentIntensity = 2f;

    private void Start()
    {
        stemManager = GetComponent<MusicStemManager>();

        if (edgeUI == null)
        {
            Debug.LogError("ChromaticScreenEdgeDriver: Assign ChromaticScreenEdgeUI RawImage component.");
            enabled = false;
            return;
        }

        bassSpectrum = new float[sampleSize];
        midSpectrum = new float[sampleSize];
        highSpectrum = new float[sampleSize];
    }

    private void Update()
    {
        if (!stemManager.IsPlaying())
        {
            if (showWithMusic)
                edgeUI.SetVisible(false);
            return;
        }

        if (showWithMusic)
            edgeUI.SetVisible(true);

        var bass = stemManager.GetBassAnalysisSource();
        var mid = stemManager.GetMidAnalysisSource();
        var high = stemManager.GetHighAnalysisSource();

        // Bass → Spread
        if (useBassReactivity && bass != null)
        {
            bass.GetSpectrumData(bassSpectrum, 0, FFTWindow.BlackmanHarris);
            float sum = Average(bassSpectrum) * bassSensitivity;
            smoothBass = Mathf.Lerp(smoothBass, sum, 1f - smoothing);

            float sMin = bassMinSpread * 0.002f;
            float sMax = bassMaxSpread * 0.002f;
            float target = Mathf.Lerp(sMin, sMax, Mathf.Clamp01(smoothBass));

            currentSpread = Mathf.Lerp(currentSpread, target, 1f - parameterSmoothing);
            edgeUI.SetChromaticSpread(currentSpread);
        }

        // Mid → Base Intensity
        float baseIntensity = 0;
        if (useMidReactivity && mid != null)
        {
            mid.GetSpectrumData(midSpectrum, 0, FFTWindow.BlackmanHarris);
            float sum = Average(midSpectrum) * midSensitivity;
            smoothMid = Mathf.Lerp(smoothMid, sum, 1f - smoothing);

            float iMin = midMinIntensity * 0.05f;
            float iMax = midMaxIntensity * 0.05f;
            baseIntensity = Mathf.Lerp(iMin, iMax, Mathf.Clamp01(smoothMid));
        }

        // High → Additive Boost
        float boost = 0;
        if (useHighReactivity && high != null)
        {
            high.GetSpectrumData(highSpectrum, 0, FFTWindow.BlackmanHarris);
            float sum = Average(highSpectrum) * highSensitivity;
            smoothHigh = Mathf.Lerp(smoothHigh, sum, 1f - smoothing);

            boost = (highIntensityBoost * 0.05f) * Mathf.Clamp01(smoothHigh);
        }

        currentIntensity = Mathf.Lerp(currentIntensity, baseIntensity + boost, 1f - parameterSmoothing);
        edgeUI.SetChromaticIntensity(currentIntensity);
    }

    private float Average(float[] data)
    {
        float sum = 0;
        foreach (float f in data) sum += f;
        return sum / data.Length;
    }
}
