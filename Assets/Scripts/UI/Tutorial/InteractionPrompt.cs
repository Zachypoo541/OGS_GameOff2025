using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InteractionPrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    public void Show()
    {
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        gameObject.SetActive(true);
        currentFadeCoroutine = StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    public void SetPromptText(string text)
    {
        if (promptText != null)
            promptText.text = text;
    }
}