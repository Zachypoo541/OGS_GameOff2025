using UnityEngine;
using UnityEngine.UI;

public class WaveformUI : MonoBehaviour
{
    [Header("References")]
    public RawImage waveformDisplay;
    public Material waveformMaterial;

    [Header("Wave Settings")]
    public WaveformType waveType = WaveformType.Sine;
    public Color waveColor = Color.green;
    public float frequency = 8.0f;
    public float amplitude = 0.35f;
    public float animationSpeed = 1.5f;

    private Material instanceMaterial;

    public enum WaveformType
    {
        Sine = 0,
        Square = 1,
        Sawtooth = 2,
        Triangle = 3
    }

    private void Start()
    {
        SetupMaterial();
    }

    private void SetupMaterial()
    {
        if (waveformDisplay == null || waveformMaterial == null)
        {
            return;
        }

        // Create material instance
        instanceMaterial = new Material(waveformMaterial);
        waveformDisplay.material = instanceMaterial;

        // Set base properties
        instanceMaterial.SetFloat("_Frequency", frequency);
        instanceMaterial.SetFloat("_Amplitude", amplitude);
        instanceMaterial.SetFloat("_Thickness", 0.06f);
        instanceMaterial.SetFloat("_Speed", animationSpeed);
        instanceMaterial.SetFloat("_Glow", 2.5f);

        UpdateMaterial();
    }

    public void SetWaveform(WaveformData waveformData)
    {
        if (waveformData == null) return;

        waveColor = waveformData.waveformColor;
        waveType = GetWaveTypeFromName(waveformData.waveformName);

        if (instanceMaterial == null)
        {
            SetupMaterial();
        }

        UpdateMaterial();
    }

    private WaveformType GetWaveTypeFromName(string name)
    {
        string lowerName = name.ToLower();

        if (lowerName.Contains("sine")) return WaveformType.Sine;
        if (lowerName.Contains("square")) return WaveformType.Square;
        if (lowerName.Contains("sawtooth") || lowerName.Contains("saw")) return WaveformType.Sawtooth;
        if (lowerName.Contains("triangle")) return WaveformType.Triangle;

        return WaveformType.Sine;
    }

    private void UpdateMaterial()
    {
        if (instanceMaterial == null) return;

        instanceMaterial.SetColor("_Color", waveColor);
        instanceMaterial.SetFloat("_WaveType", (float)waveType);

        // Force UI to refresh
        if (waveformDisplay != null)
        {
            waveformDisplay.enabled = false;
            waveformDisplay.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }
}