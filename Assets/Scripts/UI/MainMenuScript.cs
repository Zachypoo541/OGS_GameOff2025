using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private UIDocument _document;
    private Button _startButton;
    private Button _optionsButton;
    private Button _quitButton;
    
    void Awake()
    {
        _document = GetComponent<UIDocument>();
    }
    
    void OnEnable()
    {
        // Get root element
        var root = _document.rootVisualElement;
        
        // Query buttons by their names
        _startButton = root.Q<Button>("start-button");
        _optionsButton = root.Q<Button>("options-button");
        _quitButton = root.Q<Button>("quit-button");
        
        // Subscribe to button click events
        _startButton.clicked += OnStartClicked;
        _optionsButton.clicked += OnOptionsClicked;
        _quitButton.clicked += OnQuitClicked;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events when disabled
        _startButton.clicked -= OnStartClicked;
        _optionsButton.clicked -= OnOptionsClicked;
        _quitButton.clicked -= OnQuitClicked;
    }
    
    private void OnStartClicked()
    {
        Debug.Log("Start Game clicked!");
        // Load your game scene
        SceneManager.LoadScene("Forest");
    }
    
    private void OnOptionsClicked()
    {
        Debug.Log("Options clicked!");
        // Show options panel or load options scene
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("Quit clicked!");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}