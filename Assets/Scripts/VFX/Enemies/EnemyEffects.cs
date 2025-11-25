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
    [Tooltip("How quickly enemy turns white (as % of death duration, 0.1 = 10% of duration)")]
    [Range(0.05f, 0.5f)]
    private float whiteFlashSpeed = 0.15f;
    [SerializeField]
    [Tooltip("Glow intensity during death (higher = brighter glow)")]
    [Range(0f, 5f)]
    private float deathGlowIntensity = 2f;

    [Header("Material Settings")]
    [SerializeField] private Renderer enemyRenderer;

    private Material enemyMaterial;
    private Color originalColor;
    private EnemyAI enemyAI;
    private CombatEntity combatEntity;
    private bool isSpawning = false;
    private bool isDying = false;
    private Coroutine currentHitFlash;

    // Public property to check if spawn effect is complete
    public bool IsSpawningComplete => !isSpawning;

    // Public property to check if enemy is dying
    public bool IsDying => isDying;

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

        // Spawn particles if prefab exists
        if (spawnParticlePrefab != null)
        {
            for (int i = 0; i < spawnParticleCount; i++)
            {
                // Generate random position on a sphere around the enemy
                Vector3 randomDirection = Random.onUnitSphere;
                Vector3 spawnPos = transform.position + (randomDirection * spawnRadius);

                GameObject particle = Instantiate(spawnParticlePrefab, spawnPos, Quaternion.identity);
                SpawnParticle particleScript = particle.GetComponent<SpawnParticle>();

                if (particleScript == null)
                {
                    particleScript = particle.AddComponent<SpawnParticle>();
                }

                particleScript.Initialize(
                    transform.position,
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

        // Spawn death particles if prefab exists
        if (deathParticlePrefab != null)
        {
            for (int i = 0; i < deathParticleCount; i++)
            {
                // Generate random direction on a sphere for full 3D explosion effect
                Vector3 direction = Random.onUnitSphere;

                // Spawn particles offset from center by the start radius
                Vector3 spawnPosition = transform.position + (direction * deathParticleStartRadius);

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
                // Ramp up emission as we get whiter
                enemyMaterial.SetColor("_EmissionColor", Color.white);
                enemyMaterial.SetFloat("_EmissionStrength", progress * deathGlowIntensity);
            }

            yield return null;
        }

        // Ensure we're fully white with full glow
        if (enemyMaterial != null)
        {
            Color whiteColor = Color.white;
            whiteColor.a = 1f;
            enemyMaterial.SetColor("_Color", whiteColor);
            enemyMaterial.SetColor("_EmissionColor", Color.white);
            enemyMaterial.SetFloat("_EmissionStrength", deathGlowIntensity);
        }

        // Phase 2: Fade out opacity while staying white and glowing
        float fadeOutDuration = deathDuration - whiteFadeDuration;
        elapsed = 0f;

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
                enemyMaterial.SetFloat("_EmissionStrength", deathGlowIntensity);
            }

            yield return null;
        }

        // Reset emission
        if (enemyMaterial != null)
        {
            enemyMaterial.SetFloat("_EmissionStrength", 0f);
        }

        SetEnemyAlpha(0f);
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