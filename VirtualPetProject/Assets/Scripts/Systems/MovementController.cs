using UnityEngine;

/// <summary>
/// Handles cat movement using waypoints, raycasting, and interaction points
/// WebGL-optimized: No NavMesh dependency, uses furniture anchors and raycast pathfinding
/// Works with AnimalController to sync animations with movement
/// </summary>
[RequireComponent(typeof(AnimalController))]
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float rotationSpeed = 3f; // Slower, more cat-like rotation
    [SerializeField] private float stoppingDistance = 0.2f;

    [Header("Pathfinding")]
    [SerializeField] private LayerMask walkableLayer = ~0; // What surfaces cats can walk on
    [SerializeField] private float raycastCheckDistance = 0.5f; // Check ahead for obstacles
    [SerializeField] private float obstacleAvoidanceRadius = 0.3f;

    [Header("Wandering")]
    [SerializeField] private bool enableWandering = true;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderInterval = 5f; // Seconds between wanders (faster for testing)
    [SerializeField] private float idleTimeAtDestination = 3f; // Shorter idle time
    [SerializeField] private bool preferInteractionPoints = false; // Just wander randomly for now

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpDistance = 2f;
    [SerializeField] private LayerMask jumpableSurfaces = ~0;

    // Components
    private AnimalController animalController;
    private Rigidbody rb;
    private CatData catData;

    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private InteractionPoint targetInteraction;
    private float wanderTimer = 0f;
    private float idleTimer = 0f;
    private bool isJumping = false;
    private float currentMovementSpeed = 0f; // Track current movement speed (0-1)

    void Awake()
    {
        animalController = GetComponent<AnimalController>();
        rb = GetComponent<Rigidbody>();

        // Ensure Rigidbody is kinematic for direct position control
        if (rb != null)
        {
            rb.isKinematic = true;
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

        // Check if cat is in a non-movement state (grooming, eating, etc.)
        // BUT allow Exploring state to continue movement
        if (animalController != null)
        {
            CatState currentState = animalController.CurrentState;
            if (currentState == CatState.Grooming || currentState == CatState.Eating || 
                currentState == CatState.BeingPetted || currentState == CatState.Watching)
            {
                // Stop movement during these states
                if (isMoving)
                {
                    StopMovement();
                }
                // Ensure velocity is zero
                currentVelocity = Vector3.zero;
                return; // Don't update movement or wandering
            }
            // If state is Exploring, we should be moving - continue
        }

        // Update wandering - ensure it's enabled and working
        if (enableWandering && !isMoving && targetInteraction == null)
        {
            UpdateWandering();
        }

        // Update movement - only if actually moving
        if (isMoving && !isJumping)
        {
            // Make sure we're in Exploring state to allow movement
            if (animalController != null && animalController.CurrentState == CatState.Exploring)
            {
                UpdateMovement();
            }
            else
            {
                // State changed but isMoving is still true - stop movement
                Debug.LogWarning($"⚠️ isMoving=true but state is {animalController?.CurrentState}. Stopping movement.");
                StopMovement();
            }
        }
        else if (!isMoving)
        {
            // Ensure velocity is zero when not moving
            currentVelocity = Vector3.zero;
        }

        // Update movement animation
        UpdateMovementAnimation();

        // Apply movement in Update() for kinematic rigidbodies (more reliable)
        // FixedUpdate can cause timing issues with kinematic bodies
        if (isMoving && !isJumping && currentVelocity.magnitude > 0.01f)
        {
            // Use transform.position directly - works reliably for kinematic rigidbodies
            transform.position = transform.position + currentVelocity * Time.deltaTime;
            
            // Debug every few frames
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"🚶 Moving: pos={transform.position:F2}, vel={currentVelocity.magnitude:F2}m/s, target={targetPosition:F2}, dist={Vector3.Distance(transform.position, targetPosition):F2}m");
            }
        }

        // Check if reached destination
        if (isMoving && HasReachedDestination())
        {
            OnReachedDestination();
        }
    }

    void Start()
    {
        // Ensure wandering is enabled by default
        if (!enableWandering)
        {
            Debug.LogWarning("⚠️ Wandering disabled! Enabling it for cat movement.");
            enableWandering = true;
        }

        // Debug info
        Debug.Log($"🐱 MovementController Start - Wandering: {enableWandering}, Interval: {wanderInterval}s, Radius: {wanderRadius}m, Position: {transform.position}");
        
        // Check if we have a floor to walk on
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
        {
            Debug.Log($"✅ Floor detected at Y={hit.point.y:F2} (cat at Y={transform.position.y:F2})");
        }
        else
        {
            Debug.LogWarning($"⚠️ No floor detected below cat at {transform.position}! Make sure there's a ground plane with a collider.");
        }

        // Force a test movement after 2 seconds if nothing happens
        StartCoroutine(TestMovementAfterDelay());
    }

    private System.Collections.IEnumerator TestMovementAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
        if (!isMoving && enableWandering)
        {
            Debug.Log("🧪 Test: Forcing a test movement...");
            Vector3 testDestination = transform.position + new Vector3(2f, 0, 2f);
            MoveTo(testDestination, false);
        }
    }

    void FixedUpdate()
    {
        // Movement is now handled in Update() for kinematic rigidbodies
        // This is more reliable than FixedUpdate for direct position control
        // FixedUpdate is kept empty but can be used for physics-based movement in future if needed
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
    }

    /// <summary>
    /// Move to specific position using raycast pathfinding
    /// </summary>
    public void MoveTo(Vector3 position, bool shouldRun = false)
    {
        targetPosition = position;
        isMoving = true;
        currentMovementSpeed = shouldRun ? 1f : 0.5f;

        // Start moving immediately - don't wait for rotation
        // Calculate initial direction and start moving right away
        Vector3 direction = (position - transform.position);
        direction.y = 0;
        if (direction.magnitude > 0.1f)
        {
            direction.Normalize();
            // Start with small velocity immediately
            currentVelocity = direction * walkSpeed * 0.3f; // Start at 30% speed
        }

        animalController.SetState(CatState.Exploring);
        animalController.SetMovementSpeed(0.3f); // Start animation at low speed

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
        targetPosition = transform.position; // Reset target to current position
        currentVelocity = Vector3.zero;
        currentMovementSpeed = 0f;

        if (animalController != null)
        {
            animalController.SetMovementSpeed(0f);
            animalController.SetTurnDirection(0f); // Reset turn direction
        }
    }

    /// <summary>
    /// Set movement speed directly (for testing)
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        currentMovementSpeed = speed;
        if (animalController != null)
        {
            animalController.SetMovementSpeed(speed);
        }
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
    /// Random wandering behavior when idle - prefers interaction points
    /// </summary>
    private void UpdateWandering()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;

            Vector3 destination = Vector3.zero;
            bool foundDestination = false;

            // Prefer interaction points if enabled
            if (preferInteractionPoints)
            {
                InteractionPoint[] availablePoints = FindAvailableInteractionPoints();
                if (availablePoints.Length > 0)
                {
                    destination = availablePoints[Random.Range(0, availablePoints.Length)].transform.position;
                    foundDestination = true;
                    Debug.Log($"🎯 Found interaction point at {destination}");
                }
            }

            // Fallback to random point with raycast validation
            if (!foundDestination)
            {
                destination = FindRandomWalkablePoint();
                foundDestination = destination != Vector3.zero;
                
                if (foundDestination)
                {
                    Debug.Log($"🚶 Found random walkable point at {destination} (distance: {Vector3.Distance(transform.position, destination):F2}m)");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not find walkable point! Cat at {transform.position}, wanderRadius: {wanderRadius}");
                }
            }

            if (foundDestination)
            {
                MoveTo(destination, false);
            }
            else
            {
                Debug.LogWarning("⚠️ No destination found for wandering - check floor/ground exists!");
            }
        }
    }

    /// <summary>
    /// Find available interaction points within wander radius
    /// </summary>
    private InteractionPoint[] FindAvailableInteractionPoints()
    {
        InteractionPoint[] allPoints = FindObjectsOfType<InteractionPoint>();
        System.Collections.Generic.List<InteractionPoint> available = new System.Collections.Generic.List<InteractionPoint>();

        foreach (var point in allPoints)
        {
            float distance = Vector3.Distance(transform.position, point.transform.position);
            if (distance <= wanderRadius && point.CanBeUsedBy(animalController))
            {
                available.Add(point);
            }
        }

        return available.ToArray();
    }

    /// <summary>
    /// Find a random walkable point using raycasting
    /// </summary>
    private Vector3 FindRandomWalkablePoint()
    {
        // Try more attempts
        for (int attempts = 0; attempts < 20; attempts++)
        {
            // Pick random direction
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Raycast down to find floor (start higher up)
            RaycastHit hit;
            Vector3 rayStart = candidate + Vector3.up * 5f; // Start 5 units up
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, walkableLayer))
            {
                // Make sure we found a valid floor (not too far from original Y)
                float yDifference = Mathf.Abs(hit.point.y - transform.position.y);
                if (yDifference < 2f) // Allow some vertical variation
                {
                    // Check if point is clear (no obstacles) - but don't check sphere, just use the hit point
                    return hit.point;
                }
            }
        }

        // Fallback: Just pick a point nearby on the same Y level
        Vector2 fallbackCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 fallbackPoint = transform.position + new Vector3(fallbackCircle.x, 0, fallbackCircle.y);
        Debug.Log($"⚠️ Using fallback point (no raycast hit): {fallbackPoint}");
        return fallbackPoint;
    }

    #endregion

    #region Movement

    /// <summary>
    /// Update movement towards target using raycast obstacle avoidance with meandering
    /// </summary>
    private void UpdateMovement()
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0; // Keep on same level
        float distance = direction.magnitude;

        if (distance < stoppingDistance)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        direction.Normalize();

        // Add meandering - cats don't walk in straight lines!
        float meanderAmount = 0.3f; // How much to meander (0 = straight, 1 = lots of meandering)
        Vector3 meanderOffset = Vector3.Cross(direction, Vector3.up) * Mathf.Sin(Time.time * 2f) * meanderAmount;
        Vector3 meanderedDirection = (direction + meanderOffset).normalized;

        // Simple obstacle avoidance with raycast
        Vector3 moveDirection = meanderedDirection;
        RaycastHit hit;
        
        // Check ahead for obstacles
        if (Physics.SphereCast(transform.position, obstacleAvoidanceRadius, meanderedDirection, out hit, raycastCheckDistance, walkableLayer))
        {
            // Try to steer around obstacle
            Vector3 avoidance = Vector3.Cross(hit.normal, Vector3.up);
            if (Vector3.Dot(avoidance, meanderedDirection) < 0)
                avoidance = -avoidance;
            
            moveDirection = Vector3.Lerp(meanderedDirection, avoidance.normalized, 0.5f);
        }

        // Calculate speed based on distance (slow down near target)
        float targetSpeed = Mathf.Lerp(walkSpeed, runSpeed, currentMovementSpeed);
        
        // More gradual slowdown - only slow down when very close
        float slowdownDistance = stoppingDistance * 3f; // Start slowing down 3x stopping distance away
        float speedMultiplier = Mathf.Clamp01(distance / slowdownDistance);
        targetSpeed *= speedMultiplier;
        
        // Ensure minimum speed when far from target
        if (distance > stoppingDistance * 2f && targetSpeed < walkSpeed * 0.5f)
        {
            targetSpeed = walkSpeed * 0.5f; // Minimum walking speed when far
        }

        // Apply movement with gradual acceleration - but always moving forward
        Vector3 targetVelocity = moveDirection * targetSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 5f); // Faster acceleration
        
        // Ensure minimum movement speed when moving (prevents stopping during rotation)
        if (isMoving && distance > stoppingDistance)
        {
            float minSpeed = walkSpeed * 0.5f; // Higher minimum speed (50% of walk speed)
            if (currentVelocity.magnitude < minSpeed)
            {
                currentVelocity = moveDirection * minSpeed;
            }
        }
        
        // Debug movement calculation
        if (Time.frameCount % 60 == 0 && isMoving)
        {
            Debug.Log($"🎯 Movement calc: distance={distance:F2}, targetSpeed={targetSpeed:F2}, speedMult={speedMultiplier:F2}, vel={currentVelocity.magnitude:F2}, moveDir={moveDirection}");
        }
        
        // Debug if velocity is zero but we should be moving
        if (isMoving && currentVelocity.magnitude < 0.01f && distance > stoppingDistance)
        {
            Debug.LogError($"❌ Movement stuck! Distance: {distance:F2}, targetSpeed: {targetSpeed:F2}, speedMultiplier: {speedMultiplier:F2}, moveDirection: {moveDirection}, rb: {rb != null}, isKinematic: {rb?.isKinematic}");
        }

        // Calculate turn angle for directional animations
        Vector3 forward = transform.forward;
        float turnAngle = Vector3.SignedAngle(forward, moveDirection, Vector3.up);
        
        // Always rotate while moving, but use directional animations for turns
        if (moveDirection.magnitude > 0.1f)
        {
            // Use directional animations for any significant turn (>15 degrees)
            if (Mathf.Abs(turnAngle) > 15f && animalController != null)
            {
                // Normalize turn direction to -1 to 1
                float normalizedTurn = Mathf.Clamp(turnAngle / 90f, -1f, 1f);
                animalController.SetTurnDirection(normalizedTurn);
                
                // Slower rotation during turns - cat meanders
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 0.7f * Time.deltaTime);
            }
            else
            {
                // Forward movement - faster rotation
                if (animalController != null)
                {
                    animalController.SetTurnDirection(0f);
                }
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Check if cat has reached destination
    /// </summary>
    private bool HasReachedDestination()
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        return distance < stoppingDistance;
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
        if (animalController != null && isMoving)
        {
            // Map current velocity to animation speed (gradual ramp-up)
            float speed = currentVelocity.magnitude;
            float normalizedSpeed = Mathf.InverseLerp(0f, runSpeed, speed);
            
            // Gradually ramp up animation speed to match actual movement (prevents full-speed animation in place)
            currentMovementSpeed = Mathf.Lerp(currentMovementSpeed, normalizedSpeed, Time.deltaTime * 4f);
            animalController.SetMovementSpeed(currentMovementSpeed);
        }
        else if (animalController != null && !isMoving)
        {
            // Gradually ramp down to idle
            currentMovementSpeed = Mathf.Lerp(currentMovementSpeed, 0f, Time.deltaTime * 5f);
            animalController.SetMovementSpeed(currentMovementSpeed);
            
            if (currentMovementSpeed < 0.05f)
            {
                currentMovementSpeed = 0f;
            }
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

        // Ensure we're at target (raycast down to find floor)
        RaycastHit hit;
        if (Physics.Raycast(target + Vector3.up * 2f, Vector3.down, out hit, 5f, walkableLayer))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = target;
        }

        // Trigger land animation
        animalController.TriggerAction("land");

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

            // Draw movement direction
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, currentVelocity.normalized * 0.5f);
        }
    }

    #endregion

    #region Public Getters

    public bool IsMoving => isMoving;
    public bool IsJumping => isJumping;
    public Vector3 Velocity => currentVelocity;

    #endregion
}
