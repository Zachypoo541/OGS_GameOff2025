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

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupSoundVolume = 0.8f;
    [SerializeField] private Vector2 pickupSoundPitchRange = new Vector2(0.95f, 1.05f);

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

    [Header("Particle Settings (Shared)")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField] private float particleSize = 0.5f;
    [SerializeField] private int particleCount = 30;
    [SerializeField] private float particleSpeed = 5f;

    [Header("Despawn Particle Settings")]
    [SerializeField] private float particleLifetime = 1f;
    [SerializeField] private float particlePullDelay = 0.3f; // Delay before particles start moving to camera
    [SerializeField] private float particlePullSpeed = 20f; // Speed at which particles move to camera

    [Header("Spawn Effect Settings")]
    [SerializeField] private bool isSpawned = false; // Set to true if spawned by SpawnController
    [SerializeField] private float spawnScaleDuration = 0.8f;
    [SerializeField] private float spawnParticleRadius = 3f; // Initial spawn radius around object
    [SerializeField] private float spawnParticleFadeInDuration = 0.3f;

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
    private bool isScalingDown = false;
    private bool isFullySpawned = false;

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

            // If this is a spawned object, start with zero scale and play spawn effect
            if (isSpawned)
            {
                modelTransform.localScale = Vector3.zero;
                StartCoroutine(PlaySpawnEffect());
            }
            else
            {
                isFullySpawned = true;
            }
        }
        else
        {
            isFullySpawned = true;
        }
    }

    private void Update()
    {
        if (playerCamera == null)
            return;

        // Always update rotation (even when unlocked or scaling down)
        UpdateRotation();

        // Only check for interaction if not unlocked, fully spawned, and not scaling down
        if (hasBeenUnlocked || !isFullySpawned || isScalingDown)
            return;

        CheckPlayerProximityAndGaze();

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

    private IEnumerator PlaySpawnEffect()
    {
        // Create spawn particles that move inward
        CreateSpawnParticles();

        // Scale up the object
        float elapsed = 0f;
        while (elapsed < spawnScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnScaleDuration;
            // Use ease-out curve for smoother spawn
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            modelTransform.localScale = Vector3.Lerp(Vector3.zero, initialScale, easedT);
            yield return null;
        }

        modelTransform.localScale = initialScale;
        isFullySpawned = true;
    }

    private void CreateSpawnParticles()
    {
        if (transform == null)
        {
            Debug.LogError("CreateSpawnParticles: transform is null!");
            return;
        }

        // Use transform position instead of renderer bounds (which may be scaled down)
        Vector3 centerPosition = transform.position;

        // Create a temporary GameObject for the particle system
        GameObject particleObj = new GameObject("WaveformSpawnParticles");
        particleObj.transform.position = centerPosition;

        Debug.Log($"Creating spawn particles at {centerPosition} with radius {spawnParticleRadius}");

        // Add particle system component
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // STOP IT IMMEDIATELY before configuring
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Configure main module - set initial alpha to something visible
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 5f;
        main.startLifetime = particleSpeed > 0 ? spawnParticleRadius / particleSpeed : 3f;
        main.startSpeed = 0f;
        main.startSize = particleSize;
        // Start with a visible alpha instead of 0
        main.startColor = new Color(particleColor.r, particleColor.g, particleColor.b, particleColor.a);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        // Disable emission module since we'll emit manually
        var emission = ps.emission;
        emission.enabled = false;

        // Configure shape - spawn particles in a sphere around the object
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spawnParticleRadius;

        // Configure color over lifetime for fade in effect
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        float lifetime = main.startLifetime.constant;
        float fadeInRatio = Mathf.Clamp01(spawnParticleFadeInDuration / lifetime);

        // Fade from transparent to full alpha
        gradient.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(particleColor, 0f),
            new GradientColorKey(particleColor, 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(particleColor.a, fadeInRatio),
            new GradientAlphaKey(particleColor.a, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Configure renderer
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        // Simple material setup
        if (particleSprite != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = particleSprite.texture;
            renderer.material = mat;
            Debug.Log($"Using sprite texture: {particleSprite.name}");
        }
        else
        {
            // Use default particle material if no sprite
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.color = particleColor;
            renderer.material = mat;
            Debug.Log("Using default particle material (no sprite assigned)");
        }

        // Play the system first
        ps.Play(true);

        // Manually emit particles - don't override the color with EmitParams
        ps.Emit(particleCount);

        Debug.Log($"Manually emitted {particleCount} particles");

        // Verify emission immediately
        StartCoroutine(DebugAndPullParticles(ps, particleObj, centerPosition));
    }

    private IEnumerator DebugAndPullParticles(ParticleSystem ps, GameObject particleObj, Vector3 centerPosition)
    {
        // Wait one frame for particles to be created
        yield return null;

        if (ps == null)
        {
            Debug.LogError("Particle system is null!");
            if (particleObj != null)
                Destroy(particleObj);
            yield break;
        }

        int initialCount = ps.particleCount;
        Debug.Log($"Particles after emit: {initialCount}");

        if (initialCount == 0)
        {
            Debug.LogError("No particles were emitted! Something is wrong with the particle system setup.");
            if (particleObj != null)
                Destroy(particleObj);
            yield break;
        }

        // Get initial particle data
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
        int count = ps.GetParticles(particles);
        if (count > 0)
        {
            Debug.Log($"First particle - Position: {particles[0].position}, Size: {particles[0].GetCurrentSize(ps)}, Color: {particles[0].GetCurrentColor(ps)}");
        }

        // Now start pulling particles
        float maxLifetime = ps.main.startLifetime.constant;
        float elapsed = 0f;

        Debug.Log($"Starting particle pull towards {centerPosition}");

        while (elapsed < maxLifetime && ps != null)
        {
            int particleCount = ps.GetParticles(particles);

            if (particleCount == 0)
            {
                Debug.Log("All particles expired");
                break;
            }

            for (int i = 0; i < particleCount; i++)
            {
                Vector3 directionToCenter = (centerPosition - particles[i].position).normalized;
                float distanceToCenter = Vector3.Distance(particles[i].position, centerPosition);

                // Kill particle if very close to center
                if (distanceToCenter < 0.1f)
                {
                    particles[i].remainingLifetime = 0f;
                }
                else
                {
                    // Move particle towards center
                    particles[i].position += directionToCenter * particleSpeed * Time.deltaTime;
                }
            }

            ps.SetParticles(particles, particleCount);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log("Particle pull complete, cleaning up");

        // Cleanup
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        yield return null;

        if (particleObj != null)
            Destroy(particleObj);
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
        // Play pickup sound at the object's position
        if (pickupSound != null)
        {
            SoundFXManager.instance.PlaySoundFXClip(
                pickupSound,
                transform,
                pickupSoundVolume,
                pickupSoundPitchRange.x,
                pickupSoundPitchRange.y
            );
        }

        // Create and play particle burst
        CreateParticleBurst();

        // Start scale down (rotation will continue in Update)
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

        // Configure shape (sphere emitting in all directions)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Configure renderer
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Set up material
        Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (particleSprite != null)
        {
            particleMat.SetTexture("_MainTex", particleSprite.texture);
        }
        particleMat.SetColor("_Color", particleColor);
        renderer.material = particleMat;

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
        isScalingDown = true;

        // Scale down the model (rotation continues in Update)
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

        // Visualize spawn particle radius
        if (isSpawned)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, spawnParticleRadius);
        }
    }
}