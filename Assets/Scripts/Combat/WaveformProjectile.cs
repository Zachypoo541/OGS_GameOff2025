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

    public void Initialize(float damage, WaveformData waveformType, CombatEntity source, Vector3 direction, Reticle reticle = null)
    {
        this.damage = damage;
        this.waveformType = waveformType;
        this.source = source;
        this.direction = direction.normalized;
        this.speed = waveformType.projectileSpeed;
        this.reticle = reticle; // Store reticle reference (null for enemy projectiles)

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

            target.TakeDamage(damage, waveformType);

            // Apply knockback
            if (waveformType.knockbackForce > 0)
            {
                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    targetRb.AddForce(direction * waveformType.knockbackForce, ForceMode.Impulse);
                }
            }

            Destroy(gameObject);
        }
    }
}