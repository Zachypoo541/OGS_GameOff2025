using UnityEngine;

[CreateAssetMenu(fileName = "New Waveform Info", menuName = "UI/Waveform Info Data")]
public class WaveformInfoData : ScriptableObject
{
    [Header("Associated Waveform")]
    [Tooltip("The actual WaveformData that gets unlocked when player completes this tutorial")]
    public WaveformData waveformToUnlock;

    [Tooltip("Wave index to start after clicking Continue (0 = first wave, 1 = second wave, etc.)")]
    [Min(0)]
    public int waveToStart = 1;

    [Header("Basic Info")]
    public string waveformName;
    public Color waveformColor = Color.white;

    [Header("Description")]
    [TextArea(3, 6)]
    public string flavorText;

    [Header("Shoot Details")]
    [TextArea(2, 4)]
    public string shootDetailsText;

    public DamageLevel damageLevel;
    public ProjectileType projectileType;

    [Header("Effects")]
    [TextArea(2, 4)]
    public string selfCastEffect;
    [TextArea(2, 4)]
    public string counterEffect;

    public enum DamageLevel
    {
        Low,
        Medium,
        High,
        Ramping
    }

    public enum ProjectileType
    {
        Projectile,
        Hitscan,
        Homing
    }

    public string GetDamageString()
    {
        switch (damageLevel)
        {
            case DamageLevel.Low: return "Low";
            case DamageLevel.Medium: return "Med";
            case DamageLevel.High: return "High";
            case DamageLevel.Ramping: return "Ramping";
            default: return "Unknown";
        }
    }

    public string GetProjectileTypeString()
    {
        switch (projectileType)
        {
            case ProjectileType.Projectile: return "Projectile";
            case ProjectileType.Hitscan: return "Hitscan";
            case ProjectileType.Homing: return "Homing";
            default: return "Unknown";
        }
    }
}