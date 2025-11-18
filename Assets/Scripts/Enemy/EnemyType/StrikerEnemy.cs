using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StrikerEnemy : EnemyAI
{
    [Header("Striker Movement")]
    public float normalSpeed = 4f;
    public float rushSpeed = 15f;
    public float rushDistance = 10f;
    public float rushCooldown = 2f;

    [Header("Striker Attack")]
    public float lungeDamage = 30f;
    public float lungeRange = 2f;

    private Rigidbody rb;
    private bool isRushing;
    private Vector3 rushDirection;
    private float rushTimer;
    private float nextRushTime;

    protected override void OnEnemyStart()
    {
        // Striker uses Sawtooth (Red) waveform
        // Immune to red, weak to blue knockback control

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
    }

    protected override void UpdateBehavior(float distanceToPlayer)
    {
        HandleAggressiveMovement(distanceToPlayer);
        HandleLungeAttack(distanceToPlayer);
    }

    private void HandleAggressiveMovement(float distanceToPlayer)
    {
        if (isRushing)
        {
            // Continue rushing in direction
            rb.linearVelocity = new Vector3(rushDirection.x * rushSpeed, rb.linearVelocity.y, rushDirection.z * rushSpeed);

            rushTimer -= Time.deltaTime;
            if (rushTimer <= 0)
            {
                isRushing = false;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
        }
        else
        {
            // Normal aggressive movement toward player
            if (distanceToPlayer > lungeRange)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;

                Vector3 currentVel = rb.linearVelocity;
                currentVel.x = direction.x * normalSpeed;
                currentVel.z = direction.z * normalSpeed;
                rb.linearVelocity = currentVel;

                // Face player
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
            }

            // Start rush if in range and cooldown ready
            if (distanceToPlayer <= rushDistance && distanceToPlayer > lungeRange && Time.time >= nextRushTime)
            {
                StartRush();
            }
        }
    }

    private void StartRush()
    {
        isRushing = true;
        rushDirection = (player.position - transform.position).normalized;
        rushDirection.y = 0;
        rushTimer = 0.5f; // Rush duration
        nextRushTime = Time.time + rushCooldown;
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
}