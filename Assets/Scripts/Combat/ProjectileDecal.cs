using UnityEngine;

public class ProjectileDecal : MonoBehaviour
{
    // Layer mask for surfaces that should NOT receive decals
    private static int excludedLayers = (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Enemy"));

    public static void SpawnDecal(Vector3 position, Vector3 normal, GameObject hitObject, Sprite decalSprite, Color decalColor, float size = 0.5f, float lifetime = 10f)
    {
        if (decalSprite == null) return;

        // Check if the hit object is on an excluded layer
        if (hitObject != null && IsExcludedLayer(hitObject.layer))
        {
            return; // Don't spawn decal on player or enemies
        }

        // Create decal GameObject
        GameObject decalObj = new GameObject("ProjectileDecal");
        decalObj.transform.position = position + normal * 0.01f; // Slight offset to prevent z-fighting
        decalObj.transform.rotation = Quaternion.LookRotation(normal);

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = decalObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = decalSprite;
        spriteRenderer.color = decalColor;

        // Scale the decal
        decalObj.transform.localScale = Vector3.one * size;

        // Optional: Add slight random rotation for variety
        decalObj.transform.Rotate(0, 0, Random.Range(0f, 360f));

        // Fade out over time
        DecalFade fade = decalObj.AddComponent<DecalFade>();
        fade.lifetime = lifetime;

        // Destroy after lifetime
        Destroy(decalObj, lifetime);
    }

    private static bool IsExcludedLayer(int layer)
    {
        return ((1 << layer) & excludedLayers) != 0;
    }
}

// Helper component to fade out decal
public class DecalFade : MonoBehaviour
{
    public float lifetime = 10f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float timeAlive = 0f;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        timeAlive += Time.deltaTime;

        if (spriteRenderer != null)
        {
            // Start fading in the last 20% of lifetime
            float fadeStartTime = lifetime * 0.8f;
            if (timeAlive > fadeStartTime)
            {
                float fadeProgress = (timeAlive - fadeStartTime) / (lifetime - fadeStartTime);
                Color color = originalColor;
                color.a = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
                spriteRenderer.color = color;
            }
        }
    }
}