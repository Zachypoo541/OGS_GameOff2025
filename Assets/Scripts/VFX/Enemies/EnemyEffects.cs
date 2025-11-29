using UnityEngine;
using System.Collections;

/// <summary>
/// Handles visual effects for enemies including spawn, hit, and death effects.
/// Attach this component to enemy GameObjects alongside EnemyAI.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class EnemyEffects : MonoBehaviour
{
    [Header("Spawn Effect Settings")]
    [SerializeField] private GameObject spawnParticlePrefab;
    [SerializeField] private Sprite spawnParticleSprite;
    [SerializeField] private Color spawnParticleColor = Color.white;
    [SerializeField] private float spawnDuration = 1f;
    [SerializeField] private int spawnParticleCount = 30;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float particleSpeed = 2f;
    [SerializeField]
    [Tooltip("How close particles get to center before being destroyed")]
    private float spawnParticleDestructionRadius = 0.5f;
    [SerializeField]
    [Tooltip("Offset from enemy position where spawn particles originate")]
    private Vector3 spawnParticleOriginOffset = Vector3.zero;
    [SerializeField]
    [Tooltip("Target position offset where spawn particles converge (relative to enemy position)")]
    private Vector3 spawnParticleTargetOffset = Vector3.zero;

    [Header("Hit Effect Settings")]
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private AnimationCurve hitFlashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Death Effect Settings")]
    [SerializeField] private GameObject deathParticlePrefab;
    [SerializeField] private Sprite deathParticleSprite;
    [SerializeField] private Color deathParticleColor = Color.white;
    [SerializeField] private float deathDuration = 0.8f;
    [SerializeField] private int deathParticleCount = 40;
    [SerializeField] private float deathRadius = 4f;
    [SerializeField] private float deathParticleSpeed = 3f;
    [SerializeField]
    [Tooltip("How far from center death particles start spawning")]
    private float deathParticleStartRadius = 0.5f;
    [SerializeField]
    [Tooltip("Offset from enemy position where death particles originate")]
    private Vector3 deathParticleOriginOffset = Vector3.zero;
    [SerializeField]
    [Tooltip("How quickly enemy turns white (as % of death duration, 0.1 = 10% of duration)")]
    [Range(0.05f, 0.5f)]
    private float whiteFlashSpeed = 0.15f;
    [SerializeField]
    [Tooltip("Glow intensity during death (higher = brighter glow)")]
    [Range(0f, 5f)]
    private float deathGlowIntensity = 2f;
    [SerializeField]
    [Tooltip("Enable debug logging for death effect")]
    private bool debugDeathEffect = false;

    [Header("Material Settings")]
    [SerializeField] private Renderer enemyRenderer;

    private Material enemyMaterial;
    private Color originalColor;
    private EnemyAI enemyAI;
    private CombatEntity combatEntity;
    private bool isSpawning = false;
    private bool isDying = false;
    private bool deathAnimationComplete = false;
    private Coroutine currentHitFlash;

    // Public property to check if spawn effect is complete
    public bool IsSpawningComplete => !isSpawning;

    // Public property to check if enemy is dying
    public bool IsDying => isDying;

    // Public property to check if death animation is complete
    public bool IsDeathAnimationComplete => deathAnimationComplete;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        combatEntity = GetComponent<CombatEntity>();

        // Get renderer if not assigned
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
            if (enemyRenderer == null)
            {
                Debug.LogError($"{gameObject.name}: No Renderer found for EnemyEffects!");
                return;
            }
        }

        // Create instance of material to avoid affecting other enemies
        if (enemyRenderer != null)
        {
            enemyMaterial = enemyRenderer.material;
            originalColor = enemyMaterial.GetColor("_Color");

            // Check if material supports transparency
            if (debugDeathEffect)
            {
                Debug.Log($"{gameObject.name}: Material shader: {enemyMaterial.shader.name}");
                Debug.Log($"{gameObject.name}: Material name: {enemyMaterial.name}");
                Debug.Log($"{gameObject.name}: Original color: {originalColor}");
                Debug.Log($"{gameObject.name}: Original color alpha: {originalColor.a}");
                Debug.Log($"{gameObject.name}: Render queue: {enemyMaterial.renderQueue}");
                Debug.Log($"{gameObject.name}: Renderer enabled: {enemyRenderer.enabled}");
                Debug.Log($"{gameObject.name}: GameObject active: {gameObject.activeSelf}");

                // Check all material properties
                if (enemyMaterial.HasProperty("_TintColor"))
                {
                    Debug.Log($"{gameObject.name}: Tint Color: {enemyMaterial.GetColor("_TintColor")}");
                }
                if (enemyMaterial.HasProperty("_TintStrength"))
                {
                    Debug.Log($"{gameObject.name}: Tint Strength: {enemyMaterial.GetFloat("_TintStrength")}");
                }
            }
        }
    }

    private void OnEnable()
    {
        if (combatEntity != null)
        {
            combatEntity.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (combatEntity != null)
        {
            combatEntity.OnDeath -= HandleDeath;
        }
    }

    private void Start()
    {
        // Automatically play spawn effect on start
        PlaySpawnEffect();
    }

    /// <summary>
    /// Plays the spawn effect: particles move inward while enemy fades in
    /// </summary>
    public void PlaySpawnEffect()
    {
        if (isSpawning) return;
        StartCoroutine(SpawnEffectCoroutine());
    }

    /// <summary>
    /// Plays the hit effect: material flashes white briefly
    /// </summary>
    public void PlayHitEffect()
    {
        if (isDying) return;

        if (currentHitFlash != null)
        {
            StopCoroutine(currentHitFlash);
        }
        currentHitFlash = StartCoroutine(HitFlashCoroutine());
    }

    private void HandleDeath()
    {
        if (isDying) return;
        StartCoroutine(DeathEffectCoroutine());
    }

    private IEnumerator SpawnEffectCoroutine()
    {
        isSpawning = true;

        // Start enemy fully transparent
        SetEnemyAlpha(0f);

        // Calculate actual origin and target positions
        Vector3 effectOrigin = transform.position + spawnParticleOriginOffset;
        Vector3 targetPosition = transform.position + spawnParticleTargetOffset;

        // Spawn particles if prefab exists
        if (spawnParticlePrefab != null)
        {
            for (int i = 0; i < spawnParticleCount; i++)
            {
                // Generate random position on a sphere around the origin
                Vector3 randomDirection = Random.onUnitSphere;
                Vector3 spawnPos = effectOrigin + (randomDirection * spawnRadius);

                GameObject particle = Instantiate(spawnParticlePrefab, spawnPos, Quaternion.identity);
                SpawnParticle particleScript = particle.GetComponent<SpawnParticle>();

                if (particleScript == null)
                {
                    particleScript = particle.AddComponent<SpawnParticle>();
                }

                particleScript.Initialize(
                    targetPosition,
                    spawnParticleSprite,
                    spawnParticleColor,
                    spawnDuration,
                    particleSpeed,
                    spawnParticleDestructionRadius
                );
            }
        }

        // Fade in enemy over spawn duration
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / spawnDuration);
            SetEnemyAlpha(alpha);
            yield return null;
        }

        SetEnemyAlpha(1f);
        isSpawning = false;
    }

    private IEnumerator HitFlashCoroutine()
    {
        if (enemyMaterial == null) yield break;

        // Store current alpha to preserve transparency
        float currentAlpha = enemyMaterial.GetColor("_Color").a;
        Color startColor = originalColor;
        startColor.a = currentAlpha;

        float elapsed = 0f;

        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hitFlashDuration;
            float curveValue = hitFlashCurve.Evaluate(t);

            // Lerp from original color to white flash color, preserving alpha
            Color flashColor = Color.Lerp(startColor, hitFlashColor, curveValue);
            flashColor.a = currentAlpha; // Preserve current alpha

            enemyMaterial.SetColor("_Color", flashColor);

            yield return null;
        }

        // Reset to original color (preserving current alpha)
        Color resetColor = originalColor;
        resetColor.a = currentAlpha;
        enemyMaterial.SetColor("_Color", resetColor);

        currentHitFlash = null;
    }

    private IEnumerator DeathEffectCoroutine()
    {
        isDying = true;

        if (debugDeathEffect)
        {
            Debug.Log($"{gameObject.name}: Starting death effect. Material: {enemyMaterial.shader.name}");
        }

        // Calculate actual origin position for death particles
        Vector3 effectOrigin = transform.position + deathParticleOriginOffset;

        // Spawn death particles if prefab exists
        if (deathParticlePrefab != null)
        {
            for (int i = 0; i < deathParticleCount; i++)
            {
                // Generate random direction on a sphere for full 3D explosion effect
                Vector3 direction = Random.onUnitSphere;

                // Spawn particles offset from origin by the start radius
                Vector3 spawnPosition = effectOrigin + (direction * deathParticleStartRadius);

                GameObject particle = Instantiate(deathParticlePrefab, spawnPosition, Quaternion.identity);
                DeathParticle particleScript = particle.GetComponent<DeathParticle>();

                if (particleScript == null)
                {
                    particleScript = particle.AddComponent<DeathParticle>();
                }

                particleScript.Initialize(
                    direction,
                    deathParticleSprite,
                    deathParticleColor,
                    deathDuration,
                    deathParticleSpeed,
                    deathRadius
                );
            }
        }

        // Phase 1: Quickly fade to white with glow
        float whiteFadeDuration = deathDuration * whiteFlashSpeed;
        float elapsed = 0f;

        if (debugDeathEffect)
        {
            Debug.Log($"{gameObject.name}: Phase 1 - Fading to white over {whiteFadeDuration}s");
        }

        while (elapsed < whiteFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / whiteFadeDuration);

            // Lerp color from original to white, keep fully opaque
            Color deathColor = Color.Lerp(originalColor, Color.white, progress);
            deathColor.a = 1f;

            if (enemyMaterial != null)
            {
                enemyMaterial.SetColor("_Color", deathColor);

                // Set emission if the material supports it
                if (enemyMaterial.HasProperty("_EmissionColor"))
                {
                    enemyMaterial.SetColor("_EmissionColor", Color.white);
                }
                if (enemyMaterial.HasProperty("_EmissionStrength"))
                {
                    enemyMaterial.SetFloat("_EmissionStrength", progress * deathGlowIntensity);
                }
            }

            yield return null;
        }

        // Ensure we're fully white with full glow
        if (enemyMaterial != null)
        {
            Color whiteColor = Color.white;
            whiteColor.a = 1f;
            enemyMaterial.SetColor("_Color", whiteColor);

            if (enemyMaterial.HasProperty("_EmissionColor"))
            {
                enemyMaterial.SetColor("_EmissionColor", Color.white);
            }
            if (enemyMaterial.HasProperty("_EmissionStrength"))
            {
                enemyMaterial.SetFloat("_EmissionStrength", deathGlowIntensity);
            }

            if (debugDeathEffect)
            {
                Color currentColor = enemyMaterial.GetColor("_Color");
                Debug.Log($"{gameObject.name}: End of Phase 1 - Color: {currentColor}, Alpha: {currentColor.a}");
            }
        }

        // Phase 2: Fade out opacity while staying white and glowing
        float fadeOutDuration = deathDuration - whiteFadeDuration;
        elapsed = 0f;

        if (debugDeathEffect)
        {
            Debug.Log($"{gameObject.name}: Phase 2 - Fading out opacity over {fadeOutDuration}s");
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);

            // Fade alpha from 1 to 0, keep white
            float alpha = 1f - progress;

            if (enemyMaterial != null)
            {
                Color whiteColor = Color.white;
                whiteColor.a = alpha;
                enemyMaterial.SetColor("_Color", whiteColor);

                // Keep emission at full intensity during fade out
                if (enemyMaterial.HasProperty("_EmissionStrength"))
                {
                    enemyMaterial.SetFloat("_EmissionStrength", deathGlowIntensity);
                }

                if (debugDeathEffect && (int)(elapsed * 10) % 2 == 0) // Log every ~0.2 seconds
                {
                    Color currentColor = enemyMaterial.GetColor("_Color");
                    Debug.Log($"{gameObject.name}: Fading - Progress: {progress:F2}, Alpha: {alpha:F2}, Actual Alpha: {currentColor.a:F2}");
                    Debug.Log($"{gameObject.name}: Renderer enabled: {enemyRenderer.enabled}, GameObject active: {gameObject.activeSelf}");
                }
            }

            yield return null;
        }

        if (debugDeathEffect)
        {
            Color finalColor = enemyMaterial.GetColor("_Color");
            Debug.Log($"{gameObject.name}: Death effect complete - Final alpha: {finalColor.a}");
        }

        // Reset emission
        if (enemyMaterial != null)
        {
            if (enemyMaterial.HasProperty("_EmissionStrength"))
            {
                enemyMaterial.SetFloat("_EmissionStrength", 0f);
            }
        }

        SetEnemyAlpha(0f);

        // Mark death animation as complete
        deathAnimationComplete = true;

        if (debugDeathEffect)
        {
            Debug.Log($"{gameObject.name}: Death animation marked as complete. Safe to destroy now.");
        }
    }

    private void SetEnemyAlpha(float alpha)
    {
        if (enemyMaterial == null) return;

        Color color = originalColor;
        color.a = alpha;
        enemyMaterial.SetColor("_Color", color);
    }

    /// <summary>
    /// Call this method when the enemy takes damage to play the hit effect
    /// </summary>
    public void OnEnemyHit()
    {
        PlayHitEffect();
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (enemyMaterial != null)
        {
            Destroy(enemyMaterial);
        }
    }

    // Editor helper to preview effects
    private void OnValidate()
    {
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }
    }
}