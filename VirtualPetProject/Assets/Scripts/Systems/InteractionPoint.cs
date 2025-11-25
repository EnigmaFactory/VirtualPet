using System.Collections;
using UnityEngine;

/// <summary>
/// Defines an interaction point on furniture or toys
/// Cats will navigate to these points and perform specific animations
/// Examples: Sit on couch, lie on bed, play with toy
/// </summary>
public class InteractionPoint : MonoBehaviour
{
    [Header("Interaction Type")]
    [SerializeField] private InteractionType interactionType = InteractionType.Sit;
    [SerializeField] private string customAnimationName; // Override default animation

    [Header("IK Targets (Optional)")]
    [SerializeField] private Transform frontLeftPawTarget;
    [SerializeField] private Transform frontRightPawTarget;
    [SerializeField] private Transform backLeftPawTarget;
    [SerializeField] private Transform backRightPawTarget;
    [SerializeField] private Transform headLookTarget; // Where cat looks while using this

    [Header("Positioning")]
    [SerializeField] private bool snapToPosition = true; // Teleport vs smooth move
    [SerializeField] private bool matchRotation = true;  // Align cat to point rotation

    [Header("Availability")]
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private int maxOccupants = 1; // Some furniture allows multiple cats
    [SerializeField] private CatPersonality[] preferredPersonalities; // Certain cats prefer certain spots

    [Header("Toy-Specific")]
    [SerializeField] private bool isToy = false;
    [SerializeField] private Rigidbody toyRigidbody; // For physics toys
    [SerializeField] private float playDuration = 5f; // How long cat plays before leaving
    [Header("Toy Physics")]
    [SerializeField] private Vector2 toyImpulseDelayRange = new Vector2(0.8f, 1.4f);
    [SerializeField] private float toyImpulseStrength = 1.3f;
    [SerializeField] private float toyTorqueStrength = 0.6f;
    [SerializeField] private float toyMaxSpeed = 2.5f;

    // Runtime
    private GameObject currentOccupant;
    private float occupiedStartTime;
    private Coroutine toyPlayRoutine;

    /// <summary>
    /// Can this interaction point be used by given cat?
    /// </summary>
    public bool CanBeUsedBy(AnimalController cat)
    {
        if (isOccupied && maxOccupants <= 1) return false;

        // Check personality preference
        if (preferredPersonalities != null && preferredPersonalities.Length > 0)
        {
            // TODO: Get cat personality from cat controller
            // For now, allow all
        }

        return true;
    }

    void Awake()
    {
        AutoAssignToyRigidbody();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAssignToyRigidbody();
    }
#endif

    /// <summary>
    /// Cat starts using this interaction point
    /// </summary>
    public void StartInteraction(GameObject catObject)
    {
        currentOccupant = catObject;
        isOccupied = true;
        occupiedStartTime = Time.time;

        AnimalController catController = catObject.GetComponent<AnimalController>();
        IKController ikController = catObject.GetComponent<IKController>();

        if (catController == null) return;

        // Position cat
        if (snapToPosition)
        {
            catObject.transform.position = transform.position;

            if (matchRotation)
            {
                catObject.transform.rotation = transform.rotation;
            }
        }

        // Apply IK targets
        if (ikController != null)
        {
            ApplyIKTargets(ikController);

            if (headLookTarget != null)
            {
                ikController.SetLookAtTarget(headLookTarget);
            }
        }

        // Trigger appropriate animation
        TriggerInteractionAnimation(catController);

        BeginToyPlay();

        Debug.Log($"🪑 {catObject.name} started {interactionType} interaction");
    }

    /// <summary>
    /// Cat stops using this interaction point
    /// </summary>
    public void EndInteraction(GameObject catObject)
    {
        if (currentOccupant != catObject) return;

        isOccupied = false;
        currentOccupant = null;

        // Clear IK
        IKController ikController = catObject.GetComponent<IKController>();
        if (ikController != null)
        {
            ikController.ClearLookAtTarget();
        }

        StopToyPlay();

        Debug.Log($"🚶 {catObject.name} ended {interactionType} interaction");
    }

    /// <summary>
    /// Auto-end interaction after duration (for toys)
    /// </summary>
    void Update()
    {
        if (isToy && isOccupied && currentOccupant != null)
        {
            if (Time.time - occupiedStartTime > playDuration)
            {
                EndInteraction(currentOccupant);
            }
        }
    }

    void OnDisable()
    {
        StopToyPlay();
    }

    /// <summary>
    /// Set up IK targets for this interaction
    /// </summary>
    private void ApplyIKTargets(IKController ikController)
    {
        // For now, IK targets are just Transform positions
        // Could extend this to actually drive IKController paw positions
    }

