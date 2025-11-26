using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "New Waveform", menuName = "Combat/Waveform")]
[System.Serializable]
public class VideoClipWithSpeed
{
    public VideoClip clip;
    [Range(0.1f, 3f)]
    public float playbackSpeed = 1f;
}
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

    [Header("Passive Modifiers")]
    [Tooltip("Gravity multiplier applied while this waveform is equipped")]
    public float gravityMultiplier = 1f;
    [Tooltip("Movement acceleration multiplier while this waveform is equipped")]
    public float accelerationMultiplier = 1f;

    [Header("Self-Cast Ability")]
    public SelfCastType selfCastType = SelfCastType.None;
    public float selfCastEnergyCost = 20f;
    public float selfCastCooldown = 2f;

    [Header("Dash Ability (Saw Wave)")]
    [Tooltip("If true, this waveform grants a dash ability")]
    public bool usesDash = false;
    public float dashForce = 20f;
    public float dashDecayRate = 0.9f;

    [Header("Sine Wave (Reduced Gravity)")]
    [Tooltip("Gravity multiplier during reduced gravity effect")]
    public float reducedGravityMultiplier = 0.3f;
    [Tooltip("Duration of reduced gravity effect in seconds")]
    public float reducedGravityDuration = 3f;

    [Header("Square Wave (Thrust)")]
    [Tooltip("Force applied each frame while thrust is active")]
    public float thrustForce = 15f;

    [Header("Triangle Wave (Double Jump)")]
    [Tooltip("If true, grants a double jump buff when activated")]
    public bool grantsDoubleJump = false;

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

    [Header("Hand Animations")]
    public WaveformHandAnimations handAnimations;

    [Header("Self-Cast Hand Animations")]
    [Tooltip("Random selection between these two clips for single-use abilities (Sine, Saw, Triangle)")]
    public VideoClipWithSpeed[] selfCastAnimations = new VideoClipWithSpeed[2];

    [Header("Square Wave Loop Animations")]
    [Tooltip("Played once when thrust starts")]
    public VideoClipWithSpeed selfCastEnterAnimation;
    [Tooltip("Looped while thrust is held")]
    public VideoClipWithSpeed selfCastLoopAnimation;
    [Tooltip("Played once when thrust ends")]
    public VideoClipWithSpeed selfCastExitAnimation;


}
public enum SelfCastType
{
    None,
    ReducedGravity, // Sine
    Thrust,         // Square
    Dash,           // Saw
    DoubleJump      // Triangle
}


[System.Serializable]
public class WaveformHandAnimations
{
    [Header("Right Hand Waveform Animations")]
    public VideoClip enter;
    public VideoClip idle;
    public VideoClip fire;
    public VideoClip exit;
}