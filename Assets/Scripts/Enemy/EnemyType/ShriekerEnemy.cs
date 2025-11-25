using UnityEngine;

public class ShriekerEnemy : EnemyAI
{
    [Header("Shrieker Settings")]
    public float sniperRange = 25f;
    public float sniperCooldown = 3f;
    public float chargeTime = 1f;
    public float aimHeightOffset = 1f; // Adjust this to aim lower/higher on the Character

    private float nextShotTime;
    private bool isCharging;
    private float chargeTimer;

    protected override void OnEnemyStart()
    {
        // Shrieker uses Sawtooth (Red) waveform
        // Stationary sniper
        // Immune to red, weak to repeated yellow attacks
        attackRange = sniperRange;
    }

    protected override void UpdateBehavior(float distanceToPlayer)
    {
        HandleStationarySniper(distanceToPlayer);
    }

    private void HandleStationarySniper(float distanceToPlayer)
    {
        // Always face player
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (distanceToPlayer > attackRange) return;

        // Charging shot
        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0)
            {
                // Fire at the Character's adjusted position
                Vector3 targetPos = player.position;
                targetPos.y = player.position.y - aimHeightOffset;

                Vector3 direction = (targetPos - transform.position).normalized;
                FireProjectile(direction);

                isCharging = false;
                nextShotTime = Time.time + sniperCooldown;
            }
        }
        // Start charging new shot
        else if (Time.time >= nextShotTime)
        {
            isCharging = true;
            chargeTimer = chargeTime;
            // Show attack indicator
            ShowAttackIndicator(chargeTime - 0.3f); // Subtract fade-in time
        }
    }

    protected override void OnEnemyDeath()
    {
        // Indicator is automatically cleaned up in base Die() method
    }
}