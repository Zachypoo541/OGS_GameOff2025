using UnityEngine;

public enum StatusEffectType
{
    None,
    Healing,
    DamageResistance,
    DamageBoost,
    EnergyRegenBoost
}

[System.Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public float strength;
    public float duration;
    public float timeRemaining;

    public StatusEffect(StatusEffectType type, float strength, float duration)
    {
        this.type = type;
        this.strength = strength;
        this.duration = duration;
        this.timeRemaining = duration;
    }

    public bool Update(float deltaTime)
    {
        timeRemaining -= deltaTime;
        return timeRemaining <= 0;
    }
}