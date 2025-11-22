using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles cat movement, pathfinding, and navigation
/// Works with AnimalController to sync animations with movement
/// </summary>
[RequireComponent(typeof(AnimalController))]
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool useNavMesh = true;

    [Header("Wandering")]
    [SerializeField] private bool enableWandering = true;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderInterval = 10f; // Seconds between wanders
    [SerializeField] private float idleTimeAtDestination = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpDistance = 2f;
    [SerializeField] private LayerMask jumpableSurfaces = ~0;

    // Components
    private AnimalController animalController;
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private CatData catData;

    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    private InteractionPoint targetInteraction;
    private float wanderTimer = 0f;
    private float idleTimer = 0f;
    private bool isJumping = false;

    void Awake()
    {
        animalController = GetComponent<AnimalController>();
        rb = GetComponent<Rigidbody>();

        // Set up NavMesh agent if using it
        if (useNavMesh)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.speed = walkSpeed;
            navAgent.angularSpeed = rotationSpeed * 57.3f; // Convert to degrees
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 0.1f;
        }
    }

    void Update()
    {
        if (catData != null && catData.isSleeping)
        {
            // Don't move while sleeping
            StopMovement();
            return;
        }

        // Update wandering
        if (enableWandering && !isMoving && targetInteraction == null)
        {
            UpdateWandering();
        }

        // Update movement animation
        UpdateMovementAnimation();

        // Check if reached destination
        if (isMoving && HasReachedDestination())
        {
            OnReachedDestination();
        }
    }

    #region Public API

    /// <summary>
    /// Initialize with cat data
    /// </summary>
    public void Initialize(CatData data)
    {
        catData = data;

        // Personality affects movement
        switch (data.personality)
        {
            case CatPersonality.Playful:
                wanderInterval = 5f; // Wander more often
                walkSpeed = 1.5f;
                break;

            case CatPersonality.Lazy:
                wanderInterval = 20f; // Wander rarely
                walkSpeed = 0.7f;
                break;

            case CatPersonality.Curious:
                wanderRadius = 10f; // Explore further
                wanderInterval = 7f;
                break;
        }

        if (navAgent != null)
        {
            navAgent.speed = walkSpeed;
        }
    }

    /// <summary>
    /// Move to specific position
    /// </summary>
    public void MoveTo(Vector3 position, bool shouldRun = false)
    {
        targetPosition = position;
        isMoving = true;

        if (useNavMesh && navAgent != null)
        {
            navAgent.SetDestination(position);
            navAgent.speed = shouldRun ? runSpeed : walkSpeed;
        }

        // Set animation to walking/running
        animalController.SetState(CatState.Exploring);
        animalController.SetMovementSpeed(shouldRun ? 1f : 0.5f);

        Debug.Log($"🚶 Moving to {position}");
    }

    /// <summary>
    /// Move to interaction point (furniture, toy, etc.)
    /// </summary>
    public void MoveToInteraction(InteractionPoint interaction, bool shouldRun = false)
    {
        if (interaction == null || !interaction.CanBeUsedBy(animalController))
        {
            Debug.LogWarning("Cannot use this interaction point");
            return;
        }

        targetInteraction = interaction;
        MoveTo(interaction.transform.position, shouldRun);
    }

    /// <summary>
    /// Stop current movement
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
        targetInteraction = null;

        if (navAgent != null)
        {
            navAgent.ResetPath();
        }

        animalController.SetMovementSpeed(0f);
        animalController.SetState(CatState.Idle);
    }

    /// <summary>
    /// Jump to position (for climbing furniture)
    /// </summary>
    public void JumpTo(Vector3 targetPosition)
    {
        if (isJumping) return;

        StartCoroutine(PerformJump(targetPosition));
    }

    #endregion

    #region Wandering

    /// <summary>
    /// Random wandering behavior when idle
    /// </summary>
    private void UpdateWandering()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;

            // Pick random point within wander radius
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y; // Keep on same level

            // Check if point is valid (not inside walls, etc.)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                MoveTo(hit.position, false);
            }
        }
    }

    #endregion

    #region Movement Detection

    /// <summary>
    /// Check if cat has reached destination
    /// </summary>
    private bool HasReachedDestination()
    {
        if (useNavMesh && navAgent != null)
        {
            return !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance;
        }
        else
        {
            return Vector3.Distance(transform.position, targetPosition) < 0.2f;
        }
    }

    /// <summary>
    /// Called when destination is reached
    /// </summary>
    private void OnReachedDestination()
    {
        isMoving = false;

        // If target was an interaction point, use it
        if (targetInteraction != null)
        {
            targetInteraction.StartInteraction(gameObject);
            targetInteraction = null;
        }
        else
        {
            // Just wandering - go idle
            animalController.SetState(CatState.Idle);
            animalController.SetMovementSpeed(0f);

            // Start idle timer
            idleTimer = idleTimeAtDestination;
        }

        Debug.Log($"✅ Reached destination");
    }

    /// <summary>
    /// Update animation to match movement speed
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (navAgent != null && animalController != null)
        {
            // Map NavMeshAgent velocity to animation speed
            float currentSpeed = navAgent.velocity.magnitude;
            float normalizedSpeed = Mathf.InverseLerp(0f, runSpeed, currentSpeed);

            animalController.SetMovementSpeed(normalizedSpeed);
        }
    }

    #endregion

    #region Jumping

    /// <summary>
    /// Perform jump to target position
    /// </summary>
    private System.Collections.IEnumerator PerformJump(Vector3 target)
    {
        isJumping = true;

        // Disable NavMesh during jump
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        // Face target
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        // Trigger jump animation
        animalController.TriggerAction("jump");

        // Physics-based jump
        Vector3 startPos = transform.position;
        float jumpDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            // Parabolic arc
            Vector3 currentPos = Vector3.Lerp(startPos, target, t);
            currentPos.y += jumpHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = currentPos;

            yield return null;
        }

        // Ensure we're at target
        transform.position = target;

        // Trigger land animation
        animalController.TriggerAction("land");

        // Re-enable NavMesh
        if (navAgent != null)
        {
            navAgent.enabled = true;
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas);
            transform.position = hit.position;
        }

        isJumping = false;

        Debug.Log($"🦘 Jumped to {target}");
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Draw wander radius
        if (enableWandering)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }

        // Draw target position
        if (isMoving)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }

        // Draw nav path
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }

    #endregion

    #region Public Getters

    public bool IsMoving => isMoving;
    public bool IsJumping => isJumping;
    public Vector3 Velocity => navAgent != null ? navAgent.velocity : Vector3.zero;

    #endregion
}
