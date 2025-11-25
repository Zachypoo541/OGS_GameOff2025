using UnityEngine;

/// <summary>
/// Individual particle that moves toward a target position during enemy spawn.
/// Fades in opacity and destroys itself when it reaches the center.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpawnParticle : MonoBehaviour
{
    private Vector3 targetPosition;
    private SpriteRenderer spriteRenderer;
    private float duration;
    private float speed;
    private float startDistance;
    private float elapsed;
    private Color baseColor;
    private float destructionRadius;

    public void Initialize(Vector3 target, Sprite sprite, Color color, float effectDuration, float moveSpeed, float destroyRadius)
    {
        targetPosition = target;
        duration = effectDuration;
        speed = moveSpeed;
        baseColor = color;
        destructionRadius = destroyRadius;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }

        // Start fully transparent
        Color startColor = baseColor;
        startColor.a = 0f;
        spriteRenderer.color = startColor;

        startDistance = Vector3.Distance(transform.position, targetPosition);
        elapsed = 0f;

        // Random rotation for variety on all axes for 3D effect
        transform.rotation = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)
        );
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // Move toward target
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        // Calculate fade in based on time and distance
        float timeProgress = Mathf.Clamp01(elapsed / (duration * 0.3f));
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Calculate progress based on how close we are to destruction radius
        float travelDistance = startDistance - destructionRadius;
        float distanceProgress = 1f - Mathf.Clamp01((distanceToTarget - destructionRadius) / travelDistance);

        // Fade in quickly at start, then fade out as approaching destruction radius
        float alpha;
        if (distanceProgress < 0.7f)
        {
            alpha = Mathf.Lerp(0f, 1f, timeProgress);
        }
        else
        {
            alpha = Mathf.Lerp(1f, 0f, (distanceProgress - 0.7f) / 0.3f);
        }

        Color currentColor = baseColor;
        currentColor.a = alpha;
        spriteRenderer.color = currentColor;

        // Destroy when reached destruction radius or duration exceeded
        if (distanceToTarget < destructionRadius || elapsed > duration)
        {
            Destroy(gameObject);
        }
    }
}