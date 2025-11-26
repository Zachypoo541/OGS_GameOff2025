using UnityEngine;

// Shared enums and structs used across player components

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
    public bool SelfCast;
}

public struct CombatInput
{
    public bool Fire;
    public bool SelfModifier;
    public bool NextWaveform;
    public bool PrevWaveform;
    public bool Counter;
    public bool Wave1;
    public bool Wave2;
    public bool Wave3;
    public bool Wave4;
}