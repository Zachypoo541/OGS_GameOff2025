using UnityEngine;

/// <summary>
/// Simple test script to spawn WaveformUnlock objects at runtime for testing spawn effects.
/// Attach this to any GameObject in your scene.
/// </summary>
public class WaveformUnlockSpawnTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("The WaveformUnlock prefab to spawn (make sure isSpawned is checked on the prefab!)")]
    [SerializeField] private GameObject waveformUnlockPrefab;

    [Tooltip("Key to press to spawn a test object")]
    [SerializeField] private KeyCode spawnKey = KeyCode.T;

    [Header("Spawn Location")]
    [Tooltip("Where to spawn the test object. If null, spawns in front of camera.")]
    [SerializeField] private Transform spawnLocation;

    [Tooltip("Distance in front of camera to spawn if no spawn location is set")]
    [SerializeField] private float spawnDistanceFromCamera = 5f;

    [Tooltip("Height offset from spawn position")]
    [SerializeField] private float heightOffset = 1.5f;

    [Header("Random Spread (Optional)")]
    [Tooltip("Add random spread to spawn position")]
    [SerializeField] private bool useRandomSpread = false;

    [Tooltip("Radius of random spread")]
    [SerializeField] private float spreadRadius = 2f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("WaveformUnlockSpawnTester: No main camera found!");
        }

        if (waveformUnlockPrefab == null)
        {
            Debug.LogWarning("WaveformUnlockSpawnTester: No prefab assigned! Assign a WaveformUnlock prefab in the inspector.");
        }
        else
        {
            // Verify the prefab has isSpawned checked
            WaveformUnlock unlockComponent = waveformUnlockPrefab.GetComponent<WaveformUnlock>();
            if (unlockComponent != null)
            {
                Debug.Log("WaveformUnlockSpawnTester: Ready! Press 'T' to spawn test objects. Make sure 'isSpawned' is checked on the prefab in the inspector!");
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnTestObject();
        }
    }

    private void SpawnTestObject()
    {
        if (waveformUnlockPrefab == null)
        {
            Debug.LogError("WaveformUnlockSpawnTester: Cannot spawn, no prefab assigned!");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        // Determine spawn position
        if (spawnLocation != null)
        {
            spawnPosition = spawnLocation.position;
            spawnRotation = spawnLocation.rotation;
        }
        else if (mainCamera != null)
        {
            // Spawn in front of camera
            spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * spawnDistanceFromCamera;
            spawnPosition.y += heightOffset;
            spawnRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("WaveformUnlockSpawnTester: No spawn location and no camera found!");
            return;
        }

        // Apply random spread if enabled
        if (useRandomSpread && spreadRadius > 0)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            spawnPosition += new Vector3(randomOffset.x, 0, randomOffset.y);
        }

        // Instantiate the object
        GameObject spawnedObject = Instantiate(waveformUnlockPrefab, spawnPosition, spawnRotation);

        Debug.Log($"Spawned test WaveformUnlock at {spawnPosition}. Press '{spawnKey}' to spawn more.");
    }

    private void OnDrawGizmos()
    {
        // Visualize spawn location
        if (spawnLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnLocation.position, 0.5f);
            Gizmos.DrawLine(spawnLocation.position, spawnLocation.position + spawnLocation.forward * 2f);

            if (useRandomSpread)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawWireSphere(spawnLocation.position, spreadRadius);
            }
        }
        else if (Camera.main != null)
        {
            // Show where it would spawn in front of camera
            Vector3 pos = Camera.main.transform.position + Camera.main.transform.forward * spawnDistanceFromCamera;
            pos.y += heightOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, 0.5f);

            if (useRandomSpread)
            {
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawWireSphere(pos, spreadRadius);
            }
        }
    }
}