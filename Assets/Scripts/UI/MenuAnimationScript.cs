using UnityEngine;
using UnityEngine.UIElements;

public class MenuRotationAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float rotationAmount = 2f; // Degrees to rotate
    [SerializeField] private float animationSpeed = 0.5f; // Speed of rotation (lower is slower)
    
    private UIDocument uiDocument;
    private VisualElement container;
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

        // Find the container element
        container = uiDocument.rootVisualElement.Q<VisualElement>("container");
        if (container == null)
        {
            Debug.LogError("Container element not found in USS!");
            return;
        }
    }

    void Update()
    {
        if (container == null) return;

        // Calculate rotation using sine wave for smooth back and forth motion
        time += Time.deltaTime * animationSpeed;
        currentRotation = Mathf.Sin(time) * rotationAmount;

        // Apply rotation to the container
        container.style.rotate = new Rotate(Angle.Degrees(currentRotation));
        
        // Optional: Apply to all children instead of container
        // ApplyRotationToChildren();
    }

    // Alternative method to rotate individual children
    void ApplyRotationToChildren()
    {
        foreach (VisualElement child in container.Children())
        {
            child.style.rotate = new Rotate(Angle.Degrees(currentRotation));
        }
    }

    // Public method to change animation settings at runtime
    public void SetAnimationSettings(float rotation, float speed)
    {
        rotationAmount = rotation;
        animationSpeed = speed;
    }
}