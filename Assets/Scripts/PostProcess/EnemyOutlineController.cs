using UnityEngine;
using EnemySystem;

[RequireComponent(typeof(Renderer))]
public class EnemyOutlineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private EnemyVisualSettings visualSettings;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Material enemyMaterial;
    private float targetTintStrength;
    private float currentTintStrength;
    private string currentState = "Idle";

    private static readonly int TintColorID = Shader.PropertyToID("_TintColor");
    private static readonly int TintStrengthID = Shader.PropertyToID("_TintStrength");

    private void Start()
    {
        // Find EnemyAI
        if (enemyAI == null)
        {
            enemyAI = GetComponent<EnemyAI>();
            if (enemyAI == null) enemyAI = GetComponentInParent<EnemyAI>();
            if (enemyAI == null) enemyAI = GetComponentInChildren<EnemyAI>();
        }

        if (enemyAI == null)
        {
            Debug.LogError($"EnemyOutlineController on {gameObject.name} could not find EnemyAI!");
            enabled = false;
            return;
        }

        if (visualSettings == null)
        {
            Debug.LogError($"EnemyOutlineController on {gameObject.name} is missing Visual Settings! Please assign a EnemyVisualSettings asset.");
            enabled = false;
            return;
        }

        // Get material
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"EnemyOutlineController on {gameObject.name} requires a Renderer!");
            enabled = false;
            return;
        }

        enemyMaterial = renderer.material;
        enemyMaterial.SetColor(TintColorID, visualSettings.darkTintColor);

        currentTintStrength = visualSettings.idleTintStrength;
        targetTintStrength = visualSettings.idleTintStrength;
        enemyMaterial.SetFloat(TintStrengthID, currentTintStrength);

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: EnemyOutlineController initialized with {visualSettings.enemyName} settings");
        }
    }

    private void Update()
    {
        if (enemyAI == null || enemyMaterial == null || visualSettings == null) return;

        // Determine state and target values
        UpdateState();

        // Update color tint
        UpdateColorTint();
    }

    private void UpdateState()
    {
        Transform playerTransform = enemyAI.GetPlayerTransform();
        if (playerTransform == null)
        {
            currentState = "Idle";
            targetTintStrength = visualSettings.idleTintStrength;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= enemyAI.GetAttackRange())
        {
            currentState = "Attacking";
            targetTintStrength = visualSettings.attackingTintStrength;
        }
        else if (distance <= enemyAI.GetDetectionRange())
        {
            currentState = "Detected";
            targetTintStrength = visualSettings.detectedTintStrength;
        }
        else
        {
            currentState = "Idle";
            targetTintStrength = visualSettings.idleTintStrength;
        }
    }

    private void UpdateColorTint()
    {
        if (currentState == "Attacking")
        {
            // Pulse between normal and dark when attacking
            float pulse = Mathf.Sin(Time.time * visualSettings.attackingPulseSpeed) * 0.5f + 0.5f;
            currentTintStrength = Mathf.Lerp(visualSettings.detectedTintStrength, visualSettings.attackingTintStrength, pulse);
        }
        else
        {
            // Smooth transition for other states
            currentTintStrength = Mathf.Lerp(currentTintStrength, targetTintStrength, Time.deltaTime * visualSettings.transitionSpeed);
        }

        enemyMaterial.SetFloat(TintStrengthID, currentTintStrength);
    }

    public EnemyAI GetEnemyAI() => enemyAI;
    public string GetCurrentState() => currentState;

    private void OnDestroy()
    {
        if (enemyMaterial != null)
        {
            Destroy(enemyMaterial);
        }
    }

    private void OnGUI()
    {
        if (showDebugInfo && Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 250, 60),
                    $"{gameObject.name}\nState: {currentState}\nTint: {currentTintStrength:F2}");
            }
        }
    }
}