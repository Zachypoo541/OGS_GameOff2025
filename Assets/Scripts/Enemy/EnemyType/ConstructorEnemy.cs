using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ConstructorEnemy : EnemyAI
{
    [Header("Constructor Movement")]
    public float gridSize = 2f;
    public float moveInterval = 1f;
    public float moveSpeed = 5f;
    public float groundCheckDistance = 0.2f;

    [Header("Constructor Attack")]
    public int volleySize = 3;
    public float spreadAngle = 15f;
    public float volleyCooldown = 3f;

    private Vector3 targetGridPosition;
    private Vector3 currentGridPosition;
    private float nextMoveTime;
    private float nextVolleyTime;
    private bool isMoving;
    private Rigidbody rb;
    private bool isGrounded;

    protected override void OnEnemyStart()
    {
        // Constructor uses Square (Blue) waveform
        // Immune to blue, no inherent weakness

        // Setup Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;

        // Snap to grid
        currentGridPosition = SnapToGrid(transform.position);
        targetGridPosition = currentGridPosition;
    }

    protected override void UpdateBehavior(float distanceToPlayer)
    {
        CheckGrounded();
        HandleGridMovement(distanceToPlayer);
        HandleVolleyAttack(distanceToPlayer);
    }

    private void CheckGrounded()
    {
        // Raycast down to check if grounded
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f);
    }

    private void HandleGridMovement(float distanceToPlayer)
    {
        // Only move if grounded
        if (!isGrounded)
        {
            // Always face player even when falling
            Vector3 lookTarget = player.position;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);
            return;
        }

        // If moving to target grid position
        if (isMoving)
        {
            Vector3 currentPos = transform.position;
            Vector3 moveDir = (targetGridPosition - currentPos).normalized;
            moveDir.y = 0; // Keep horizontal

            rb.MovePosition(currentPos + moveDir * moveSpeed * Time.deltaTime);

            // Check if reached target (only XZ plane)
            Vector2 currentXZ = new Vector2(currentPos.x, currentPos.z);
            Vector2 targetXZ = new Vector2(targetGridPosition.x, targetGridPosition.z);

            if (Vector2.Distance(currentXZ, targetXZ) < 0.1f)
            {
                currentGridPosition = new Vector3(targetGridPosition.x, transform.position.y, targetGridPosition.z);
                isMoving = false;
            }
        }
        // Time to choose new grid position
        else if (Time.time >= nextMoveTime)
        {
            ChooseNextGridPosition(distanceToPlayer);
            nextMoveTime = Time.time + moveInterval;
        }

        // Always face player
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    private void ChooseNextGridPosition(float distanceToPlayer)
    {
        // Choose a random adjacent grid position (crab-like: side-to-side preferred)
        Vector3 current = new Vector3(currentGridPosition.x, transform.position.y, currentGridPosition.z);

        Vector3[] possibleMoves = new Vector3[]
        {
            current + Vector3.right * gridSize,      // Right
            current + Vector3.left * gridSize,       // Left
            current + Vector3.forward * gridSize,    // Forward
            current + Vector3.back * gridSize        // Back
        };

        // Prefer horizontal movement (crab-like)
        int moveChoice = Random.value < 0.6f ? Random.Range(0, 2) : Random.Range(0, 4);
        targetGridPosition = possibleMoves[moveChoice];

        // Make sure we don't move too far from player
        if (Vector3.Distance(targetGridPosition, player.position) > detectionRange)
        {
            targetGridPosition = current;
            return;
        }

        // Check if target position has ground beneath it
        RaycastHit hit;
        if (Physics.Raycast(targetGridPosition + Vector3.up * 2f, Vector3.down, out hit, 5f))
        {
            targetGridPosition.y = hit.point.y;
            isMoving = true;
        }
        else
        {
            // No ground, don't move there
            targetGridPosition = current;
        }
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            position.y,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    private void HandleVolleyAttack(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange) return;
        if (Time.time < nextVolleyTime) return;

        // Fire volley of projectiles with spread
        Vector3 baseDirection = (player.position - transform.position).normalized;

        for (int i = 0; i < volleySize; i++)
        {
            // Calculate spread angle for this projectile
            float angleOffset = 0f;
            if (volleySize > 1)
            {
                angleOffset = Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (volleySize - 1));
            }

            // Rotate direction by spread angle
            Vector3 direction = Quaternion.Euler(0f, angleOffset, 0f) * baseDirection;
            FireProjectile(direction);
        }

        nextVolleyTime = Time.time + volleyCooldown;
    }
}