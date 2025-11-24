using UnityEngine;

[CreateAssetMenu(fileName = "New Waveform", menuName = "Combat/Waveform")]
public class WaveformData : ScriptableObject
{
    [Header("Identity")]
    public string waveformName;
    public Color waveformColor;
    public int unlockLevel = 1;

    [Header("Projectile Attack")]
    public bool isHitscan = false;
    public float hitscanRange = 100f;
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

    [Header("Decal Settings")]
    public Sprite decalSprite; // The sprite to use for impact decals
    public Color decalColor = Color.white; // Color tint for the decal
    public float decalSize = 0.5f; // Size of the decal
    public float decalLifetime = 10f; // How long the decal lasts before fading

    [Header("Homing Settings")]
    public bool enableHoming = false;
    [Tooltip("How strongly the projectile turns towards target (radians per second)")]
    public float homingStrength = 3f;
    [Tooltip("Max range to detect and home towards targets")]
    public float homingRange = 20f;

    [Header("Damage Ramp Settings")]
    public bool enableDamageRamp = false;
    [Tooltip("Base damage increase per consecutive hit")]
    public float damageRampPerHit = 2f;
    [Tooltip("Maximum damage ramp multiplier")]
    public float maxDamageRampMultiplier = 3f;
    [Tooltip("Time before damage ramp resets (seconds)")]
    public float damageRampResetTime = 3f;
}