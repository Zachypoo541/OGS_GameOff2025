using UnityEngine;

/// <summary>
/// Place this component on GameObjects in your scene to mark spawn locations.
/// Designers can visually position these and reference them by name in wave configs.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("Identification")]
    [Tooltip("Unique name for this spawn point (e.g., 'entrance_left', 'pillar_1')")]
    public string spawnPointID;

    [Header("Visual Settings")]
    [Tooltip("Show spawn point gizmo in scene view")]
    public bool showGizmo = true;

    [Tooltip("Color of the gizmo in scene view")]
    public Color gizmoColor = Color.green;

    [Tooltip("Size of the gizmo sphere")]
    public float gizmoSize = 0.5f;

    private void OnValidate()
    {
        // Auto-generate ID from GameObject name if empty
        if (string.IsNullOrEmpty(spawnPointID))
        {
            spawnPointID = gameObject.name;
        }
    }

    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);

            // Draw arrow pointing forward to show spawn direction
            Gizmos.DrawRay(transform.position, transform.forward * gizmoSize * 2);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            // Draw solid sphere when selected
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, gizmoSize);
        }
    }
}