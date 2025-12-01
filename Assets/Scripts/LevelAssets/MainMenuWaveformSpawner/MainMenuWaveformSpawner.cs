using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenuWaveformSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("Array of WaveformUnlock prefabs to spawn randomly")]
    public GameObject[] waveformPrefabs;

    [Header("Movement Settings")]
    [Tooltip("Direction the waveforms will move (will be normalized)")]
    public Vector3 movementDirection = Vector3.right;

    [Tooltip("Speed at which waveforms move")]
    public float movementSpeed = 5f;

    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 45f;

    [Tooltip("Axis around which waveforms rotate (local space)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Header("Spawn Settings")]
    [Tooltip("Size of the box area where waveforms can spawn")]
    public Vector3 spawnBoxSize = new Vector3(10f, 10f, 1f);

    [Tooltip("Time in seconds between spawns")]
    public float spawnInterval = 2f;

    [Tooltip("How long each waveform lives before being destroyed")]
    public float waveformLifetime = 10f;

    [Header("Debug")]
    [Tooltip("Show the spawn box in the editor")]
    public bool showSpawnBox = true;

    private void Start()
    {
        if (waveformPrefabs == null || waveformPrefabs.Length == 0)
        {
            Debug.LogError("WaveformSpawner: No prefabs assigned!");
            return;
        }

        // Normalize the movement direction
        movementDirection = movementDirection.normalized;

        // Start spawning
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnWaveform();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnWaveform()
    {
        // Pick a random prefab
        GameObject prefabToSpawn = waveformPrefabs[Random.Range(0, waveformPrefabs.Length)];

        // Calculate random position within the spawn box
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnBoxSize.x / 2f, spawnBoxSize.x / 2f),
            Random.Range(-spawnBoxSize.y / 2f, spawnBoxSize.y / 2f),
            Random.Range(-spawnBoxSize.z / 2f, spawnBoxSize.z / 2f)
        );

        Vector3 spawnPosition = transform.position + transform.TransformDirection(randomOffset);

        // Spawn the waveform
        GameObject waveform = Instantiate(prefabToSpawn, spawnPosition, transform.rotation);

        // Add the movement and rotation component
        WaveformBehavior behavior = waveform.AddComponent<WaveformBehavior>();
        behavior.Initialize(movementDirection, movementSpeed, rotationAxis, rotationSpeed, waveformLifetime);
    }

    private void OnDrawGizmos()
    {
        if (showSpawnBox)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, spawnBoxSize);
        }
    }
}

// Helper component that manages individual waveform behavior
public class WaveformBehavior : MonoBehaviour
{
    private Vector3 movementDirection;
    private float movementSpeed;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private float lifetime;

    public void Initialize(Vector3 moveDir, float moveSpeed, Vector3 rotAxis, float rotSpeed, float life)
    {
        movementDirection = moveDir.normalized;
        movementSpeed = moveSpeed;
        rotationAxis = rotAxis.normalized;
        rotationSpeed = rotSpeed;
        lifetime = life;

        // Destroy after lifetime expires
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move in the specified direction
        transform.position += movementDirection * movementSpeed * Time.deltaTime;

        // Rotate around the specified axis
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}