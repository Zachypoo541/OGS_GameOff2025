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

        // FIXED: Use Player.Instance singleton to get the correct moving transform
        if (Player.Instance != null)
        {
            player = Player.Instance.GetPlayerTransform();
        }
        else
        {
            // Fallback to tag-based finding (less reliable)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                // Try to get the Player component
                Player playerComponent = playerObj.GetComponent<Player>();
                if (playerComponent != null)
                {
                    player = playerComponent.GetPlayerTransform();
                }
                else
                {
                    // Last resort: try to find PlayerCharacter in children
                    PlayerCharacter character = playerObj.GetComponentInChildren<PlayerCharacter>();
                    if (character != null)
                    {
                        player = character.GetMotorTransform();
                    }
                    else
                    {
                        // Absolute fallback
                        player = playerObj.transform;
                        Debug.LogWarning($"{gameObject.name}: Could not find proper player transform. AI may not work correctly.");
                    }
                }
            }
            else
            {
                Debug.LogError($"{gameObject.name}: Could not find player! Make sure player has 'Player' tag or Player.Instance is set.");
            }
        }

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

    // Optional: Add this for debugging in the Scene view
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw line to player if detected
        if (player != null && isPlayerDetected)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}