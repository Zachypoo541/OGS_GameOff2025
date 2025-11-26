using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Movement Input")]
    public Vector3 RequestedMovement { get; private set; }
    public Quaternion RequestedRotation { get; private set; }
    public bool RequestedJump { get; private set; }
    public bool RequestedSustainedJump { get; private set; }
    public bool RequestedCrouch { get; private set; }
    public bool RequestedCrouchInAir { get; private set; }

    [Header("Self-Cast Input")]
    public bool RequestedSelfCast { get; private set; }

    [Header("Jump Timing")]
    public float TimeSinceJumpRequest { get; private set; }

    public void UpdateInput(CharacterInput input)
    {
        // Convert Vector2 move input to Vector3 movement
        Vector3 forward = input.Rotation * Vector3.forward;
        Vector3 right = input.Rotation * Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        RequestedMovement = (forward * input.Move.y + right * input.Move.x);
        RequestedRotation = input.Rotation;

        // IMPORTANT: Set jump to true if input is pressed, but don't reset it to false here
        // Only ConsumeJumpRequest() should set it to false
        if (input.Jump)
        {
            RequestedJump = true;
            TimeSinceJumpRequest = 0f; // Reset timer when new jump is requested
        }

        RequestedSustainedJump = input.JumpSustain;

        // Handle crouch toggle
        if (input.Crouch == CrouchInput.Toggle)
        {
            RequestedCrouch = !RequestedCrouch;
        }

        // SelfCast comes from the SelfModifier input being held
        RequestedSelfCast = input.SelfCast;
    }

    public void ConsumeJumpRequest()
    {
        RequestedJump = false;
        TimeSinceJumpRequest = 0f;
    }

    public void UpdateTimeSinceJumpRequest(float deltaTime)
    {
        // Only update timer if we have an active jump request
        if (RequestedJump)
        {
            TimeSinceJumpRequest += deltaTime;
        }
    }

    public void SetRequestedCrouch(bool value)
    {
        RequestedCrouch = value;
    }

    public void SetRequestedCrouchInAir(bool value)
    {
        RequestedCrouchInAir = value;
    }

    public void SetRequestedSelfCast(bool value)
    {
        RequestedSelfCast = value;
    }

    public void ConsumeSelfCastRequest()
    {
        RequestedSelfCast = false;
    }
}