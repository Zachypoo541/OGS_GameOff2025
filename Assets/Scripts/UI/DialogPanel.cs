using UnityEngine;
using UnityEngine.UIElements;

public class ItemDialogController : MonoBehaviour
{
    private VisualElement root;
    private Label itemName;
    private Label itemDescription;

    void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        itemName = root.Q<Label>("itemName");
        itemDescription = root.Q<Label>("itemDescription");

        root.style.display = DisplayStyle.None; // Hide initially
    }

    public void Show(string name, string description)
    {
        itemName.text = name;
        itemDescription.text = description;
        root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        root.style.display = DisplayStyle.None;
    }
}
