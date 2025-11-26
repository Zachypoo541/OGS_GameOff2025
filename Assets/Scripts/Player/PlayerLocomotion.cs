using UnityEngine;
using KinematicCharacterController;

public class PlayerLocomotion : MonoBehaviour
{
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

    [Header("Dash Settings")]
    [SerializeField] private float dashDecayRate = 0.85f;
    [SerializeField] private float minDashSpeed = 0.5f;

    private KinematicCharacterMotor _motor;
    private Transform _cameraTransform;
    private CombatEntity _combatEntity;
    private CounterSystem _counterSystem;
    private PlayerInput _playerInput;
    private SelfCastController _selfCastController;

    private float _timeSinceUngrounded;
    private bool _ungroundedDueToJump;
    private Vector3 _dashVelocity;

    public void Initialize(KinematicCharacterMotor motor, Transform cameraTransform, CombatEntity combatEntity, CounterSystem counterSystem, PlayerInput playerInput, SelfCastController selfCastController)
    {
        _motor = motor;
        _cameraTransform = cameraTransform;
        _combatEntity = combatEntity;
        _counterSystem = counterSystem;
        _playerInput = playerInput;
        _selfCastController = selfCastController;
    }

    public void UpdateLocomotion(float deltaTime)
    {
        UpdateDashVelocity();
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterState state, CharacterState lastState)
    {
        float accelModifier = _combatEntity.GetAccelerationMultiplier();

        if (_counterSystem != null)
        {
            accelModifier *= _counterSystem.GetMovementSpeedMultiplier();
        }

        if (_motor.GroundingStatus.IsStableOnGround)
        {
            // IMPORTANT: Reset these flags FIRST before notifying self-cast controller
            _ungroundedDueToJump = false;
            _timeSinceUngrounded = 0f;

            // Notify self-cast controller that player landed
            if (_selfCastController != null)
            {
                _selfCastController.OnPlayerLanded();
            }

            var groundedMovement = _motor.GetDirectionTangentToSurface(
                direction: _playerInput.RequestedMovement,
                surfaceNormal: _motor.GroundingStatus.GroundNormal
            ) * _playerInput.RequestedMovement.magnitude;

            // Start sliding
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = state.Stance is Stance.Crouch;
                var wasStanding = lastState.Stance is Stance.Stand;
                var wasInAir = !lastState.Grounded;
                if (moving && crouching && (wasStanding || wasInAir))
                {
                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(
                            vector: lastState.Velocity,
                            planeNormal: _motor.GroundingStatus.GroundNormal
                        );
                    }

                    var effectiveSlideStartSpeed = slideStartSpeed;
                    if (!lastState.Grounded && !_playerInput.RequestedCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        _playerInput.SetRequestedCrouchInAir(false);
                    }

                    var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = _motor.GetDirectionTangentToSurface(
                        direction: currentVelocity.normalized,
                        surfaceNormal: _motor.GroundingStatus.GroundNormal
                    ) * slideSpeed;
                }
            }

            // Walk/crouch
            if (state.Stance is Stance.Stand or Stance.Crouch)
            {
                var speed = state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;
                var response = state.Stance is Stance.Stand ? walkResponse : crouchResponse;

                speed *= accelModifier;

                var targetVelocity = groundedMovement * speed;
                currentVelocity = Vector3.Lerp(
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );
            }

            // Slide
            if (state.Stance is Stance.Slide)
            {
                var planarVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);

                var slopeAcceleration = Vector3.ProjectOnPlane(
                    -_motor.CharacterUp * slideGravity,
                    _motor.GroundingStatus.GroundNormal
                );

                var steerForce = groundedMovement * slideSteerAcceleration * deltaTime;
                steerForce = Vector3.ProjectOnPlane(steerForce, planarVelocity.normalized);

                currentVelocity += slopeAcceleration * deltaTime;
                currentVelocity += steerForce;
                currentVelocity *= Mathf.Pow(slideFriction, deltaTime);

