using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen chromatic RGB edge effect using a RawImage and a self-created material.
/// Guaranteed to avoid UI tinting, missing textures, pink screens, etc.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ChromaticScreenEdgeUI : MonoBehaviour
{
    private RawImage rawImage;
    private Material runtimeMaterial;

    private const string SHADER_NAME = "UI/ChromaticScreenEdge_RawImage";

    [Header("Chromatic Aberration")]
    [Range(0f, 0.2f)] public float chromaticSpread = 0.05f;
    [Range(0f, 5f)] public float chromaticIntensity = 2.0f;
    [Range(0f, 0.5f)] public float edgeThickness = 0.15f;

    [Header("RGB Line Colors")]
    public Color redColor = Color.red;
    public Color greenColor = Color.green;
    public Color blueColor = Color.blue;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();

        // Must assign a texture or UI will overwrite material
        rawImage.texture = Texture2D.whiteTexture;

        // Create material
        Shader shader = Shader.Find(SHADER_NAME);
        runtimeMaterial = new Material(shader);
        runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;

        // Force RawImage to use our material
        rawImage.material = runtimeMaterial;
        rawImage.SetMaterialDirty();
        rawImage.SetVerticesDirty();

        // Stretch to screen
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        UpdateShaderProperties();

        Debug.Log("ChromaticScreenEdgeUI: Material auto-created and assigned.");
    }

    private void Update()
    {
        if (runtimeMaterial != null)
            UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        runtimeMaterial.SetFloat("_ChromaticSpread", chromaticSpread);
        runtimeMaterial.SetFloat("_ChromaticIntensity", chromaticIntensity);
        runtimeMaterial.SetFloat("_EdgeThickness", edgeThickness);

        runtimeMaterial.SetColor("_RedColor", redColor);
        runtimeMaterial.SetColor("_GreenColor", greenColor);
        runtimeMaterial.SetColor("_BlueColor", blueColor);
    }

    public void SetChromaticSpread(float v) => chromaticSpread = Mathf.Clamp(v, 0f, 0.2f);
    public void SetChromaticIntensity(float v) => chromaticIntensity = Mathf.Clamp(v, 0f, 5f);
    public void SetEdgeThickness(float v) => edgeThickness = Mathf.Clamp(v, 0f, 0.5f);

    public void SetVisible(bool vis)
    {
        rawImage.enabled = vis;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
