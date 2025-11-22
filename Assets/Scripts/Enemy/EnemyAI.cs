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

    [Header("Attack Indicator")]
    public GameObject attackIndicatorPrefab;
    [SerializeField] private Sprite attackIndicatorSprite;
    public Color attackIndicatorColor = Color.red;
    public Vector3 indicatorOffset = Vector3.up * 2f;

    protected Transform player;
    protected bool isPlayerDetected;
    protected GameObject currentIndicator;

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

    /// <summary>
    /// Spawns an attack indicator at the enemy's position.
    /// Call this when the enemy begins charging/preparing an attack.
    /// </summary>
    /// <param name="attackDuration">How long until the attack fires (duration of the indicator effect)</param>
    protected void ShowAttackIndicator(float attackDuration)
    {
        // Clean up existing indicator
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }

        // Create new indicator
        if (attackIndicatorPrefab != null)
        {
            Vector3 spawnPos = transform.position + indicatorOffset;
            currentIndicator = Instantiate(attackIndicatorPrefab, spawnPos, Quaternion.identity, transform);

            AttackIndicator indicator = currentIndicator.GetComponent<AttackIndicator>();
            if (indicator != null && attackIndicatorSprite != null)
            {
                indicator.StartIndicator(attackDuration, attackIndicatorSprite, attackIndicatorColor);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No attack indicator prefab assigned!");
        }
    }

    /// <summary>
    /// Manually destroys the current attack indicator.
    /// Useful if an attack is cancelled or interrupted.
    /// </summary>
    protected void ClearAttackIndicator()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }

    protected override void Die()
    {
        base.Die();

        // Clean up indicator on death
        ClearAttackIndicator();

        // Drop energy pickup based on chance
        if (energyPickupPrefab != null && Random.value <= dropChance)
        {
            Instantiate(energyPickupPrefab, transform.position, Quaternion.identity);
        }

        OnEnemyDeath();
        Destroy(gameObject);
    }

    // Public getters for EnemyOutlineController
    public Transform GetPlayerTransform()
    {
        return player;
    }

    public float GetAttackRange()
    {
        return attackRange;
    }

    public float GetDetectionRange()
    {
        return detectionRange;
    }

    public bool IsPlayerDetected()
    {
        return isPlayerDetected;
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

        // Draw indicator position
        Gizmos.color = attackIndicatorColor;
        Gizmos.DrawWireSphere(transform.position + indicatorOffset, 0.5f);
    }
}