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
        HandleFloatyMovement(distanceToPlayer);
        HandleStaggeredAttack(distanceToPlayer);
    }

    private void HandleFloatyMovement(float distanceToPlayer)
    {
        // Slow drift toward player
        if (distanceToPlayer > attackRange * 0.8f)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            transform.position += toPlayer * driftSpeed * Time.deltaTime;
        }

        // Add floating bobbing motion
        floatOffset += floatSpeed * Time.deltaTime;
        float yOffset = Mathf.Sin(floatOffset) * floatHeight;
        Vector3 targetPos = transform.position;
        targetPos.y = startPosition.y + yOffset;
        transform.position = new Vector3(transform.position.x, targetPos.y, transform.position.z);

        // Slow drift in random direction
        transform.position += driftDirection * (driftSpeed * 0.3f) * Time.deltaTime;

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
            Vector3 direction = (player.position - transform.position).normalized;
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
}