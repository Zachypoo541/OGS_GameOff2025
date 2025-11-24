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
    [SerializeField] private float sineRegenRate = 10f; // HP per second at max stacks
    [SerializeField] private float sineRegenDuration = 5f;

    [Header("Square (Blue) Counter Effects")]
    [SerializeField] private float squareBaseDamageReduction = 0.3f; // 30%
    [SerializeField] private float squareReductionPerStack = 0.1f; // Additional 10% per stack
    [SerializeField] private float squareBuffDuration = 3f;
    [SerializeField] private float squareReflectionDuration = 5f;

    [Header("Saw (Red) Counter Effects")]
    [SerializeField] private float sawBaseDamageBonus = 1.5f; // 50% bonus
    [SerializeField] private float sawDamagePerStack = 0.2f; // Additional 20% per stack
    [SerializeField] private float sawDamageReceivedMultiplier = 1.3f; // 30% more damage taken
    [SerializeField] private float sawChainRange = 15f;

    [Header("Triangle (Yellow) Counter Effects")]
    [SerializeField] private float triangleBaseEnergyRegen = 1.5f; // 1.5x multiplier
    [SerializeField] private float triangleRegenPerStack = 0.3f; // Additional 0.3x per stack
    [SerializeField] private float triangleBuffDuration = 4f;
    [SerializeField] private float triangleMovementSpeedBonus = 1.5f; // 50% speed increase
    [SerializeField] private float triangleUnlimitedEnergyDuration = 6f;

    [Header("Chromatic Saturation")]
    [SerializeField] private int maxStacksForChromaticEffect = 5;

    // State tracking
    private bool _isCounterActive;
    private float _counterWindowTimer;
    private bool _isOnCooldown;
    private float _cooldownTimer;
    private PlayerCharacter _playerCharacter;

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

            // Continuously check for projectiles during the window
            CheckForProjectilesToCounter();

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

        Debug.Log("Counter window opened!");
    }

    private void CheckForProjectilesToCounter()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, counterRadius, projectileLayer);

        if (_isCounterActive && hits.Length > 0)
        {
            Debug.Log($"Found {hits.Length} colliders in counter radius");
        }

        foreach (Collider hit in hits)
        {
            WaveformProjectile projectile = hit.GetComponent<WaveformProjectile>();
            if (projectile != null)
            {
                Debug.Log($"Found projectile: {projectile.Waveform?.name ?? "null"}");
                if (CanCounterProjectile(projectile))
                {
                    CounterProjectile(projectile);
                    return; // Only counter one projectile per attempt
                }
            }
        }
    }

    private bool CanCounterProjectile(WaveformProjectile projectile)
    {
        // Check if player has the matching waveform equipped
        if (_playerCharacter.equippedWaveform != projectile.Waveform)
        {
            Debug.Log($"Cannot counter: Equipped {_playerCharacter.equippedWaveform?.name ?? "null"} but projectile is {projectile.Waveform?.name ?? "null"}");
            return false;
        }

        // Don't counter own projectiles
        if (projectile.Owner == _playerCharacter)
        {
            Debug.Log("Cannot counter: Own projectile");
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
            // Clear effects from previous waveform type
            ClearActiveEffects();

            // Reset stack counts for other waveforms
            var keysToReset = new List<WaveformData>(_stackCounts.Keys);
            foreach (var key in keysToReset)
            {
                if (key != waveformType)
                    _stackCounts[key] = 0;
            }
        }

        _stackCounts[waveformType]++;
        int currentStacks = _stackCounts[waveformType];

        // Cap at max stacks
        if (currentStacks > maxStacksForChromaticEffect)
        {
            currentStacks = maxStacksForChromaticEffect;
            _stackCounts[waveformType] = maxStacksForChromaticEffect;
        }

        // Update current effect
        _currentEffect = new CounterEffect
        {
            waveformType = waveformType,
            activationTime = Time.time,
            stackCount = currentStacks
        };

        // Apply counter effect based on waveform type
        ApplyCounterEffect(waveformType, currentStacks);

        // Reset stacks to 0 after reaching Chromatic threshold
        if (currentStacks >= maxStacksForChromaticEffect)
        {
            _stackCounts[waveformType] = 0;
            Debug.Log($"Stack count reset to 0 after Chromatic activation");
        }

        // Successfully countered, so no cooldown
        EndCounterWindow(true);

        Debug.Log($"Countered {waveformType.name} projectile! Stack count: {currentStacks}");
    }

    private void ApplyCounterEffect(WaveformData waveformType, int stackCount)
    {
        // Determine which waveform type this is by checking the name or a property
        string waveformName = waveformType.name.ToLower();
        Debug.Log($"Applying counter effect for waveform: '{waveformName}' (stack {stackCount})");

        if (waveformName.Contains("sine") || waveformName.Contains("green"))
        {
            Debug.Log("Detected as Sine/Green waveform");
            ApplySineEffect(stackCount);
        }
        else if (waveformName.Contains("square") || waveformName.Contains("blue"))
        {
            Debug.Log("Detected as Square/Blue waveform");
            ApplySquareEffect(stackCount);
        }
        else if (waveformName.Contains("saw") || waveformName.Contains("red"))
        {
            Debug.Log("Detected as Saw/Red waveform");
            ApplySawEffect(stackCount);
        }
        else if (waveformName.Contains("triangle") || waveformName.Contains("yellow"))
        {
            Debug.Log("Detected as Triangle/Yellow waveform");
            ApplyTriangleEffect(stackCount);
        }
        else
        {
            Debug.LogWarning($"Waveform '{waveformName}' does not match any known type! Make sure it contains: sine/green, square/blue, saw/red, or triangle/yellow");
        }
    }

    private void ApplySineEffect(int stackCount)
    {
        // Calculate healing amount
        float healAmount = sineBaseHealing + (sineHealingPerStack * (stackCount - 1));
        _playerCharacter.Heal(healAmount);

        // At max stacks, activate regeneration aura
        if (stackCount >= maxStacksForChromaticEffect)
        {
            _regenRate = sineRegenRate;
            _regenEndTime = Time.time + sineRegenDuration;
            Debug.Log($"CHROMATIC SATURATION: Regeneration Aura activated!");
        }
    }

    private void ApplySquareEffect(int stackCount)
    {
        // Calculate damage resistance
        _damageResistanceAmount = squareBaseDamageReduction + (squareReductionPerStack * (stackCount - 1));
        _damageResistanceAmount = Mathf.Min(_damageResistanceAmount, 0.9f); // Cap at 90%
        _damageResistanceEndTime = Time.time + squareBuffDuration;

        Debug.Log($"Square buff applied: {_damageResistanceAmount * 100f}% damage resistance for {squareBuffDuration}s");

        // At max stacks, activate reflection
        if (stackCount >= maxStacksForChromaticEffect)
        {
            _isReflecting = true;
            _reflectionEndTime = Time.time + squareReflectionDuration;
            Debug.Log($"CHROMATIC SATURATION: Damage Reflection activated!");
        }
    }

    private void ApplySawEffect(int stackCount)
    {
        // Calculate damage multiplier
        _nextAttackDamageMultiplier = sawBaseDamageBonus + (sawDamagePerStack * (stackCount - 1));

        // At max stacks, enable chain attacks
        if (stackCount >= maxStacksForChromaticEffect)
        {
            _hasChainAttack = true;
            Debug.Log($"CHROMATIC SATURATION: Chain Attack activated!");
        }
    }

    private void ApplyTriangleEffect(int stackCount)
    {
        // Calculate energy regen multiplier
        _energyRegenMultiplier = triangleBaseEnergyRegen + (triangleRegenPerStack * (stackCount - 1));
        _energyRegenEndTime = Time.time + triangleBuffDuration;

        // At max stacks, activate unlimited energy and movement boost
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
        // Regeneration
        if (Time.time < _regenEndTime)
        {
            _playerCharacter.Heal(_regenRate * deltaTime);
        }

        // Damage resistance expires
        if (Time.time >= _damageResistanceEndTime)
        {
            _damageResistanceAmount = 0f;
        }

        // Reflection expires
        if (Time.time >= _reflectionEndTime)
        {
            _isReflecting = false;
        }

        // Energy regen expires
        if (Time.time >= _energyRegenEndTime)
        {
            _energyRegenMultiplier = 1f;
        }

        // Unlimited energy expires
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

        if (!successful)
        {
            // Failed counter, apply cooldown
            _isOnCooldown = true;
            _cooldownTimer = counterCooldown;
        }
        // Successful counter has no cooldown
    }

    // Public getters for buff modifiers
    public float GetDamageResistance() => _damageResistanceAmount;
    public bool IsReflecting() => _isReflecting;
    public float GetDamageMultiplier()
    {
        float multiplier = _nextAttackDamageMultiplier;
        if (multiplier > 1f)
        {
            _nextAttackDamageMultiplier = 1f; // Reset after retrieving (consumed on next attack)
        }
        return multiplier;
    }
    public float GetDamageReceivedMultiplier()
    {
        // Only apply increased damage taken if we have Saw stacks active
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

    // Reset chain attack flag after use
    public void ConsumeChainAttack()
    {
        _hasChainAttack = false;
    }

    // Check if a hitscan attack should be countered (called before damage is applied)
    public bool TryCounterHitscan(WaveformData waveformType, CombatEntity attacker, out bool applyEffect)
    {
        applyEffect = false;

        if (!_isCounterActive)
            return false;

        // Check if player has matching waveform equipped
        if (_playerCharacter.equippedWaveform != waveformType)
        {
            Debug.Log($"Cannot counter hitscan: Equipped {_playerCharacter.equippedWaveform?.name ?? "null"} but attack is {waveformType?.name ?? "null"}");
            return false;
        }

        // Don't counter own attacks
        if (attacker == _playerCharacter)
        {
            Debug.Log("Cannot counter: Own attack");
            return false;
        }

        // Successfully countered
        applyEffect = true;

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

        // Cap at max stacks
        if (currentStacks > maxStacksForChromaticEffect)
        {
            currentStacks = maxStacksForChromaticEffect;
            _stackCounts[waveformType] = maxStacksForChromaticEffect;
        }

        // Update current effect
        _currentEffect = new CounterEffect
        {
            waveformType = waveformType,
            activationTime = Time.time,
            stackCount = currentStacks
        };

        // Apply counter effect
        ApplyCounterEffect(waveformType, currentStacks);

        // Reset stacks after Chromatic
        if (currentStacks >= maxStacksForChromaticEffect)
        {
            _stackCounts[waveformType] = 0;
            Debug.Log($"Stack count reset to 0 after Chromatic activation");
        }

        // Successful counter, no cooldown
        EndCounterWindow(true);

        Debug.Log($"Countered {waveformType.name} hitscan attack! Stack count: {currentStacks}");

        return true;
    }

    // For debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, counterRadius);
    }
}