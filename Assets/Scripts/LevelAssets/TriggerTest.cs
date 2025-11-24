using UnityEngine;

public class TriggerTest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"=== TRIGGER TEST ===");
        Debug.Log($"Triggered by: {other.gameObject.name}");
        Debug.Log($"Object layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        Debug.Log($"This object layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log($"==================");
    }
}