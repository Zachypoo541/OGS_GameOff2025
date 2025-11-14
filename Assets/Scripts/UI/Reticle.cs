using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class WaveformReticleSprite
{
    public string waveformName; // "Sine", "Square", "Saw", "Triangle"
    public Sprite reticleSprite;
}

public class Reticle : MonoBehaviour
{
    [Header("Reticle Style")]
    [SerializeField] private Image reticleImage;
    [SerializeField] private Color reticleColor = Color.white;
    [SerializeField] private float reticleSize = 64f;

    [Header("Waveform-Specific Sprites")]
    [SerializeField] private WaveformReticleSprite[] waveformReticles;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.25f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool scaleOnTransition = true;
    [SerializeField] private float transitionScaleMultiplier = 1.2f;

    [Header("Dynamic Behavior")]
    [SerializeField] private bool scaleOnFire = true;
    [SerializeField] private float fireScaleMultiplier = 1.3f;
    [SerializeField] private float scaleReturnSpeed = 10f;

    [Header("Hit Effect - Chromatic Aberration")]
    [SerializeField] private bool chromaticAberrationOnHit = true;
    [SerializeField] private Material chromaticMaterial;
    [SerializeField] private float chromaticIntensity = 0.15f;
    [SerializeField] private float chromaticDuration = 0.3f;
    [SerializeField] private AnimationCurve chromaticFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("References")]
    [SerializeField] private PlayerCharacter player;

    private float currentScale = 1f;
    private float targetScale = 1f;
    private Material reticleMaterialInstance;
    private float chromaticTimer = 0f;
    private bool isPlayingChromaticEffect = false;
    private bool isTransitioning = false;

    // Second image for cross-fade transitions
    private Image transitionImage;
    private GameObject transitionImageObject;

    private void Start()
    {
        if (reticleImage == null)
        {
            reticleImage = GetComponent<Image>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerCharacter>();
        }

        // Create material instance for chromatic effect
        if (chromaticMaterial != null && reticleImage != null)
        {
            reticleMaterialInstance = new Material(chromaticMaterial);
            reticleImage.material = reticleMaterialInstance;
        }

        // Create transition image (hidden by default)
        CreateTransitionImage();

        UpdateReticleAppearance();

        // Set initial reticle based on equipped waveform
        if (player != null && player.equippedWaveform != null)
        {
            UpdateReticleForWaveform(player.equippedWaveform, immediate: true);
        }
    }

    private void CreateTransitionImage()
    {
        // Create a duplicate image for transitions
        transitionImageObject = new GameObject("ReticleTransition");
        transitionImageObject.transform.SetParent(transform, false);

        transitionImage = transitionImageObject.AddComponent<Image>();

        // Copy properties from main image
        RectTransform transitionRect = transitionImage.rectTransform;
        RectTransform mainRect = reticleImage.rectTransform;

        transitionRect.anchorMin = mainRect.anchorMin;
        transitionRect.anchorMax = mainRect.anchorMax;
        transitionRect.pivot = mainRect.pivot;
        transitionRect.anchoredPosition = mainRect.anchoredPosition;
        transitionRect.sizeDelta = mainRect.sizeDelta;

        // Start invisible
        Color c = reticleColor;
        c.a = 0;
        transitionImage.color = c;

        // Share the same material for chromatic effect
        if (reticleMaterialInstance != null)
        {
            transitionImage.material = reticleMaterialInstance;
        }

        // Make sure it's behind the main image initially
        transitionImageObject.transform.SetSiblingIndex(0);
    }

    private void Update()
    {
        // Handle scale animation
        if (currentScale != targetScale)
        {
            currentScale = Mathf.Lerp(currentScale, targetScale, scaleReturnSpeed * Time.deltaTime);
            if (Mathf.Abs(currentScale - targetScale) < 0.01f)
            {
                currentScale = targetScale;
            }
            UpdateReticleSize();
        }

        // Handle chromatic aberration effect
        if (isPlayingChromaticEffect)
        {
            chromaticTimer += Time.deltaTime;
            float normalizedTime = chromaticTimer / chromaticDuration;

            if (normalizedTime >= 1f)
            {
                // Effect finished
                isPlayingChromaticEffect = false;
                chromaticTimer = 0f;
                if (reticleMaterialInstance != null)
                {
                    reticleMaterialInstance.SetFloat("_ChromaticIntensity", 0f);
                }
            }
            else
            {
                // Update effect intensity based on curve
                float intensity = chromaticFalloff.Evaluate(normalizedTime) * chromaticIntensity;
                if (reticleMaterialInstance != null)
                {
                    reticleMaterialInstance.SetFloat("_ChromaticIntensity", intensity);
                }
            }
        }
    }

    private void UpdateReticleAppearance()
    {
        if (reticleImage != null)
        {
            UpdateReticleSize();
            UpdateReticleColor();
        }
    }

    private void UpdateReticleSize()
    {
        if (reticleImage != null)
        {
            float scaledSize = reticleSize * currentScale;
            reticleImage.rectTransform.sizeDelta = new Vector2(scaledSize, scaledSize);

            // Keep transition image in sync
            if (transitionImage != null)
            {
                transitionImage.rectTransform.sizeDelta = new Vector2(scaledSize, scaledSize);
            }
        }
    }

    private void UpdateReticleColor()
    {
        if (reticleImage != null)
        {
            reticleImage.color = reticleColor;
        }
    }

    /// <summary>
    /// Called when projectile is fired. Only animates if projectile was actually spawned.
    /// </summary>
    public void OnFire()
    {
        if (scaleOnFire && !isTransitioning)
        {
            targetScale = fireScaleMultiplier;
            // Reset back to normal after a moment
            Invoke(nameof(ResetScale), 0.1f);
        }
    }

    /// <summary>
    /// Called when projectile hits an enemy. Triggers chromatic aberration effect.
    /// </summary>
    public void OnHit()
    {
        if (chromaticAberrationOnHit && reticleMaterialInstance != null)
        {
            // Start chromatic aberration effect
            isPlayingChromaticEffect = true;
            chromaticTimer = 0f;
        }
    }

    /// <summary>
    /// Updates reticle sprite based on equipped waveform with smooth transition
    /// </summary>
    public void UpdateReticleForWaveform(WaveformData waveform, bool immediate = false)
    {
        if (waveform == null || reticleImage == null) return;

        // Find matching sprite for this waveform
        Sprite newSprite = null;
        foreach (var reticleConfig in waveformReticles)
        {
            if (reticleConfig.waveformName.Equals(waveform.waveformName, System.StringComparison.OrdinalIgnoreCase))
            {
                newSprite = reticleConfig.reticleSprite;
                break;
            }
        }

        if (newSprite == null)
        {
            Debug.LogWarning($"No reticle sprite found for waveform: {waveform.waveformName}");
            return;
        }

        // If same sprite, don't transition
        if (reticleImage.sprite == newSprite)
            return;

        if (immediate)
        {
            // Instant change (for initialization)
            reticleImage.sprite = newSprite;
        }
        else
        {
            // Smooth transition
            if (!isTransitioning)
            {
                StartCoroutine(TransitionToSprite(newSprite));
            }
        }
    }

    private IEnumerator TransitionToSprite(Sprite newSprite)
    {
        isTransitioning = true;

        // Setup transition image with new sprite
        transitionImage.sprite = newSprite;
        Color transitionColor = reticleColor;
        transitionColor.a = 0;
        transitionImage.color = transitionColor;

        // Bring transition image to front
        transitionImageObject.transform.SetAsLastSibling();

        float elapsed = 0f;
        Color mainColor = reticleColor;

        // Store original scale to restore after
        float originalTargetScale = targetScale;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float curveValue = transitionCurve.Evaluate(t);

            // Fade out old sprite
            mainColor.a = Mathf.Lerp(1f, 0f, curveValue);
            reticleImage.color = mainColor;

            // Fade in new sprite
            transitionColor.a = Mathf.Lerp(0f, 1f, curveValue);
            transitionImage.color = transitionColor;

            // Optional: Scale animation during transition
            if (scaleOnTransition)
            {
                // Scale up in first half, down in second half (creates a "pop" effect)
                float scaleT = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0
                currentScale = Mathf.Lerp(1f, transitionScaleMultiplier, scaleT);
                UpdateReticleSize();
            }

            yield return null;
        }

        // Swap sprites and reset
        reticleImage.sprite = newSprite;
        mainColor.a = 1f;
        reticleImage.color = mainColor;

        transitionColor.a = 0f;
        transitionImage.color = transitionColor;

        // Restore scale
        currentScale = 1f;
        targetScale = originalTargetScale;
        UpdateReticleSize();

        // Move transition image back behind
        transitionImageObject.transform.SetSiblingIndex(0);

        isTransitioning = false;
    }

    private void ResetScale()
    {
        targetScale = 1f;
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (reticleMaterialInstance != null)
        {
            Destroy(reticleMaterialInstance);
        }

        // Clean up transition image
        if (transitionImageObject != null)
        {
            Destroy(transitionImageObject);
        }
    }

    // Call this from inspector to update appearance in edit mode
    private void OnValidate()
    {
        if (reticleImage != null)
        {
            reticleImage.color = reticleColor;
            reticleImage.rectTransform.sizeDelta = new Vector2(reticleSize, reticleSize);
        }
    }
}