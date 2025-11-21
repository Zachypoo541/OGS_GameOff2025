using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private Reticle reticle;
    [SerializeField] private CounterSystem counterSystem;

    [Header("Debug")]
    [SerializeField] private bool debugInput = false;

    private PlayerInputActions _inputActions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple Player instances detected! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        // Find components if not assigned
        if (reticle == null)
        {
            reticle = FindFirstObjectByType<Reticle>();
        }

        if (counterSystem == null)
        {
            counterSystem = playerCharacter.GetComponent<CounterSystem>();
            if (counterSystem == null)
            {
                Debug.LogError("CounterSystem not found on PlayerCharacter! Please add the CounterSystem component.");
            }
        }

        // Pass reticle to PlayerCharacter
        playerCharacter.Initialize(playerCamera.transform, reticle);
        playerCamera.Initialize(playerCharacter.GetCameraTarget());
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        _inputActions?.Dispose();
    }

    void Update()
    {
        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;

        // Debug input (enable in inspector to troubleshoot)
        if (debugInput)
        {
            if (input.Fire.WasPressedThisFrame() ||
                input.NextWaveform.WasPressedThisFrame() ||
                input.PrevWaveform.WasPressedThisFrame() ||
                input.SelfModifier.WasPressedThisFrame() ||
                input.Counter.WasPressedThisFrame())
            {
                Debug.Log($"Combat Input - Fire: {input.Fire.WasPressedThisFrame()}, " +
                         $"Next: {input.NextWaveform.WasPressedThisFrame()}, " +
                         $"Prev: {input.PrevWaveform.WasPressedThisFrame()}, " +
                         $"Modifier: {input.SelfModifier.WasPressedThisFrame()}, " +
                         $"Counter: {input.Counter.WasPressedThisFrame()}");
            }
        }

        // Get camera input and update its rotation and position
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };

        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
        playerCamera.UpdateRotation(cameraInput);

        // Get character movement input and update it
        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame()
                ? CrouchInput.Toggle
                : CrouchInput.None
        };
        playerCharacter.UpdateInput(characterInput);

        // Get combat input and update it
        var combatInput = new CombatInput
        {
            Fire = input.Fire.WasPressedThisFrame(),
            SelfModifier = input.SelfModifier.WasPressedThisFrame(),
            NextWaveform = input.NextWaveform.WasPressedThisFrame(),
            PrevWaveform = input.PrevWaveform.WasPressedThisFrame(),
            Counter = input.Counter.WasPressedThisFrame()
        };
        playerCharacter.UpdateCombatInput(combatInput);

        playerCharacter.UpdateBody(deltaTime);

#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Teleport(hit.point);
            }
        }
#endif
    }

    public void Teleport(Vector3 position)
    {
        playerCharacter.SetPosition(position);
    }

    // Public method for enemies to get the correct player transform
    public Transform GetPlayerTransform()
    {
        return playerCharacter.GetMotorTransform();
    }
}