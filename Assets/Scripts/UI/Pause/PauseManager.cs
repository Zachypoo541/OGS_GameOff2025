using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPrefab;

    private PlayerInputActions inputActions;
    private GameObject pauseMenuInstance;
    private Transform playerTransform;
    private bool isPaused = false;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Find the player at runtime
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player GameObject not found! Make sure it has the 'Player' tag.");
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Gameplay.Pause.performed += OnPausePressed;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Pause.performed -= OnPausePressed;
        inputActions.Disable();
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        // Instantiate the pause menu as a child of the Player
        if (playerTransform != null)
        {
            pauseMenuInstance = Instantiate(pauseMenuPrefab, playerTransform);

            // Set the reference to this manager using SendMessage to avoid circular dependency
            pauseMenuInstance.SendMessage("SetPauseManager", this);
        }
        else
        {
            pauseMenuInstance = Instantiate(pauseMenuPrefab);
            Debug.LogWarning("Player not found, instantiating pause menu at root.");
        }

        Time.timeScale = 0f;
        isPaused = true;

        // Pause the player's input
        if (Player.Instance != null)
        {
            Player.Instance.SetPaused(true);
        }

        // Show and unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        // Destroy the pause menu instance
        if (pauseMenuInstance != null)
        {
            Destroy(pauseMenuInstance);
        }

        Time.timeScale = 1f;
        isPaused = false;

        // Unpause the player's input
        if (Player.Instance != null)
        {
            Player.Instance.SetPaused(false);
        }

        // Hide and lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Reset time scale before loading scene
        SceneManager.LoadScene("MainMenu"); // Make sure your main menu scene is named "MainMenu"
    }
}