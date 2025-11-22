using UnityEngine;
using UnityEngine.UI;

public class AttackIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    public Image indicatorImage;
    public float fadeInDuration = 0.3f;
    public float scaleDuration = 1f;
    public float startScale = 3f;
    public float endScale = 0.1f;

    [Header("Color")]
    public Color indicatorColor = Color.red;

    private Canvas canvas;
    private RectTransform rectTransform;
    private float timer;
    private float totalDuration;
    private bool isActive;

    private void Awake()
    {
        // Create canvas if not present
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // Add CanvasScaler for consistent sizing
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
        }

        // Set up RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        // Configure canvas size
        rectTransform.sizeDelta = new Vector2(100, 100);

        // Create Image GameObject if not assigned
        if (indicatorImage == null)
        {
            GameObject imageObj = new GameObject("IndicatorImage");
            imageObj.transform.SetParent(transform);
            imageObj.transform.localPosition = Vector3.zero;
            imageObj.transform.localRotation = Quaternion.identity;

            indicatorImage = imageObj.AddComponent<Image>();
            RectTransform imgRect = imageObj.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0.5f, 0.5f);
            imgRect.anchorMax = new Vector2(0.5f, 0.5f);
            imgRect.pivot = new Vector2(0.5f, 0.5f);
            imgRect.sizeDelta = new Vector2(100, 100);
        }

        // Set initial state
        if (indicatorImage != null)
        {
            Color color = indicatorColor;
            color.a = 0;
            indicatorImage.color = color;
        }
    }

    public void StartIndicator(float duration, Sprite sprite, Color color)
    {
        indicatorColor = color;
        scaleDuration = duration;
        totalDuration = fadeInDuration + scaleDuration;

        if (indicatorImage != null && sprite != null)
        {
            indicatorImage.sprite = sprite;
            Color startColor = indicatorColor;
            startColor.a = 0;
            indicatorImage.color = startColor;
        }

        // Set initial scale
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one * startScale;
        }

        timer = 0f;
        isActive = true;
    }

    // Overload for Texture2D
    public void StartIndicator(float duration, Texture2D texture, Color color)
    {
        if (texture != null)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            StartIndicator(duration, sprite, color);
        }
        else
        {
            StartIndicator(duration, (Sprite)null, color);
        }
    }

    private void Update()
    {
        if (!isActive || indicatorImage == null) return;

        timer += Time.deltaTime;

        // Fade in phase
        if (timer <= fadeInDuration)
        {
            float fadeProgress = timer / fadeInDuration;
            Color color = indicatorColor;
            color.a = Mathf.Lerp(0, 1, fadeProgress);
            indicatorImage.color = color;
        }
        // Scale down phase
        else
        {
            float scaleProgress = (timer - fadeInDuration) / scaleDuration;
            float currentScale = Mathf.Lerp(startScale, endScale, scaleProgress);
            rectTransform.localScale = Vector3.one * currentScale;

            // Keep fully visible during scale
            Color color = indicatorColor;
            color.a = 1f;
            indicatorImage.color = color;
        }

        // Make canvas face camera
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }

        // Destroy when complete
        if (timer >= totalDuration)
        {
            Destroy(gameObject);
        }
    }

    public float GetTotalDuration()
    {
        return totalDuration;
    }

    public bool IsComplete()
    {
        return timer >= totalDuration;
    }
}