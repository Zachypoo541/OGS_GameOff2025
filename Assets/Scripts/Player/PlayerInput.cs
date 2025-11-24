using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // Input tracking
    private Quaternion _requestedRotation;
    private Vector3 _requestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;

    // Jump timing
    private float _timeSinceJumpRequest;

    // Public accessors
    public Quaternion RequestedRotation => _requestedRotation;
    public Vector3 RequestedMovement => _requestedMovement;
    public bool RequestedJump => _requestedJump;
    public bool RequestedSustainedJump => _requestedSustainedJump;
    public bool RequestedCrouch => _requestedCrouch;
    public bool RequestedCrouchInAir => _requestedCrouchInAir;
    public float TimeSinceJumpRequest => _timeSinceJumpRequest;

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
            _requestedCrouchInAir = input.Crouch != CrouchInput.None;
        else if (!_requestedCrouch && wasRequestingCrouch)
            _requestedCrouchInAir = false;
    }

    public void SetRequestedCrouchInAir(bool value)
    {
        _requestedCrouchInAir = value;
    }

    public void ConsumeJumpRequest()
    {
        _requestedJump = false;
    }

    public void SetRequestedCrouch(bool value)
    {
        _requestedCrouch = value;
    }

    public void UpdateTimeSinceJumpRequest(float deltaTime)
    {
        _timeSinceJumpRequest += deltaTime;
    }
}