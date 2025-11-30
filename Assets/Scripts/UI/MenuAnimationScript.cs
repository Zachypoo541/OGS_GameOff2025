using UnityEngine;
using UnityEngine.UIElements;

public class MenuRotationAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float rotationAmount = 2f; // Degrees to rotate
    [SerializeField] private float animationSpeed = 0.5f; // Speed of rotation (lower is slower)
    
    private UIDocument uiDocument;
    private VisualElement mainMenuPanel;
    private VisualElement settingsPanel;
    private float currentRotation = 0f;
    private float time = 0f;

    void Start()
    {
        // Get the UI Document component
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }

        // Find both panel elements
        mainMenuPanel = uiDocument.rootVisualElement.Q<VisualElement>("main-menu-panel");
        settingsPanel = uiDocument.rootVisualElement.Q<VisualElement>("settings-panel");
        
        if (mainMenuPanel == null)
        {
            Debug.LogError("Main menu panel element not found!");
        }
        if (settingsPanel == null)
        {
            Debug.LogError("Settings panel element not found!");
        }
    }

    void Update()
    {
        if (mainMenuPanel == null && settingsPanel == null) return;

        // Calculate rotation using sine wave for smooth back and forth motion
        time += Time.deltaTime * animationSpeed;
        currentRotation = Mathf.Sin(time) * rotationAmount;

        // Apply rotation to both panels (only the visible one will be seen)
        if (mainMenuPanel != null)
        {
            mainMenuPanel.style.rotate = new Rotate(Angle.Degrees(currentRotation));
        }
        if (settingsPanel != null)
        {
            settingsPanel.style.rotate = new Rotate(Angle.Degrees(currentRotation));
        }
        
        // Optional: Apply to all children instead of container
        // ApplyRotationToChildren();
    }

    // Alternative method to rotate individual children
    void ApplyRotationToChildren()
    {
        if (mainMenuPanel != null)
        {
            foreach (VisualElement child in mainMenuPanel.Children())
            {
                child.style.rotate = new Rotate(Angle.Degrees(currentRotation));
            }
        }
        
        if (settingsPanel != null)
        {
            foreach (VisualElement child in settingsPanel.Children())
            {
                child.style.rotate = new Rotate(Angle.Degrees(currentRotation));
            }
        }
    }

    // Public method to change animation settings at runtime
    public void SetAnimationSettings(float rotation, float speed)
    {
        rotationAmount = rotation;
        animationSpeed = speed;
    }
}
