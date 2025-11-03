using UnityEngine;

public abstract class EnemyAI : CombatEntity
{
    [Header("AI Settings")]
    public float attackRange = 15f;
    public float detectionRange = 30f;

    [Header("Drops")]
    public GameObject energyPickupPrefab;
    [Range(0f, 1f)]
    public float dropChance = 0.7f;

    protected Transform player;
    protected bool isPlayerDetected;

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Enemy is immune to its own waveform type
        if (equippedWaveform != null)
        {
            immuneToWaveforms.Clear();
            immuneToWaveforms.Add(equippedWaveform);
        }

        OnEnemyStart();
    }

    protected override void Update()
    {
        base.Update();

        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerDetected = distToPlayer <= detectionRange;

        if (isPlayerDetected)
        {
            UpdateBehavior(distToPlayer);
        }
    }

    protected abstract void UpdateBehavior(float distanceToPlayer);

    protected override void Die()
    {
        base.Die();

        // Drop energy pickup based on chance
        if (energyPickupPrefab != null && Random.value <= dropChance)
        {
            Instantiate(energyPickupPrefab, transform.position, Quaternion.identity);
        }

        OnEnemyDeath();
        Destroy(gameObject);
    }

    // Override these in child classes for custom behavior
    protected virtual void OnEnemyStart() { }
    protected virtual void OnEnemyDeath() { }
}
