using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages transitions between arenas including fade effects and UI
/// Attach to a GameObject in your scene (one per arena)
/// </summary>
public class ArenaTransitionManager : MonoBehaviour
{
    [Header("References (Auto-Found at Runtime)")]
    [Tooltip("Reference to the player GameObject (auto-found by tag)")]
    public GameObject player;

    [Tooltip("Reference to the main camera (auto-found in player hierarchy)")]
    public Camera mainCamera;

    [Header("UI")]
    [Tooltip("Prefab containing the Arena Completed UI Panel")]
    public GameObject arenaCompletedUIPrefab;

    [Header("Particle Overlay Settings")]
    [Tooltip("Sprite to use for the particles (use same as EnergyPickup)")]
    public Sprite overlayParticleSprite;

    [Tooltip("Color of the particles")]
    public Color overlayParticleColor = Color.white;

    [Tooltip("Size of individual particles")]
    public float particleSize = 0.5f;

    [Tooltip("Number of particles to spawn")]
    public int particleCount = 100;

    [Tooltip("Radius around player where particles swirl")]
    public float swirlRadius = 3f;

    [Tooltip("Speed at which particles orbit around player (degrees/second)")]
    public float swirlSpeed = 50f;

    [Tooltip("Height above player where particles initially spawn")]
    public float initialSpawnHeight = 10f;

    [Tooltip("How long particles take to move from spawn to orbit position")]
    public float particleDescentDuration = 1.5f;

    [Tooltip("Vertical offset variation for particles while orbiting")]
    public float heightVariation = 1f;

    // Private references
    private GameObject particleOverlay;
    private GameObject completionUIInstance;
    private Player playerScript;
    private Canvas playerCanvas;
    private Coroutine swirlCoroutine;

    private void Awake()
    {
        FindPlayerReferences();
    }

