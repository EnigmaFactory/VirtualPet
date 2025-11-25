using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth camera system for virtual pet game
/// Supports orbit mode (free rotation) and focus mode (follow cat)
/// Handles mouse, touch, and keyboard input
/// Auto-frames cats and smoothly transitions between targets
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Modes")]
    [SerializeField] private CameraMode currentMode = CameraMode.Orbit;
    [SerializeField] private Transform focusTarget; // Current cat or room center
    public bool autoFocusOnAdopt = true;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitDistance = 5f;
    [SerializeField] private float orbitHeight = 2f;
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float orbitAngle = 0f; // Current horizontal angle
    [SerializeField] private float tiltAngle = 30f; // Vertical tilt (0-90)

    [Header("Focus Settings")]
    [SerializeField] private float focusDistance = 3f;
    [SerializeField] private float focusHeight = 1.5f;
    [SerializeField] private float focusFollowSpeed = 5f;
    [SerializeField] private Vector3 focusOffset = Vector3.zero;
    
    [Header("Orbit Settings - Offset")]
    [SerializeField] private Vector3 orbitOffset = new Vector3(0f, 0.3f, 0f); // Offset from target position (e.g., to focus on cat's body instead of feet)

    [Header("Zoom Settings")]
    [SerializeField] private float minDistance = 0.5f; // Allow very close to cat
    [SerializeField] private float maxDistance = 6f; // Closer max distance
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothTime = 0.2f;
    private float zoomVelocity = 0f;

    [Header("Input Settings")]
    [SerializeField] private bool allowOrbitDrag = true;
    [SerializeField] private bool allowZoom = true;
    [SerializeField] private bool allowClickToFocus = true;
    [SerializeField] private float dragSensitivity = 0.3f;
    [SerializeField] private LayerMask clickableLayer; // What can be clicked to focus

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.2f;
    [SerializeField] private float modeSwitchDuration = 0.5f;

    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private Bounds roomBounds = new Bounds(Vector3.zero, new Vector3(10f, 5f, 10f));

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showBounds = true;

    // Runtime state
    private Camera cam;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 positionVelocity;
    private Vector3 rotationVelocity;

    // Input state
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private Vector2 dragDelta;

    // Target tracking
    private List<GameObject> trackedCats = new List<GameObject>();
    private Vector3 catsCenter = Vector3.zero;

    // Mode switching
    private float modeSwitchProgress = 1f;
    private CameraMode previousMode;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
            enabled = false;
        }
    }

    void Start()
    {
        // Find all cats in scene
        RefreshCatList();

        // Set initial focus target
        if (focusTarget == null)
        {
            focusTarget = FindOrCreateRoomCenter();
        }

        // Initialize camera position
        UpdateTargetTransform();
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    void LateUpdate()
    {
        // Handle input
        HandleInput();

        // Update target transform based on mode
        UpdateTargetTransform();

        // Smooth camera movement
        SmoothCameraMovement();

        // Debug visualization
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    #region Input Handling

    void HandleInput()
    {
        // Mouse/Touch drag for orbit
        if (allowOrbitDrag)
        {
            HandleOrbitDrag();
        }

        // Scroll wheel zoom
        if (allowZoom)
        {
            HandleZoom();
        }

        // Click to focus on cat
        if (allowClickToFocus && Input.GetMouseButtonDown(0))
        {
            HandleClickToFocus();
        }

        // Keyboard shortcuts
        HandleKeyboardInput();
    }

    void HandleOrbitDrag()
    {
        // Start drag
        if (Input.GetMouseButtonDown(1)) // Right-click drag
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        // End drag
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        // Update drag
        if (isDragging)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            dragDelta = (currentMousePosition - lastMousePosition) * dragSensitivity;
            lastMousePosition = currentMousePosition;

            // Apply rotation
            orbitAngle += dragDelta.x;
            tiltAngle -= dragDelta.y;

            // Clamp tilt
            tiltAngle = Mathf.Clamp(tiltAngle, 10f, 80f);

            // Normalize orbit angle
            orbitAngle = orbitAngle % 360f;
        }
    }

    void HandleZoom()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            if (currentMode == CameraMode.Orbit)
            {
                orbitDistance -= scrollDelta * zoomSpeed;
                orbitDistance = Mathf.Clamp(orbitDistance, minDistance, maxDistance);
            }
            else if (currentMode == CameraMode.Focus)
            {
                focusDistance -= scrollDelta * zoomSpeed;
                focusDistance = Mathf.Clamp(focusDistance, minDistance, maxDistance);
            }
        }
    }

    void HandleClickToFocus()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayer))
        {
            // Check if we hit a cat
            GameObject hitObject = hit.collider.gameObject;
            Transform catTransform = hitObject.transform;

            // Try to find root cat object (might hit child collider)
            while (catTransform != null && catTransform.GetComponent<AnimalController>() == null)
            {
                catTransform = catTransform.parent;
            }

            if (catTransform != null && catTransform.GetComponent<AnimalController>() != null)
            {
                FocusOnTarget(catTransform);
                Debug.Log($"📷 Focusing on cat: {catTransform.name}");
            }
        }
    }

    void HandleKeyboardInput()
    {
        // Tab: Switch mode
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMode();
        }

        // F: Focus on nearest cat
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOnNearestCat();
        }

        // Home: Reset to orbit mode
        if (Input.GetKeyDown(KeyCode.Home))
        {
            SetMode(CameraMode.Orbit);
        }

        // Frame all cats (Auto-zoom to fit all)
        if (Input.GetKeyDown(KeyCode.A))
        {
            FrameAllCats();
        }
    }

    #endregion

    #region Camera Positioning

    void UpdateTargetTransform()
    {
        switch (currentMode)
        {
            case CameraMode.Orbit:
                UpdateOrbitMode();
                break;

            case CameraMode.Focus:
                UpdateFocusMode();
                break;

            case CameraMode.Free:
                UpdateFreeMode();
                break;
        }
    }

    void UpdateOrbitMode()
    {
        // Orbit around focus target (or room center) with offset
        Vector3 center = focusTarget != null ? focusTarget.position + orbitOffset : catsCenter;

        // Calculate position from angle and distance
        float radians = orbitAngle * Mathf.Deg2Rad;
        float tiltRadians = tiltAngle * Mathf.Deg2Rad;

        float horizontalDistance = orbitDistance * Mathf.Cos(tiltRadians);
        float verticalDistance = orbitDistance * Mathf.Sin(tiltRadians);

        Vector3 offset = new Vector3(
            Mathf.Sin(radians) * horizontalDistance,
            verticalDistance,
            Mathf.Cos(radians) * horizontalDistance
        );

        targetPosition = center + offset;

        // Look at center
        targetRotation = Quaternion.LookRotation(center - targetPosition);

        // Apply boundaries
        if (useBoundaries)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
    }

    void UpdateFocusMode()
    {
        if (focusTarget == null)
        {
            // Fall back to orbit if no target
            SetMode(CameraMode.Orbit);
            return;
        }

        // Position behind and above the target
        Vector3 targetForward = focusTarget.forward;
        Vector3 offset = -targetForward * focusDistance + Vector3.up * focusHeight + focusOffset;

        targetPosition = focusTarget.position + offset;
        targetRotation = Quaternion.LookRotation(focusTarget.position - targetPosition);

        // Apply boundaries
        if (useBoundaries)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
    }

    void UpdateFreeMode()
    {
        // Free mode: Camera doesn't auto-update
        // Used for manual control or cinematic sequences
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void SmoothCameraMovement()
    {
        // Smooth position
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime / rotationSmoothTime
        );
    }

    Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, roomBounds.min.x, roomBounds.max.x),
            Mathf.Clamp(position.y, roomBounds.min.y + 1f, roomBounds.max.y),
            Mathf.Clamp(position.z, roomBounds.min.z, roomBounds.max.z)
        );
    }

    #endregion

    #region Mode Switching

    public void SetMode(CameraMode newMode)
    {
        if (currentMode == newMode) return;

        previousMode = currentMode;
        currentMode = newMode;
        modeSwitchProgress = 0f;

        Debug.Log($"📷 Camera mode: {previousMode} → {currentMode}");
    }

    public void ToggleMode()
    {
        CameraMode nextMode = currentMode == CameraMode.Orbit
            ? CameraMode.Focus
            : CameraMode.Orbit;

        SetMode(nextMode);
    }

    #endregion

    #region Focus Targets

    public void FocusOnTarget(Transform target)
    {
        focusTarget = target;
        // Keep in Orbit mode - just orbit around the target, don't follow it
        SetMode(CameraMode.Orbit);
    }

    public void FocusOnNearestCat()
    {
        RefreshCatList();

        if (trackedCats.Count == 0)
        {
            Debug.LogWarning("No cats in scene to focus on!");
            return;
        }

        // Find nearest cat to camera
        GameObject nearest = trackedCats[0];
        float nearestDist = Vector3.Distance(transform.position, nearest.transform.position);

        foreach (var cat in trackedCats)
        {
            float dist = Vector3.Distance(transform.position, cat.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = cat;
            }
        }

        FocusOnTarget(nearest.transform);
    }

    public void FrameAllCats()
    {
        RefreshCatList();

        if (trackedCats.Count == 0)
        {
            Debug.LogWarning("No cats to frame!");
            return;
        }

        // Calculate bounding box of all cats
        Bounds catsBounds = new Bounds(trackedCats[0].transform.position, Vector3.zero);

        foreach (var cat in trackedCats)
        {
            catsBounds.Encapsulate(cat.transform.position);
        }

        // Calculate required distance to fit all cats
        float maxExtent = Mathf.Max(catsBounds.size.x, catsBounds.size.z);
        float requiredDistance = maxExtent / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));

        // Set orbit to frame all
        catsCenter = catsBounds.center;
        focusTarget = null; // Focus on center, not specific cat
        orbitDistance = Mathf.Clamp(requiredDistance * 1.5f, minDistance, maxDistance);
        SetMode(CameraMode.Orbit);

        Debug.Log($"📷 Framing {trackedCats.Count} cats (distance: {orbitDistance:F1})");
    }

    #endregion

    #region Cat Tracking

    public void RefreshCatList()
    {
        trackedCats.Clear();

        // Find all cats with AnimalController
        AnimalController[] controllers = FindObjectsOfType<AnimalController>();

        foreach (var controller in controllers)
        {
            trackedCats.Add(controller.gameObject);
        }

        // Update cats center
        if (trackedCats.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            foreach (var cat in trackedCats)
            {
                sum += cat.transform.position;
            }
            catsCenter = sum / trackedCats.Count;
        }
    }

    public void OnCatAdopted(GameObject cat)
    {
        trackedCats.Add(cat);
        RefreshCatList();

        if (autoFocusOnAdopt)
        {
            FocusOnTarget(cat.transform);
        }
    }

    public void OnCatRemoved(GameObject cat)
    {
        trackedCats.Remove(cat);
        RefreshCatList();

        // If we were focusing on removed cat, switch to orbit
        if (focusTarget != null && focusTarget.gameObject == cat)
        {
            SetMode(CameraMode.Orbit);
        }
    }

    #endregion

    #region Helpers

    Transform FindOrCreateRoomCenter()
    {
        GameObject roomCenter = GameObject.Find("RoomCenter");

        if (roomCenter == null)
        {
            roomCenter = new GameObject("RoomCenter");
            roomCenter.transform.position = roomBounds.center;
        }

        return roomCenter.transform;
    }

    #endregion

    #region Debug Visualization

    void DrawDebugInfo()
    {
        // Draw focus target
        if (focusTarget != null)
        {
            Debug.DrawLine(transform.position, focusTarget.position, Color.yellow);
        }

        // Draw room bounds
        if (showBounds && useBoundaries)
        {
            DrawBounds(roomBounds, Color.cyan);
        }

        // Draw cats center
        if (trackedCats.Count > 0)
        {
            Debug.DrawRay(catsCenter, Vector3.up * 2f, Color.green);
        }
    }

    void DrawBounds(Bounds bounds, Color color)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // Bottom face
        Debug.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), color);
        Debug.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), color);
        Debug.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z), color);
        Debug.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z), color);

        // Top face
        Debug.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), color);
        Debug.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), color);
        Debug.DrawLine(new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z), color);
        Debug.DrawLine(new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z), color);

        // Vertical edges
        Debug.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z), color);
        Debug.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), color);
        Debug.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), color);
        Debug.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z), color);
    }

    void OnDrawGizmosSelected()
    {
        // Draw room bounds in editor
        if (useBoundaries)
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);
        }

        // Draw focus target
        if (focusTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(focusTarget.position, 0.3f);
        }
    }

    #endregion

    #region Getters

    public CameraMode CurrentMode => currentMode;
    public Transform FocusTarget => focusTarget;
    public int TrackedCatsCount => trackedCats.Count;

    #endregion
}

public enum CameraMode
{
    Orbit,  // Free rotation around target
    Focus,  // Follow specific cat
    Free    // Manual control (for cinematics)
}
