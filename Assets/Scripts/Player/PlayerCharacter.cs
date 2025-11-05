using UnityEngine;
using KinematicCharacterController;
using Unity.VisualScripting;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch
}
public struct CharacterInput
{
    public Quaternion Rotation;

    public Vector2 Move;

    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;

    [SerializeField] private Transform cameraTarget;
    [Space]

    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]

    [SerializeField] private float jumpSpeed = 20f;

    [SerializeField] private float gravity = -90f;

    [SerializeField] private float standHeight = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchHeight = 1f;

    [SerializeField] private float crouchHeightResponse = 15f;

    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]

    [SerializeField] private float crouchCameraTargetHeight = 0.7f;
    [Range(0f, 1f)]

    private Stance _stance;

    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedCrouch;

    private Collider[] _uncrouchOverlapResults;
    public void Initialize()
    {
        _stance = Stance.Stand;
        _uncrouchOverlapResults = new Collider[8];

        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;

        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;

        _requestedJump = _requestedJump || input.Jump;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch,
        };
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        var forward = Vector3.ProjectOnPlane
            (
                _requestedRotation * Vector3.forward,
                motor.CharacterUp
            );

        if (forward != Vector3.zero)
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;

        var cameraTargetHeight = currentHeight *
        (
            _stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight

        );
        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);

        cameraTarget.localPosition = Vector3.Lerp
        (
            a: cameraTarget.localPosition,
            b: new Vector3(0f, cameraTargetHeight, 0f),
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        root.localScale = Vector3.Lerp
        (
            a: root.localScale,
            b: rootTargetScale,
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
    }


    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // If on the ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            // Snap the requested movement direction to the angle of the surface
            // the character is currently walking on.
            var groundedMovement = motor.GetDirectionTangentToSurface
            (
                direction: _requestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            // Calculate the speed and responsiveness of movement based on the character's stance
            var speed = _stance is Stance.Stand
                ? walkSpeed
                : crouchSpeed;
            var response = _stance is Stance.Stand
                ? walkResponse
                : crouchResponse;

            // And smoothly move along the ground in that direction.
            var targetVelocity = groundedMovement * speed;
            currentVelocity = Vector3.Lerp
            ( 
                a: currentVelocity, 
                b: targetVelocity, 
                t: 1f - Mathf.Exp(-response * deltaTime) 
            );
        }

        // else, in the air...
        else
        {
            // Gravity
            currentVelocity += motor.CharacterUp * gravity * deltaTime;
        }

        if (_requestedJump)
        {
            _requestedJump = false;

            // Unstick the player from the ground
            motor.ForceUnground(time: 0.1f);

            // Set minimum vertical speed to the jump speed.
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
            // Add the difference in current and target vertical speed to the character's velocity.
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }
    }

    public Transform GetCameraTarget() => cameraTarget;
    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Crouch...
        if (_requestedCrouch && _stance is Stance.Stand)
        {
            _stance = Stance.Crouch;
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }
    public void AfterCharacterUpdate(float deltaTime)
    {
        // Uncrouch...
        if (!_requestedCrouch && _stance is not Stance.Stand)
        {
            // Tentatively "standup" the character capsule.
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );

            // Then see if the capsule overlaps any colliders before actually allowing the character to standup.
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if (motor.CharacterOverlap(pos, rot, _uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                // Re-crouch
                _requestedCrouch = true;
                motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
            }
            else
            {
                _stance = Stance.Stand;
            }
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        // Return true to allow collisions with all colliders
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        // Empty implementation (required by interface)
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        // Empty implementation (required by interface)
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        // Empty implementation (required by interface)
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Empty implementation (required by interface)
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        // Empty implementation (required by interface)
    }
}