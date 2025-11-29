using UnityEngine;

public class PulseEnemy : EnemyAI
{
    [Header("Pulse Movement")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.5f;
    public float driftSpeed = 1f;

    [Header("Pulse Attack")]
    public float staggerDelay = 0.3f;
    public int projectilesInSequence = 3;
    public float timeBetweenSequences = 2.5f;

    [Header("Collision Settings")]
    public LayerMask obstacleLayerMask = ~0; // Default to all layers

    private float floatOffset;
    private Vector3 driftDirection;
    private float nextSequenceTime;
    private int projectilesRemaining;
    private float nextProjectileTime;
    private Vector3 startPosition;

    protected override void OnEnemyStart()
    {
        // Pulse uses Sine (Green) waveform
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
        driftDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        startPosition = transform.position;
    }

    protected override void UpdateBehavior(float distanceToPlayer)
    {
        // Wait for spawn effect to complete before moving
        if (enemyEffects != null && !enemyEffects.IsSpawningComplete)
        {
            return;
        }

        HandleFloatyMovement(distanceToPlayer);
        HandleStaggeredAttack(distanceToPlayer);
    }

    private void HandleFloatyMovement(float distanceToPlayer)
    {
        Vector3 targetPosition = transform.position;

        // Slow drift toward player
        if (distanceToPlayer > attackRange * 0.8f)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            toPlayer.y = 0; // Keep movement horizontal
            toPlayer.Normalize();
            Vector3 movement = toPlayer * driftSpeed * Time.deltaTime;
            if (CanMove(movement))
            {
                targetPosition += movement;
            }
        }

        // Slow drift in random direction
        Vector3 driftMovement = driftDirection * (driftSpeed * 0.3f) * Time.deltaTime;
        if (CanMove(driftMovement))
        {
            targetPosition += driftMovement;
        }
        else
        {
            // Hit obstacle, change drift direction
            driftDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        }

        // Add floating bobbing motion
        floatOffset += floatSpeed * Time.deltaTime;
        float yOffset = Mathf.Sin(floatOffset) * floatHeight;
        targetPosition.y = startPosition.y + yOffset;

        // Apply position
        transform.position = targetPosition;

        // Occasionally change drift direction
        if (Random.value < 0.01f)
        {
            driftDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        }

        // Look at player
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    private void HandleStaggeredAttack(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange) return;

        // Start new sequence
        if (projectilesRemaining == 0 && Time.time >= nextSequenceTime)
        {
            projectilesRemaining = projectilesInSequence;
            nextProjectileTime = Time.time;
        }

        // Fire staggered projectiles
        if (projectilesRemaining > 0 && Time.time >= nextProjectileTime)
        {
            // FIXED: Use GetAimPosition() instead of player.position directly
            Vector3 direction = (GetAimPosition() - transform.position).normalized;
            FireProjectile(direction);

            projectilesRemaining--;
            nextProjectileTime = Time.time + staggerDelay;

            // If sequence complete, set cooldown
            if (projectilesRemaining == 0)
            {
                nextSequenceTime = Time.time + timeBetweenSequences;
            }
        }
    }

    private bool CanMove(Vector3 movement)
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return true;

        // Get collision radius
        float radius = 0.5f;
        if (col is SphereCollider sphere)
            radius = sphere.radius * transform.localScale.x;
        else if (col is CapsuleCollider capsule)
            radius = capsule.radius * transform.localScale.x;
        else if (col is BoxCollider box)
            radius = Mathf.Max(box.size.x, box.size.z) * 0.5f * transform.localScale.x;

        // Check if path is clear (use slightly smaller radius to avoid getting stuck)
        float checkRadius = radius * 0.8f;
        float checkDistance = movement.magnitude + (radius * 0.2f);

        return !Physics.SphereCast(transform.position, checkRadius, movement.normalized,
                                   out RaycastHit hit, checkDistance, obstacleLayerMask);
    }
}