using UnityEngine;

public class StrikerEnemy : EnemyAI
{
    [Header("Striker Movement")]
    public float normalSpeed = 4f;
    public float rushSpeed = 15f;
    public float rushDistance = 10f;
    public float rushCooldown = 2f;
    public float flyHeight = 0f; // Offset from player height
    public float heightAdjustSpeed = 5f; // How fast it adjusts to target height

    [Header("Striker Attack")]
    public float lungeDamage = 30f;
    public float lungeRange = 2f;

    [Header("Collision Settings")]
    public LayerMask obstacleLayerMask = ~0; // Default to all layers
    public float obstacleAvoidanceAngle = 45f; // Degrees to try turning when blocked

    private bool isRushing;
    private Vector3 rushDirection;
    private float rushTimer;
    private float nextRushTime;
    private float stuckTimer;
    private Vector3 lastPosition;

    protected override void OnEnemyStart()
    {
        // Striker uses Sawtooth (Red) waveform
        // Immune to red, weak to blue knockback control

        // Remove Rigidbody requirement since we're using transform-based movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        lastPosition = transform.position;
    }

    protected override void UpdateBehavior(float distanceToPlayer)
    {
        // Wait for spawn effect to complete before moving
        if (enemyEffects != null && !enemyEffects.IsSpawningComplete)
        {
            return;
        }

        HandleAggressiveMovement(distanceToPlayer);
        HandleLungeAttack(distanceToPlayer);
        CheckIfStuck();
    }

    private void HandleAggressiveMovement(float distanceToPlayer)
    {
        if (isRushing)
        {
            // Continue rushing in direction
            Vector3 movement = rushDirection * rushSpeed * Time.deltaTime;

            if (CanMove(movement))
            {
                transform.position += movement;
            }
            else
            {
                // Try to slide along obstacle during rush
                Vector3 alternateMovement = TryAvoidObstacle(movement);
                if (alternateMovement != Vector3.zero && CanMove(alternateMovement))
                {
                    transform.position += alternateMovement;
                }
                else
                {
                    // Completely blocked, stop rushing
                    isRushing = false;
                }
            }

            rushTimer -= Time.deltaTime;
            if (rushTimer <= 0)
            {
                isRushing = false;
            }
        }
        else
        {
            // Normal aggressive movement toward player
            if (distanceToPlayer > lungeRange)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                Vector3 movement = direction * normalSpeed * Time.deltaTime;

                if (CanMove(movement))
                {
                    transform.position += movement;
                }
                else
                {
                    // Try alternative paths around obstacle
                    Vector3 alternateMovement = TryAvoidObstacle(movement);
                    if (alternateMovement != Vector3.zero && CanMove(alternateMovement))
                    {
                        transform.position += alternateMovement;
                    }
                }

                // Face player
                Vector3 lookPos = player.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);
            }

            // Adjust height to maintain flying altitude
            AdjustFlyingHeight();

            // Start rush if in range and cooldown ready
            if (distanceToPlayer <= rushDistance && distanceToPlayer > lungeRange && Time.time >= nextRushTime)
            {
                StartRush();
            }
        }
    }

    private void AdjustFlyingHeight()
    {
        // Match player's Y position with optional offset
        float targetY = player.position.y + flyHeight;

        // Smoothly adjust to target height
        float currentY = transform.position.y;
        float newY = Mathf.Lerp(currentY, targetY, heightAdjustSpeed * Time.deltaTime);

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void StartRush()
    {
        isRushing = true;
        rushDirection = (player.position - transform.position).normalized;
        rushTimer = 0.5f; // Rush duration
        nextRushTime = Time.time + rushCooldown;

        // Face rush direction
        Vector3 lookPos = transform.position + rushDirection;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    private void HandleLungeAttack(float distanceToPlayer)
    {
        if (distanceToPlayer <= lungeRange && !isRushing)
        {
            // Fire high-damage projectile at close range
            Vector3 direction = (player.position - transform.position).normalized;
            FireProjectile(direction);
        }
    }

    private Vector3 TryAvoidObstacle(Vector3 blockedMovement)
    {
        // Try turning left and right to find a clear path
        Vector3 direction = blockedMovement.normalized;
        float magnitude = blockedMovement.magnitude;

        // Try right turn
        Vector3 rightDirection = Quaternion.Euler(0, obstacleAvoidanceAngle, 0) * direction;
        Vector3 rightMovement = rightDirection * magnitude;
        if (CanMove(rightMovement))
        {
            return rightMovement;
        }

        // Try left turn
        Vector3 leftDirection = Quaternion.Euler(0, -obstacleAvoidanceAngle, 0) * direction;
        Vector3 leftMovement = leftDirection * magnitude;
        if (CanMove(leftMovement))
        {
            return leftMovement;
        }

        // Try more extreme angles
        rightDirection = Quaternion.Euler(0, obstacleAvoidanceAngle * 2, 0) * direction;
        rightMovement = rightDirection * magnitude;
        if (CanMove(rightMovement))
        {
            return rightMovement;
        }

        leftDirection = Quaternion.Euler(0, -obstacleAvoidanceAngle * 2, 0) * direction;
        leftMovement = leftDirection * magnitude;
        if (CanMove(leftMovement))
        {
            return leftMovement;
        }

        // Try moving up or down
        Vector3 upMovement = direction + Vector3.up * 0.5f;
        upMovement.Normalize();
        upMovement *= magnitude;
        if (CanMove(upMovement))
        {
            return upMovement;
        }

        Vector3 downMovement = direction + Vector3.down * 0.5f;
        downMovement.Normalize();
        downMovement *= magnitude;
        if (CanMove(downMovement))
        {
            return downMovement;
        }

        return Vector3.zero; // No clear path found
    }

    private void CheckIfStuck()
    {
        // Check if enemy hasn't moved much
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < 0.01f)
        {
            stuckTimer += Time.deltaTime;

            // If stuck for too long, teleport slightly away from obstacle
            if (stuckTimer > 1f)
            {
                Vector3 randomOffset = Random.onUnitSphere * 2f;
                randomOffset.y *= 0.5f; // Less vertical offset

                Vector3 newPosition = transform.position + randomOffset;

                // Make sure new position doesn't collide
                if (CanMove(newPosition - transform.position))
                {
                    transform.position = newPosition;
                }

                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
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