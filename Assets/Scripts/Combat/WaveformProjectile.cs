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
    private Reticle reticle;

    // Chain attack support
    private bool hasChainAttack;
    private float chainRange;

    // Homing support
    private bool hasHoming;
    private float homingStrength;
    private float homingRange;
    private CombatEntity homingTarget;

    public WaveformData Waveform => waveformType;
    public CombatEntity Owner => source;

    private void Awake()
    {
        // Ensure projectile is on the Projectile layer
        gameObject.layer = LayerMask.NameToLayer("Projectile");
    }

    public void Initialize(float damage, WaveformData waveformType, CombatEntity source, Vector3 direction, Reticle reticle = null, bool enableChain = false, float chainRange = 0f)
    {
        this.damage = damage;
        this.waveformType = waveformType;
        this.source = source;
        this.direction = direction.normalized;
        this.speed = waveformType.projectileSpeed;
        this.reticle = reticle;
        this.hasChainAttack = enableChain;
        this.chainRange = chainRange;

        // Setup homing if waveform supports it
        this.hasHoming = waveformType.enableHoming;
        this.homingStrength = waveformType.homingStrength;
        this.homingRange = waveformType.homingRange;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Set collision detection to Continuous for fast projectiles
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = direction * speed;
        }

        // Ignore collision with the source entity
        if (source != null)
        {
            Collider[] projectileColliders = GetComponents<Collider>();
            Collider[] sourceColliders = source.GetComponentsInChildren<Collider>();

            foreach (Collider projCol in projectileColliders)
            {
                foreach (Collider sourceCol in sourceColliders)
                {
                    Physics.IgnoreCollision(projCol, sourceCol);
                }
            }
        }

        // Find initial homing target if homing is enabled
        if (hasHoming)
        {
            FindHomingTarget();
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

    private void FixedUpdate()
    {
        if (hasHoming && rb != null)
        {
            ApplyHoming();
        }
    }

    private void FindHomingTarget()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, homingRange);
        CombatEntity closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            CombatEntity entity = col.GetComponent<CombatEntity>();

            if (entity == null || entity == source)
                continue;

            // Determine if valid target based on source type
            bool isValidTarget = false;
            if (source is PlayerCharacter)
            {
                // Player projectile homes to non-players (enemies)
                isValidTarget = !(entity is PlayerCharacter);
            }
            else
            {
                // Enemy projectile homes to player
                isValidTarget = entity is PlayerCharacter;
            }

            if (!isValidTarget)
                continue;

            float distance = Vector3.Distance(transform.position, entity.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = entity;
            }
        }

        homingTarget = closestEnemy;
    }

    private void ApplyHoming()
    {
        // Check if target still exists and is in range
        if (homingTarget == null || homingTarget.gameObject == null)
        {
            FindHomingTarget(); // Try to find a new target
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, homingTarget.transform.position);
        if (distanceToTarget > homingRange)
        {
            FindHomingTarget(); // Target out of range, find new one
            return;
        }

        // Calculate direction to target
        Vector3 directionToTarget = (homingTarget.transform.position - transform.position).normalized;

        // Smoothly rotate velocity towards target
        Vector3 newVelocity = Vector3.RotateTowards(
            rb.linearVelocity,
            directionToTarget * speed,
            homingStrength * Time.fixedDeltaTime,
            0f
        );

        rb.linearVelocity = newVelocity.normalized * speed;

        // Update visual rotation to match velocity
        if (rb.linearVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore UI layer
        if (other.gameObject.layer == LayerMask.NameToLayer("UI"))
            return;

        // Ignore other projectiles
        if (other.gameObject.layer == LayerMask.NameToLayer("Projectile"))
            return;

        CombatEntity target = other.GetComponent<CombatEntity>();

        if (target != null && target != source)
        {
            // Trigger reticle hit effect (only if player shot this)
            if (reticle != null)
            {
                reticle.OnHit();
            }

            // Apply damage with ramping if this waveform supports it
            float finalDamage = damage;
            if (waveformType.enableDamageRamp)
            {
                finalDamage = target.ApplyDamageRamp(damage, waveformType, source);
            }
            else
            {
                target.TakeDamage(finalDamage, waveformType, source);
            }

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

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore UI layer
        if (collision.gameObject.layer == LayerMask.NameToLayer("UI"))
            return;

        // Ignore other projectiles
        if (collision.gameObject.layer == LayerMask.NameToLayer("Projectile"))
            return;

        // Double-check we're not hitting our source
        if (source != null && collision.gameObject == source.gameObject)
        {
            return;
        }

        // Check if this is a blocking surface (not a CombatEntity)
        CombatEntity entity = collision.gameObject.GetComponent<CombatEntity>();

        if (entity == null)
        {
            // Hit a non-entity object (wall, floor, obstacle, etc.)
            // Spawn decal at impact point
            if (collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];
                ProjectileDecal.SpawnDecal(
                    contact.point,
                    contact.normal,
                    collision.gameObject,
                    waveformType.decalSprite,
                    waveformType.decalColor,
                    waveformType.decalSize,
                    waveformType.decalLifetime
                );
            }

            Destroy(gameObject);
        }
        else if (entity != source)
        {
            // Hit a different entity
            // Don't spawn decal, just destroy (damage is handled by OnTriggerEnter)
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

        if (hasHoming)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, homingRange);

            if (homingTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, homingTarget.transform.position);
            }
        }
    }
}