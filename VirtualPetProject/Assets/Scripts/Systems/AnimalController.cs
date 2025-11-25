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
    [Header("Sit Animations")]
    [SerializeField] private AnimationClip sitStartClip; // Stand → Sit (transition)
    [SerializeField] private AnimationClip[] sitClips; // Sit loops (loop_1, loop_2, loop_3, loop_4)
    [SerializeField] private AnimationClip sitEndClip; // Sit → Stand (transition)
    
    [Header("Lie Belly (Awake - for chilling)")]
    [SerializeField] private AnimationClip lieBellyStartClip; // Stand → Lie on belly (transition)
    [SerializeField] private AnimationClip[] lieBellyClips; // Lie on belly awake loops (loop_1, loop_2, loop_3)
    [SerializeField] private AnimationClip lieBellyEndClip; // Lie on belly → Stand (transition)
    
    [Header("Lie Side (Awake - for chilling)")]
    [SerializeField] private AnimationClip lieSideStartClip; // Stand → Lie on side (transition)
    [SerializeField] private AnimationClip[] lieSideClips; // Lie on side awake loops (loop_1, loop_2)
    [SerializeField] private AnimationClip lieSideEndClip; // Lie on side → Stand (transition)
    
    [Header("Sleep - Belly")]
    [SerializeField] private AnimationClip lieBellySleepStartClip; // Lie belly awake → Lie belly sleep (transition)
    [SerializeField] private AnimationClip[] lieBellySleepClips; // Lie belly sleep loops
    [SerializeField] private AnimationClip lieBellySleepEndClip; // Lie belly sleep → Stand (transition)
    
    [Header("Sleep - Side")]
    [SerializeField] private AnimationClip lieSideSleepStartClip; // Lie side awake → Lie side sleep (transition)
    [SerializeField] private AnimationClip[] lieSideSleepClips; // Lie side sleep loops
    [SerializeField] private AnimationClip lieSideSleepEndClip; // Lie side sleep → Stand (transition)
    
    // Current lying state tracking
    private bool isLyingBelly = false;
    private bool isLyingSide = false;
    private int currentLieVariationIndex = 0;
    
    // Current sitting state tracking
    private bool isSitting = false;
    private int currentSitVariationIndex = 0;
    [SerializeField] private AnimationClip groomClip;
    [SerializeField] private AnimationClip stretchClip;
    [SerializeField] private AnimationClip jumpClip;
    [SerializeField] private AnimationClip landClip;
    [SerializeField] private AnimationClip[] playClips; // Pounce, bat, chase
    [SerializeField] private AnimationClip eatClip;
    [SerializeField] private AnimationClip drinkClip;

    [Header("Petting/Caress Animations")]
    [SerializeField] private AnimationClip caressIdleClip; // Being petted while standing
    [SerializeField] private AnimationClip caressSitClip; // Being petted while sitting
    [SerializeField] private AnimationClip caressLieClip; // Being petted while lying

    [Header("Directional Movement (Optional)")]
    [SerializeField] private AnimationClip walkLeftClip;
    [SerializeField] private AnimationClip walkRightClip;
    [SerializeField] private AnimationClip walkBackClip;
    [SerializeField] private AnimationClip runLeftClip;
    [SerializeField] private AnimationClip runRightClip;

    [Header("Scratching Animations")]
    [SerializeField] private AnimationClip scratchHorizClip; // Horizontal scratching
    [SerializeField] private AnimationClip scratchVertClip; // Vertical scratching

    [Header("Transition Animations")]
    [SerializeField] private AnimationClip transSitToLieBellyClip;
    [SerializeField] private AnimationClip transSitToLieSideClip;
    [SerializeField] private AnimationClip transLieBellyToSitClip;
    [SerializeField] private AnimationClip transLieSideToSitClip;

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
    [SerializeField] private float currentTurnDirection = 0f; // -1 = left, 0 = forward, 1 = right

    // Components
    private Animation animComponent;
    private IKController ikController;
    private CatData catData; // Reference to data model

    // Animation tracking
    private AnimationClip currentClip;
    private float idleVariationTimer = 0f;
    private float nextIdleVariation = 5f;
    private bool isTransitioning = false;
    private bool isStateLocked = false; // Prevent state changes during animations
    private float stateLockTimer = 0f;
    private float minStateDuration = 2f; // Minimum time to stay in a state

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
        // Update state lock timer
        if (isStateLocked)
        {
            stateLockTimer -= Time.deltaTime;
            if (stateLockTimer <= 0f)
            {
                isStateLocked = false;
            }
        }

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

        // Handle playing state - cycle through animations
        if (currentState == CatState.Playing)
        {
            UpdatePlayingState();
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
        
        // Prevent rapid state changes
        if (isStateLocked && stateLockTimer > 0f)
        {
            Debug.Log($"🔒 State locked, ignoring change: {currentState} → {newState}");
            return;
        }

        CatState previousState = currentState;
        currentState = newState;
        isStateLocked = true;
        stateLockTimer = minStateDuration;

        switch (newState)
        {
            case CatState.Idle:
                // Idle can be standing or sitting - check if we should sit
                if (Random.value < 0.3f && sitClips != null && sitClips.Length > 0)
                {
                    // 30% chance to sit when going idle
                    SetSitting();
                }
                else
                {
                    // Stand idle - will cycle through idle variations
                    PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                }
                break;

            case CatState.Sleeping:
                // Sleep flow: Choose belly or side → Start → Sleep loop → End when waking
                if (previousState != CatState.Sleeping)
                {
                    // Decide belly or side (random or based on current pose)
                    bool sleepOnBelly = isLyingBelly || (!isLyingSide && Random.value < 0.5f);
                    
                    if (sleepOnBelly)
                    {
                        // Belly sleep: Start → Sleep start → Sleep loop
                        if (isLyingBelly && lieBellySleepStartClip != null)
                        {
                            // Already lying belly awake → transition to sleep
                            PlayAnimation(lieBellySleepStartClip, slowBlendTime, WrapMode.Once, () =>
                            {
                                PlayAnimation(GetRandomBellySleepAnimation(), slowBlendTime, WrapMode.Loop);
                            });
                        }
                        else
                        {
                            // Stand → Lie belly start → Lie belly sleep start → Sleep loop
                            // First lie down awake, then transition to sleep
                            if (lieBellyStartClip != null)
                            {
                                PlayAnimation(lieBellyStartClip, slowBlendTime, WrapMode.Once, () =>
                                {
                                    // Play a brief awake lie loop, then transition to sleep
                                    if (lieBellyClips != null && lieBellyClips.Length > 0)
                                    {
                                        PlayAnimation(lieBellyClips[0], slowBlendTime * 0.5f, WrapMode.Once, () =>
                                        {
                                            if (lieBellySleepStartClip != null)
                                            {
                                                PlayAnimation(lieBellySleepStartClip, slowBlendTime, WrapMode.Once, () =>
                                                {
                                                    PlayAnimation(GetRandomBellySleepAnimation(), slowBlendTime, WrapMode.Loop);
                                                });
                                            }
                                            else
                                            {
                                                PlayAnimation(GetRandomBellySleepAnimation(), slowBlendTime, WrapMode.Loop);
                                            }
                                        });
                                    }
                                    else if (lieBellySleepStartClip != null)
                                    {
                                        PlayAnimation(lieBellySleepStartClip, slowBlendTime, WrapMode.Once, () =>
                                        {
                                            PlayAnimation(GetRandomBellySleepAnimation(), slowBlendTime, WrapMode.Loop);
                                        });
                                    }
                                    else
                                    {
                                        PlayAnimation(GetRandomBellySleepAnimation(), slowBlendTime, WrapMode.Loop);
                                    }
                                });
                            }
                            else
                            {
                                PlayAnimation(GetRandomBellySleepAnimation(), slowBlendTime, WrapMode.Loop);
                            }
                        }
                        isLyingBelly = true;
                        isLyingSide = false;
                    }
                    else
                    {
                        // Side sleep: Start → Sleep start → Sleep loop
                        if (isLyingSide && lieSideSleepStartClip != null)
                        {
                            // Already lying side awake → transition to sleep
                            PlayAnimation(lieSideSleepStartClip, slowBlendTime, WrapMode.Once, () =>
                            {
                                PlayAnimation(GetRandomSideSleepAnimation(), slowBlendTime, WrapMode.Loop);
                            });
                        }
                        else
                        {
                            // Stand → Lie side start → Lie side sleep start → Sleep loop
                            // First lie down awake, then transition to sleep
                            if (lieSideStartClip != null)
                            {
                                PlayAnimation(lieSideStartClip, slowBlendTime, WrapMode.Once, () =>
                                {
                                    // Play a brief awake lie loop, then transition to sleep
                                    if (lieSideClips != null && lieSideClips.Length > 0)
                                    {
                                        PlayAnimation(lieSideClips[0], slowBlendTime * 0.5f, WrapMode.Once, () =>
                                        {
                                            if (lieSideSleepStartClip != null)
                                            {
                                                PlayAnimation(lieSideSleepStartClip, slowBlendTime, WrapMode.Once, () =>
                                                {
                                                    PlayAnimation(GetRandomSideSleepAnimation(), slowBlendTime, WrapMode.Loop);
                                                });
                                            }
                                            else
                                            {
                                                PlayAnimation(GetRandomSideSleepAnimation(), slowBlendTime, WrapMode.Loop);
                                            }
                                        });
                                    }
                                    else if (lieSideSleepStartClip != null)
                                    {
                                        PlayAnimation(lieSideSleepStartClip, slowBlendTime, WrapMode.Once, () =>
                                        {
                                            PlayAnimation(GetRandomSideSleepAnimation(), slowBlendTime, WrapMode.Loop);
                                        });
                                    }
                                    else
                                    {
                                        PlayAnimation(GetRandomSideSleepAnimation(), slowBlendTime, WrapMode.Loop);
                                    }
                                });
                            }
                            else
                            {
                                PlayAnimation(GetRandomSideSleepAnimation(), slowBlendTime, WrapMode.Loop);
                            }
                        }
                        isLyingSide = true;
                        isLyingBelly = false;
                    }
                }
                break;

            case CatState.Grooming:
                // Play grooming animation for full cycle (stand -> sit -> lick foot -> stand)
                // This is a one-shot animation that shouldn't repeat frequently
                if (groomClip != null)
                {
                    // Unlock state after animation completes (grooming is a full cycle)
                    isStateLocked = true;
                    stateLockTimer = groomClip.length + 1f; // Lock for animation duration + 1 second
                    
                    PlayAnimation(groomClip, defaultBlendTime, WrapMode.Once, () =>
                    {
                        // After grooming completes, return to idle
                        if (currentState == CatState.Grooming)
                        {
                            isStateLocked = false; // Unlock state
                            SetState(CatState.Idle);
                        }
                    });
                }
                else
                {
                    PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                }
                break;

            case CatState.Eating:
                PlayAnimation(eatClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.Playing:
                // Cycle through play animations instead of looping one
                PlayNextPlayAnimation();
                break;

            case CatState.Exploring:
                // Start walking animation (speed controlled by movement)
                PlayAnimation(walkClip, defaultBlendTime, WrapMode.Loop);
                break;

            case CatState.Watching:
                // Sit and watch - use sit variations
                if (!isSitting)
                {
                    SetSitting();
                }
                else
                {
                    // Already sitting - just ensure we're playing a sit loop
                    if (sitClips != null && sitClips.Length > 0 && currentClip != sitClips[currentSitVariationIndex])
                    {
                        PlayAnimation(sitClips[currentSitVariationIndex], defaultBlendTime, WrapMode.Loop);
                    }
                }
                break;

            case CatState.BeingPetted:
                // Play appropriate caress animation based on current pose
                PlayCaressAnimation();
                break;
        }

        Debug.Log($"🎬 AnimController: {previousState} → {newState}");
    }

    /// <summary>
    /// Set movement speed (0 = idle, 0.5 = walk, 1 = run)
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        // Gradually ramp up speed instead of instant change
        currentSpeed = Mathf.Lerp(currentSpeed, Mathf.Clamp01(speed), Time.deltaTime * 5f);
    }

    /// <summary>
    /// Set turn direction for directional animations (-1 = left, 0 = forward, 1 = right)
    /// </summary>
    public void SetTurnDirection(float turnDirection)
    {
        // Store turn direction for directional animation selection
        // This will be used in UpdateMovementBlending
        currentTurnDirection = Mathf.Clamp(turnDirection, -1f, 1f);
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
                SetSitting();
                break;

            case "stand":
                GetUpFromSitting();
                break;

            case "scratch_horiz":
                PlayAnimation(scratchHorizClip ?? groomClip, defaultBlendTime, WrapMode.Loop);
                break;

            case "scratch_vert":
                PlayAnimation(scratchVertClip ?? groomClip, defaultBlendTime, WrapMode.Loop);
                break;

            case "scratch":
                // Randomly choose horizontal or vertical
                if (Random.value < 0.5f && scratchHorizClip != null)
                    PlayAnimation(scratchHorizClip, defaultBlendTime, WrapMode.Loop);
                else if (scratchVertClip != null)
                    PlayAnimation(scratchVertClip, defaultBlendTime, WrapMode.Loop);
                else
                    PlayAnimation(groomClip, defaultBlendTime, WrapMode.Loop);
                break;
        }
    }

    #region Sit Methods

    /// <summary>
    /// Set cat to sit down
    /// Uses: Stand → Sit_start → Sit_loop variations
    /// </summary>
    public void SetSitting()
    {
        if (sitClips == null || sitClips.Length == 0) return;
        
        if (isSitting)
        {
            // Already sitting - just cycle to next variation
            CycleSitVariation();
            return;
        }
        
        // Transition: Stand → Sit
        if (sitStartClip != null)
        {
            PlayAnimation(sitStartClip, defaultBlendTime, WrapMode.Once, () =>
            {
                PlayRandomSitVariation();
            });
        }
        else
        {
            PlayRandomSitVariation();
        }
        
        isSitting = true;
    }

    /// <summary>
    /// Play a random sit variation (can cycle through different loops)
    /// </summary>
    private void PlayRandomSitVariation()
    {
        if (sitClips == null || sitClips.Length == 0) return;
        
        // Cycle through variations or pick random
        currentSitVariationIndex = Random.Range(0, sitClips.Length);
        AnimationClip clip = sitClips[currentSitVariationIndex];
        
        if (clip != null)
        {
            PlayAnimation(clip, defaultBlendTime, WrapMode.Loop);
        }
    }

    /// <summary>
    /// Cycle to next sit variation (for variety while sitting)
    /// </summary>
    public void CycleSitVariation()
    {
        if (isSitting && sitClips != null && sitClips.Length > 0)
        {
            currentSitVariationIndex = (currentSitVariationIndex + 1) % sitClips.Length;
            PlayAnimation(sitClips[currentSitVariationIndex], defaultBlendTime, WrapMode.Loop);
        }
    }

    /// <summary>
    /// Get up from sitting position
    /// Uses: Sit_loop → Sit_end → Stand
    /// </summary>
    public void GetUpFromSitting()
    {
        if (!isSitting) return;
        
        if (sitEndClip != null)
        {
            PlayAnimation(sitEndClip, defaultBlendTime, WrapMode.Once, () =>
            {
                PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                isSitting = false;
            });
        }
        else
        {
            PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
            isSitting = false;
        }
    }

    /// <summary>
    /// Check if cat is currently sitting
    /// </summary>
    public bool IsSitting()
    {
        return isSitting;
    }

    #endregion

    /// <summary>
    /// Play appropriate caress animation based on current state
    /// </summary>
    private void PlayCaressAnimation()
    {
        AnimationClip caressClip = null;

        if (currentState == CatState.Sleeping)
        {
            caressClip = caressLieClip;
        }
        else if (currentState == CatState.Watching || isSitting)
        {
            caressClip = caressSitClip;
        }
        else
        {
            caressClip = caressIdleClip;
        }

        // Fallback to idle if caress clip not available
        if (caressClip != null)
        {
            PlayAnimation(caressClip, defaultBlendTime, WrapMode.Loop);
        }
        else
        {
            // Just stay in current animation
            Debug.Log("Caress animation not assigned, staying in current pose");
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

        if (animComponent == null)
        {
            Debug.LogError("Animation component is null!");
            return;
        }

        currentClip = clip;
        currentAnimationName = clip.name;

        // Ensure clip is marked as Legacy
        if (!clip.legacy)
        {
            clip.legacy = true;
            Debug.LogWarning($"Animation clip {clip.name} was not marked as Legacy. Fixed automatically.");
        }

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
            // Walking - use directional animations when turning
            AnimationClip targetWalkClip = walkClip;
            
            // Use directional animations for any turn (more sensitive threshold)
            if (Mathf.Abs(currentTurnDirection) > 0.2f)
            {
                // Use directional walk animations
                if (currentTurnDirection > 0.2f && walkRightClip != null)
                {
                    targetWalkClip = walkRightClip;
                }
                else if (currentTurnDirection < -0.2f && walkLeftClip != null)
                {
                    targetWalkClip = walkLeftClip;
                }
            }
            
            if (currentClip != targetWalkClip)
            {
                PlayAnimation(targetWalkClip, defaultBlendTime, WrapMode.Loop);
            }

            // Speed up/slow down walk animation - use the actual clip being played
            AnimationClip clipToSpeed = targetWalkClip ?? walkClip;
            if (clipToSpeed != null && animComponent[clipToSpeed.name] != null)
            {
                animComponent[clipToSpeed.name].speed = Mathf.Lerp(0.5f, 1f, currentSpeed / 0.6f);
            }
        }
        else
        {
            // Running - use directional run animations when turning
            AnimationClip targetRunClip = runClip;
            
            // Use directional animations for any turn
            if (Mathf.Abs(currentTurnDirection) > 0.2f)
            {
                // Use directional run animations
                if (currentTurnDirection > 0.2f && runRightClip != null)
                {
                    targetRunClip = runRightClip;
                }
                else if (currentTurnDirection < -0.2f && runLeftClip != null)
                {
                    targetRunClip = runLeftClip;
                }
            }
            
            if (currentClip != targetRunClip)
            {
                PlayAnimation(targetRunClip, quickBlendTime, WrapMode.Loop);
            }

            // Speed up run animation with speed - use the actual clip being played
            AnimationClip clipToSpeed = targetRunClip ?? runClip;
            if (clipToSpeed != null && animComponent[clipToSpeed.name] != null)
            {
                animComponent[clipToSpeed.name].speed = Mathf.Lerp(1f, 1.5f, (currentSpeed - 0.6f) / 0.4f);
            }
        }
    }

    #endregion

    #region Idle Variations

    /// <summary>
    /// Play random idle variation to keep cat looking alive
    /// More frequent variations for more lively idle behavior
    /// </summary>
    private void UpdateIdleVariations()
    {
        idleVariationTimer += Time.deltaTime;

        if (idleVariationTimer >= nextIdleVariation)
        {
            idleVariationTimer = 0f;
            // More frequent variations: 3-8 seconds instead of 5-15
            nextIdleVariation = Random.Range(3f, 8f);

            // Higher chance to play a variation: 70% instead of 50%
            if (Random.value < 0.7f && idleVariations != null && idleVariations.Length > 0)
            {
                var variation = idleVariations[Random.Range(0, idleVariations.Length)];
                if (variation != null && idleClip != null) // Check both clips are not null
                {
                    PlayAnimation(variation, defaultBlendTime, WrapMode.Once, () =>
                    {
                        PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
                    });
                }
            }
        }
    }

    #endregion

    #region Play Animations

    private int currentPlayIndex = 0;
    private float playAnimationTimer = 0f;
    private float playAnimationDuration = 0f;

    /// <summary>
    /// Play next play animation in sequence
    /// </summary>
    private void PlayNextPlayAnimation()
    {
        if (playClips == null || playClips.Length == 0)
        {
            // Fallback to scratch if no play clips
            if (scratchHorizClip != null)
            {
                PlayAnimation(scratchHorizClip, defaultBlendTime, WrapMode.Once, () =>
                {
                    // Cycle to next animation
                    if (currentState == CatState.Playing)
                    {
                        PlayNextPlayAnimation();
                    }
                });
                playAnimationDuration = scratchHorizClip.length;
            }
            else
            {
                PlayAnimation(idleClip, defaultBlendTime, WrapMode.Loop);
            }
            return;
        }

        // Cycle through play animations
        var clip = playClips[currentPlayIndex];
        currentPlayIndex = (currentPlayIndex + 1) % playClips.Length;
        
        // Play animation once, then cycle to next
        PlayAnimation(clip, defaultBlendTime, WrapMode.Once, () =>
        {
            // After animation completes, play next one if still in playing state
            if (currentState == CatState.Playing)
            {
                playAnimationTimer = 0f;
                PlayNextPlayAnimation();
            }
        });
        
        playAnimationDuration = clip.length;
        playAnimationTimer = 0f;
    }

    /// <summary>
    /// Update playing state - cycle through animations
    /// </summary>
    private void UpdatePlayingState()
    {
        // Animation cycling is handled by completion callbacks
        // This is just for any per-frame updates if needed
    }

    #endregion

    #region Sleep Variations

    /// <summary>
    /// Get random belly sleep animation (variations of Lie_belly_sleep)
    /// </summary>
    private AnimationClip GetRandomBellySleepAnimation()
    {
        if (lieBellySleepClips != null && lieBellySleepClips.Length > 0)
        {
            return lieBellySleepClips[Random.Range(0, lieBellySleepClips.Length)];
        }
        return idleClip; // Fallback
    }

    /// <summary>
    /// Get random side sleep animation (variations of Lie_side_sleep)
    /// </summary>
    private AnimationClip GetRandomSideSleepAnimation()
    {
        if (lieSideSleepClips != null && lieSideSleepClips.Length > 0)
        {
            return lieSideSleepClips[Random.Range(0, lieSideSleepClips.Length)];
        }
        return idleClip; // Fallback
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
