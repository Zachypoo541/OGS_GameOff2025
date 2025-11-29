using UnityEngine;
using UnityEngine.UI;

public class SelfCastVignetteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Material borderMaterial;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float borderIntensity = 1.0f;
    [SerializeField] private float borderWidth = 0.1f;
    [SerializeField] private float borderSoftness = 0.05f;
    [SerializeField] private float noiseScale = 10.0f;
    [SerializeField] private float noiseSpeed = 0.5f;
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseAmount = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // State
    private Color _currentColor = Color.white;
    private float _targetAlpha = 0f;
    private float _currentAlpha = 0f;
    private bool _isActive = false;
    private Material _materialInstance;

    private void Awake()
    {
        if (borderImage == null)
        {
            Debug.LogError("[SelfCastVignetteController] Border Image is not assigned!");
            enabled = false;
            return;
        }

        // Create a material instance if we have a border material assigned
        if (borderMaterial != null)
        {
            _materialInstance = new Material(borderMaterial);
            borderImage.material = _materialInstance;

            // Initialize material properties
            UpdateMaterialProperties();
        }
        else
        {
            Debug.LogWarning("[SelfCastVignetteController] No border material assigned. Effect will be limited.");
        }

        // Start fully transparent
        SetAlpha(0f);
    }

    private void Update()
    {
        if (!_isActive && _currentAlpha <= 0f)
            return;

        // Smoothly interpolate alpha
        float fadeSpeed = (_targetAlpha > _currentAlpha) ?
            (1f / fadeInDuration) : (1f / fadeOutDuration);

        _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, fadeSpeed * Time.deltaTime);

        // Apply alpha (shader handles pulsing internally)
        SetAlpha(_currentAlpha);
    }

    public void ShowVignette(Color color)
    {
        _currentColor = color;
        _targetAlpha = 1f;
        _isActive = true;

        // Update the color in both the image and material
        if (borderImage != null)
        {
            // Set the base color (alpha will be set in SetAlpha)
            Color imageColor = _currentColor;
            imageColor.a = _currentAlpha;
            borderImage.color = imageColor;
        }

        // Update material color if using custom shader
        if (_materialInstance != null)
        {
            _materialInstance.SetColor("_Color", _currentColor);
        }

        if (enableDebugLogs)
            Debug.Log($"[SelfCastVignetteController] Showing border with color: {color}");
    }

    public void HideVignette()
    {
        _targetAlpha = 0f;
        _isActive = false;

        if (enableDebugLogs)
            Debug.Log("[SelfCastVignetteController] Hiding border");
    }

    public void HideVignetteImmediate()
    {
        _targetAlpha = 0f;
        _currentAlpha = 0f;
        _isActive = false;
        SetAlpha(0f);

        if (enableDebugLogs)
            Debug.Log("[SelfCastVignetteController] Hiding border immediately");
    }

    private void SetAlpha(float alpha)
    {
        if (borderImage != null)
        {
            Color color = _currentColor;
            color.a = Mathf.Clamp01(alpha);
            borderImage.color = color;
        }

        // Also update material intensity to fade in/out
        if (_materialInstance != null)
        {
            _materialInstance.SetFloat("_Intensity", borderIntensity * Mathf.Clamp01(alpha));
        }
    }

    private void UpdateMaterialProperties()
    {
        if (_materialInstance == null)
            return;

        _materialInstance.SetFloat("_BorderWidth", borderWidth);
        _materialInstance.SetFloat("_BorderSoftness", borderSoftness);
        _materialInstance.SetFloat("_NoiseScale", noiseScale);
        _materialInstance.SetFloat("_NoiseSpeed", noiseSpeed);
        _materialInstance.SetFloat("_GlowIntensity", glowIntensity);
        _materialInstance.SetFloat("_PulseSpeed", pulseSpeed);
        _materialInstance.SetFloat("_PulseAmount", pulseAmount);
    }

    // Allow runtime adjustment of properties
    public void SetBorderWidth(float width)
    {
        borderWidth = width;
        if (_materialInstance != null)
            _materialInstance.SetFloat("_BorderWidth", borderWidth);
    }

    public void SetNoiseSpeed(float speed)
    {
        noiseSpeed = speed;
        if (_materialInstance != null)
            _materialInstance.SetFloat("_NoiseSpeed", noiseSpeed);
    }

    public void SetGlowIntensity(float intensity)
    {
        glowIntensity = intensity;
        if (_materialInstance != null)
            _materialInstance.SetFloat("_GlowIntensity", intensity);
    }

    public bool IsVisible()
    {
        return _currentAlpha > 0.01f;
    }

    public float GetCurrentAlpha()
    {
        return _currentAlpha;
    }

    private void OnValidate()
    {
        // Update material properties when values change in inspector
        if (Application.isPlaying && _materialInstance != null)
        {
            UpdateMaterialProperties();
        }
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }
}