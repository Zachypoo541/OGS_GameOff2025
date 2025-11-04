using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class WaveformTrailRenderer : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private Material trailMaterial;
    private WaveformData waveformData;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
    }

    public void Initialize(WaveformData waveform)
    {
        this.waveformData = waveform;

        if (trailRenderer != null && waveform != null)
        {
            // Create material instance
            if (waveform.trailMaterial != null)
            {
                trailMaterial = new Material(waveform.trailMaterial);
                trailRenderer.material = trailMaterial;
            }

            // Set trail properties
            trailRenderer.time = waveform.trailLifetime;
            trailRenderer.startWidth = waveform.trailWidth;
            trailRenderer.endWidth = waveform.trailWidth * 0.5f;

            // Set shader properties
            UpdateShaderProperties();
        }
    }

    private void UpdateShaderProperties()
    {
        if (trailMaterial == null || waveformData == null) return;

        // Set color
        trailMaterial.SetColor("_Color", waveformData.waveformColor);

        // Set wave type (0=Sine, 1=Square, 2=Sawtooth, 3=Triangle)
        float waveType = GetWaveTypeValue(waveformData.waveformName);
        trailMaterial.SetFloat("_WaveType", waveType);

        // Set other properties
        trailMaterial.SetFloat("_Frequency", 5.0f);
        trailMaterial.SetFloat("_Amplitude", 0.3f);
        trailMaterial.SetFloat("_Speed", 2.0f);
        trailMaterial.SetFloat("_Thickness", 0.1f);
        trailMaterial.SetFloat("_Glow", 2.0f);
    }

    private float GetWaveTypeValue(string waveName)
    {
        if (waveName.ToLower().Contains("sine")) return 0f;
        if (waveName.ToLower().Contains("square")) return 1f;
        if (waveName.ToLower().Contains("sawtooth") || waveName.ToLower().Contains("saw")) return 2f;
        if (waveName.ToLower().Contains("triangle")) return 3f;
        return 0f; // Default to sine
    }

    private void OnDestroy()
    {
        if (trailMaterial != null)
        {
            Destroy(trailMaterial);
        }
    }
}