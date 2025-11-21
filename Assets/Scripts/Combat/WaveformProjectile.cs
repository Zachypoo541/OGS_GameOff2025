using UnityEngine;

public class WaveformProjectile : MonoBehaviour
{
    private float damage;
    private WaveformData waveformType;
    private CombatEntity source;
    private Vector3 direction;
    private float speed;
    private Rigidbody rb;
    private TrailRenderer trailRenderer;
    private Reticle reticle; // Reference to reticle (only for player projectiles)

    // Chain attack support
    private bool hasChainAttack;
    private float chainRange;

    public WaveformData Waveform => waveformType; // Public getter for CounterSystem
    public CombatEntity Owner => source; // Public getter for CounterSystem

    public void Initialize(float damage, WaveformData waveformType, CombatEntity source, Vector3 direction, Reticle reticle = null, bool enableChain = false, float chainRange = 0f)
    {
        this.damage = damage;
        this.waveformType = waveformType;
        this.source = source;
        this.direction = direction.normalized;
        this.speed = waveformType.projectileSpeed;
        this.reticle = reticle; // Store reticle reference (null for enemy projectiles)
        this.hasChainAttack = enableChain;
        this.chainRange = chainRange;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Set color for projectile head
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = waveformType.waveformColor;
        }

        // Setup trail renderer
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer != null && waveformType.trailMaterial != null)
        {
            trailRenderer.material = waveformType.trailMaterial;
            trailRenderer.startColor = waveformType.waveformColor;
            trailRenderer.endColor = new Color(waveformType.waveformColor.r,
                                               waveformType.waveformColor.g,
                                               waveformType.waveformColor.b,
                                               0f);
            trailRenderer.time = waveformType.trailLifetime;
            trailRenderer.startWidth = waveformType.trailWidth;
            trailRenderer.endWidth = waveformType.trailWidth * 0.5f;
        }

        // Auto-destroy after 5 seconds
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        CombatEntity target = other.GetComponent<CombatEntity>();

        if (target != null && target != source)
        {
            // Trigger reticle hit effect (only if player shot this)
            if (reticle != null)
            {
                reticle.OnHit();
            }

            target.TakeDamage(damage, waveformType, source);

            // Apply knockback
            if (waveformType.knockbackForce > 0)
            {
                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    targetRb.AddForce(direction * waveformType.knockbackForce, ForceMode.Impulse);
                }
            }

            // Trigger chain attack if enabled
            if (hasChainAttack)
            {
                TriggerChainAttack(target, transform.position);
            }

            Destroy(gameObject);
        }
    }

    private void TriggerChainAttack(CombatEntity hitEntity, Vector3 hitPosition)
    {
        // Find nearby enemies within chain range
        Collider[] nearbyColliders = Physics.OverlapSphere(hitPosition, chainRange);
        CombatEntity closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            CombatEntity entity = col.GetComponent<CombatEntity>();

            // Skip if not a valid target
            if (entity == null || entity == hitEntity || entity == source)
                continue;

            // Determine if valid target based on source type
            bool isValidTarget = false;
            if (source is PlayerCharacter)
            {
                // Player projectile chains to non-players (enemies)
                isValidTarget = !(entity is PlayerCharacter);
            }
            else
            {
                // Enemy projectile chains to player
                isValidTarget = entity is PlayerCharacter;
            }

            if (!isValidTarget)
                continue;

            float distance = Vector3.Distance(hitPosition, entity.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = entity;
            }
        }

        // Spawn chain projectile toward closest enemy
        if (closestEnemy != null && waveformType.projectilePrefab != null)
        {
            Vector3 chainDirection = (closestEnemy.transform.position - hitPosition).normalized;

            GameObject chainProj = Instantiate(waveformType.projectilePrefab, hitPosition, Quaternion.LookRotation(chainDirection));

            WaveformProjectile chainProjectile = chainProj.GetComponent<WaveformProjectile>();
            if (chainProjectile != null)
            {
                // Initialize chain projectile without further chaining (prevents infinite loops)
                chainProjectile.Initialize(damage, waveformType, source, chainDirection, null, false, 0f);
            }

            Debug.Log($"Chain attack! {waveformType.name} projectile chaining to {closestEnemy.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hasChainAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chainRange);
        }
    }
}