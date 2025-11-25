using UnityEngine;

/// <summary>
/// Handles all IK (Inverse Kinematics) for realistic animal posing
/// Works with AnimalController for procedural enhancements
/// Integrates with Dynamic Bone for tail/ear physics
/// </summary>
public class IKController : MonoBehaviour
{
    [Header("Bone References - Assign from Red Deer rig")]
    [SerializeField] private Transform spine;
    [SerializeField] private Transform chest;
    [SerializeField] private Transform head;
    [SerializeField] private Transform neck;

    [Header("Paw Bones")]
    [SerializeField] private Transform frontLeftPaw;
    [SerializeField] private Transform frontRightPaw;
    [SerializeField] private Transform backLeftPaw;
    [SerializeField] private Transform backRightPaw;

    [Header("Tail Bones")]
    [SerializeField] private Transform tailBase;
    [SerializeField] private Transform[] tailBones; // Full tail chain

    [Header("Ear Bones (if separate from head)")]
    [SerializeField] private Transform leftEar;
    [SerializeField] private Transform rightEar;

    [Header("IK Settings")]
    [SerializeField] private bool enablePawIK = true;
    [SerializeField] private bool enableHeadIK = true;
    [SerializeField] private bool enableTailIK = false; // Disable if using Dynamic Bone
    [SerializeField] private float pawIKWeight = 1f;
    [SerializeField] private float headIKWeight = 0.5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float pawRaycastDistance = 0.5f;
    [SerializeField] private float pawGroundOffset = 0.05f;

    [Header("Look At")]
    [SerializeField] private Transform lookAtTarget; // Set by interaction system
    [SerializeField] private float headTurnSpeed = 5f;
    [SerializeField] private float maxHeadRotation = 80f; // Max degrees head can turn

    [Header("Body Lean")]
    [SerializeField] private bool enableBodyLean = true;
    [SerializeField] private float leanAmount = 5f; // Degrees

    [Header("Dynamic Bone Integration")]
    [SerializeField] private bool useDynamicBone = false;
    [SerializeField] private Component tailDynamicBone; // Assign DynamicBone component if you have it

    // Runtime state
    private Vector3[] pawTargets = new Vector3[4];
    private bool[] pawGrounded = new bool[4];
    private float currentBreathScale = 1f;
    private float earTwitchTimer = 0f;
    private bool earTwitchLeft = false;

    // Smoothing
    private Quaternion targetHeadRotation;
    private Quaternion targetTailRotation;

    void Start()
    {
        InitializePawTargets();
        targetHeadRotation = head != null ? head.localRotation : Quaternion.identity;
    }

    void LateUpdate()
    {
        // IK happens in LateUpdate after animation has run

        if (enablePawIK)
            UpdatePawIK();

        if (enableHeadIK && lookAtTarget != null)
            UpdateHeadLookAt();

        if (enableTailIK && !useDynamicBone && tailBase != null)
            UpdateTailIK();

        if (enableBodyLean)
            UpdateBodyLean();

        UpdateEarTwitch();
    }

    #region Paw IK

    /// <summary>
    /// Initialize paw targets to current positions
    /// </summary>
    private void InitializePawTargets()
    {
        pawTargets[0] = frontLeftPaw != null ? frontLeftPaw.position : transform.position;
        pawTargets[1] = frontRightPaw != null ? frontRightPaw.position : transform.position;
        pawTargets[2] = backLeftPaw != null ? backLeftPaw.position : transform.position;
        pawTargets[3] = backRightPaw != null ? backRightPaw.position : transform.position;
    }

    /// <summary>
    /// Adjust paw positions to ground using raycasts
    /// Makes cat stand naturally on uneven surfaces (furniture, stairs, etc.)
    /// </summary>
    private void UpdatePawIK()
    {
        Transform[] paws = { frontLeftPaw, frontRightPaw, backLeftPaw, backRightPaw };

        for (int i = 0; i < paws.Length; i++)
        {
            if (paws[i] == null) continue;

            // Raycast down from paw
            RaycastHit hit;
            Vector3 rayStart = paws[i].position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, pawRaycastDistance, groundLayer))
            {
                // Ground found
                pawGrounded[i] = true;
                pawTargets[i] = hit.point + Vector3.up * pawGroundOffset;

                // Apply IK (smoothly move paw to target)
                paws[i].position = Vector3.Lerp(
                    paws[i].position,
                    pawTargets[i],
                    pawIKWeight * Time.deltaTime * 10f
                );

                // Rotate paw to match surface normal
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * paws[i].rotation;
                paws[i].rotation = Quaternion.Slerp(paws[i].rotation, targetRotation, pawIKWeight * Time.deltaTime * 5f);
            }
            else
            {
                pawGrounded[i] = false;
            }

