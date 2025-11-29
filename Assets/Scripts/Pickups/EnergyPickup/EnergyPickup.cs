using UnityEngine;
using System.Collections;

public class EnergyPickup : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private float viewAngleThreshold = 0.7f; // Dot product threshold for "looking at"
    [SerializeField] private KeyCode interactionKey = KeyCode.G;
    [SerializeField] private string interactionPromptText = "Press G to Collect Energy";

    [Header("Energy Boost Settings")]
    [SerializeField] private float energyRegenBoostAmount = 5f; // Added to base regen rate
    [SerializeField] private float boostDuration = 10f; // How long the boost lasts

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupSoundVolume = 0.8f;
    [SerializeField] private Vector2 pickupSoundPitchRange = new Vector2(0.95f, 1.05f);

    [Header("UI Prefabs")]
    [SerializeField] private GameObject interactionPromptPrefab;

    [Header("Hand Animation")]
    [SerializeField] private float grabAnimationDelay = 0.5f; // Delay before pickup activates after grab starts

    [Header("Visual Effects")]
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private Transform modelTransform; // The actual model to rotate/scale
    [SerializeField] private float scaleDownDuration = 0.5f;

    [Header("Rotation Settings")]
    [SerializeField] private float minRotationSpeed = 20f; // Degrees per second
    [SerializeField] private float maxRotationSpeed = 180f; // Degrees per second
    [SerializeField] private float rotationRampDistance = 5f; // Distance where rotation starts ramping up

    [Header("Particle Settings")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Color particleColor = Color.cyan;
    [SerializeField] private float particleSize = 0.5f;
    [SerializeField] private int particleCount = 30;
    [SerializeField] private float particleSpeed = 5f;
    [SerializeField] private float particleLifetime = 1f;
    [SerializeField] private float particlePullDelay = 0.3f; // Delay before particles start moving to camera
    [SerializeField] private float particlePullSpeed = 20f; // Speed at which particles move to camera

    [Header("Spawn Effect Settings")]
    [SerializeField] private bool isSpawned = false; // Set to true if spawned by SpawnController
    [SerializeField] private float spawnScaleDuration = 0.8f;
    [SerializeField] private float spawnParticleRadius = 3f; // Initial spawn radius around object
    [SerializeField] private float spawnParticleFadeInDuration = 0.3f;

    private Transform playerTransform;
    private Camera playerCamera;
    private CombatEntity playerCombatEntity;
    private InteractionPrompt currentPrompt;
    private bool hasBeenCollected = false;
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

            // Get player's CombatEntity component - PlayerCharacter is on a child object
            PlayerCharacter playerCharacter = player.GetComponentInChildren<PlayerCharacter>();
            if (playerCharacter != null)
            {
                playerCombatEntity = playerCharacter;
            }
            else
            {
                // Fallback: try to get CombatEntity directly from children
                playerCombatEntity = player.GetComponentInChildren<CombatEntity>();
            }

            if (playerCombatEntity == null)
            {
                Debug.LogError("EnergyPickup: Player does not have PlayerCharacter or CombatEntity component in hierarchy!");
            }

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

        // Always update rotation (even when collected or scaling down)
        UpdateRotation();

        // Only check for interaction if not collected, fully spawned, and not scaling down
        if (hasBeenCollected || !isFullySpawned || isScalingDown)
            return;

        CheckPlayerProximityAndGaze();

        if (isPlayerInRange && isPlayerLookingAt)
        {
            ShowPrompt();

            if (Input.GetKeyDown(interactionKey))
            {
                StartCoroutine(PlayGrabAndCollect());
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

        Vector3 centerPosition = transform.position;

        // Create a temporary GameObject for the particle system
        GameObject particleObj = new GameObject("EnergyPickupSpawnParticles");
        particleObj.transform.position = centerPosition;

        // Add particle system component
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // STOP IT IMMEDIATELY before configuring
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Configure main module
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 5f;
        main.startLifetime = particleSpeed > 0 ? spawnParticleRadius / particleSpeed : 3f;
        main.startSpeed = 0f;
        main.startSize = particleSize;
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
        }
        else
        {
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.color = particleColor;
            renderer.material = mat;
        }

        // Play the system first
        ps.Play(true);

        // Manually emit particles
        ps.Emit(particleCount);

        // Start pulling particles to center
        StartCoroutine(PullParticlesToCenter(ps, particleObj, centerPosition));
    }

    private IEnumerator PullParticlesToCenter(ParticleSystem ps, GameObject particleObj, Vector3 centerPosition)
    {
        // Wait one frame for particles to be created
        yield return null;

        if (ps == null)
        {
            if (particleObj != null)
                Destroy(particleObj);
            yield break;
        }

        // Get initial particle data
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
        float maxLifetime = ps.main.startLifetime.constant;
        float elapsed = 0f;

        while (elapsed < maxLifetime && ps != null)
        {
            int particleCount = ps.GetParticles(particles);

            if (particleCount == 0)
                break;

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

    private IEnumerator PlayGrabAndCollect()
    {
        // Immediately mark as collected to prevent multiple interactions
        hasBeenCollected = true;

        // Hide prompt
        HidePrompt();

        // Play pickup-specific grab animation if controller is assigned
        if (handAnimController != null)
        {
            handAnimController.PlayPickupGrabAction(); // Uses the new pickup grab animation
        }

        // Wait for grab animation delay
        yield return new WaitForSeconds(grabAnimationDelay);

        // Now perform collection
        CollectPickup();
    }

    private void CollectPickup()
    {
        // Apply energy regen boost to player
        if (playerCombatEntity != null)
        {
            PlayerCharacter player = playerCombatEntity as PlayerCharacter;
            if (player != null)
            {
                player.ApplyEnergyRegenBoost(energyRegenBoostAmount, boostDuration);
            }
            else
            {
                Debug.LogWarning("EnergyPickup: playerCombatEntity is not a PlayerCharacter. Energy boost will not apply.");
            }
        }

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
        StartCoroutine(ScaleDownAndDestroy());
    }
    private void CreateParticleBurst()
    {
        if (objectRenderer == null || playerCamera == null)
            return;

        // Create a temporary GameObject for the particle system at the object's center
        GameObject particleObj = new GameObject("EnergyPickupBurstParticles");
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

    private IEnumerator ScaleDownAndDestroy()
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

        // Destroy the pickup object
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize interaction range in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Visualize spawn particle radius
        if (isSpawned)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnParticleRadius);
        }

        // Visualize rotation ramp distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rotationRampDistance);
    }
}