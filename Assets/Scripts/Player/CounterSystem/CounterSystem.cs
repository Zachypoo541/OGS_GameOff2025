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
    [SerializeField] private float counterRadius = 5f;
    [SerializeField] private LayerMask projectileLayer;

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

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();
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

        Debug.Log("Counter window opened!");
    }

    // This should be called by an external system when a projectile is about to hit the player
    // For example, call this from PlayerCharacter.TakeDamage BEFORE applying damage
    public bool TryCounterProjectile(WaveformProjectile projectile)
    {
        if (!_isCounterActive)
            return false;

        // Don't process the same projectile twice
        if (_processedProjectiles.Contains(projectile))
            return false;

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

        EndCounterWindow(true);

        Debug.Log($"Countered {waveformType.name} projectile! Stack count: {currentStacks}");
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
            Debug.Log($"CHROMATIC SATURATION: Regeneration Aura activated!");
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
            Debug.Log($"CHROMATIC SATURATION: Damage Reflection activated!");
        }
    }

    private void ApplySawEffect(int stackCount)
    {
        _nextAttackDamageMultiplier = sawBaseDamageBonus + (sawDamagePerStack * (stackCount - 1));

        if (stackCount >= maxStacksForChromaticEffect)
        {
            _hasChainAttack = true;
            Debug.Log($"CHROMATIC SATURATION: Chain Attack activated!");
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
            Debug.Log($"CHROMATIC SATURATION: Unlimited Energy + Speed Boost activated!");
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
            return false;

        if (_playerCharacter.equippedWaveform != waveformType)
        {
            return false;
        }

        if (attacker == _playerCharacter)
        {
            return false;
        }

        applyEffect = true;

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

        EndCounterWindow(true);

        Debug.Log($"Countered {waveformType.name} hitscan attack! Stack count: {currentStacks}");

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, counterRadius);
    }
}