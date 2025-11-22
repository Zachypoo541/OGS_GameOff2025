using UnityEngine;

public class HitscanProjectile : MonoBehaviour
{
    private float damage;
    private WaveformData waveformType;
    private CombatEntity source;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float travelDistance;
    private Reticle reticle;
    private TrailRenderer trailRenderer;

    private float visualSpeed = 100f; // Speed the visual trail travels
    private float distanceTraveled = 0f;
    private bool hasHit = false;

    public void Initialize(float damage, WaveformData waveformType, CombatEntity source, Vector3 direction, float maxRange = 100f, Reticle reticle = null)
    {
        this.damage = damage;
        this.waveformType = waveformType;
        this.source = source;
        this.reticle = reticle;
        this.startPosition = transform.position;

        // Use higher visual speed if specified in waveform
        if (waveformType.projectileSpeed > 100f)
        {
            visualSpeed = waveformType.projectileSpeed;
        }

        // Perform instant raycast
        RaycastHit hit;
        if (Physics.Raycast(startPosition, direction, out hit, maxRange))
        {
            endPosition = hit.point;
            travelDistance = hit.distance;

            // Check if we hit a valid target
            CombatEntity target = hit.collider.GetComponent<CombatEntity>();
            if (target != null && target != source)
            {
                // Apply damage immediately
                target.TakeDamage(damage, waveformType, source);

                // Trigger reticle hit effect
                if (reticle != null)
                {
                    reticle.OnHit();
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

                hasHit = true;
            }
        }
        else
        {
            // No hit, travel full distance
            endPosition = startPosition + direction * maxRange;
            travelDistance = maxRange;
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

        // Setup waveform trail if present
        WaveformTrailRenderer waveformTrail = GetComponent<WaveformTrailRenderer>();
        if (waveformTrail != null)
        {
            waveformTrail.Initialize(waveformType);
        }

        // Destroy after trail fades
        float lifetime = waveformType.trailLifetime + (travelDistance / visualSpeed);
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (distanceTraveled < travelDistance)
        {
            // Move visual representation along the path
            float step = visualSpeed * Time.deltaTime;
            distanceTraveled += step;

            float progress = Mathf.Clamp01(distanceTraveled / travelDistance);
            transform.position = Vector3.Lerp(startPosition, endPosition, progress);
        }
    }
}