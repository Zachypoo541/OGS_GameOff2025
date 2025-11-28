using UnityEngine;
using System.Collections;

public class WaveformUnlock : MonoBehaviour
{
    [Header("Info Panel Data")]
    [SerializeField] private WaveformInfoData infoData;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private float viewAngleThreshold = 0.7f; // Dot product threshold for "looking at"
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string interactionPromptText = "Press E to Synthesize Waveform";

    [Header("UI Prefabs")]
    [SerializeField] private GameObject interactionPromptPrefab;
    [SerializeField] private GameObject waveformInfoPanelPrefab;

    [Header("Hand Animation")]
    [SerializeField] private float grabAnimationDelay = 0.5f; // Delay before unlock activates after grab starts

    [Header("Visual Effects")]
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private Transform modelTransform; // The actual model to rotate/scale
    [SerializeField] private float scaleDownDuration = 0.5f;

    [Header("Rotation Settings")]
    [SerializeField] private float minRotationSpeed = 20f; // Degrees per second
    [SerializeField] private float maxRotationSpeed = 180f; // Degrees per second
    [SerializeField] private float rotationRampDistance = 5f; // Distance where rotation starts ramping up

    [Header("Particle Burst Settings")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField] private float particleSize = 0.5f;
    [SerializeField] private int particleCount = 30;
    [SerializeField] private float particleSpeed = 5f;
    [SerializeField] private float particleLifetime = 1f;
    [SerializeField] private float particlePullDelay = 0.3f; // Delay before particles start moving to camera
    [SerializeField] private float particlePullSpeed = 20f; // Speed at which particles move to camera

    [Header("Timing")]
    [SerializeField] private float delayBeforeInfoPanel = 1.5f;

    private Transform playerTransform;
    private Camera playerCamera;
    private InteractionPrompt currentPrompt;
    private bool hasBeenUnlocked = false;
    private bool isPlayerInRange = false;
    private bool isPlayerLookingAt = false;
    private Vector3 initialScale;
    private HandAnimationController handAnimController;

    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCamera = Camera.main;

            if (playerCamera == null)
                Debug.LogError("Camera.main not found! Tag your camera with 'MainCamera'");

