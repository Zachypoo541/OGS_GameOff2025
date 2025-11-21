using UnityEngine;
using System.Collections.Generic;

public class CombatEntity : MonoBehaviour
{
    [Header("Resources")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float baseEnergyRegenRate = 10f;

    [Header("Combat")]
    public WaveformData equippedWaveform;
    public Transform projectileSpawnPoint;

    [Header("Immunity")]
    public List<WaveformData> immuneToWaveforms = new List<WaveformData>();

    // Active effects
    protected List<StatusEffect> activeEffects = new List<StatusEffect>();

    // Ramping damage tracking
    private Dictionary<WaveformData, int> rampStacks = new Dictionary<WaveformData, int>();
    private Dictionary<WaveformData, float> rampTimers = new Dictionary<WaveformData, float>();
    private const float RAMP_RESET_TIME = 2f;

    // Movement modifiers
    protected float currentGravityMod = 1f;
    protected float currentAccelMod = 1f;
    protected bool isUsingConstantVelocity = false;
    protected float constantVelSpeed = 0f;
    protected Vector3 constantVelDirection;

    // Events
    public System.Action<float, float> OnHealthChanged;
    public System.Action<float, float> OnEnergyChanged;
    public System.Action OnDeath;
    public System.Action<StatusEffect> OnStatusEffectAdded;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
    }

    protected virtual void Update()
    {
        RegenerateEnergy();
        UpdateStatusEffects();
        UpdateRampTimers();
    }

    // ========================================
    // RESOURCE MANAGEMENT
    // ========================================

