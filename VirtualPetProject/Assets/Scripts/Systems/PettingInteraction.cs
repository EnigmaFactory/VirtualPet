using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Handles petting interaction with cats using mouse or touch input
/// Detects pet spots on cat models, provides feedback, and increases affection
/// Supports stroke gestures (drag) and tap petting
/// </summary>
public class PettingInteraction : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask pettableLayer;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private bool allowTapPetting = true;
    [SerializeField] private bool allowStrokePetting = true;

    [Header("Petting Mechanics")]
    [SerializeField] private float minStrokeDistance = 0.1f; // Minimum drag to count as stroke
    [SerializeField] private float strokeSpeed = 1f; // Affects affection gain
    [SerializeField] private float tapCooldown = 0.5f; // Prevent spam tapping
    [SerializeField] private float maxPettingDuration = 10f; // Max continuous petting

    [Header("Affection Rewards")]
    [SerializeField] private float affectionPerStroke = 2f;
    [SerializeField] private float affectionPerTap = 1f;
    [SerializeField] private float bonusForFavoriteSpot = 1.5f; // Multiplier

    [Header("Feedback")]
    [SerializeField] private bool spawnParticles = true;
    [SerializeField] private GameObject petParticlePrefab;
    [SerializeField] private bool playSound = true;
    [SerializeField] private AudioClip petSound;
    [SerializeField] private bool showFloatingText = true;

    [Header("Animation")]
    [SerializeField] private bool triggerReactionAnimation = true;
    [SerializeField] private string happyAnimationTrigger = "Happy";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Runtime state
    private Camera cam;
    private AudioSource audioSource;

    // Petting state
    private bool isPetting = false;
    private Vector3 petStartPosition;
    private Vector3 lastPetPosition;
    private float strokeDistance = 0f;
    private float lastTapTime = 0f;
    private PetSpot currentPetSpot;
    private GameObject currentCat;
    private float pettingStartTime = 0f;

    // Input tracking
    private bool wasMouseDown = false;
    private Vector2 dragStartPosition;

    void Awake()
    {
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    void Update()
    {
        // Skip if over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandlePettingInput();
    }

    #region Input Handling

    void HandlePettingInput()
    {
        // Mouse/Touch down - Start petting
        if (Input.GetMouseButtonDown(0))
        {
            TryStartPetting(Input.mousePosition);
            wasMouseDown = true;
            dragStartPosition = Input.mousePosition;
        }

        // Mouse/Touch drag - Continue petting (stroke)
        if (Input.GetMouseButton(0) && isPetting && allowStrokePetting)
        {
            ContinueStrokePetting(Input.mousePosition);
        }

        // Mouse/Touch up - End petting
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 dragEndPosition = Input.mousePosition;
            float dragDistance = Vector2.Distance(dragStartPosition, dragEndPosition);

            // If barely moved, count as tap
            if (dragDistance < minStrokeDistance * 100f && allowTapPetting)
            {
                TryTapPet(Input.mousePosition);
            }

            EndPetting();
            wasMouseDown = false;
        }

        // Check for max duration
        if (isPetting && Time.time - pettingStartTime > maxPettingDuration)
        {
            EndPetting();
        }
    }

    void TryStartPetting(Vector2 screenPosition)
    {
        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, pettableLayer))
        {
            // Check if we hit a pet spot
            PetSpot petSpot = hit.collider.GetComponent<PetSpot>();

            if (petSpot != null && petSpot.IsPettable)
            {
                StartPetting(petSpot, hit.point);
            }
        }
    }

    void StartPetting(PetSpot petSpot, Vector3 worldPosition)
    {
        isPetting = true;
        currentPetSpot = petSpot;
        currentCat = petSpot.GetCatGameObject();
        petStartPosition = worldPosition;
        lastPetPosition = worldPosition;
        strokeDistance = 0f;
        pettingStartTime = Time.time;

        // Notify pet spot
        petSpot.OnPettingStarted();

        if (showDebugInfo)
        {
            Debug.Log($"🤗 Started petting {currentCat?.name} on {petSpot.SpotName}");
        }
    }

    void ContinueStrokePetting(Vector2 screenPosition)
    {
        if (!isPetting || currentPetSpot == null) return;

        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, pettableLayer))
        {
            Vector3 currentPosition = hit.point;

            // Calculate stroke distance
            float stepDistance = Vector3.Distance(lastPetPosition, currentPosition);
            strokeDistance += stepDistance;

            // Award affection for continuous stroking
            if (strokeDistance >= minStrokeDistance)
            {
                float affectionGain = CalculateAffectionGain(affectionPerStroke);
                ApplyAffection(affectionGain);

                // Spawn feedback
                SpawnPetFeedback(hit.point);

                // Reset stroke distance
                strokeDistance = 0f;
            }

            lastPetPosition = currentPosition;
        }
    }

    void TryTapPet(Vector2 screenPosition)
    {
        // Check cooldown
        if (Time.time - lastTapTime < tapCooldown)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, pettableLayer))
        {
            PetSpot petSpot = hit.collider.GetComponent<PetSpot>();

            if (petSpot != null && petSpot.IsPettable)
            {
                // Quick tap pet
                currentPetSpot = petSpot;
                currentCat = petSpot.GetCatGameObject();

                float affectionGain = CalculateAffectionGain(affectionPerTap);
                ApplyAffection(affectionGain);

                // Spawn feedback
                SpawnPetFeedback(hit.point);

                // Trigger reaction
                TriggerReaction();

                // Notify pet spot
                petSpot.OnPetted();

                lastTapTime = Time.time;

                if (showDebugInfo)
                {
                    Debug.Log($"👋 Tapped {currentCat?.name} on {petSpot.SpotName} (+{affectionGain:F1} affection)");
                }
            }
        }
    }

    void EndPetting()
    {
        if (!isPetting) return;

        // Notify pet spot
        currentPetSpot?.OnPettingEnded();

        // Trigger reaction animation
        TriggerReaction();

        if (showDebugInfo)
        {
            Debug.Log($"✋ Stopped petting {currentCat?.name}");
        }

        isPetting = false;
        currentPetSpot = null;
        currentCat = null;
    }

    #endregion

    #region Affection and Rewards

    float CalculateAffectionGain(float baseAffection)
    {
        float affection = baseAffection;

        // Bonus for favorite spot
        if (currentPetSpot != null && currentPetSpot.IsFavoriteSpot)
        {
            affection *= bonusForFavoriteSpot;
        }

        // Bonus based on personality
        if (currentCat != null)
        {
            CatData catData = GetCatData(currentCat);

            if (catData != null)
            {
                switch (catData.personality)
                {
                    case CatPersonality.Affectionate:
                        affection *= 1.5f; // Loves petting
                        break;
                    case CatPersonality.Independent:
                        affection *= 0.7f; // Tolerates petting
                        break;
                    case CatPersonality.Playful:
                        affection *= 1.2f; // Enjoys petting
                        break;
                }
            }
        }

        return affection;
    }

    void ApplyAffection(float amount)
    {
        if (currentCat == null) return;

        CatData catData = GetCatData(currentCat);

        if (catData != null)
        {
            catData.affection = Mathf.Min(catData.affection + amount, 100f);

            if (showDebugInfo)
            {
                Debug.Log($"❤️ {catData.name} affection: {catData.affection:F1}/100 (+{amount:F1})");
            }

            // Notify GameManager
            GameManager.Instance?.OnCatAffectionChanged(catData.catId, catData.affection);
        }
    }

    CatData GetCatData(GameObject catObject)
    {
        // Try to find CatData from GameManager
        if (GameManager.Instance?.PlayerProfile == null) return null;

        // Search for matching cat by name or ID
        foreach (var cat in GameManager.Instance.PlayerProfile.cats.Values)
        {
            if (catObject.name.Contains(cat.name) || catObject.name.Contains(cat.catId))
            {
                return cat;
            }
        }

        return null;
    }

    #endregion

    #region Feedback

    void SpawnPetFeedback(Vector3 worldPosition)
    {
        // Spawn particles
        if (spawnParticles && petParticlePrefab != null)
        {
            GameObject particles = Instantiate(petParticlePrefab, worldPosition, Quaternion.identity);
            Destroy(particles, 2f);
        }

        // Play sound
        if (playSound && petSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(petSound);
        }

        // Show floating text
        if (showFloatingText)
        {
            ShowFloatingHeart(worldPosition);
        }
    }

    void ShowFloatingHeart(Vector3 worldPosition)
    {
        // TODO: Implement floating heart/text effect
        // For now, just debug
        if (showDebugInfo)
        {
            Debug.DrawRay(worldPosition, Vector3.up * 0.5f, Color.magenta, 0.5f);
        }
    }

    void TriggerReaction()
    {
        if (!triggerReactionAnimation || currentCat == null) return;

        // Get AnimalController
        AnimalController animalController = currentCat.GetComponent<AnimalController>();

        if (animalController != null)
        {
            // Trigger happy animation or purr
            animalController.TriggerAction("Happy");
        }

        // Get IKController for head movement
        IKController ikController = currentCat.GetComponent<IKController>();

        if (ikController != null && currentPetSpot != null)
        {
            // Look at petting hand (camera)
            ikController.SetLookAtTarget(cam.transform);

            // Start tail curl up (happy)
            StartCoroutine(HappyTailCurl(ikController));
        }
    }

    IEnumerator HappyTailCurl(IKController ikController)
    {
        // Curl tail up (happy)
        ikController.CurlTailUp();
        yield return new WaitForSeconds(1f);

        // Return to normal
        ikController.SetTailBaseRotation(0f);
    }

    #endregion

    #region Debug

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Draw petting ray
        if (isPetting && currentPetSpot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastPetPosition, 0.05f);
        }
    }

    #endregion
}