            // Find HandAnimationController on player's Canvas
            Canvas playerCanvas = player.GetComponentInChildren<Canvas>();
            if (playerCanvas != null)
            {
                handAnimController = playerCanvas.GetComponent<HandAnimationController>();
            }
        }

        // Store initial scale
        if (modelTransform != null)
        {
            initialScale = modelTransform.localScale;
        }
    }

    private void Update()
    {
        if (hasBeenUnlocked || playerCamera == null)
            return;

        CheckPlayerProximityAndGaze();
        UpdateRotation();

        if (isPlayerInRange && isPlayerLookingAt)
        {
            ShowPrompt();

            if (Input.GetKeyDown(interactionKey))
            {
                StartCoroutine(PlayGrabAndUnlock());
            }
        }
        else
        {
            HidePrompt();
        }
    }

    private void UpdateRotation()
    {
        if (modelTransform == null || playerCamera == null)
            return;

        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);

        // Calculate rotation speed based on distance
        float rotationSpeed;
        if (distance >= rotationRampDistance)
        {
            // Outside ramp distance, use minimum speed
            rotationSpeed = minRotationSpeed;
        }
        else
        {
            // Inside ramp distance, interpolate between min and max
            float t = 1f - (distance / rotationRampDistance); // 0 at edge, 1 at center
            rotationSpeed = Mathf.Lerp(minRotationSpeed, maxRotationSpeed, t);
        }

        // Rotate around Y axis
        modelTransform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void CheckPlayerProximityAndGaze()
    {
        // Check distance from camera
        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        isPlayerInRange = distance <= interactionRange;

        if (!isPlayerInRange)
        {
            isPlayerLookingAt = false;
            return;
        }

        // Check if camera is looking at object
        Vector3 directionToObject = (transform.position - playerCamera.transform.position).normalized;
        float dotProduct = Vector3.Dot(playerCamera.transform.forward, directionToObject);
        isPlayerLookingAt = dotProduct >= viewAngleThreshold;
    }

    private void ShowPrompt()
    {
        if (currentPrompt == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null && interactionPromptPrefab != null)
            {
                GameObject promptObj = Instantiate(interactionPromptPrefab, canvas.transform);
                currentPrompt = promptObj.GetComponent<InteractionPrompt>();

                if (currentPrompt != null)
                {
                    currentPrompt.SetPromptText(interactionPromptText);
                    currentPrompt.Show();
                }
            }
        }
    }

    private void HidePrompt()
    {
        if (currentPrompt != null)
        {
            currentPrompt.Hide();
            currentPrompt = null;
        }
    }

    private IEnumerator PlayGrabAndUnlock()
    {
        // Immediately mark as unlocked to prevent multiple interactions
        hasBeenUnlocked = true;

        // Hide prompt
        HidePrompt();

        // Play grab animation if controller is assigned
        if (handAnimController != null)
        {
            handAnimController.PlayGrabAction();
        }

        // Wait for grab animation delay
        yield return new WaitForSeconds(grabAnimationDelay);

        // Now perform unlock
        Unlock();
    }

    private void Unlock()
    {
        // Create and play particle burst
        CreateParticleBurst();

        // Start scale down
        StartCoroutine(ScaleDownAndShowInfo());
    }

    private void CreateParticleBurst()
    {
        if (objectRenderer == null || playerCamera == null)
            return;

        // Create a temporary GameObject for the particle system at the object's center
        GameObject particleObj = new GameObject("WaveformBurstParticles");
        Vector3 burstPosition = objectRenderer.bounds.center;
        particleObj.transform.position = burstPosition;
        particleObj.transform.rotation = Quaternion.identity;

        // Add particle system component
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // Stop it immediately to prevent auto-play
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Configure main module
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Configure emission
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, particleCount)
        });

        // Configure shape - sphere emitting in all directions
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Configure renderer
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Set up material
        if (particleSprite != null)
        {
            Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
            particleMat.SetTexture("_MainTex", particleSprite.texture);
            particleMat.SetColor("_Color", particleColor);
            renderer.material = particleMat;
        }
        else
        {
            Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
            particleMat.SetColor("_Color", particleColor);
            renderer.material = particleMat;
        }

        // Now play the configured system
        ps.Play();

        // Start coroutine to control particle movement towards camera
        StartCoroutine(PullParticlesToCamera(ps, particleObj));
    }

    private IEnumerator PullParticlesToCamera(ParticleSystem ps, GameObject particleObj)
    {
        // Wait for pull delay
        yield return new WaitForSeconds(particlePullDelay);

        if (ps == null || playerCamera == null)
        {
            if (particleObj != null)
                Destroy(particleObj);
            yield break;
        }

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];

        // Track total time to ensure cleanup
        float totalElapsed = particlePullDelay;
        float maxTotalTime = particleLifetime;

        while (totalElapsed < maxTotalTime && ps != null)
        {
            int particleCount = ps.GetParticles(particles);

            if (particleCount == 0)
                break;

            Vector3 cameraPos = playerCamera.transform.position;

            for (int i = 0; i < particleCount; i++)
            {
                Vector3 directionToCamera = (cameraPos - particles[i].position).normalized;
                float distanceToCamera = Vector3.Distance(particles[i].position, cameraPos);

                // Kill particle if very close to camera
                if (distanceToCamera < 0.2f)
                {
                    particles[i].remainingLifetime = 0f;
                }
                else
                {
                    // Move particle towards camera
                    particles[i].position += directionToCamera * particlePullSpeed * Time.deltaTime;
                }
            }

            ps.SetParticles(particles, particleCount);

            totalElapsed += Time.deltaTime;
            yield return null;
        }

        // Force kill all remaining particles
        if (ps != null)
        {
            int remainingCount = ps.GetParticles(particles);
            for (int i = 0; i < remainingCount; i++)
            {
                particles[i].remainingLifetime = 0f;
            }
            ps.SetParticles(particles, remainingCount);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Wait a frame then cleanup
        yield return null;

        if (particleObj != null)
            Destroy(particleObj);
    }

    private IEnumerator ScaleDownAndShowInfo()
    {
        // Scale down the model
        if (modelTransform != null)
        {
            float elapsed = 0f;
            Vector3 targetScale = Vector3.zero;

            while (elapsed < scaleDownDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleDownDuration;
                modelTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);
                yield return null;
            }

            modelTransform.localScale = targetScale;
        }

        // Wait for delay before showing info panel
        yield return new WaitForSeconds(delayBeforeInfoPanel);

        // Show waveform info panel
        ShowWaveformInfo();

        // Destroy the unlock object
        Destroy(gameObject);
    }

    private void ShowWaveformInfo()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null && waveformInfoPanelPrefab != null && infoData != null)
        {
            GameObject panelObj = Instantiate(waveformInfoPanelPrefab, canvas.transform);
            WaveformInfoPanel panel = panelObj.GetComponent<WaveformInfoPanel>();

            if (panel != null)
            {
                panel.Initialize(infoData);

                // Subscribe to continue button event if you need to trigger level start
                panel.OnContinueClicked.AddListener(OnInfoPanelClosed);
            }
        }
    }

    private void OnInfoPanelClosed()
    {
        // This is where you can trigger your level start event
        Debug.Log($"Info panel closed for {infoData.waveformName}");
        // Example: LevelManager.Instance.StartLevel();
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}