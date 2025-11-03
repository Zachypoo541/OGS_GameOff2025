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

        // Move in current direction
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

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
        if (Physics.Raycast(newPosition + Vector3.up * 5f, Vector3.down, out hit, 10f))
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
}