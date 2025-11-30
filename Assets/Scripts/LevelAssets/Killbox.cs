using UnityEngine;

/// <summary>
/// Killbox that instantly kills the player when they enter it.
/// Use for fall zones, out-of-bounds areas, or hazardous regions.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Killbox : MonoBehaviour
{
    [Header("Killbox Settings")]
    [Tooltip("The tag to check for (default: 'Player')")]
    [SerializeField] private string playerTag = "Player";

    [Header("Optional Effects")]
    [Tooltip("Play a sound when player enters killbox")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.3f); // Red with transparency

    private Collider killboxCollider;
    private Rigidbody killboxRigidbody;

    private void Awake()
    {
        // Get the collider and ensure it's set as a trigger
        killboxCollider = GetComponent<Collider>();

        if (killboxCollider != null)
        {
            if (!killboxCollider.isTrigger)
            {
                killboxCollider.isTrigger = true;
            }
        }

        // Ensure we have a Rigidbody for trigger detection
        // This is CRITICAL when the player has non-trigger colliders
        killboxRigidbody = GetComponent<Rigidbody>();
        if (killboxRigidbody == null)
        {
            killboxRigidbody = gameObject.AddComponent<Rigidbody>();
            killboxRigidbody.isKinematic = true;
            killboxRigidbody.useGravity = false;
        }
        else
        {
            // Make sure it's set up correctly
            if (!killboxRigidbody.isKinematic)
            {
                killboxRigidbody.isKinematic = true;
            }
            if (killboxRigidbody.useGravity)
            {
                killboxRigidbody.useGravity = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Try to find PlayerCharacter component (it's on the Character child object)
        PlayerCharacter playerCharacter = other.GetComponent<PlayerCharacter>();

        if (playerCharacter == null)
        {
            // Try looking in parent (Player root might have it)
            playerCharacter = other.GetComponentInParent<PlayerCharacter>();
        }

        if (playerCharacter == null)
        {
            // Check if this object or its parent has the player tag
            bool isPlayer = other.CompareTag(playerTag) ||
                           (other.transform.parent != null && other.transform.parent.CompareTag(playerTag));

            if (isPlayer && Player.Instance != null)
            {
                // Last resort: get from Player.Instance
                playerCharacter = Player.Instance.GetPlayerCharacter();
            }
        }

        if (playerCharacter != null)
        {
            // Play death sound if assigned
            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
            }

            // Kill the player by setting health to 0
            // This triggers all the normal death events
            playerCharacter.currentHealth = 0f;

            // Manually call TakeDamage to ensure death processing happens
            playerCharacter.TakeDamage(playerCharacter.maxHealth);

            Debug.Log("Killbox: Player killed");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        Collider col = GetComponent<Collider>();
        if (col == null)
            return;

        // Set gizmo color
        Gizmos.color = gizmoColor;

        // Draw based on collider type
        if (col is BoxCollider boxCollider)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            // Draw wireframe for better visibility
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (col is SphereCollider sphereCollider)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (col is CapsuleCollider capsuleCollider)
        {
            // Capsules are more complex to draw, so just draw a wire sphere at center
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireSphere(transform.position + capsuleCollider.center, capsuleCollider.radius);
        }
        else if (col is MeshCollider)
        {
            // For mesh colliders, draw the bounds
            Bounds bounds = col.bounds;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(bounds.center, bounds.size);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}