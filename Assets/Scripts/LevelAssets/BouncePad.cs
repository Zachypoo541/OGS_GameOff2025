using UnityEngine;
using KinematicCharacterController;

public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 25f;
    [SerializeField] private bool preserveHorizontalVelocity = true;

    [Header("Visuals (Optional)")]
    [SerializeField] private float bounceAnimationDuration = 0.2f;
    [SerializeField] private float bounceAnimationScale = 0.8f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bounceSound;

    private Vector3 _originalScale;
    private float _bounceAnimationTimer;

    private void Start()
    {
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        // Handle bounce animation
        if (_bounceAnimationTimer > 0f)
        {
            _bounceAnimationTimer -= Time.deltaTime;
            float progress = 1f - (_bounceAnimationTimer / bounceAnimationDuration);

            // Bounce down then back up
            float scaleMultiplier = progress < 0.5f
                ? Mathf.Lerp(1f, bounceAnimationScale, progress * 2f)
                : Mathf.Lerp(bounceAnimationScale, 1f, (progress - 0.5f) * 2f);

            transform.localScale = new Vector3(
                _originalScale.x,
                _originalScale.y * scaleMultiplier,
                _originalScale.z
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"BouncePad triggered by: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // The collider is on the Character child, which should have all the components we need
        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        KinematicCharacterMotor motor = other.GetComponent<KinematicCharacterMotor>();

        // If not on same GameObject, try parent
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerCharacter>();
            Debug.Log($"Checked parent for PlayerCharacter: {player != null}");
        }

        if (motor == null)
        {
            motor = other.GetComponentInParent<KinematicCharacterMotor>();
            Debug.Log($"Checked parent for Motor: {motor != null}");
        }

        // Also try children
        if (player == null)
        {
            player = other.GetComponentInChildren<PlayerCharacter>();
            Debug.Log($"Checked children for PlayerCharacter: {player != null}");
        }

        if (motor == null)
        {
            motor = other.GetComponentInChildren<KinematicCharacterMotor>();
            Debug.Log($"Checked children for Motor: {motor != null}");
        }

        if (player != null && motor != null)
        {
            Debug.Log("Player and Motor found! Applying bounce...");
            ApplyBounce(motor);
        }
        else
        {
            Debug.LogWarning($"BouncePad: Missing components - Player: {player != null}, Motor: {motor != null}");
        }
    }

    private void ApplyBounce(KinematicCharacterMotor motor)
    {
        Vector3 currentVelocity = motor.BaseVelocity;

        if (preserveHorizontalVelocity)
        {
            // Keep horizontal velocity, only change vertical
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            Vector3 newVelocity = horizontalVelocity + (motor.CharacterUp * bounceForce);
            motor.BaseVelocity = newVelocity;
        }
        else
        {
            // Just add upward force
            motor.BaseVelocity = currentVelocity + (motor.CharacterUp * bounceForce);
        }

        // Force unground to ensure bounce happens
        motor.ForceUnground(0.1f);

        // Play effects
        PlayBounceEffects();

        Debug.Log($"Bounce! Applied force: {bounceForce}, Final velocity: {motor.BaseVelocity}");
    }

    private void PlayBounceEffects()
    {
        // Trigger visual animation
        _bounceAnimationTimer = bounceAnimationDuration;

        // Play sound
        if (audioSource != null && bounceSound != null)
        {
            audioSource.PlayOneShot(bounceSound);
        }
    }

    private void OnDrawGizmos()
    {
        // Draw upward arrow to show bounce direction
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Vector3 arrowEnd = center + Vector3.up * 2f;

        Gizmos.DrawLine(center, arrowEnd);

        // Draw arrow head
        Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(-0.3f, -0.3f, 0f));
        Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(0.3f, -0.3f, 0f));
        Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(0f, -0.3f, -0.3f));
        Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(0f, -0.3f, 0.3f));
    }
}