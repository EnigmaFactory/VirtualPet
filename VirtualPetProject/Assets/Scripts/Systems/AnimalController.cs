using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles all animation playback and blending for animals
/// Designed to work with Red Deer Cat Family Pack animations
/// Manual blending - lighter than full Animator Controller
/// </summary>
[RequireComponent(typeof(Animation))]
public class AnimalController : MonoBehaviour
{
    [Header("Animation Clips - Assign from Red Deer pack")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip[] idleVariations; // Multiple idle poses
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip sitClip;
    [SerializeField] private AnimationClip sitToStandClip;
    [SerializeField] private AnimationClip layDownClip;
    [SerializeField] private AnimationClip sleepClip;
    [SerializeField] private AnimationClip[] sleepVariations; // Breathing, twitching
    [SerializeField] private AnimationClip groomClip;
    [SerializeField] private AnimationClip stretchClip;
    [SerializeField] private AnimationClip jumpClip;
    [SerializeField] private AnimationClip landClip;
    [SerializeField] private AnimationClip[] playClips; // Pounce, bat, chase
    [SerializeField] private AnimationClip eatClip;
    [SerializeField] private AnimationClip drinkClip;

    [Header("Blend Settings")]
    [SerializeField] private float defaultBlendTime = 0.3f;
    [SerializeField] private float quickBlendTime = 0.1f;
    [SerializeField] private float slowBlendTime = 0.5f;

    [Header("Procedural Animation")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingSpeed = 1f;
    [SerializeField] private bool enableEarTwitch = true;
    [SerializeField] private float earTwitchChance = 0.02f; // Per frame
    [SerializeField] private bool enableTailSway = true;
    [SerializeField] private float tailSwaySpeed = 0.5f;

    [Header("Runtime State")]
    [SerializeField] private CatState currentState = CatState.Idle;
    [SerializeField] private string currentAnimationName;
    [SerializeField] private float currentSpeed = 0f; // 0 = idle, 1 = walk, 2 = run

    // Components
    private Animation animComponent;
    private IKController ikController;
    private CatData catData; // Reference to data model

    // Animation tracking
    private AnimationClip currentClip;
    private float idleVariationTimer = 0f;
    private float nextIdleVariation = 5f;
    private bool isTransitioning = false;

    // Procedural animation state
    private float breathingPhase = 0f;
    private float tailSwayPhase = 0f;

    void Awake()
    {
        animComponent = GetComponent<Animation>();
        ikController = GetComponent<IKController>();

        // Set animation component to always animate (even when offscreen for now)
        animComponent.playAutomatically = false;
        animComponent.cullingType = AnimationCullingType.AlwaysAnimate;
    }

    void Start()
    {
        // Start with idle
        PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
    }

    void Update()
    {
        // Update procedural animations
        if (enableBreathing)
            UpdateBreathing();

        if (enableTailSway && currentState != CatState.Sleeping)
            UpdateTailSway();

        if (enableEarTwitch && Random.value < earTwitchChance)
            TwitchEars();

        // Handle idle variations
        if (currentState == CatState.Idle)
        {
            UpdateIdleVariations();
        }

        // Update current speed for blend tree-like behavior
        UpdateMovementBlending();
    }

    #region Public API - Called by GameManager

    /// <summary>
    /// Set the cat data reference
    /// </summary>
    public void Initialize(CatData data)
    {
        catData = data;
    }

    /// <summary>
    /// Transition to a new state with appropriate animation
    /// </summary>
    public void SetState(CatState newState)
    {
        if (currentState == newState) return;

        CatState previousState = currentState;
        currentState = newState;

        switch (newState)
        {
            case CatState.Idle:
                PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.Sleeping:
                // Transition: Idle -> Lay Down -> Sleep
                if (previousState != CatState.Sleeping)
                {
                    PlayAnimation(layDownClip, slowBlendTime, WrapMode.Once, () =>
                    {
                        PlayAnimation(GetRandomSleepAnimation(), slowBlendTime, WrapMode.Loop);
                    });
                }
                break;

            case CatState.Grooming:
                PlayAnimation(groomClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.Eating:
                PlayAnimation(eatClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.Playing:
                PlayRandomPlayAnimation();
                break;

            case CatState.Exploring:
                // Start walking animation (speed controlled by movement)
                PlayAnimation(walkClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.Watching:
                // Sit and watch
                PlayAnimation(sitClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.BeingPetted:
                // Stay in current pose but add purring animation layer
                // (Can be additive animation if you have it)
                break;
        }

        Debug.Log($"🎬 AnimController: {previousState} → {newState}");
    }

    /// <summary>
    /// Set movement speed (0 = idle, 0.5 = walk, 1 = run)
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp01(speed);
    }

    /// <summary>
    /// Trigger one-shot animation (jump, stretch, etc.)
    /// </summary>
    public void TriggerAction(string action)
    {
        switch (action.ToLower())
        {
            case "jump":
                PlayAnimation(jumpClip, quickBlendTime, WrapMode.Once, () =>
                {
                    // Return to previous state
                    SetState(currentState);
                });
                break;

            case "land":
                PlayAnimation(landClip, quickBlendTime, WrapMode.Once);
                break;

            case "stretch":
                PlayAnimation(stretchClip, defaultBlendTime, WrapMode.Once);
                break;

            case "sit":
                PlayAnimation(sitClip, defaultBlendTime, WrapMode.Loop);
                break;

            case "stand":
                if (currentClip == sitClip && sitToStandClip != null)
                {
                    PlayAnimation(sitToStandClip, quickBlendTime, WrapMode.Once, () =>
                    {
                        PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                    });
                }
                break;
        }
    }

    #endregion

    #region Animation Playback

    /// <summary>
    /// Play animation with blending
    /// </summary>
    private void PlayAnimation(AnimationClip clip, float blendTime, WrapMode wrapMode, System.Action onComplete = null)
    {
        if (clip == null)
        {
            Debug.LogWarning($"Tried to play null animation clip!");
            return;
        }

        currentClip = clip;
        currentAnimationName = clip.name;

        // Add to animation component if not already there
        if (!animComponent[clip.name])
        {
            animComponent.AddClip(clip, clip.name);
        }

        // Set wrap mode
        animComponent[clip.name].wrapMode = wrapMode;

        // Blend to new animation
        if (animComponent.isPlaying)
        {
            animComponent.CrossFade(clip.name, blendTime);
        }
        else
        {
            animComponent.Play(clip.name);
        }

        // Handle one-shot completion
        if (wrapMode == WrapMode.Once && onComplete != null)
        {
            StartCoroutine(WaitForAnimationComplete(clip, onComplete));
        }
    }

    private System.Collections.IEnumerator WaitForAnimationComplete(AnimationClip clip, System.Action callback)
    {
        yield return new WaitForSeconds(clip.length);
        callback?.Invoke();
    }

    #endregion

    #region Movement Blending (Walk/Run)

    /// <summary>
    /// Blend between idle, walk, and run based on currentSpeed
    /// </summary>
    private void UpdateMovementBlending()
    {
        if (currentState != CatState.Exploring) return;

        if (currentSpeed < 0.1f)
        {
            // Stopped - go to idle
            if (currentClip != idleClip)
            {
                PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
            }
        }
        else if (currentSpeed < 0.6f)
        {
            // Walking
            if (currentClip != walkClip)
            {
                PlayAnimation(walkClip, defaultBlendTime, WrapMode.Loop);
            }

            // Speed up/slow down walk animation
            if (walkClip != null && animComponent[walkClip.name] != null)
            {
                animComponent[walkClip.name].speed = Mathf.Lerp(0.5f, 1f, currentSpeed / 0.6f);
            }
        }
        else
        {
            // Running
            if (currentClip != runClip)
            {
                PlayAnimation(runClip, quickBlendTime, WrapMode.Loop);
            }

            // Speed up run animation with speed
            if (runClip != null && animComponent[runClip.name] != null)
            {
                animComponent[runClip.name].speed = Mathf.Lerp(1f, 1.5f, (currentSpeed - 0.6f) / 0.4f);
            }
        }
    }

    #endregion

    #region Idle Variations

    /// <summary>
    /// Play random idle variation to keep cat looking alive
    /// </summary>
    private void UpdateIdleVariations()
    {
        idleVariationTimer += Time.deltaTime;

        if (idleVariationTimer >= nextIdleVariation)
        {
            idleVariationTimer = 0f;
            nextIdleVariation = Random.Range(5f, 15f);

            // Random chance to play a variation
            if (Random.value < 0.5f && idleVariations != null && idleVariations.Length > 0)
            {
                var variation = idleVariations[Random.Range(0, idleVariations.Length)];
                PlayAnimation(variation, defaultBlendTime, WrapMode.Once, () =>
                {
                    PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                });
            }
        }
    }

    #endregion

    #region Play Animations

    /// <summary>
    /// Play random play animation (pounce, bat, etc.)
    /// </summary>
    private void PlayRandomPlayAnimation()
    {
        if (playClips == null || playClips.Length == 0)
        {
            PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
            return;
        }

        var clip = playClips[Random.Range(0, playClips.Length)];
        PlayAnimation(clip, defaultBlendTime, WrapMode.Loop);
    }

    #endregion

    #region Sleep Variations

    /// <summary>
    /// Get random sleep animation (breathing, twitching, etc.)
    /// </summary>
    private AnimationClip GetRandomSleepAnimation()
    {
        if (sleepVariations != null && sleepVariations.Length > 0 && Random.value < 0.5f)
        {
            return sleepVariations[Random.Range(0, sleepVariations.Length)];
        }

        return sleepClip ?? idleClip;
    }

    #endregion

    #region Procedural Animation

    /// <summary>
    /// Subtle breathing motion
    /// </summary>
    private void UpdateBreathing()
    {
        breathingPhase += Time.deltaTime * breathingSpeed;

        // Apply to chest/spine bones (if IK controller has them)
        if (ikController != null)
        {
            float breathScale = 1f + Mathf.Sin(breathingPhase) * 0.02f; // 2% scale variation
            ikController.SetBreathingScale(breathScale);
        }
    }

    /// <summary>
    /// Gentle tail swaying when idle/calm
    /// </summary>
    private void UpdateTailSway()
    {
        tailSwayPhase += Time.deltaTime * tailSwaySpeed;

        // Let Dynamic Bone handle physics, but we can set base rotation
        if (ikController != null && !catData.isSleeping)
        {
            float swayAngle = Mathf.Sin(tailSwayPhase) * 10f; // ±10 degrees
            ikController.SetTailBaseRotation(swayAngle);
        }
    }

    /// <summary>
    /// Random ear twitch
    /// </summary>
    private void TwitchEars()
    {
        if (ikController != null)
        {
            ikController.TriggerEarTwitch(Random.value < 0.5f);
        }
    }

    #endregion

    #region Public Getters

    public CatState CurrentState => currentState;
    public AnimationClip CurrentClip => currentClip;
    public bool IsTransitioning => isTransitioning;

    #endregion
}
