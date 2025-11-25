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
    private Vector3 initialDirection;

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
        this.initialDirection = direction.normalized;
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
        // Create layer mask for valid targets
        int targetLayerMask;
        if (source is PlayerCharacter)
        {
            targetLayerMask = 1 << LayerMask.NameToLayer("Enemy");
        }
        else
        {
            targetLayerMask = 1 << LayerMask.NameToLayer("Player");
        }

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, homingRange, targetLayerMask);
        CombatEntity bestTarget = null;
        float bestScore = float.MaxValue;
        Vector3 aimDirection = initialDirection;

        foreach (Collider col in nearbyColliders)
        {
            CombatEntity entity = col.GetComponent<CombatEntity>();

            if (entity == null || entity == source)
                continue;

            // Use collider bounds center instead of transform position for better targeting
            Vector3 targetCenter = col.bounds.center;
            Vector3 directionToTarget = (targetCenter - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetCenter);
            float angle = Vector3.Angle(aimDirection, directionToTarget);
            float angleWeight = 2f;
            float score = (angle * angleWeight) + distance;
            float maxAngle = 60f;

            if (angle <= maxAngle && score < bestScore)
            {
                bestScore = score;
                bestTarget = entity;
            }
        }

        homingTarget = bestTarget;
    }

    private void ApplyHoming()
    {
        // Check if target still exists and is in range
        if (homingTarget == null || homingTarget.gameObject == null)
        {
            return;
        }

        // Get the target's collider center
        Collider targetCollider = homingTarget.GetComponent<Collider>();
        Vector3 targetPosition;

        if (targetCollider != null)
        {
            // Aim for the center of the collider bounds
            targetPosition = targetCollider.bounds.center;
        }
        else
        {
            // Fallback to transform position if no collider
            targetPosition = homingTarget.transform.position;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget > homingRange)
        {
            homingTarget = null;
            return;
        }

        // Check if target is still in front of us
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 currentDirection = rb.linearVelocity.normalized;
        float angleToTarget = Vector3.Angle(currentDirection, directionToTarget);

        // If target is too far off to the side or behind us, stop homing to it
        if (angleToTarget > 90f)
        {
            homingTarget = null;
            return;
        }

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
        Debug.Log($"Projectile hit: {other.gameObject.name} on layer: {LayerMask.LayerToName(other.gameObject.layer)}");

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
            if (waveformType.enableDamageRamp)
            {
                target.ApplyDamageRamp(damage, waveformType, source);
            }
            else
            {
                target.TakeDamage(damage, waveformType, source);
                Debug.Log($"{target.name} hit by {waveformType.name} for {damage} damage");
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
        }
    }

    private void OnDrawGizmos()
    {
        if (!hasHoming || !Application.isPlaying) return;

        // Draw homing range sphere
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawWireSphere(transform.position, homingRange);

        // Draw initial direction ray (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, initialDirection * 5f);

        // Draw current velocity direction (red)
        if (rb != null && rb.linearVelocity != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 5f);
        }

        // Draw cone
        float maxAngle = 60f;
        int numRays = 12;
        Gizmos.color = Color.yellow;

        for (int i = 0; i < numRays; i++)
        {
            float angle = (i / (float)numRays) * 360f;

            Vector3 perpendicular = Vector3.Cross(initialDirection, Vector3.up);
            if (perpendicular.magnitude < 0.001f)
                perpendicular = Vector3.Cross(initialDirection, Vector3.right);
            perpendicular.Normalize();

            Vector3 rotated = Quaternion.AngleAxis(angle, initialDirection) * perpendicular;
            Vector3 coneDir = Quaternion.AngleAxis(maxAngle, rotated) * initialDirection;

            Gizmos.DrawRay(transform.position, coneDir * homingRange);
        }

        // Draw line to target (green)
        if (homingTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, homingTarget.transform.position);
            Gizmos.DrawWireSphere(homingTarget.transform.position, 1f);
        }
    }
}