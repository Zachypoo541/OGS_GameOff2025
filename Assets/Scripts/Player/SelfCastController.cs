using UnityEngine;
using UnityEngine.Video;

public class SelfCastController : MonoBehaviour
{
    [Header("References")]
    private CombatEntity _combatEntity;
    private PlayerLocomotion _locomotion;
    private HandAnimationController _handAnimController;
    private Transform _cameraTransform;
    private PlayerInput _playerInput;
    private KinematicCharacterController.KinematicCharacterMotor _motor;
    private SelfCastVignetteController _vignetteController;

    [Header("State")]
    private WaveformData _currentWaveform;
    private float _selfCastCooldownTimer = 0f;
    private bool _isThrusting = false;
    private bool _hasDoubleJumpBuff = false;
    private int _jumpCount = 0;
    private bool _lastFrameRequestedSelfCast = false;

    // Reduced gravity effect
    private float _reducedGravityTimer = 0f;
    private bool _isGravityReduced = false;

    // Thrust animation state
    private bool _thrustEnterPlayed = false;
    private bool _thrustLoopStarted = false;
    private float _thrustEnterTimer = 0f;
    private const float THRUST_ENTER_DURATION = 0.5f; // Adjust based on your animation length

    // Dash vignette state
    private float _dashVignetteTimer = 0f;
    private const float DASH_VIGNETTE_DURATION = 0.5f; // How long the dash vignette stays visible

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public void Initialize(CombatEntity combatEntity, PlayerLocomotion locomotion,
                          HandAnimationController handAnimController, Transform cameraTransform,
                          PlayerInput playerInput, KinematicCharacterController.KinematicCharacterMotor motor,
                          SelfCastVignetteController vignetteController)
    {
        _combatEntity = combatEntity;
        _locomotion = locomotion;
        _handAnimController = handAnimController;
        _cameraTransform = cameraTransform;
        _playerInput = playerInput;
        _motor = motor;
        _vignetteController = vignetteController;

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Initialized");
    }

    public void SetCurrentWaveform(WaveformData waveform)
    {
        _currentWaveform = waveform;

        // Reset thrust state when switching waveforms
        if (_isThrusting)
        {
            EndThrust();
        }

        // Reset reduced gravity if active
        if (_isGravityReduced)
        {
            EndReducedGravity();
        }

        // Hide vignette when switching waveforms
        if (_vignetteController != null)
        {
            _vignetteController.HideVignette();
        }
    }

    public void UpdateSelfCast(float deltaTime)
    {
        // Update cooldown - but never let it go below 0
        if (_selfCastCooldownTimer > 0f)
        {
            _selfCastCooldownTimer -= deltaTime;

            // Clamp to 0 (never negative)
            if (_selfCastCooldownTimer < 0f)
            {
                _selfCastCooldownTimer = 0f;
            }
        }

        // Update reduced gravity timer
        if (_isGravityReduced)
        {
            _reducedGravityTimer -= deltaTime;
            if (_reducedGravityTimer <= 0f)
            {
                EndReducedGravity();
            }
        }

        // Update dash vignette timer
        if (_dashVignetteTimer > 0f)
        {
            _dashVignetteTimer -= deltaTime;
            if (_dashVignetteTimer <= 0f && _vignetteController != null)
            {
                _vignetteController.HideVignette();
            }
        }

        // Detect input rising edge (button just pressed this frame)
        bool justPressed = _playerInput.RequestedSelfCast && !_lastFrameRequestedSelfCast;

        // Store for next frame
        _lastFrameRequestedSelfCast = _playerInput.RequestedSelfCast;

        // Handle thrust input (Square wave) - uses hold behavior
        if (_currentWaveform != null && _currentWaveform.selfCastType == SelfCastType.Thrust)
        {
            if (_playerInput.RequestedSelfCast)
            {
                if (!_isThrusting && _selfCastCooldownTimer == 0f)
                {
                    StartThrust();
                }
                else if (_isThrusting)
                {
                    UpdateThrust(deltaTime);
                }
            }
            else
            {
                if (_isThrusting)
                {
                    EndThrust();
                }
            }
        }
        // Handle other self-cast abilities (press once) - uses press behavior
        else if (justPressed)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[SelfCastController] Button JUST PRESSED - Cooldown={_selfCastCooldownTimer:F2}s");
            }

