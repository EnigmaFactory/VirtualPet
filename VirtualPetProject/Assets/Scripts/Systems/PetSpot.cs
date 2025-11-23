using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines a pettable area on a cat model
/// Attach to colliders on cat (head, back, belly, etc.)
/// Tracks petting state and triggers reactions
/// </summary>
[RequireComponent(typeof(Collider))]
public class PetSpot : MonoBehaviour
{
    [Header("Spot Configuration")]
    [SerializeField] private string spotName = "Head";
    [SerializeField] private PetSpotType spotType = PetSpotType.Head;
    [SerializeField] private bool isFavoriteSpot = false;
    [SerializeField] private bool isPettable = true;

    [Header("Reaction Settings")]
    [SerializeField] private float reactionDelay = 0.1f;
    [SerializeField] private AnimationCurve reactionIntensity = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Visual Feedback")]
    [SerializeField] private bool highlightOnHover = false;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.8f, 0.3f);

    [Header("Events")]
    public UnityEvent OnPettingStartedEvent;
    public UnityEvent OnPettedEvent;
    public UnityEvent OnPettingEndedEvent;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Runtime state
    private Renderer spotRenderer;
    private Material originalMaterial;
    private bool isBeingPetted = false;
    private float pettingDuration = 0f;
    private GameObject catGameObject;

    void Awake()
    {
        // Get renderer for highlighting
        spotRenderer = GetComponent<Renderer>();

        if (spotRenderer != null)
        {
            originalMaterial = spotRenderer.material;
        }

        // Find parent cat GameObject
        catGameObject = FindCatParent();

        // Ensure collider is trigger for overlap detection
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false; // NOT trigger - we want raycast hits
        }
    }

    void Update()
    {
        if (isBeingPetted)
        {
            pettingDuration += Time.deltaTime;
        }
    }

    #region Petting Events

    public void OnPettingStarted()
    {
        isBeingPetted = true;
        pettingDuration = 0f;

        // Trigger event
        OnPettingStartedEvent?.Invoke();

        // Visual feedback
        if (highlightOnHover)
        {
            HighlightSpot();
        }

        if (showDebugInfo)
        {
            Debug.Log($"🤗 Petting started: {spotName} ({spotType})");
        }
    }

    public void OnPetted()
    {
        // Trigger event
        OnPettedEvent?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log($"👋 Petted: {spotName}");
        }
    }

    public void OnPettingEnded()
    {
        isBeingPetted = false;

        // Trigger event
        OnPettingEndedEvent?.Invoke();

        // Remove highlight
        if (highlightOnHover)
        {
            RemoveHighlight();
        }

        if (showDebugInfo)
        {
            Debug.Log($"✋ Petting ended: {spotName} (duration: {pettingDuration:F1}s)");
        }

        pettingDuration = 0f;
    }

    #endregion

    #region Visual Feedback

    void HighlightSpot()
    {
        if (spotRenderer == null) return;

        if (highlightMaterial != null)
        {
            spotRenderer.material = highlightMaterial;
        }
        else
        {
            // Tint with highlight color
            Material tempMat = new Material(originalMaterial);
            tempMat.color = highlightColor;
            spotRenderer.material = tempMat;
        }
    }

    void RemoveHighlight()
    {
        if (spotRenderer == null) return;

        spotRenderer.material = originalMaterial;
    }

    #endregion

    #region Helper Methods

    GameObject FindCatParent()
    {
        Transform current = transform;

        // Walk up hierarchy to find root cat object
        while (current != null)
        {
            // Check if this is the cat root (has AnimalController)
            if (current.GetComponent<AnimalController>() != null)
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        // Fallback: use root of hierarchy
        return transform.root.gameObject;
    }

    public GameObject GetCatGameObject()
    {
        return catGameObject;
    }

    public float GetReactionIntensity()
    {
        return reactionIntensity.Evaluate(pettingDuration);
    }

    #endregion

    #region Getters

    public string SpotName => spotName;
    public PetSpotType SpotType => spotType;
    public bool IsFavoriteSpot => isFavoriteSpot;
    public bool IsPettable => isPettable;
    public bool IsBeingPetted => isBeingPetted;
    public float PettingDuration => pettingDuration;

    #endregion

    #region Debug

    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            Gizmos.color = isFavoriteSpot ? Color.yellow : Color.cyan;

            if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius * transform.lossyScale.x);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is CapsuleCollider capsule)
            {
                // Draw approximate capsule
                Gizmos.DrawWireSphere(transform.position, capsule.radius * transform.lossyScale.x);
            }
        }
    }

    #endregion
}

/// <summary>
/// Types of pet spots on a cat
/// Different spots may have different reactions
/// </summary>
public enum PetSpotType
{
    Head,       // Favorite for most cats
    Ears,       // Gentle petting
    Chin,       // Scratching spot
    Back,       // Long strokes
    Belly,      // Risky! Some cats hate this
    Tail,       // Usually off-limits
    Chest,      // Gentle petting
    Paws        // Most cats dislike
}
