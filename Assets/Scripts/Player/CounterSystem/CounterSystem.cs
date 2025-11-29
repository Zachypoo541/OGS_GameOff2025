using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CounterEffect
{
    public WaveformData waveformType;
    public float activationTime;
    public int stackCount;
}

public class CounterSystem : MonoBehaviour
{
    [Header("Counter Settings")]
    [SerializeField] private float counterWindowDuration = 0.3f;
    [SerializeField] private float counterCooldown = 2f;
    [SerializeField] private LayerMask projectileLayer;

    [Header("Counter Collider Settings")]
    [SerializeField] private Vector3 counterColliderOffset = Vector3.zero;
    [SerializeField] private float counterColliderRadius = 0.6f;
    [SerializeField] private float counterColliderHeight = 2f;

    [Header("Sine (Green) Counter Effects")]
    [SerializeField] private float sineBaseHealing = 20f;
    [SerializeField] private float sineHealingPerStack = 5f;
    [SerializeField] private float sineRegenRate = 10f;
    [SerializeField] private float sineRegenDuration = 5f;

    [Header("Square (Blue) Counter Effects")]
    [SerializeField] private float squareBaseDamageReduction = 0.3f;
    [SerializeField] private float squareReductionPerStack = 0.1f;
    [SerializeField] private float squareBuffDuration = 3f;
    [SerializeField] private float squareReflectionDuration = 5f;

    [Header("Saw (Red) Counter Effects")]
    [SerializeField] private float sawBaseDamageBonus = 1.5f;
    [SerializeField] private float sawDamagePerStack = 0.2f;
    [SerializeField] private float sawDamageReceivedMultiplier = 1.3f;
    [SerializeField] private float sawChainRange = 15f;

    [Header("Triangle (Yellow) Counter Effects")]
    [SerializeField] private float triangleBaseEnergyRegen = 1.5f;
    [SerializeField] private float triangleRegenPerStack = 0.3f;
    [SerializeField] private float triangleBuffDuration = 4f;
    [SerializeField] private float triangleMovementSpeedBonus = 1.5f;
    [SerializeField] private float triangleUnlimitedEnergyDuration = 6f;

    [Header("Chromatic Saturation")]
    [SerializeField] private int maxStacksForChromaticEffect = 5;

    // State tracking
    private bool _isCounterActive;
    private float _counterWindowTimer;
    private bool _isOnCooldown;
    private float _cooldownTimer;
    private PlayerCharacter _playerCharacter;
    private HashSet<WaveformProjectile> _processedProjectiles = new HashSet<WaveformProjectile>();
    private CapsuleCollider _counterCollider;

    // Current counter effect tracking
    private CounterEffect _currentEffect;
    private Dictionary<WaveformData, int> _stackCounts = new Dictionary<WaveformData, int>();

    // Active buff tracking
    private float _damageResistanceAmount;
    private float _damageResistanceEndTime;
    private bool _isReflecting;
    private float _reflectionEndTime;

    private float _nextAttackDamageMultiplier = 1f;

    private float _energyRegenMultiplier = 1f;
    private float _energyRegenEndTime;
    private bool _hasUnlimitedEnergy;
    private float _unlimitedEnergyEndTime;
    private float _movementSpeedMultiplier = 1f;

    private float _regenRate;
    private float _regenEndTime;

    private bool _hasChainAttack;

    public bool IsCounterActive => _isCounterActive;
    public bool IsOnCooldown => _isOnCooldown;
    public float CooldownRemaining => _isOnCooldown ? _cooldownTimer : 0f;

