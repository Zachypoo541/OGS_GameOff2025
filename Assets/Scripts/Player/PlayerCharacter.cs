using UnityEngine;
using KinematicCharacterController;
using System.Collections.Generic;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide
}

public struct CharacterState
{
    public bool Grounded;
    public Stance Stance;
    public Vector3 Velocity;
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public struct CombatInput
{
    public bool Fire;
    public bool SelfModifier;
    public bool NextWaveform;
    public bool PrevWaveform;
    public bool Counter;
}

public class PlayerCharacter : CombatEntity, ICharacterController
{
    [Header("Locomotion Components")]
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [Space]
    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = 90f;
    [Space]
    [SerializeField] private float standHeight = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;

    [Header("Player Combat Settings")]
    public List<WaveformData> unlockedWaveforms = new List<WaveformData>();
    public int currentWaveformIndex = 0;

    [Header("Dash Settings")]
    [SerializeField] private float dashDecayRate = 0.85f;
    [SerializeField] private float minDashSpeed = 0.5f;

    [Header("Aiming Settings")]
    [SerializeField] private float maxAimDistance = 1000f;
    [SerializeField] private LayerMask aimRaycastMask = -1; // Set in inspector to exclude player layer

    // Locomotion state
    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;

    // Input tracking
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;

    // Jump timing
    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;

    // Dash velocity (separate from main velocity)
    private Vector3 _dashVelocity;

    // Camera reference
    private Transform _cameraTransform;
    private Reticle _reticle;

    // Collider overlap detection
    private Collider[] _uncrouchOverlapResults;

    // Counter system reference
    private CounterSystem _counterSystem;

    public void Initialize(Transform cameraTransform, Reticle reticle = null)
    {
        _cameraTransform = cameraTransform;
        _reticle = reticle;

        _state.Stance = Stance.Stand;
        _lastState = _state;
        _uncrouchOverlapResults = new Collider[8];
        motor.CharacterController = this;

        // Get counter system component
        _counterSystem = GetComponent<CounterSystem>();
        if (_counterSystem == null)
        {
            Debug.LogWarning("CounterSystem component not found on PlayerCharacter. Counter mechanics will not work.");
        }

        // Initialize combat system
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;

        if (unlockedWaveforms.Count > 0)
        {
            EquipWaveform(0);
        }

        // Update reticle to match starting waveform
        if (_reticle != null && equippedWaveform != null)
        {
            _reticle.UpdateReticleForWaveform(equippedWaveform);
        }
    }

    protected override void Update()
    {
        base.Update(); // Handles energy regen, status effects, ramp timers
        UpdateDashVelocity();

        // Apply unlimited energy from counter system
        if (_counterSystem != null && _counterSystem.HasUnlimitedEnergy())
        {
            currentEnergy = maxEnergy; // Keep energy at max
        }
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;

        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;

        var wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if (_requestedJump && !wasRequestingJump)
            _timeSinceJumpRequest = 0f;

        _requestedSustainedJump = input.JumpSustain;

        var wasRequestingCrouch = _requestedCrouch;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch,
        };
        if (_requestedCrouch && !wasRequestingCrouch)
            _requestedCrouchInAir = !_state.Grounded;
        else if (!_requestedCrouch && wasRequestingCrouch)
            _requestedCrouchInAir = false;
    }

    public void UpdateCombatInput(CombatInput input)
    {
        // Handle counter input
        if (input.Counter && _counterSystem != null)
        {
            _counterSystem.AttemptCounter();
        }

        // Switch waveforms
        if (input.NextWaveform)
        {
            CycleWaveform(1);
        }
        if (input.PrevWaveform)
        {
            CycleWaveform(-1);
        }

        // Fire projectile with raycast-based aiming
        if (input.Fire && _cameraTransform != null)
        {
            Vector3 aimDir = GetAimDirection();
            FireProjectile(aimDir);
        }

        // Self modifier (includes dash)
        if (input.SelfModifier)
        {
            ApplySelfModifier();
        }
    }

    private Vector3 GetAimDirection()
    {
        // Raycast from screen center (where reticle is) through camera
        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);

        Vector3 targetPoint;

