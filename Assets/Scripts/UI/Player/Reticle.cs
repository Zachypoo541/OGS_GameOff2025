using UnityEngine;
using UnityEngine.UI;

public class Reticle : MonoBehaviour
{
    [Header("Reticle Elements")]
    [SerializeField] private Image reticleImage;
    [SerializeField] private Image shieldImage; // Assign your shield PNG here in Inspector

    [Header("Reticle Settings")]
    [SerializeField] private float defaultSize = 20f;
    [SerializeField] private float expandedSize = 30f;
    [SerializeField] private float expandSpeed = 10f;
    [SerializeField] private float shrinkSpeed = 15f;

    [Header("Shield Settings")]
    [SerializeField] private float shieldFadeInSpeed = 8f;
    [SerializeField] private float shieldFadeOutSpeed = 5f;
    [SerializeField] private float shieldSuccessFlashDuration = 0.2f;
    [SerializeField] private Color shieldDefaultColor = Color.white;

    [Header("Waveform Reticle Sprites")]
    [SerializeField] private Sprite sineReticleSprite;
    [SerializeField] private Sprite squareReticleSprite;
    [SerializeField] private Sprite sawReticleSprite;
    [SerializeField] private Sprite triangleReticleSprite;

    [Header("Color Settings")]
    [SerializeField] private Color reticleColor = Color.white;

    private float _targetSize;
    private float _currentSize;

    // Shield state
    private float _shieldAlpha = 0f;
    private float _targetShieldAlpha = 0f;
    private Color _shieldColor;
    private bool _isFlashingSuccess = false;
    private float _successFlashTimer = 0f;

    private void Start()
    {
        _targetSize = defaultSize;
        _currentSize = defaultSize;
        _shieldColor = shieldDefaultColor;

        if (reticleImage != null)
        {
            reticleImage.color = reticleColor; // Always white
        }

        if (shieldImage != null)
        {
            shieldImage.color = new Color(_shieldColor.r, _shieldColor.g, _shieldColor.b, 0f);
        }
    }

    private void Update()
    {
        UpdateReticleSize();
        UpdateShieldVisual();
    }

    private void UpdateReticleSize()
    {
        // Smoothly interpolate to target size
        float speed = _currentSize < _targetSize ? expandSpeed : shrinkSpeed;
        _currentSize = Mathf.Lerp(_currentSize, _targetSize, Time.deltaTime * speed);

        if (reticleImage != null)
        {
            reticleImage.rectTransform.sizeDelta = new Vector2(_currentSize, _currentSize);
        }
    }

    private void UpdateShieldVisual()
    {
        if (shieldImage == null) return;

        // Handle success flash
        if (_isFlashingSuccess)
        {
            _successFlashTimer -= Time.deltaTime;
            if (_successFlashTimer <= 0f)
            {
                _isFlashingSuccess = false;
                // Start fading out after flash
                _targetShieldAlpha = 0f;
            }
        }

        // Smoothly interpolate shield alpha
        float fadeSpeed = _shieldAlpha < _targetShieldAlpha ? shieldFadeInSpeed : shieldFadeOutSpeed;
        _shieldAlpha = Mathf.Lerp(_shieldAlpha, _targetShieldAlpha, Time.deltaTime * fadeSpeed);

        // Apply alpha to shield (keep shield color separate from reticle color)
        shieldImage.color = new Color(_shieldColor.r, _shieldColor.g, _shieldColor.b, _shieldAlpha);
    }

    #region Public Methods

    public void OnFire()
    {
        _targetSize = expandedSize;
    }

    public void OnFireEnd()
    {
        _targetSize = defaultSize;
    }

    /// <summary>
    /// Called when a projectile hits something (for visual feedback)
    /// </summary>
    public void OnHit()
    {
        // Brief expand on hit
        OnFire();
    }

    public void UpdateReticleForWaveform(WaveformData waveform)
    {
        if (waveform == null || reticleImage == null)
            return;

        // Update reticle sprite based on waveform type (but keep color white)
        string waveformName = waveform.waveformName.ToLower();

        if (waveformName.Contains("sine"))
        {
            reticleImage.sprite = sineReticleSprite;
        }
        else if (waveformName.Contains("square"))
        {
            reticleImage.sprite = squareReticleSprite;
        }
        else if (waveformName.Contains("saw"))
        {
            reticleImage.sprite = sawReticleSprite;
        }
        else if (waveformName.Contains("triangle"))
        {
            reticleImage.sprite = triangleReticleSprite;
        }

        // Always keep reticle white
        reticleImage.color = reticleColor;
    }

    public void SetReticleColor(Color color)
    {
        reticleColor = color;
        if (reticleImage != null)
        {
            reticleImage.color = color;
        }
    }

    #endregion

    #region Shield/Parry Methods

    /// <summary>
    /// Called when the parry window opens
    /// </summary>
    public void OnParryWindowStart()
    {
        _targetShieldAlpha = 1f;
        _shieldColor = shieldDefaultColor;
        _isFlashingSuccess = false;
    }

    /// <summary>
    /// Called when the parry window closes (without a successful parry)
    /// </summary>
    public void OnParryWindowEnd()
    {
        _targetShieldAlpha = 0f;
        _isFlashingSuccess = false;
    }

    /// <summary>
    /// Called when a parry is successful - flashes shield with attack waveform color
    /// </summary>
    public void OnParrySuccess(WaveformData attackWaveform)
    {

        if (attackWaveform != null && shieldImage != null)
        {
            // Change SHIELD color to the countered attack's color
            _shieldColor = attackWaveform.waveformColor;
            _shieldAlpha = 1f; // Set to full immediately

            // Apply the attack color to the shield only
            shieldImage.color = new Color(_shieldColor.r, _shieldColor.g, _shieldColor.b, _shieldAlpha);

            // Start flash timer
            _isFlashingSuccess = true;
            _successFlashTimer = shieldSuccessFlashDuration;
        }
    }

    #endregion
}