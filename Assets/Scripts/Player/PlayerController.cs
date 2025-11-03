using UnityEngine;
using System.Collections.Generic;

public class PlayerController : CombatEntity
{
    [Header("Player Settings")]
    public List<WaveformData> unlockedWaveforms = new List<WaveformData>();
    public int currentWaveformIndex = 0;

    [Header("Input")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode selfModifierKey = KeyCode.Mouse1;
    public KeyCode nextWaveformKey = KeyCode.E;
    public KeyCode prevWaveformKey = KeyCode.Q;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private Vector3 dashVelocity;
    private bool isGrounded;

    protected override void Start()
    {
        base.Start();
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;

        if (unlockedWaveforms.Count > 0)
        {
            EquipWaveform(0);
        }
    }

    protected override void Update()
    {
        base.Update();
        HandleInput();
        HandleMovement();
    }

    private void HandleInput()
    {
        // Switch waveforms
        if (Input.GetKeyDown(nextWaveformKey))
        {
            CycleWaveform(1);
        }
        if (Input.GetKeyDown(prevWaveformKey))
        {
            CycleWaveform(-1);
        }

        // Fire projectile
        if (Input.GetKeyDown(fireKey))
        {
            Vector3 aimDir = playerCamera.transform.forward;
            FireProjectile(aimDir);
        }

        // Self modifier
        if (Input.GetKeyDown(selfModifierKey))
        {
            ApplySelfModifier();
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = jumpForce;
        }
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;

        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Calculate movement direction relative to where player is looking
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move = move.normalized * moveSpeed * currentAccelMod;

        // Apply gravity
        if (!isGrounded)
        {
            velocity.y += Physics.gravity.y * currentGravityMod * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f; // Keep grounded
        }

        // Apply dash decay
        if (dashVelocity.magnitude > 0.1f)
        {
            dashVelocity *= equippedWaveform?.dashDecayRate ?? 0.9f;
        }
        else
        {
            dashVelocity = Vector3.zero;
        }

        // Combine all movement
        Vector3 finalMove = (move + dashVelocity) * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        characterController.Move(finalMove);
    }

    protected override void ApplyDash()
    {
        if (equippedWaveform == null) return;
        Vector3 dashDir = playerCamera.transform.forward;
        dashDir.y = 0; // Keep dash horizontal
        dashVelocity = dashDir.normalized * equippedWaveform.dashForce;
    }

    public void CycleWaveform(int direction)
    {
        if (unlockedWaveforms.Count == 0) return;

        currentWaveformIndex = (currentWaveformIndex + direction) % unlockedWaveforms.Count;
        if (currentWaveformIndex < 0) currentWaveformIndex = unlockedWaveforms.Count - 1;

        EquipWaveform(currentWaveformIndex);
    }

    public void EquipWaveform(int index)
    {
        if (index >= 0 && index < unlockedWaveforms.Count)
        {
            equippedWaveform = unlockedWaveforms[index];
            currentWaveformIndex = index;

            // Update immunity (player counters their equipped waveform)
            immuneToWaveforms.Clear();
            immuneToWaveforms.Add(equippedWaveform);
        }
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
        // Implement respawn or restart logic here
    }
}