    /// <summary>
    /// Find player and camera references at runtime
    /// </summary>
    private void FindPlayerReferences()
    {
        // Find player by tag
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("ArenaTransitionManager: Could not find Player GameObject with 'Player' tag!");
                return;
            }
        }

        // Get Player script
        playerScript = player.GetComponent<Player>();
        if (playerScript == null)
        {
            Debug.LogError("ArenaTransitionManager: Player GameObject does not have Player script!");
        }

        // Find the Canvas in Player hierarchy (Player->Canvas)
        Transform canvasTransform = player.transform.Find("Canvas");
        if (canvasTransform != null)
        {
            playerCanvas = canvasTransform.GetComponent<Canvas>();
            if (playerCanvas == null)
            {
                Debug.LogError("ArenaTransitionManager: Canvas component not found on Player->Canvas!");
            }
        }
        else
        {
            Debug.LogError("ArenaTransitionManager: Could not find Canvas in Player hierarchy!");
        }

        // Find main camera in Player hierarchy (Player->Camera->Main Camera)
        if (mainCamera == null)
        {
            Transform cameraParent = player.transform.Find("Camera");
            if (cameraParent != null)
            {
                Transform mainCameraTransform = cameraParent.Find("Main Camera");
                if (mainCameraTransform != null)
                {
                    mainCamera = mainCameraTransform.GetComponent<Camera>();
                }
            }

            if (mainCamera == null)
            {
                Debug.LogError("ArenaTransitionManager: Could not find Main Camera in Player->Camera->Main Camera!");
            }
        }
    }

    /// <summary>
    /// Trigger the arena completion sequence
    /// </summary>
    public void TriggerArenaCompletion(ArenaConfiguration completedArena)
    {
        StartCoroutine(ArenaCompletionSequence(completedArena));
    }

    /// <summary>
    /// Main sequence for arena completion
    /// </summary>
    private IEnumerator ArenaCompletionSequence(ArenaConfiguration completedArena)
    {
        // Disable player input
        if (playerScript != null)
        {
            playerScript.SetInputEnabled(false);
        }

        // Enable cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Create particle overlay
        CreateSwirlParticleOverlay();

        // Wait for particles to descend
        yield return new WaitForSeconds(particleDescentDuration);

        // Show completion UI
        ShowCompletionUI(completedArena);
    }

    /// <summary>
    /// Creates a particle system that swirls particles around the player
    /// </summary>
    private void CreateSwirlParticleOverlay()
    {
        if (mainCamera == null)
        {
            Debug.LogError("ArenaTransitionManager: Cannot create particle overlay, camera is null!");
            return;
        }

        // Create a GameObject for the particle system, parented to the camera
        particleOverlay = new GameObject("Victory Particle Swirl");
        particleOverlay.transform.SetParent(mainCamera.transform.parent); // Parent to Camera object (Player->Camera)
        particleOverlay.transform.localPosition = Vector3.zero; // Local position at camera center
        particleOverlay.transform.localRotation = Quaternion.identity;

        // Add particle system component
        ParticleSystem ps = particleOverlay.AddComponent<ParticleSystem>();

        // Stop it immediately before configuring
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Configure main module
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = float.MaxValue; // Infinite lifetime
        main.startSpeed = 0f;
        main.startSize = particleSize;
        main.startColor = overlayParticleColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // Local space so it moves with camera
        main.maxParticles = particleCount * 2;

        // Configure emission
        var emission = ps.emission;
        emission.enabled = false; // We'll emit manually

        // Configure shape - not used since we're setting positions manually
        var shape = ps.shape;
        shape.enabled = false;

        // Configure renderer (same as EnergyPickup)
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        // Set up material (same as EnergyPickup)
        Material particleMat;
        if (overlayParticleSprite != null)
        {
            particleMat = new Material(Shader.Find("Sprites/Default"));
            particleMat.mainTexture = overlayParticleSprite.texture;
        }
        else
        {
            particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        }
        particleMat.SetColor("_Color", overlayParticleColor);
        renderer.material = particleMat;

        // Play and emit particles
        ps.Play();
        ps.Emit(particleCount);

        // Start the swirl animation
        swirlCoroutine = StartCoroutine(SwirlParticlesAroundPlayer(ps));
    }

    /// <summary>
    /// Animate particles to descend and then swirl around the player
    /// </summary>
    private IEnumerator SwirlParticlesAroundPlayer(ParticleSystem ps)
    {
        if (ps == null || player == null) yield break;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleCount * 2];
        int count = ps.GetParticles(particles);

        // Store initial data for each particle
        Vector3[] targetOrbitPositions = new Vector3[count];
        float[] heightOffsets = new float[count];
        float[] angleOffsets = new float[count];

        // Initialize each particle's spawn position and target orbit position
        // NOTE: All positions are now in LOCAL space (relative to player)
        for (int i = 0; i < count; i++)
        {
            // Random angle around the player
            float angle = Random.Range(0f, 360f);
            angleOffsets[i] = angle;

            // Random height offset for variation
            heightOffsets[i] = Random.Range(-heightVariation, heightVariation);

            // Start particles above (in local space)
            Vector3 spawnPos = Vector3.up * initialSpawnHeight;
            spawnPos += Random.insideUnitSphere * 2f; // Add some spread
            particles[i].position = spawnPos;

            // Calculate target orbit position (at swirl radius, in local space)
            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 orbitOffset = new Vector3(
                Mathf.Cos(angleRad) * swirlRadius,
                heightOffsets[i],
                Mathf.Sin(angleRad) * swirlRadius
            );
            targetOrbitPositions[i] = orbitOffset;
        }

        ps.SetParticles(particles, count);

        // Phase 1: Descend to orbit positions
        float descentElapsed = 0f;
        while (descentElapsed < particleDescentDuration)
        {
            count = ps.GetParticles(particles);
            descentElapsed += Time.deltaTime;
            float t = descentElapsed / particleDescentDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Ease-out curve

            for (int i = 0; i < count; i++)
            {
                // Move from spawn position towards orbit position (both in local space)
                Vector3 startPos = Vector3.up * initialSpawnHeight;
                particles[i].position = Vector3.Lerp(startPos, targetOrbitPositions[i], easedT);
            }

            ps.SetParticles(particles, count);
            yield return null;
        }

        // Phase 2: Continuous swirl around player
        float swirlElapsed = 0f;
        while (ps != null && player != null)
        {
            count = ps.GetParticles(particles);

            if (count == 0)
                break;

            // Get player's yaw (Y rotation) - but we're in local space now so we just rotate around local Y
            for (int i = 0; i < count; i++)
            {
                // Update angle based on swirl speed
                angleOffsets[i] += swirlSpeed * Time.deltaTime;

                // Calculate position orbiting in local space (player's local Y is already their yaw)
                float totalAngle = angleOffsets[i] * Mathf.Deg2Rad;

                // Calculate orbit position (in local space, automatically follows player rotation)
                Vector3 orbitOffset = new Vector3(
                    Mathf.Cos(totalAngle) * swirlRadius,
                    heightOffsets[i] + Mathf.Sin(swirlElapsed * 2f + i * 0.5f) * 0.2f, // Add bobbing
                    Mathf.Sin(totalAngle) * swirlRadius
                );

                particles[i].position = orbitOffset;
            }

            ps.SetParticles(particles, count);
            swirlElapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Show the arena completion UI
    /// </summary>
    private void ShowCompletionUI(ArenaConfiguration completedArena)
    {
        if (arenaCompletedUIPrefab == null)
        {
            Debug.LogError("ArenaTransitionManager: No UI prefab assigned!");
            return;
        }

        if (playerCanvas == null)
        {
            Debug.LogError("ArenaTransitionManager: Cannot show UI, player canvas is null!");
            return;
        }

        // Instantiate as child of player canvas
        completionUIInstance = Instantiate(arenaCompletedUIPrefab, playerCanvas.transform);

        // Ensure it renders on top
        completionUIInstance.transform.SetAsLastSibling();

        // Configure the UI
        ArenaCompletionUI uiScript = completionUIInstance.GetComponent<ArenaCompletionUI>();
        if (uiScript != null)
        {
            uiScript.Initialize(completedArena);
        }
    }

    /// <summary>
    /// Cleanup particles before scene transition
    /// </summary>
    private void CleanupParticles()
    {
        if (swirlCoroutine != null)
        {
            StopCoroutine(swirlCoroutine);
            swirlCoroutine = null;
        }

        if (particleOverlay != null)
        {
            ParticleSystem ps = particleOverlay.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            Destroy(particleOverlay);
            particleOverlay = null;
        }
    }

    /// <summary>
    /// Load the next arena scene
    /// </summary>
    public void LoadNextArena(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("ArenaTransitionManager: No scene name provided!");
            return;
        }

        CleanupParticles();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void LoadMainMenu()
    {
        CleanupParticles();
        SceneManager.LoadScene("MainMenu"); // Adjust to your main menu scene name
    }
}