                if (currentVelocity.magnitude < slideEndSpeed)
                {
                    // Signal stance change (handled by PlayerCharacter)
                }
            }
        }
        else
        {
            _timeSinceUngrounded += deltaTime;

            if (_playerInput.RequestedMovement.sqrMagnitude > 0f)
            {
                var planarMovement = Vector3.ProjectOnPlane(
                    vector: _playerInput.RequestedMovement,
                    planeNormal: _motor.CharacterUp
                ) * _playerInput.RequestedMovement.magnitude;

                var currentPlanarVelocity = Vector3.ProjectOnPlane(
                    vector: currentVelocity,
                    planeNormal: _motor.CharacterUp
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

                if (_motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        var obstructionNormal = Vector3.Cross(
                            _motor.CharacterUp,
                            _motor.GroundingStatus.GroundNormal
                        ).normalized;
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }

            var effectiveGravity = gravity * _combatEntity.GetGravityMultiplier();
            var verticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
            if (_playerInput.RequestedSustainedJump && verticalSpeed > 0f)
                effectiveGravity *= jumpSustainGravity;

            currentVelocity += _motor.CharacterUp * effectiveGravity * deltaTime;
        }

        // Apply dash velocity
        if (_dashVelocity.magnitude > minDashSpeed)
        {
            currentVelocity += _dashVelocity;
        }

        // Apply thrust velocity from self-cast system
        if (_selfCastController != null)
        {
            _selfCastController.ApplyThrustVelocity(ref currentVelocity, deltaTime);
        }

        // Handle jump input
        if (_playerInput.RequestedJump)
        {
            var grounded = _motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;

            // Check for double jump capability
            var jumpCount = _selfCastController != null ? _selfCastController.GetJumpCount() : 0;
            var hasDoubleJump = _selfCastController != null && _selfCastController.HasDoubleJumpBuff();
            var canDoubleJump = !grounded && jumpCount == 1 && hasDoubleJump;

            if (grounded || canCoyoteJump || canDoubleJump)
            {
                // Successfully jumping - consume the request
                _playerInput.ConsumeJumpRequest();
                _playerInput.SetRequestedCrouch(false);
                _playerInput.SetRequestedCrouchInAir(false);

                // If this is a double jump, consume the buff
                if (canDoubleJump)
                {
                    _selfCastController.ConsumeDoubleJump();
                }
                else
                {
                    _motor.ForceUnground(time: 0.1f);
                    _ungroundedDueToJump = true;
                }

                // Notify self-cast controller about the jump
                if (_selfCastController != null)
                {
                    _selfCastController.OnPlayerJumped();
                }

                var currentVerticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                currentVelocity += _motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
            }
            else
            {
                // Can't jump yet, update the timer
                _playerInput.UpdateTimeSinceJumpRequest(deltaTime);

                // Check if we've waited too long (past coyote time)
                if (_playerInput.TimeSinceJumpRequest >= coyoteTime)
                {
                    // Give up on this jump request
                    _playerInput.ConsumeJumpRequest();
                }
            }
        }
    }

    private void UpdateDashVelocity()
    {
        if (_dashVelocity.magnitude > minDashSpeed)
        {
            float decayRate = _combatEntity.equippedWaveform?.dashDecayRate ?? dashDecayRate;
            _dashVelocity *= decayRate;
        }
        else
        {
            _dashVelocity = Vector3.zero;
        }
    }

    public void ApplyDash()
    {
        if (_combatEntity.equippedWaveform == null || _cameraTransform == null) return;

        Vector3 dashDir = _cameraTransform.forward;
        dashDir.y = 0;
        _dashVelocity = dashDir.normalized * _combatEntity.equippedWaveform.dashForce;
    }

    public void ResetDashVelocity()
    {
        _dashVelocity = Vector3.zero;
    }

    public bool ShouldTransitionToSlide(CharacterState state, CharacterState lastState)
    {
        var moving = _playerInput.RequestedMovement.sqrMagnitude > 0f;
        var crouching = state.Stance is Stance.Crouch;
        var wasStanding = lastState.Stance is Stance.Stand;
        var wasInAir = !lastState.Grounded;
        return moving && crouching && (wasStanding || wasInAir);
    }

    public bool ShouldExitSlide(Vector3 velocity)
    {
        return velocity.magnitude < slideEndSpeed;
    }
}