        // Raycast to find what we're aiming at
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimRaycastMask))
        {
            // Hit something - aim at that point
            targetPoint = hit.point;
        }
        else
        {
            // Didn't hit anything - aim at a point far away
            targetPoint = ray.origin + ray.direction * maxAimDistance;
        }

        // Calculate direction from projectile spawn point to target point
        Vector3 direction = (targetPoint - projectileSpawnPoint.position).normalized;

        return direction;
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane(
            _requestedRotation * Vector3.forward,
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
        // Apply acceleration modifier from combat system
        float accelModifier = GetAccelerationMultiplier();

        // Apply movement speed modifier from counter system
        if (_counterSystem != null)
        {
            accelModifier *= _counterSystem.GetMovementSpeedMultiplier();
        }

        // If on the ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _ungroundedDueToJump = false;
            _timeSinceUngrounded = 0f;

            var groundedMovement = motor.GetDirectionTangentToSurface(
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            // Start sliding
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = _state.Stance is Stance.Crouch;
                var wasStanding = _lastState.Stance is Stance.Stand;
                var wasInAir = !_lastState.Grounded;
                if (moving && crouching && (wasStanding || wasInAir))
                {
                    _state.Stance = Stance.Slide;

                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(
                            vector: _lastState.Velocity,
                            planeNormal: motor.GroundingStatus.GroundNormal
                        );
                    }

                    var effectiveSlideStartSpeed = slideStartSpeed;
                    if (!_lastState.Grounded && !_requestedCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        _requestedCrouchInAir = false;
                    }

                    var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface(
                        direction: currentVelocity.normalized,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                    ) * slideSpeed;
                }
            }

            // Walk/crouch
            if (_state.Stance is Stance.Stand or Stance.Crouch)
            {
                var speed = _state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;
                var response = _state.Stance is Stance.Stand ? walkResponse : crouchResponse;

                // Apply acceleration modifier
                speed *= accelModifier;

                var targetVelocity = groundedMovement * speed;
                currentVelocity = Vector3.Lerp(
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );
            }

            // Slide
            if (_state.Stance is Stance.Slide)
            {
                var planarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);

                var slopeAcceleration = Vector3.ProjectOnPlane(
                    -motor.CharacterUp * slideGravity,
                    motor.GroundingStatus.GroundNormal
                );

                var steerForce = groundedMovement * slideSteerAcceleration * deltaTime;
                steerForce = Vector3.ProjectOnPlane(steerForce, planarVelocity.normalized);

                currentVelocity += slopeAcceleration * deltaTime;
                currentVelocity += steerForce;
                currentVelocity *= Mathf.Pow(slideFriction, deltaTime);

                if (currentVelocity.magnitude < slideEndSpeed)
                    _state.Stance = Stance.Crouch;
            }
        }
        // In the air...
        else
        {
            _timeSinceUngrounded += deltaTime;

            if (_requestedMovement.sqrMagnitude > 0f)
            {
                var planarMovement = Vector3.ProjectOnPlane(
                    vector: _requestedMovement,
                    planeNormal: motor.CharacterUp
                ) * _requestedMovement.magnitude;

                var currentPlanarVelocity = Vector3.ProjectOnPlane(
                    vector: currentVelocity,
                    planeNormal: motor.CharacterUp
                );

                var movementForce = planarMovement * airAcceleration * accelModifier * deltaTime;

                if (currentPlanarVelocity.magnitude < airSpeed)
                {
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                {
                    var constrainedMovementForce = Vector3.ProjectOnPlane(
                        vector: movementForce,
                        planeNormal: currentPlanarVelocity.normalized
                    );
                    movementForce = constrainedMovementForce;
                }

                if (motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        var obstructionNormal = Vector3.Cross(
                            motor.CharacterUp,
                            motor.GroundingStatus.GroundNormal
                        ).normalized;
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }

            // Apply gravity with modifier from combat system
            var effectiveGravity = gravity * GetGravityMultiplier();
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedSustainedJump && verticalSpeed > 0f)
                effectiveGravity *= jumpSustainGravity;

            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }

        // Apply dash velocity
        if (_dashVelocity.magnitude > minDashSpeed)
        {
            currentVelocity += _dashVelocity;
        }

        // Handle jump
        if (_requestedJump)
        {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;

            if (grounded || canCoyoteJump)
            {
                _requestedJump = false;
                _requestedCrouch = false;
                _requestedCrouchInAir = false;

                motor.ForceUnground(time: 0.1f);
                _ungroundedDueToJump = true;

                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                _timeSinceJumpRequest += deltaTime;
                var canJumpLater = _timeSinceJumpRequest < coyoteTime;
                _requestedJump = canJumpLater;
            }
        }
    }

    private void UpdateDashVelocity()
    {
        if (_dashVelocity.magnitude > minDashSpeed)
        {
            float decayRate = equippedWaveform?.dashDecayRate ?? dashDecayRate;
            _dashVelocity *= decayRate;
        }
        else
        {
            _dashVelocity = Vector3.zero;
        }
    }

    protected override void ApplyDash()
    {
        if (equippedWaveform == null || _cameraTransform == null) return;

        Vector3 dashDir = _cameraTransform.forward;
        dashDir.y = 0; // Keep dash horizontal
        _dashVelocity = dashDir.normalized * equippedWaveform.dashForce;
    }

    // Waveform management
    public void CycleWaveform(int direction)
    {
        if (unlockedWaveforms.Count == 0) return;

        currentWaveformIndex = (currentWaveformIndex + direction) % unlockedWaveforms.Count;
        if (currentWaveformIndex < 0) currentWaveformIndex = unlockedWaveforms.Count - 1;

        EquipWaveform(currentWaveformIndex);
        Debug.Log($"Switched to waveform: {equippedWaveform.name}");
    }

    public void EquipWaveform(int index)
    {
        if (index >= 0 && index < unlockedWaveforms.Count)
        {
            equippedWaveform = unlockedWaveforms[index];
            currentWaveformIndex = index;

            // Don't add to immunity list - counter system handles this
            immuneToWaveforms.Clear();

            // Update reticle to match new waveform
            if (_reticle != null)
            {
                _reticle.UpdateReticleForWaveform(equippedWaveform);
            }
        }
    }

    public override void FireProjectile(Vector3 direction)
    {
        if (!CanUseAttack()) return;

        // Calculate damage (with ramping if applicable)
        float damage = CalculateDamage();

        // Apply damage multiplier from counter system
        if (_counterSystem != null)
        {
            damage *= _counterSystem.GetDamageMultiplier();
        }

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
                // Check if we have chain attack buff
                bool enableChain = _counterSystem != null && _counterSystem.HasChainAttack();
                float chainRange = enableChain ? _counterSystem.GetChainRange() : 0f;

                // Pass reticle reference to projectile (only player has reticle)
                projectile.Initialize(damage, equippedWaveform, this, direction, _reticle, enableChain, chainRange);

                // If chain attack was used, consume it
                if (enableChain)
                {
                    _counterSystem.ConsumeChainAttack();
                }

                // Trigger fire animation on reticle (only if projectile was spawned)
                if (_reticle != null)
                {
                    _reticle.OnFire();
                }
            }
        }
    }

    public override void TakeDamage(float amount, WaveformData sourceWaveform, CombatEntity attacker = null)
    {
        // Apply damage resistance from counter system
        if (_counterSystem != null)
        {
            float resistance = _counterSystem.GetDamageResistance();
            amount *= (1f - resistance);

            // Check for reflection
            if (_counterSystem.IsReflecting() && attacker != null)
            {
                // Reflect damage back to attacker
                attacker.TakeDamage(amount, this);
                Debug.Log($"Reflected {amount} damage back to {attacker.name}!");
                return; // Don't take damage ourselves
            }

            // Apply damage received multiplier (from Saw stacks)
            amount *= _counterSystem.GetDamageReceivedMultiplier();
        }

        base.TakeDamage(amount, sourceWaveform, attacker);
    }

    public override void TakeDamage(float amount, CombatEntity attacker = null)
    {
        // Apply damage resistance from counter system
        if (_counterSystem != null)
        {
            float resistance = _counterSystem.GetDamageResistance();
            amount *= (1f - resistance);

            // Check for reflection
            if (_counterSystem.IsReflecting() && attacker != null)
            {
                // Reflect damage back to attacker
                attacker.TakeDamage(amount, this);
                Debug.Log($"Reflected {amount} damage back to {attacker.name}!");
                return; // Don't take damage ourselves
            }

            // Apply damage received multiplier (from Saw stacks)
            amount *= _counterSystem.GetDamageReceivedMultiplier();
        }

        base.TakeDamage(amount, attacker);
    }

    protected override float GetEnergyRegenRate()
    {
        float baseRegen = base.GetEnergyRegenRate();

        // Apply energy regen multiplier from counter system
        if (_counterSystem != null)
        {
            baseRegen *= _counterSystem.GetEnergyRegenMultiplier();
        }

        return baseRegen;
    }

    public void UnlockWaveform(WaveformData waveform)
    {
        if (!unlockedWaveforms.Contains(waveform))
        {
            unlockedWaveforms.Add(waveform);
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player died!");
        // TODO: Implement respawn or restart logic here
    }

    // ICharacterController interface methods
    public Transform GetCameraTarget() => cameraTarget;

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;

        if (_requestedCrouch && _state.Stance is Stance.Stand)
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
        if (!_requestedCrouch && _state.Stance is not Stance.Stand)
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
                _requestedCrouch = true;
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
            _dashVelocity = Vector3.zero;
        }
    }

    // Helper to get the motor's transform (for AI targeting)
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
}