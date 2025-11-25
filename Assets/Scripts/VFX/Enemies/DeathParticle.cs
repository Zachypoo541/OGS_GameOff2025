using UnityEngine;

/// <summary>
/// Individual particle that moves outward from the enemy during death.
/// Fades out opacity as it travels and destroys itself after duration.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DeathParticle : MonoBehaviour
{
    private Vector3 direction;
    private SpriteRenderer spriteRenderer;
    private float duration;
    private float speed;
    private float maxDistance;
    private float elapsed;
    private Color baseColor;
    private Vector3 startPosition;

    public void Initialize(Vector3 moveDirection, Sprite sprite, Color color, float effectDuration, float moveSpeed, float travelDistance)
    {
        direction = moveDirection.normalized;
        duration = effectDuration;
        speed = moveSpeed;
        maxDistance = travelDistance;
        baseColor = color;
        startPosition = transform.position;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }

        // Start fully opaque
        spriteRenderer.color = baseColor;

        elapsed = 0f;

        // Random rotation for variety on all axes for 3D effect
        transform.rotation = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)
        );

        // Slight random variation in all directions for more organic 3D spread
        float angleVariationXZ = Random.Range(-15f, 15f);
        float angleVariationY = Random.Range(-15f, 15f);

        // Apply horizontal variation
        direction = Quaternion.Euler(0, angleVariationXZ, 0) * direction;

        // Apply vertical variation by rotating around a perpendicular axis
        Vector3 perpendicularAxis = Vector3.Cross(direction, Vector3.up);
        if (perpendicularAxis.magnitude < 0.1f)
        {
            perpendicularAxis = Vector3.Cross(direction, Vector3.forward);
        }
        direction = Quaternion.AngleAxis(angleVariationY, perpendicularAxis) * direction;
        direction.Normalize();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // Move outward
        transform.position += direction * speed * Time.deltaTime;

        // Calculate progress
        float progress = Mathf.Clamp01(elapsed / duration);
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        float distanceProgress = Mathf.Clamp01(distanceTraveled / maxDistance);

        // Fade out over time with an easing curve
        float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(progress, 0.7f));

        // Also fade based on distance
        alpha *= Mathf.Lerp(1f, 0f, distanceProgress);

        Color currentColor = baseColor;
        currentColor.a = alpha;
        spriteRenderer.color = currentColor;

        // Destroy when duration exceeded or traveled too far
        if (elapsed > duration || distanceTraveled > maxDistance)
        {
            Destroy(gameObject);
        }
    }
}