    /// <summary>
    /// Trigger the right animation based on interaction type
    /// </summary>
    private void TriggerInteractionAnimation(AnimalController catController)
    {
        if (!string.IsNullOrEmpty(customAnimationName))
        {
            catController.TriggerAction(customAnimationName);
            return;
        }

        switch (interactionType)
        {
            case InteractionType.Sit:
                catController.TriggerAction("sit");
                catController.SetState(CatState.Idle);
                break;

            case InteractionType.LayDown:
            case InteractionType.Sleep:
                catController.SetState(CatState.Sleeping);
                break;

            case InteractionType.Eat:
                catController.SetState(CatState.Eating);
                break;

            case InteractionType.Drink:
                catController.SetState(CatState.Eating); // Use same animation
                break;

            case InteractionType.Play:
                catController.SetState(CatState.Playing);
                break;

            case InteractionType.Scratch:
                catController.SetState(CatState.Grooming);
                break;

            case InteractionType.Climb:
                // Trigger climb animation
                catController.TriggerAction("jump");
                break;

            case InteractionType.Watch:
                catController.SetState(CatState.Watching);
                break;
        }
    }

    #region Gizmos

    void OnDrawGizmos()
    {
        // Visualize interaction point in editor
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);

        // Draw IK targets
        if (frontLeftPawTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(frontLeftPawTarget.position, 0.03f);
        }

        if (frontRightPawTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(frontRightPawTarget.position, 0.03f);
        }

        if (headLookTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, headLookTarget.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show more detail when selected
        if (isToy && toyRigidbody != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }

    #endregion

    void BeginToyPlay()
    {
        if (!isToy)
        {
            return;
        }

        AutoAssignToyRigidbody();

        if (toyRigidbody == null)
        {
            return;
        }

        ApplyToyImpulse(true);

        if (toyPlayRoutine != null)
        {
            StopCoroutine(toyPlayRoutine);
        }

        toyPlayRoutine = StartCoroutine(ToyPlayRoutine());
    }

    void StopToyPlay()
    {
        if (toyPlayRoutine != null)
        {
            StopCoroutine(toyPlayRoutine);
            toyPlayRoutine = null;
        }
    }

    IEnumerator ToyPlayRoutine()
    {
        while (isToy && isOccupied && currentOccupant != null && toyRigidbody != null)
        {
            yield return new WaitForSeconds(GetNextToyImpulseDelay());
            ApplyToyImpulse();
        }

        toyPlayRoutine = null;
    }

    void ApplyToyImpulse(bool boosted = false)
    {
        if (toyRigidbody == null)
        {
            return;
        }

        Vector3 catForward = currentOccupant != null ? currentOccupant.transform.forward : transform.forward;
        Vector3 random = Random.onUnitSphere;
        random.y = Mathf.Abs(random.y) * 0.4f;

        Vector3 impulseDirection = (catForward * 0.7f + random).normalized;
        float strength = boosted ? toyImpulseStrength * 1.3f : toyImpulseStrength;

        toyRigidbody.AddForce(impulseDirection * strength, ForceMode.Impulse);
        toyRigidbody.AddTorque(Random.onUnitSphere * toyTorqueStrength, ForceMode.Impulse);

        ClampToyVelocity();
    }

    float GetNextToyImpulseDelay()
    {
        float min = Mathf.Max(0.1f, Mathf.Min(toyImpulseDelayRange.x, toyImpulseDelayRange.y));
        float max = Mathf.Max(min + 0.01f, Mathf.Max(toyImpulseDelayRange.x, toyImpulseDelayRange.y));
        return Random.Range(min, max);
    }

    void ClampToyVelocity()
    {
        if (toyRigidbody == null || toyMaxSpeed <= 0f)
        {
            return;
        }

        float maxSpeedSqr = toyMaxSpeed * toyMaxSpeed;
        if (toyRigidbody.linearVelocity.sqrMagnitude > maxSpeedSqr)
        {
            toyRigidbody.linearVelocity = toyRigidbody.linearVelocity.normalized * toyMaxSpeed;
        }
    }

    void AutoAssignToyRigidbody()
    {
        if (!isToy || toyRigidbody != null)
        {
            return;
        }

        toyRigidbody = GetComponentInParent<Rigidbody>();
    }
}

/// <summary>
/// Types of interactions cats can have with objects
/// </summary>
public enum InteractionType
{
    Sit,        // Chair, couch, windowsill
    LayDown,    // Bed, rug, cardboard box
    Sleep,      // Bed (enter sleep state)
    Eat,        // Food bowl
    Drink,      // Water bowl
    Play,       // Toy (ball, mouse, feather wand)
    Scratch,    // Scratching post
    Climb,      // Cat tower, shelves
    Watch,      // Window, high spot
    Hide        // Box, under furniture
}