            // Debug visualization
            if (Application.isEditor)
            {
                Debug.DrawRay(rayStart, Vector3.down * pawRaycastDistance,
                              pawGrounded[i] ? Color.green : Color.red);
            }
        }
    }

    #endregion

    #region Head Look At

    /// <summary>
    /// Make head look at target (toy, player cursor, etc.)
    /// Smooth and constrained to realistic neck rotation
    /// </summary>
    private void UpdateHeadLookAt()
    {
        if (head == null || lookAtTarget == null) return;

        // Calculate look direction
        Vector3 directionToTarget = (lookAtTarget.position - head.position).normalized;

        // Check if target is within viewable range
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        if (angleToTarget > maxHeadRotation)
        {
            // Target too far to the side - don't look
            // Could trigger body turn here
            return;
        }

        // Calculate target rotation
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        // Blend toward target
        targetHeadRotation = Quaternion.Slerp(
            head.rotation,
            lookRotation,
            headIKWeight * headTurnSpeed * Time.deltaTime
        );

        head.rotation = targetHeadRotation;
    }

    /// <summary>
    /// Set what the cat should look at
    /// </summary>
    public void SetLookAtTarget(Transform target)
    {
        lookAtTarget = target;
    }

    /// <summary>
    /// Clear look at target (return to neutral)
    /// </summary>
    public void ClearLookAtTarget()
    {
        lookAtTarget = null;
        // Smoothly return to forward
        if (head != null)
        {
            targetHeadRotation = Quaternion.identity;
        }
    }

    #endregion

    #region Tail IK

    /// <summary>
    /// Position tail (if not using Dynamic Bone)
    /// </summary>
    private void UpdateTailIK()
    {
        // Simple tail curl/sway
        // If using Dynamic Bone, this is disabled
        if (tailBase != null)
        {
            tailBase.localRotation = Quaternion.Slerp(
                tailBase.localRotation,
                targetTailRotation,
                Time.deltaTime * 5f
            );
        }
    }

    /// <summary>
    /// Set tail base rotation (called by AnimalController for swaying)
    /// </summary>
    public void SetTailBaseRotation(float angle)
    {
        if (!useDynamicBone && tailBase != null)
        {
            targetTailRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    /// <summary>
    /// Curl tail (happy emotion)
    /// </summary>
    public void CurlTailUp()
    {
        if (!useDynamicBone && tailBase != null)
        {
            targetTailRotation = Quaternion.Euler(-30, 0, 0);
        }
    }

    /// <summary>
    /// Lower tail (scared/submissive)
    /// </summary>
    public void LowerTail()
    {
        if (!useDynamicBone && tailBase != null)
        {
            targetTailRotation = Quaternion.Euler(30, 0, 0);
        }
    }

    /// <summary>
    /// Puff tail (scared/aggressive)
    /// </summary>
    public void PuffTail()
    {
        // This would require mesh scaling or shader effect
        // Placeholder for Dynamic Bone parameter
        Debug.Log("Tail puffed! (Visual effect needed)");
    }

    #endregion

    #region Ear Animation

    /// <summary>
    /// Trigger ear twitch animation
    /// </summary>
    public void TriggerEarTwitch(bool leftEarTwitch)
    {
        earTwitchLeft = leftEarTwitch;
        earTwitchTimer = 0.2f; // Twitch duration
    }

    private void UpdateEarTwitch()
    {
        if (earTwitchTimer > 0)
        {
            earTwitchTimer -= Time.deltaTime;

            Transform earToTwitch = earTwitchLeft ? leftEar : rightEar;

            if (earToTwitch != null)
            {
                // Quick rotation and back
                float twitchAmount = Mathf.Sin((0.2f - earTwitchTimer) * Mathf.PI / 0.2f) * 15f;
                earToTwitch.localRotation = Quaternion.Euler(twitchAmount, 0, 0);
            }
        }
        else
        {
            // Return ears to neutral
            if (leftEar != null)
                leftEar.localRotation = Quaternion.Slerp(leftEar.localRotation, Quaternion.identity, Time.deltaTime * 10f);

            if (rightEar != null)
                rightEar.localRotation = Quaternion.Slerp(rightEar.localRotation, Quaternion.identity, Time.deltaTime * 10f);
        }
    }

    #endregion

    #region Body Lean

    /// <summary>
    /// Lean body during turns or jumps
    /// </summary>
    private void UpdateBodyLean()
    {
        // Get movement velocity from Rigidbody if available
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && spine != null)
        {
            // Lean into movement direction
            Vector3 velocity = rb.linearVelocity;
            float lateralSpeed = Vector3.Dot(velocity, transform.right);

            // Lean spine
            float targetLean = -lateralSpeed * leanAmount;
            Quaternion leanRotation = Quaternion.Euler(0, 0, targetLean);

            spine.localRotation = Quaternion.Slerp(
                spine.localRotation,
                leanRotation,
                Time.deltaTime * 5f
            );
        }
    }

    #endregion

    #region Breathing (called by AnimalController)

    public void SetBreathingScale(float scale)
    {
        currentBreathScale = scale;

        // Apply to chest bone
        if (chest != null)
        {
            chest.localScale = new Vector3(1f, scale, scale);
        }
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Visualize IK targets
        if (!Application.isPlaying) return;

        // Draw paw targets
        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++)
        {
            if (pawGrounded[i])
            {
                Gizmos.DrawSphere(pawTargets[i], 0.02f);
            }
        }

        // Draw look at target
        if (lookAtTarget != null && head != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(head.position, lookAtTarget.position);
        }
    }

    #endregion
}