    private Reticle _reticle;

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();
        SetupCounterCollider();
    }

    private void SetupCounterCollider()
    {
        // Create a child object for the counter collider
        GameObject counterColliderObj = new GameObject("CounterCollider");
        counterColliderObj.transform.SetParent(transform);
        counterColliderObj.transform.localPosition = counterColliderOffset;
        counterColliderObj.transform.localRotation = Quaternion.identity;

        // Add and configure the capsule collider
        _counterCollider = counterColliderObj.AddComponent<CapsuleCollider>();
        _counterCollider.isTrigger = true;
        _counterCollider.radius = counterColliderRadius;
        _counterCollider.height = counterColliderHeight;
        _counterCollider.direction = 1; // Y-axis

        // Set to the same layer as the player's trigger collider
        counterColliderObj.layer = gameObject.layer;

        // Initially disable it
        _counterCollider.enabled = false;
    }

    public void Initialize(Reticle reticle)
    {
        _reticle = reticle;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Update counter window
        if (_isCounterActive)
        {
            _counterWindowTimer -= deltaTime;

            if (_counterWindowTimer <= 0f)
            {
                EndCounterWindow(false);
            }
        }

        // Update cooldown
        if (_isOnCooldown)
        {
            _cooldownTimer -= deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _isOnCooldown = false;
            }
        }

        // Update active buffs
        UpdateActiveBuffs(deltaTime);
    }

    public void AttemptCounter()
    {

        if (_isOnCooldown || _isCounterActive)
            return;

        _isCounterActive = true;
        _counterWindowTimer = counterWindowDuration;
        _processedProjectiles.Clear();

        // Enable the counter collider
        if (_counterCollider != null)
        {
            _counterCollider.enabled = true;
        }
        else
        {
            Debug.LogError("[CounterSystem] Counter collider is NULL!");
        }

        // Notify reticle that parry window started
        if (_reticle != null)
        {
            _reticle.OnParryWindowStart();
        }

    }

    public bool TryCounterProjectile(WaveformProjectile projectile)
    {

        if (!_isCounterActive)
        {
            return false;
        }

        // Don't process the same projectile twice
        if (_processedProjectiles.Contains(projectile))
        {
            return false;
        }

        if (CanCounterProjectile(projectile))
        {
            _processedProjectiles.Add(projectile);
            CounterProjectile(projectile);
            return true;
        }
        return false;
    }

    private bool CanCounterProjectile(WaveformProjectile projectile)
    {
        // Check if player has the matching waveform equipped
        if (_playerCharacter.equippedWaveform != projectile.Waveform)
        {
            return false;
        }

        // Don't counter own projectiles
        if (projectile.Owner == _playerCharacter)
        {
            return false;
        }

        return true;
    }

    private void CounterProjectile(WaveformProjectile projectile)
    {
        WaveformData waveformType = projectile.Waveform;

        // Notify reticle of successful parry
        if (_reticle != null)
        {
            _reticle.OnParrySuccess(waveformType);
        }

        // Destroy the projectile
        Destroy(projectile.gameObject);

        // Update stack count
        if (!_stackCounts.ContainsKey(waveformType))
            _stackCounts[waveformType] = 0;

        // Check if switching waveform types (reset other stacks)
        if (_currentEffect != null && _currentEffect.waveformType != waveformType)
        {
            ClearActiveEffects();
            var keysToReset = new List<WaveformData>(_stackCounts.Keys);
            foreach (var key in keysToReset)
            {
                if (key != waveformType)
                    _stackCounts[key] = 0;
            }
        }

        _stackCounts[waveformType]++;
        int currentStacks = _stackCounts[waveformType];

        if (currentStacks > maxStacksForChromaticEffect)
        {
            currentStacks = maxStacksForChromaticEffect;
            _stackCounts[waveformType] = maxStacksForChromaticEffect;
        }

        _currentEffect = new CounterEffect
        {
            waveformType = waveformType,
            activationTime = Time.time,
            stackCount = currentStacks
        };

        ApplyCounterEffect(waveformType, currentStacks);

        if (currentStacks >= maxStacksForChromaticEffect)
        {
            _stackCounts[waveformType] = 0;
        }

        // DON'T end the counter window here - let it stay open for multiple counters!
        // EndCounterWindow(true);  // REMOVED

    }

    private void ApplyCounterEffect(WaveformData waveformType, int stackCount)
    {
        string waveformName = waveformType.name.ToLower();

        if (waveformName.Contains("sine") || waveformName.Contains("green"))
        {
            ApplySineEffect(stackCount);
        }
        else if (waveformName.Contains("square") || waveformName.Contains("blue"))
        {
            ApplySquareEffect(stackCount);
        }
        else if (waveformName.Contains("saw") || waveformName.Contains("red"))
        {
            ApplySawEffect(stackCount);
        }
        else if (waveformName.Contains("triangle") || waveformName.Contains("yellow"))
        {
            ApplyTriangleEffect(stackCount);
        }
    }

    private void ApplySineEffect(int stackCount)
    {
        float healAmount = sineBaseHealing + (sineHealingPerStack * (stackCount - 1));
        _playerCharacter.Heal(healAmount);

        if (stackCount >= maxStacksForChromaticEffect)
        {
            _regenRate = sineRegenRate;
            _regenEndTime = Time.time + sineRegenDuration;
        }
    }

    private void ApplySquareEffect(int stackCount)
    {
        _damageResistanceAmount = squareBaseDamageReduction + (squareReductionPerStack * (stackCount - 1));
        _damageResistanceAmount = Mathf.Min(_damageResistanceAmount, 0.9f);
        _damageResistanceEndTime = Time.time + squareBuffDuration;

        if (stackCount >= maxStacksForChromaticEffect)
        {
            _isReflecting = true;
            _reflectionEndTime = Time.time + squareReflectionDuration;
        }
    }

    private void ApplySawEffect(int stackCount)
    {
        _nextAttackDamageMultiplier = sawBaseDamageBonus + (sawDamagePerStack * (stackCount - 1));

        if (stackCount >= maxStacksForChromaticEffect)
        {
            _hasChainAttack = true;
        }
    }

    private void ApplyTriangleEffect(int stackCount)
    {
        _energyRegenMultiplier = triangleBaseEnergyRegen + (triangleRegenPerStack * (stackCount - 1));
        _energyRegenEndTime = Time.time + triangleBuffDuration;

        if (stackCount >= maxStacksForChromaticEffect)
        {
            _hasUnlimitedEnergy = true;
            _unlimitedEnergyEndTime = Time.time + triangleUnlimitedEnergyDuration;
            _movementSpeedMultiplier = triangleMovementSpeedBonus;
        }
    }

    private void UpdateActiveBuffs(float deltaTime)
    {
        if (Time.time < _regenEndTime)
        {
            _playerCharacter.Heal(_regenRate * deltaTime);
        }

        if (Time.time >= _damageResistanceEndTime)
        {
            _damageResistanceAmount = 0f;
        }

        if (Time.time >= _reflectionEndTime)
        {
            _isReflecting = false;
        }

        if (Time.time >= _energyRegenEndTime)
        {
            _energyRegenMultiplier = 1f;
        }

        if (Time.time >= _unlimitedEnergyEndTime)
        {
            _hasUnlimitedEnergy = false;
            _movementSpeedMultiplier = 1f;
        }
    }

    private void ClearActiveEffects()
    {
        _damageResistanceAmount = 0f;
        _damageResistanceEndTime = 0f;
        _isReflecting = false;
        _reflectionEndTime = 0f;
        _nextAttackDamageMultiplier = 1f;
        _energyRegenMultiplier = 1f;
        _energyRegenEndTime = 0f;
        _hasUnlimitedEnergy = false;
        _unlimitedEnergyEndTime = 0f;
        _movementSpeedMultiplier = 1f;
        _regenRate = 0f;
        _regenEndTime = 0f;
        _hasChainAttack = false;
    }

    private void EndCounterWindow(bool successful)
    {

        _isCounterActive = false;
        _processedProjectiles.Clear();

        // Disable the counter collider
        if (_counterCollider != null)
        {
            _counterCollider.enabled = false;
        }

        // Notify reticle that parry window ended (only if not successful, as successful parry handles its own visual)
        if (_reticle != null && !successful)
        {
            _reticle.OnParryWindowEnd();
        }

        if (!successful)
        {
            _isOnCooldown = true;
            _cooldownTimer = counterCooldown;
        }
    }

    public float GetDamageResistance() => _damageResistanceAmount;
    public bool IsReflecting() => _isReflecting;
    public float GetDamageMultiplier()
    {
        float multiplier = _nextAttackDamageMultiplier;
        if (multiplier > 1f)
        {
            _nextAttackDamageMultiplier = 1f;
        }
        return multiplier;
    }
    public float GetDamageReceivedMultiplier()
    {
        if (_currentEffect != null && _currentEffect.waveformType != null)
        {
            string waveformName = _currentEffect.waveformType.name.ToLower();
            if ((waveformName.Contains("saw") || waveformName.Contains("red")) && _currentEffect.stackCount > 0)
            {
                return sawDamageReceivedMultiplier;
            }
        }
        return 1f;
    }
    public float GetEnergyRegenMultiplier() => _energyRegenMultiplier;
    public bool HasUnlimitedEnergy() => _hasUnlimitedEnergy;
    public float GetMovementSpeedMultiplier() => _movementSpeedMultiplier;
    public bool HasChainAttack() => _hasChainAttack;
    public float GetChainRange() => sawChainRange;

    public void ConsumeChainAttack()
    {
        _hasChainAttack = false;
    }

    public bool TryCounterHitscan(WaveformData waveformType, CombatEntity attacker, out bool applyEffect)
    {

        applyEffect = false;

        if (!_isCounterActive)
        {
            return false;
        }

        if (_playerCharacter.equippedWaveform != waveformType)
        {
            return false;
        }

        if (attacker == _playerCharacter)
        {
            return false;
        }

        applyEffect = true;

        // Notify reticle of successful parry
        if (_reticle != null)
        {
            _reticle.OnParrySuccess(waveformType);
        }

        if (!_stackCounts.ContainsKey(waveformType))
            _stackCounts[waveformType] = 0;

        if (_currentEffect != null && _currentEffect.waveformType != waveformType)
        {
            ClearActiveEffects();
            var keysToReset = new List<WaveformData>(_stackCounts.Keys);
            foreach (var key in keysToReset)
            {
                if (key != waveformType)
                    _stackCounts[key] = 0;
            }
        }

        _stackCounts[waveformType]++;
        int currentStacks = _stackCounts[waveformType];

        if (currentStacks > maxStacksForChromaticEffect)
        {
            currentStacks = maxStacksForChromaticEffect;
            _stackCounts[waveformType] = maxStacksForChromaticEffect;
        }

        _currentEffect = new CounterEffect
        {
            waveformType = waveformType,
            activationTime = Time.time,
            stackCount = currentStacks
        };

        ApplyCounterEffect(waveformType, currentStacks);

        if (currentStacks >= maxStacksForChromaticEffect)
        {
            _stackCounts[waveformType] = 0;
        }

        // DON'T end the counter window here - let it stay open for multiple counters!
        // EndCounterWindow(true);  // REMOVED

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the counter collider visualization
        Gizmos.color = _isCounterActive ? Color.green : Color.cyan;

        Vector3 gizmoPosition = transform.position + counterColliderOffset;

        // Draw the capsule
        DrawWireCapsule(gizmoPosition, counterColliderRadius, counterColliderHeight);
    }

    private void DrawWireCapsule(Vector3 position, float radius, float height)
    {
        // Calculate hemisphere positions
        float halfHeight = height * 0.5f;
        Vector3 topSphere = position + Vector3.up * (halfHeight - radius);
        Vector3 bottomSphere = position + Vector3.down * (halfHeight - radius);

        // Draw the top hemisphere
        Gizmos.DrawWireSphere(topSphere, radius);

        // Draw the bottom hemisphere
        Gizmos.DrawWireSphere(bottomSphere, radius);

        // Draw the connecting lines (cylinder sides)
        Vector3 forward = Vector3.forward * radius;
        Vector3 back = Vector3.back * radius;
        Vector3 left = Vector3.left * radius;
        Vector3 right = Vector3.right * radius;

        Gizmos.DrawLine(topSphere + forward, bottomSphere + forward);
        Gizmos.DrawLine(topSphere + back, bottomSphere + back);
        Gizmos.DrawLine(topSphere + left, bottomSphere + left);
        Gizmos.DrawLine(topSphere + right, bottomSphere + right);
    }
}