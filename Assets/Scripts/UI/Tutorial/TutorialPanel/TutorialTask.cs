using UnityEngine;

[CreateAssetMenu(fileName = "New Tutorial Task", menuName = "Tutorial/Task")]
public class TutorialTask : ScriptableObject
{
    public string displayText;
    public string actionName; // Must match your InputActionAsset action name
    public string actionMapName = "Gameplay"; // The action map this action belongs to
}