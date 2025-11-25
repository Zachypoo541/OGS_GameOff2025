using UnityEngine;
using KinematicCharacterController;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerLocomotion))]
[RequireComponent(typeof(CounterSystem))]
public class PlayerCharacter : CombatEntity, ICharacterController
{
    [Header("Locomotion Components")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;

    [Header("Body Settings")]
    [SerializeField] private float standHeight = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    [Header("Camera Shake")]
    [SerializeField] private float damageShakeIntensity = 0.3f;
    [SerializeField] private float damageShakeScaling = 0.01f;

    [Header("Hand Animations")]
    [SerializeField] private HandAnimationController handAnimationController;

    // Component references
    private PlayerInput _playerInput;
    private PlayerCombat _playerCombat;
    private PlayerLocomotion _playerLocomotion;
    private CounterSystem _counterSystem;

    // State tracking
    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;
    private Collider[] _uncrouchOverlapResults;

    public void Initialize(Transform cameraTransform, Reticle reticle = null)
    {
        // Get required components (they should already exist due to RequireComponent)
        _playerInput = GetComponent<PlayerInput>();
        _playerCombat = GetComponent<PlayerCombat>();
        _playerLocomotion = GetComponent<PlayerLocomotion>();
        _counterSystem = GetComponent<CounterSystem>();

        if (_playerInput == null || _playerCombat == null || _playerLocomotion == null)
        {
            Debug.LogError("Missing required player components! Make sure PlayerInput, PlayerCombat, and PlayerLocomotion are attached.");
            return;
        }

        if (_counterSystem == null)
        {
            Debug.LogWarning("CounterSystem component not found on PlayerCharacter. Counter mechanics will not work.");
        }

        // Warn if HandAnimationController is not assigned
        if (handAnimationController == null)
        {
            Debug.LogWarning("HandAnimationController not assigned in Inspector. Hand animations will not play.");
        }

        // Initialize components
        _playerCombat.Initialize(cameraTransform, reticle, _counterSystem, this, projectileSpawnPoint, handAnimationController);
        _playerLocomotion.Initialize(motor, cameraTransform, this, _counterSystem, _playerInput);

        // Initialize state
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _uncrouchOverlapResults = new Collider[8];
        motor.CharacterController = this;

        // Initialize combat system
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
    }

    protected override void Update()
    {
        base.Update();
        _playerLocomotion.UpdateLocomotion(Time.deltaTime);

        if (_counterSystem != null && _counterSystem.HasUnlimitedEnergy())
        {
            currentEnergy = maxEnergy;
        }
    }

    public void UpdateInput(CharacterInput input)
    {
        _playerInput.UpdateInput(input);
    }

    public void UpdateCombatInput(CombatInput input)
    {
        _playerCombat.UpdateCombatInput(input);
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(
            _playerInput.RequestedRotation * Vector3.forward,
            motor.CharacterUp
        );

        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;

        var cameraTargetHeight = currentHeight * (
            _state.Stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight
        );

        cameraTarget.localPosition = Vector3.Lerp(
            a: cameraTarget.localPosition,
            b: new Vector3(0f, cameraTargetHeight, 0f),
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Check for slide transition
        if (_playerLocomotion.ShouldTransitionToSlide(_state, _lastState))
        {
            _state.Stance = Stance.Slide;
        }

        // Update velocity through locomotion component
        _playerLocomotion.UpdateVelocity(ref currentVelocity, deltaTime, _state, _lastState);

        // Check for slide exit
        if (_state.Stance is Stance.Slide && _playerLocomotion.ShouldExitSlide(currentVelocity))
        {
            _state.Stance = Stance.Crouch;
        }
    }

    protected override void ApplyDash()
    {
        _playerLocomotion.ApplyDash();
    }

    public override void TakeDamage(float amount, WaveformData sourceWaveform, CombatEntity attacker = null)
    {
        // Check for projectile counter FIRST (before any other counter logic)
        if (_counterSystem != null && sourceWaveform != null && attacker != null)
        {
            // Try to find the projectile component on the attacker
            WaveformProjectile projectile = attacker.GetComponentInChildren<WaveformProjectile>();
            if (projectile == null)
            {
                // The attacker might BE the projectile in some collision scenarios
                projectile = (attacker as MonoBehaviour)?.GetComponent<WaveformProjectile>();
            }

            if (projectile != null && _counterSystem.TryCounterProjectile(projectile))
            {
                Debug.Log("Projectile countered!");
                return; // Exit early, don't take damage
            }
        }

        // Now check hitscan counter
        if (_counterSystem != null && sourceWaveform != null)
        {
            bool shouldApplyEffect;
            if (_counterSystem.TryCounterHitscan(sourceWaveform, attacker, out shouldApplyEffect))
            {
                Debug.Log("Countered hitscan attack!");
                return;
            }
        }

        // Rest of your existing TakeDamage code...
        if (_counterSystem != null && _counterSystem.IsReflecting() && attacker != null)
        {
            Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;

            if (sourceWaveform != null && sourceWaveform.projectilePrefab != null && projectileSpawnPoint != null)
            {
                GameObject reflectedProj = Instantiate(
                    sourceWaveform.projectilePrefab,
                    projectileSpawnPoint.position,
                    Quaternion.LookRotation(directionToAttacker)
                );

                WaveformProjectile projectile = reflectedProj.GetComponent<WaveformProjectile>();
                if (projectile != null)
                {
                    projectile.Initialize(amount, sourceWaveform, this, directionToAttacker, null);
                }

                Debug.Log($"Reflected {sourceWaveform.name} projectile back to {attacker.name}!");
            }

            return;
        }

        if (_counterSystem != null)
        {
            float resistance = _counterSystem.GetDamageResistance();
            amount *= (1f - resistance);
            amount *= _counterSystem.GetDamageReceivedMultiplier();
        }

        // Trigger camera shake based on damage amount
        float shakeAmount = damageShakeIntensity + (amount * damageShakeScaling);
        if (Player.Instance != null)
        {
            Player.Instance.TriggerCameraShake(shakeAmount);
        }

        base.TakeDamage(amount, sourceWaveform, attacker);
    }

    public override void TakeDamage(float amount, CombatEntity attacker = null)
    {
        if (_counterSystem != null && _counterSystem.IsReflecting())
        {
            Debug.Log("Reflecting damage (no projectile - direct damage source)");
            return;
        }

        if (_counterSystem != null)
        {
            float resistance = _counterSystem.GetDamageResistance();
            amount *= (1f - resistance);
            amount *= _counterSystem.GetDamageReceivedMultiplier();
        }

        // Trigger camera shake based on damage amount
        float shakeAmount = damageShakeIntensity + (amount * damageShakeScaling);
        if (Player.Instance != null)
        {
            Player.Instance.TriggerCameraShake(shakeAmount);
        }

        base.TakeDamage(amount, attacker);
    }

    protected override float GetEnergyRegenRate()
    {
        float baseRegen = base.GetEnergyRegenRate();

        if (_counterSystem != null)
        {
            baseRegen *= _counterSystem.GetEnergyRegenMultiplier();
        }

        return baseRegen;
    }

    public void UnlockWaveform(WaveformData waveform)
    {
        _playerCombat.UnlockWaveform(waveform);
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player died!");
    }

    public Transform GetCameraTarget() => cameraTarget;

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;

        if (_playerInput.RequestedCrouch && _state.Stance is Stance.Stand)
        {
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        if (!_playerInput.RequestedCrouch && _state.Stance is not Stance.Stand)
        {
            motor.SetCapsuleDimensions(
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );

            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if (motor.CharacterOverlap(pos, rot, _uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                _playerInput.SetRequestedCrouch(true);
                motor.SetCapsuleDimensions(
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
            }
            else
            {
                _state.Stance = Stance.Stand;
            }
        }

        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;
        _lastState = _tempState;
    }

    public void SetPosition(Vector3 position, bool killVelocity = true)
    {
        motor.SetPosition(position);
        if (killVelocity)
        {
            motor.BaseVelocity = Vector3.zero;
            _playerLocomotion.ResetDashVelocity();
        }
    }

    public Transform GetMotorTransform() => motor.transform;

    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
            _state.Stance = Stance.Crouch;
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Check setup
        if (motor != null && motor.Capsule != null)
        {
            Debug.Log($"PlayerCharacter collider layer: {LayerMask.LayerToName(motor.Capsule.gameObject.layer)}");
            Debug.Log($"PlayerCharacter collider is trigger: {motor.Capsule.isTrigger}");
        }
    }
#endif
}