            // Check if we can activate (not on cooldown)
            if (_selfCastCooldownTimer == 0f)
            {
                TryActivateSelfCast();
            }
        }
    }

    private void TryActivateSelfCast()
    {

        if (_currentWaveform == null || _currentWaveform.selfCastType == SelfCastType.None)
        {
            return;
        }

        // DOUBLE CHECK: If we somehow got here with cooldown active, abort
        if (_selfCastCooldownTimer > 0f)
        {
            return;
        }

        // Check energy cost
        float currentEnergy = _combatEntity.GetCurrentEnergy();

        if (currentEnergy < _currentWaveform.selfCastEnergyCost)
        {
            return;
        }

        // Consume energy
        _combatEntity.ConsumeEnergy(_currentWaveform.selfCastEnergyCost);

        // CRITICAL: Start cooldown IMMEDIATELY
        float cooldownDuration = _currentWaveform.selfCastCooldown;
        _selfCastCooldownTimer = cooldownDuration;

        // Execute ability based on type
        switch (_currentWaveform.selfCastType)
        {
            case SelfCastType.ReducedGravity:
                ActivateReducedGravity();
                break;
            case SelfCastType.Dash:
                ActivateDash();
                break;
            case SelfCastType.DoubleJump:
                ActivateDoubleJump();
                break;
        }

        // Play random hand animation (if available) WITH AUDIO
        PlayRandomSelfCastAnimation();
    }

    #region Sine Wave (Reduced Gravity)

    private void ActivateReducedGravity()
    {
        _isGravityReduced = true;
        _reducedGravityTimer = _currentWaveform.reducedGravityDuration;
        _combatEntity.SetGravityMultiplier(_currentWaveform.reducedGravityMultiplier);

        // Show vignette
        if (_vignetteController != null && _currentWaveform.waveformColor != null)
        {
            _vignetteController.ShowVignette(_currentWaveform.waveformColor);
        }

        if (enableDebugLogs)
            Debug.Log($"[SelfCastController] Reduced gravity activated for {_currentWaveform.reducedGravityDuration}s");
    }

    private void EndReducedGravity()
    {
        _isGravityReduced = false;

        // Reset to passive gravity (waveform's base gravity multiplier)
        _combatEntity.ResetToPassiveGravity();

        // Hide vignette
        if (_vignetteController != null)
        {
            _vignetteController.HideVignette();
        }

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Reduced gravity ended");
    }

    public bool IsGravityReduced()
    {
        return _isGravityReduced;
    }

    #endregion

    #region Square Wave (Thrust)

    private void StartThrust()
    {
        // IMPORTANT: Check cooldown before allowing thrust
        if (_selfCastCooldownTimer > 0f)
        {
            if (enableDebugLogs)
                Debug.Log($"[SelfCastController] Cannot start thrust - on cooldown: {_selfCastCooldownTimer:F2}s remaining");
            return;
        }

        // Check energy
        if (_combatEntity.GetCurrentEnergy() < _currentWaveform.selfCastEnergyCost)
        {
            if (enableDebugLogs)
                Debug.Log("[SelfCastController] Cannot start thrust - not enough energy");
            return;
        }

        // Consume energy
        _combatEntity.ConsumeEnergy(_currentWaveform.selfCastEnergyCost);

        _isThrusting = true;
        _thrustEnterPlayed = false;
        _thrustLoopStarted = false;
        _thrustEnterTimer = 0f;

        // Start cooldown
        _selfCastCooldownTimer = _currentWaveform.selfCastCooldown;

        // Show vignette
        if (_vignetteController != null && _currentWaveform.waveformColor != null)
        {
            _vignetteController.ShowVignette(_currentWaveform.waveformColor);
        }

        // Play enter animation if available (WITH AUDIO via StartLeftHandLoop)
        if (_currentWaveform.selfCastEnterAnimation != null &&
            _currentWaveform.selfCastEnterAnimation.clip != null &&
            _handAnimController != null)
        {
            // *** CHANGED: Pass waveform data for audio - but this is just the enter animation ***
            _handAnimController.PlayLeftHandAction(
                _currentWaveform.selfCastEnterAnimation.clip,
                false,
                _currentWaveform.selfCastEnterAnimation.playbackSpeed,
                null  // Don't pass waveform here - we'll handle thrust audio in StartThrustLoop
            );
            _thrustEnterPlayed = true;
        }
        else
        {
            // If no enter animation, start loop immediately
            StartThrustLoop();
        }

        if (enableDebugLogs)
            Debug.Log($"[SelfCastController] Thrust started. Cooldown: {_selfCastCooldownTimer}s");
    }

    private void UpdateThrust(float deltaTime)
    {
        // If we played an enter animation and haven't started the loop yet
        if (_thrustEnterPlayed && !_thrustLoopStarted)
        {
            _thrustEnterTimer += deltaTime;

            // Wait for enter animation to complete before starting loop
            if (_thrustEnterTimer >= THRUST_ENTER_DURATION)
            {
                StartThrustLoop();
            }
        }
    }

    private void StartThrustLoop()
    {
        if (_thrustLoopStarted)
            return;

        _thrustLoopStarted = true;

        // Start looping animation WITH AUDIO
        if (_currentWaveform.selfCastLoopAnimation != null &&
            _currentWaveform.selfCastLoopAnimation.clip != null &&
            _handAnimController != null)
        {
            // *** CHANGED: Pass waveform data for thrust audio (start->loop->end sequence) ***
            _handAnimController.StartLeftHandLoop(
                _currentWaveform.selfCastLoopAnimation.clip,
                _currentWaveform.selfCastLoopAnimation.playbackSpeed,
                _currentWaveform  // Pass waveform for thrust audio
            );
        }

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Thrust loop started");
    }

    private void EndThrust()
    {
        if (!_isThrusting)
            return;

        _isThrusting = false;
        _thrustEnterPlayed = false;
        _thrustLoopStarted = false;
        _thrustEnterTimer = 0f;

        // Hide vignette
        if (_vignetteController != null)
        {
            _vignetteController.HideVignette();
        }

        // *** CHANGED: Stop the left hand loop WITH AUDIO (plays thrust end sound) ***
        if (_handAnimController != null)
        {
            _handAnimController.StopLeftHandLoop(_currentWaveform);  // Pass waveform for end sound
        }

        // Play exit animation (this will hide when done via PlayLeftHandAction)
        if (_currentWaveform.selfCastExitAnimation != null &&
            _currentWaveform.selfCastExitAnimation.clip != null &&
            _handAnimController != null)
        {
            _handAnimController.PlayLeftHandAction(
                _currentWaveform.selfCastExitAnimation.clip,
                true,
                _currentWaveform.selfCastExitAnimation.playbackSpeed,
                null  // Don't pass waveform - we already played end sound above
            );
        }

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Thrust ended");
    }

    public void ApplyThrustVelocity(ref Vector3 velocity, float deltaTime)
    {
        if (!_isThrusting || _currentWaveform == null)
            return;

        // Get opposite direction of camera look
        Vector3 thrustDirection = -_cameraTransform.forward;
        thrustDirection.y = 0f;
        thrustDirection.Normalize();

        // Apply thrust force
        velocity += thrustDirection * _currentWaveform.thrustForce * deltaTime;
    }

    public bool IsThrusting()
    {
        return _isThrusting;
    }

    #endregion

    #region Saw Wave (Dash)

    private void ActivateDash()
    {
        _locomotion.ApplyDash();

        // Show vignette briefly
        if (_vignetteController != null && _currentWaveform.waveformColor != null)
        {
            _vignetteController.ShowVignette(_currentWaveform.waveformColor);
            _dashVignetteTimer = DASH_VIGNETTE_DURATION;
        }

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Dash activated");
    }

    #endregion

    #region Triangle Wave (Double Jump)

    private void ActivateDoubleJump()
    {
        _hasDoubleJumpBuff = true;

        // Show vignette
        if (_vignetteController != null && _currentWaveform.waveformColor != null)
        {
            _vignetteController.ShowVignette(_currentWaveform.waveformColor);
        }

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Double jump buff granted");
    }

    public bool HasDoubleJumpBuff()
    {
        return _hasDoubleJumpBuff;
    }

    public void ConsumeDoubleJump()
    {
        _hasDoubleJumpBuff = false;

        // Hide vignette when buff is consumed
        if (_vignetteController != null)
        {
            _vignetteController.HideVignette();
        }

        if (enableDebugLogs)
            Debug.Log("[SelfCastController] Double jump buff consumed");
    }

    public void OnPlayerJumped()
    {
        _jumpCount++;
    }

    public void OnPlayerLanded()
    {
        _jumpCount = 0;
    }

    public int GetJumpCount()
    {
        return _jumpCount;
    }

    #endregion

    #region Animation Handling

    private void PlayRandomSelfCastAnimation()
    {

        if (_currentWaveform.selfCastAnimations == null)
        {
            return;
        }

        if (_currentWaveform.selfCastAnimations.Length == 0)
        {
            return;
        }

        // Filter out null clips
        var validClips = System.Array.FindAll(_currentWaveform.selfCastAnimations,
            clipWithSpeed => clipWithSpeed != null && clipWithSpeed.clip != null);

        if (validClips.Length == 0)
        {
            return;
        }

        // Select random clip
        int randomIndex = Random.Range(0, validClips.Length);
        VideoClipWithSpeed selected = validClips[randomIndex];

        // *** CHANGED: Pass waveform data for audio support ***
        if (_handAnimController != null)
        {
            _handAnimController.PlayLeftHandAction(
                selected.clip,
                true,
                selected.playbackSpeed,
                _currentWaveform  // Pass waveform for self-cast audio
            );
        }

    }

    #endregion

    #region Public Getters

    public float GetSelfCastCooldownPercent()
    {
        if (_currentWaveform == null || _currentWaveform.selfCastCooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(_selfCastCooldownTimer / _currentWaveform.selfCastCooldown);
    }

    public float GetReducedGravityTimeRemaining()
    {
        return _isGravityReduced ? _reducedGravityTimer : 0f;
    }

    #endregion
}