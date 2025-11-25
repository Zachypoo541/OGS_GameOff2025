using UnityEngine;

public class OscillatorEnemy : EnemyAI
{
    [Header("Oscillator Movement")]
    public bool useUnpredictableMovement = true; // true = erratic movement, false = teleporting
    public float changeDirectionInterval = 0.5f;
    public float moveSpeed = 3f;

    [Header("Teleport Settings (if not using unpredictable movement)")]
    public float teleportInterval = 3f;
    public float teleportRadius = 10f;
    public GameObject teleportEffectPrefab;

    [Header("Oscillator Attack")]
    public int maxRampShots = 5;
    public float timeBetweenShots = 0.4f;
    public float sequenceCooldown = 3f;

    [Header("Collision Settings")]
    public LayerMask obstacleLayerMask = ~0; // Default to all layers

    private Vector3 moveDirection;
    private float nextDirectionChangeTime;
    private float nextTeleportTime;
    private int currentRampShots;
    private float nextShotTime;
    private float nextSequenceTime;

    protected override void OnEnemyStart()
    {
        // Oscillator uses Triangle (Yellow) waveform
        // Immune to yellow, weak to red

        moveDirection = Random.onUnitSphere;
        moveDirection.y = 0;
        moveDirection.Normalize();
    }

    protected override void UpdateBehavior(float distanceToPlayer)
    {
        // Wait for spawn effect to complete before moving
        if (enemyEffects != null && !enemyEffects.IsSpawningComplete)
        {
            return;
        }

        if (useUnpredictableMovement)
        {
            HandleUnpredictableMovement(distanceToPlayer);
        }
        else
        {
            HandleTeleportMovement(distanceToPlayer);
        }

        HandleRampingAttack(distanceToPlayer);
    }

    private void HandleUnpredictableMovement(float distanceToPlayer)
    {
        // Erratic, unpredictable movement
        if (Time.time >= nextDirectionChangeTime)
        {
            // Randomly change direction
            moveDirection = Random.onUnitSphere;
            moveDirection.y = 0;
            moveDirection.Normalize();

            // Sometimes move toward or away from player
            if (Random.value < 0.3f)
            {
                Vector3 toPlayer = (player.position - transform.position).normalized;
                moveDirection = Random.value < 0.5f ? toPlayer : -toPlayer;
            }

            nextDirectionChangeTime = Time.time + changeDirectionInterval;
        }

        // Check for obstacles before moving
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
        if (CanMove(movement))
        {
            transform.position += movement;
        }
        else
        {
            // Hit obstacle, pick new random direction
            moveDirection = Random.onUnitSphere;
            moveDirection.y = 0;
            moveDirection.Normalize();
            nextDirectionChangeTime = Time.time; // Change direction immediately
        }

        // Face player
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    private void HandleTeleportMovement(float distanceToPlayer)
    {
        // Stationary with spontaneous teleports
        if (Time.time >= nextTeleportTime)
        {
            PerformTeleport();
            nextTeleportTime = Time.time + teleportInterval;
        }

        // Face player
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    private void PerformTeleport()
    {
        // Spawn effect at old position
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }

        // Calculate random position around player
        Vector2 randomCircle = Random.insideUnitCircle * teleportRadius;
        Vector3 newPosition = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Make sure new position is valid
        RaycastHit hit;
        if (Physics.Raycast(newPosition + Vector3.up * 5f, Vector3.down, out hit, 10f, obstacleLayerMask))
        {
            transform.position = hit.point + Vector3.up * 0.5f;
        }

        // Spawn effect at new position
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void HandleRampingAttack(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange) return;

        // Start new sequence
        if (currentRampShots == 0 && Time.time >= nextSequenceTime)
        {
            currentRampShots = maxRampShots;
            nextShotTime = Time.time;
        }

        // Fire ramping shots (damage increases with each shot due to Triangle waveform)
        if (currentRampShots > 0 && Time.time >= nextShotTime)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            FireProjectile(direction);

            currentRampShots--;
            nextShotTime = Time.time + timeBetweenShots;

            // If sequence complete, set cooldown
            if (currentRampShots == 0)
            {
                nextSequenceTime = Time.time + sequenceCooldown;
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