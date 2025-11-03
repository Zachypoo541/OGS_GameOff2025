using UnityEngine;

[CreateAssetMenu(fileName = "New Waveform", menuName = "Combat/Waveform")]
public class WaveformData : ScriptableObject
{
    [Header("Identity")]
    public string waveformName;
    public Color waveformColor;
    public int unlockLevel = 1;

    [Header("Projectile Attack")]
    public float baseDamage = 10f;
    public float energyCost = 15f;
    public float projectileSpeed = 20f;
    public GameObject projectilePrefab;
    public float knockbackForce = 0f;

    [Header("Trail Effect")]
    public Material trailMaterial;
    public float trailWidth = 0.5f;
    public float trailLifetime = 0.5f;

    [Header("Special Mechanics")]
    public bool usesHealthWhenEnergyEmpty = false;
    public float healthCostWhenEmpty = 0f;
    public bool hasRampingDamage = false;
    public float maxRampedDamage = 35f;
    public int rampStacks = 3;

    [Header("Self Modifier")]
    public float gravityMultiplier = 1f;
    public float accelerationMultiplier = 1f;
    public bool usesConstantVelocity = false;
    public float constantVelocitySpeed = 10f;
    public bool usesDash = false;
    public float dashForce = 20f;
    public float dashDecayRate = 0.9f;

    [Header("Counter Effect")]
    public StatusEffectType counterEffectType;
    public float counterEffectStrength = 1f;
    public float counterEffectDuration = 3f;
}