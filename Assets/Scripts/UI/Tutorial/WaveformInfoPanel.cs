using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class WaveformInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image borderImage;

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI waveformNameText;
    [SerializeField] private TextMeshProUGUI flavorText;
    [SerializeField] private TextMeshProUGUI shootDetailsText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI projectileTypeText;
    [SerializeField] private TextMeshProUGUI selfCastEffectText;
    [SerializeField] private TextMeshProUGUI counterEffectText;

    [Header("Button")]
    [SerializeField] private Button continueButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float initialDelay = 0.2f;

    public UnityEvent OnContinueClicked;

    private WaveformInfoData currentInfoData;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            Debug.Log("WaveformInfoPanel: Continue button listener added");
        }
        else
        {
            Debug.LogError("WaveformInfoPanel: Continue button not assigned!");
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);

        // Re-enable player control if panel is destroyed without clicking continue
        EnablePlayerControl();
    }

    private void DisablePlayerControl()
    {
        // Pause the game
        Time.timeScale = 0f;

        // Show and unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player movement script if it exists
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerScript = player.GetComponent<Player>();
            if (playerScript != null)
                playerScript.enabled = false;
        }
    }

    private void EnablePlayerControl()
    {
        // Resume the game
        Time.timeScale = 1f;

        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable player movement script
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerScript = player.GetComponent<Player>();
            if (playerScript != null)
                playerScript.enabled = true;
        }
    }

    public void Initialize(WaveformInfoData data)
    {
        if (data == null)
        {
            Debug.LogError("WaveformInfoData is null!");
            return;
        }

        // Store reference for later use
        currentInfoData = data;

        // Ensure EventSystem exists
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("WaveformInfoPanel: Created EventSystem");
        }

        // Ensure Canvas has GraphicRaycaster
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("WaveformInfoPanel: Added GraphicRaycaster to Canvas");
        }

        // Set all text fields
        if (waveformNameText != null)
            waveformNameText.text = data.waveformName;

        if (flavorText != null)
            flavorText.text = data.flavorText;

        if (shootDetailsText != null)
            shootDetailsText.text = data.shootDetailsText;

        if (damageText != null)
            damageText.text = $"Damage: {data.GetDamageString()}";

        if (projectileTypeText != null)
            projectileTypeText.text = data.GetProjectileTypeString();

        if (selfCastEffectText != null)
            selfCastEffectText.text = data.selfCastEffect;

        if (counterEffectText != null)
            counterEffectText.text = data.counterEffect;

        // Set border color with transparency
        if (borderImage != null)
        {
            Color borderColor = data.waveformColor;
            borderColor.a = 0.3f;
            borderImage.color = borderColor;
        }

        // Disable player control and show cursor
        DisablePlayerControl();

        // Start fade in animation
        StartCoroutine(FadeInSequence());
    }

    private IEnumerator FadeInSequence()
    {
        // Initial delay (unscaled)
        yield return new WaitForSecondsRealtime(initialDelay);

        // Fade in (unscaled)
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void OnContinueButtonClicked()
    {
        Debug.Log("WaveformInfoPanel: Continue button clicked!");

        // Unlock the waveform
        UnlockWaveform();

        // Re-enable player control
        EnablePlayerControl();

        OnContinueClicked?.Invoke();
        StartCoroutine(FadeOutAndDestroy());
    }

    private void UnlockWaveform()
    {
        if (currentInfoData == null || currentInfoData.waveformToUnlock == null)
        {
            Debug.LogWarning("WaveformInfoPanel: No waveform to unlock!");
            return;
        }

        // Find PlayerCombat on Character child of Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Look for Character child
            Transform characterTransform = player.transform.Find("Character");
            if (characterTransform != null)
            {
                PlayerCombat playerCombat = characterTransform.GetComponent<PlayerCombat>();
                if (playerCombat != null)
                {
                    bool isFirstWaveform = playerCombat.unlockedWaveforms.Count == 0;

                    playerCombat.UnlockWaveform(currentInfoData.waveformToUnlock);
                    Debug.Log($"WaveformInfoPanel: Unlocked {currentInfoData.waveformToUnlock.waveformName}");

                    // Automatically equip the newly unlocked waveform
                    playerCombat.EquipWaveformByName(currentInfoData.waveformToUnlock.waveformName);

                    // Show right hand if this is the first waveform
                    if (isFirstWaveform)
                    {
                        // Find HandAnimationController on player's Canvas
                        Canvas playerCanvas = player.GetComponentInChildren<Canvas>();
                        if (playerCanvas != null)
                        {
                            HandAnimationController handAnimController = playerCanvas.GetComponent<HandAnimationController>();
                            if (handAnimController != null)
                            {
                                ShowRightHand(handAnimController);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("WaveformInfoPanel: PlayerCombat component not found on Character!");
                }
            }
            else
            {
                Debug.LogError("WaveformInfoPanel: Character child not found on Player!");
            }
        }
        else
        {
            Debug.LogError("WaveformInfoPanel: Player GameObject not found!");
        }

        // Start the first wave
        StartFirstWave();
    }

    private void StartFirstWave()
    {
        // Find Managers GameObject
        GameObject manager = GameObject.Find("Managers");
        if (manager != null)
        {
            // Find WaveManager child
            Transform waveManagerTransform = manager.transform.Find("WaveManager");
            if (waveManagerTransform != null)
            {
                SpawnController spawnController = waveManagerTransform.GetComponent<SpawnController>();
                if (spawnController != null)
                {
                    int waveIndex = currentInfoData != null ? currentInfoData.waveToStart : 1;
                    spawnController.StartWave(waveIndex);
                    Debug.Log($"WaveformInfoPanel: Started wave {waveIndex}");
                }
                else
                {
                    Debug.LogError("WaveformInfoPanel: SpawnController component not found on WaveManager!");
                }
            }
            else
            {
                Debug.LogError("WaveformInfoPanel: WaveManager child not found on Managers!");
            }
        }
        else
        {
            Debug.LogError("WaveformInfoPanel: Managers GameObject not found!");
        }
    }

    private void ShowRightHand(HandAnimationController handAnimController)
    {
        // Access the right hand video player through reflection or public field
        // Assuming rightHandPlayer is serialized and accessible
        var rightHandPlayerField = handAnimController.GetType().GetField("rightHandPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (rightHandPlayerField != null)
        {
            UnityEngine.Video.VideoPlayer rightHandPlayer = rightHandPlayerField.GetValue(handAnimController) as UnityEngine.Video.VideoPlayer;

            if (rightHandPlayer != null)
            {
                var canvasGroup = rightHandPlayer.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    Debug.Log("WaveformInfoPanel: Right hand shown");
                }
            }
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        // Disable button to prevent multiple clicks
        if (continueButton != null)
            continueButton.interactable = false;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    public void ForceClose()
    {
        StartCoroutine(FadeOutAndDestroy());
    }
}