    protected virtual void RegenerateEnergy()
    {
        if (currentEnergy < maxEnergy)
        {
            float regenRate = GetEnergyRegenRate();
            currentEnergy = Mathf.Min(currentEnergy + regenRate * Time.deltaTime, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }
    }

    protected virtual float GetEnergyRegenRate()
    {
        return baseEnergyRegenRate * GetEnergyRegenMultiplier();
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public virtual void TakeDamage(float damage, WaveformData sourceWaveform, CombatEntity attacker = null)
    {
        // Check immunity
        if (IsImmuneTo(sourceWaveform))
        {
            TriggerCounterEffect(sourceWaveform);
            return;
        }

        // Apply damage resistance
        float damageResist = GetDamageResistance();
        float finalDamage = damage * (1f - damageResist);

        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Overload for Counter system (allows passing attacker without waveform)
    public virtual void TakeDamage(float damage, CombatEntity attacker = null)
    {
        // Apply damage resistance
        float damageResist = GetDamageResistance();
        float finalDamage = damage * (1f - damageResist);

        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke();
    }

    // ========================================
    // ATTACK SYSTEM
    // ========================================

    public virtual bool CanUseAttack()
    {
        if (equippedWaveform == null) return false;

        // Check energy requirement
        if (currentEnergy >= equippedWaveform.energyCost) return true;

        // Check health fallback for Red waveform
        if (equippedWaveform.usesHealthWhenEnergyEmpty && currentEnergy < equippedWaveform.energyCost)
        {
            return currentHealth > equippedWaveform.healthCostWhenEmpty;
        }

        return false;
    }

    public virtual void FireProjectile(Vector3 direction)
    {
        if (!CanUseAttack()) return;

        // Calculate damage (with ramping if applicable)
        float damage = CalculateDamage();

        // Consume resources
        ConsumeAttackResources();

        // Spawn projectile
        if (equippedWaveform.projectilePrefab != null && projectileSpawnPoint != null)
        {
            GameObject proj = Instantiate(equippedWaveform.projectilePrefab,
                projectileSpawnPoint.position,
                Quaternion.LookRotation(direction));

            WaveformProjectile projectile = proj.GetComponent<WaveformProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(damage, equippedWaveform, this, direction);
            }
        }
    }

    protected float CalculateDamage()
    {
        float damage = equippedWaveform.baseDamage;

        // Apply ramping damage
        if (equippedWaveform.hasRampingDamage)
        {
            if (!rampStacks.ContainsKey(equippedWaveform))
                rampStacks[equippedWaveform] = 0;

            int stacks = rampStacks[equippedWaveform];
            float rampPercent = (float)stacks / equippedWaveform.rampStacks;
            damage = Mathf.Lerp(equippedWaveform.baseDamage, equippedWaveform.maxRampedDamage, rampPercent);

            // Increment stacks
            rampStacks[equippedWaveform] = Mathf.Min(stacks + 1, equippedWaveform.rampStacks);
            rampTimers[equippedWaveform] = RAMP_RESET_TIME;
        }

        // Apply damage boost from status effects
        damage *= GetDamageMultiplier();

        return damage;
    }

    protected void ConsumeAttackResources()
    {
        float energyCost = equippedWaveform.energyCost;

        // Handle ramping energy cost
        if (equippedWaveform.hasRampingDamage && rampStacks.ContainsKey(equippedWaveform))
        {
            int stacks = rampStacks[equippedWaveform];
            float costMultiplier = 1f + (stacks * 0.5f);
            energyCost *= costMultiplier;
        }

        if (currentEnergy >= energyCost)
        {
            currentEnergy -= energyCost;
        }
        else if (equippedWaveform.usesHealthWhenEnergyEmpty)
        {
            currentHealth -= equippedWaveform.healthCostWhenEmpty;
            currentEnergy = 0;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    private void UpdateRampTimers()
    {
        List<WaveformData> toRemove = new List<WaveformData>();

        foreach (var kvp in rampTimers)
        {
            rampTimers[kvp.Key] -= Time.deltaTime;
            if (rampTimers[kvp.Key] <= 0)
            {
                rampStacks[kvp.Key] = 0;
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            rampTimers.Remove(key);
        }
    }

    // ========================================
    // COUNTER SYSTEM
    // ========================================

    public bool IsImmuneTo(WaveformData waveform)
    {
        return immuneToWaveforms.Contains(waveform);
    }

    protected void TriggerCounterEffect(WaveformData waveform)
    {
        StatusEffect effect = new StatusEffect(
            waveform.counterEffectType,
            waveform.counterEffectStrength,
            waveform.counterEffectDuration
        );

        AddStatusEffect(effect);

        // Immediate effects
        if (effect.type == StatusEffectType.Healing)
        {
            Heal(effect.strength);
        }
    }

    // ========================================
    // STATUS EFFECTS
    // ========================================

    public void AddStatusEffect(StatusEffect effect)
    {
        // Check if effect already exists, refresh duration
        StatusEffect existing = activeEffects.Find(e => e.type == effect.type);
        if (existing != null)
        {
            existing.timeRemaining = effect.duration;
            existing.strength = Mathf.Max(existing.strength, effect.strength);
        }
        else
        {
            activeEffects.Add(effect);
        }

        OnStatusEffectAdded?.Invoke(effect);
    }

    protected void UpdateStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Update(Time.deltaTime))
            {
                activeEffects.RemoveAt(i);
            }
        }
    }

    protected float GetDamageResistance()
    {
        float resist = 0f;
        foreach (var effect in activeEffects)
        {
            if (effect.type == StatusEffectType.DamageResistance)
                resist += effect.strength;
        }
        return Mathf.Clamp01(resist);
    }

    protected float GetDamageMultiplier()
    {
        float mult = 1f;
        foreach (var effect in activeEffects)
        {
            if (effect.type == StatusEffectType.DamageBoost)
                mult += effect.strength;
        }
        return mult;
    }

    protected float GetEnergyRegenMultiplier()
    {
        float mult = 1f;
        foreach (var effect in activeEffects)
        {
            if (effect.type == StatusEffectType.EnergyRegenBoost)
                mult += effect.strength;
        }
        return mult;
    }

    // ========================================
    // MOVEMENT MODIFIERS
    // ========================================

    public void ApplySelfModifier()
    {
        if (equippedWaveform == null) return;

        currentGravityMod = equippedWaveform.gravityMultiplier;
        currentAccelMod = equippedWaveform.accelerationMultiplier;

        if (equippedWaveform.usesConstantVelocity)
        {
            isUsingConstantVelocity = true;
            constantVelSpeed = equippedWaveform.constantVelocitySpeed;
        }

        if (equippedWaveform.usesDash)
        {
            ApplyDash();
        }
    }

    protected virtual void ApplyDash()
    {
        // Override in child classes
    }

    public float GetGravityMultiplier() => currentGravityMod;
    public float GetAccelerationMultiplier() => currentAccelMod;
}