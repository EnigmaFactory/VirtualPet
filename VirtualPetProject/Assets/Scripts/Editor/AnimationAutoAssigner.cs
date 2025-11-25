using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Auto-assigns Red Deer IP animations to AnimalController based on naming patterns
/// Window > Virtual Pet > Auto-Assign Animations
/// </summary>
public class AnimationAutoAssigner : EditorWindow
{
    private Object animationFBX;
    private AnimalController targetController;
    private Dictionary<string, AnimationClip> clipMap = new Dictionary<string, AnimationClip>();
    private Vector2 scrollPosition;

    [MenuItem("Window/Virtual Pet/Auto-Assign Animations")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationAutoAssigner>("Animation Auto-Assigner");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Red Deer Animation Auto-Assigner", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Select animation FBX
        EditorGUILayout.LabelField("Step 1: Select Animation FBX", EditorStyles.boldLabel);
        animationFBX = EditorGUILayout.ObjectField(
            "IP Animation FBX",
            animationFBX,
            typeof(Object),
            false
        );

        EditorGUILayout.HelpBox(
            "Select the Cat_Simple_anim_IP.fbx file from:\n" +
            "Assets/Red_Deer/CatFamily/Cats/Cat_Simple/Cat/FBX/Anim/",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Select target AnimalController
        EditorGUILayout.LabelField("Step 2: Select AnimalController", EditorStyles.boldLabel);
        targetController = EditorGUILayout.ObjectField(
            "Target AnimalController",
            targetController,
            typeof(AnimalController),
            true
        ) as AnimalController;

        EditorGUILayout.Space(10);

        if (animationFBX != null && targetController != null)
        {
            if (GUILayout.Button("🔍 Scan Animations", GUILayout.Height(30)))
            {
                ScanAnimations();
            }

            EditorGUILayout.Space(10);

            if (clipMap.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {clipMap.Count} animation clips", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                DrawAnimationMapping();

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                if (GUILayout.Button("✅ Auto-Assign All", GUILayout.Height(40)))
                {
                    AutoAssignAnimations();
                }
            }
        }
    }

    void ScanAnimations()
    {
        clipMap.Clear();

        if (animationFBX == null) return;

        string assetPath = AssetDatabase.GetAssetPath(animationFBX);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                clipMap[clip.name] = clip;
            }
        }

        Debug.Log($"✅ Scanned {clipMap.Count} animation clips from {assetPath}");
    }

    void DrawAnimationMapping()
    {
        EditorGUILayout.LabelField("Animation Mapping Preview:", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Core animations
        DrawMappingRow("Idle", FindBestMatch("Idle"));
        DrawMappingRow("Walk", FindBestMatch("Walk_F_IP"));
        DrawMappingRow("Run", FindBestMatch("Run_F_IP"));
        
        EditorGUILayout.LabelField("Sit Animations:", EditorStyles.miniLabel);
        DrawMappingRow("  Sit Start", FindBestMatch("Sit_start"));
        var sitLoops = FindMatches("Sit_loop").Take(4).ToList();
        for (int i = 0; i < sitLoops.Count; i++)
        {
            DrawMappingRow($"  Sit Loop {i+1}", sitLoops[i]);
        }
        DrawMappingRow("  Sit End", FindBestMatch("Sit_end"));
        
        EditorGUILayout.LabelField("Lie Belly (Awake - for chilling):", EditorStyles.miniLabel);
        DrawMappingRow("  Lie Belly Start", FindBestMatch("Lie_belly_start"));
        var bellyLoops = FindMatches("Lie_belly_loop").Take(3).ToList();
        for (int i = 0; i < bellyLoops.Count; i++)
        {
            DrawMappingRow($"  Lie Belly Loop {i+1}", bellyLoops[i]);
        }
        DrawMappingRow("  Lie Belly End", FindBestMatch("Lie_belly_end"));
        
        EditorGUILayout.LabelField("Lie Side (Awake - for chilling):", EditorStyles.miniLabel);
        DrawMappingRow("  Lie Side Start", FindBestMatch("Lie_side_start"));
        var sideLoops = FindMatches("Lie_side_loop").Take(2).ToList();
        for (int i = 0; i < sideLoops.Count; i++)
        {
            DrawMappingRow($"  Lie Side Loop {i+1}", sideLoops[i]);
        }
        DrawMappingRow("  Lie Side End", FindBestMatch("Lie_side_end"));
        
        EditorGUILayout.LabelField("Sleep - Belly:", EditorStyles.miniLabel);
        DrawMappingRow("  Belly Sleep Start", FindBestMatch("Lie_belly_sleep_start"));
        var bellySleep = FindMatches("Lie_belly_sleep").Take(2).ToList();
        for (int i = 0; i < bellySleep.Count; i++)
        {
            DrawMappingRow($"  Belly Sleep Loop {i+1}", bellySleep[i]);
        }
        DrawMappingRow("  Belly Sleep End", FindBestMatch("Lie_belly_sleep_end"));
        
        EditorGUILayout.LabelField("Sleep - Side:", EditorStyles.miniLabel);
        DrawMappingRow("  Side Sleep Start", FindBestMatch("Lie_side_sleep_start"));
        var sideSleep = FindMatches("Lie_side_sleep").Take(2).ToList();
        for (int i = 0; i < sideSleep.Count; i++)
        {
            DrawMappingRow($"  Side Sleep Loop {i+1}", sideSleep[i]);
        }
        DrawMappingRow("  Side Sleep End", FindBestMatch("Lie_side_sleep_end"));
        DrawMappingRow("Groom", FindBestMatch("Lick"));
        DrawMappingRow("Eat", FindBestMatch("Eating"));
        DrawMappingRow("Drink", FindBestMatch("Drinking"));
        DrawMappingRow("Jump", FindBestMatch("JumpStart.*F_IP"));
        DrawMappingRow("Land", FindBestMatch("JumpLand"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Petting/Caress Animations:", EditorStyles.boldLabel);
        DrawMappingRow("  Caress Idle", FindBestMatch("Caress_idle"));
        DrawMappingRow("  Caress Sit", FindBestMatch("Caress_sit"));
        DrawMappingRow("  Caress Lie", FindBestMatch("Caress_lie"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Directional Movement:", EditorStyles.boldLabel);
        DrawMappingRow("  Walk Left", FindBestMatch("Walk_L_IP"));
        DrawMappingRow("  Walk Right", FindBestMatch("Walk_R_IP"));
        DrawMappingRow("  Walk Back", FindBestMatch("Walk_B_IP"));
        DrawMappingRow("  Run Left", FindBestMatch("Run_L_IP"));
        DrawMappingRow("  Run Right", FindBestMatch("Run_R_IP"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Scratching Animations:", EditorStyles.boldLabel);
        DrawMappingRow("  Scratch Horizontal", FindBestMatch("SharpenClaws_Horiz"));
        DrawMappingRow("  Scratch Vertical", FindBestMatch("SharpenClaws_Vert"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Transition Animations:", EditorStyles.boldLabel);
        DrawMappingRow("  Sit → Lie Belly", FindBestMatch("Trans_Sit_LieBelly"));
        DrawMappingRow("  Sit → Lie Side", FindBestMatch("Trans_Sit_LieSide"));
        DrawMappingRow("  Lie Belly → Sit", FindBestMatch("Trans_LieBelly_Sit"));
        DrawMappingRow("  Lie Side → Sit", FindBestMatch("Trans_LieSide_Sit"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Idle Variations:", EditorStyles.boldLabel);
        var idleVariations = FindMatches("Idle_[0-9]").Take(7).ToList();
        for (int i = 0; i < idleVariations.Count; i++)
        {
            DrawMappingRow($"  Idle Var {i + 1}", idleVariations[i]);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Play Animations:", EditorStyles.boldLabel);
        var playAnims = FindMatches("Attack|Scratching|SharpenClaws").Take(5).ToList();
        for (int i = 0; i < playAnims.Count; i++)
        {
            DrawMappingRow($"  Play {i + 1}", playAnims[i]);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sleep Variations:", EditorStyles.boldLabel);
        var sleepVars = FindMatches("Lie.*sleep|Lie.*loop").Take(3).ToList();
        for (int i = 0; i < sleepVars.Count; i++)
        {
            DrawMappingRow($"  Sleep Var {i + 1}", sleepVars[i]);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Available Animations (All):", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        foreach (var kvp in clipMap.OrderBy(k => k.Key))
        {
            EditorGUILayout.LabelField($"  • {kvp.Key}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawMappingRow(string label, AnimationClip clip)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
        EditorGUILayout.EndHorizontal();
    }

    AnimationClip FindBestMatch(string pattern)
    {
        // Try exact match first
        if (clipMap.ContainsKey(pattern))
            return clipMap[pattern];

        // Try contains match
        var match = clipMap.Keys.FirstOrDefault(k => k.Contains(pattern));
        if (match != null)
            return clipMap[match];

        // Try regex-like pattern matching
        var regexMatch = clipMap.Keys.FirstOrDefault(k =>
            System.Text.RegularExpressions.Regex.IsMatch(k, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        );
        if (regexMatch != null)
            return clipMap[regexMatch];

        return null;
    }

    List<AnimationClip> FindMatches(string pattern)
    {
        return clipMap.Keys
            .Where(k => System.Text.RegularExpressions.Regex.IsMatch(k, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .Select(k => clipMap[k])
            .ToList();
    }

    void AutoAssignAnimations()
    {
        if (targetController == null) return;

        SerializedObject so = new SerializedObject(targetController);

        // Core animations
        AssignClip(so, "idleClip", FindBestMatch("Idle_1"));
        AssignClip(so, "walkClip", FindBestMatch("Walk_F_IP"));
        AssignClip(so, "runClip", FindBestMatch("Run_F_IP"));
        // Sit animations - proper structure
        AssignClip(so, "sitStartClip", FindBestMatch("Sit_start"));
        var sitLoops = FindMatches("Sit_loop").ToList();
        AssignArray(so, "sitClips", sitLoops);
        AssignClip(so, "sitEndClip", FindBestMatch("Sit_end"));
        
        // Lie Belly (awake - for chilling) - ARRAYS
        AssignClip(so, "lieBellyStartClip", FindBestMatch("Lie_belly_start"));
        var bellyLoops = FindMatches("Lie_belly_loop").ToList();
        AssignArray(so, "lieBellyClips", bellyLoops);
        AssignClip(so, "lieBellyEndClip", FindBestMatch("Lie_belly_end"));
        
        // Lie Side (awake - for chilling) - ARRAYS
        AssignClip(so, "lieSideStartClip", FindBestMatch("Lie_side_start"));
        var sideLoops = FindMatches("Lie_side_loop").ToList();
        AssignArray(so, "lieSideClips", sideLoops);
        AssignClip(so, "lieSideEndClip", FindBestMatch("Lie_side_end"));
        
        // Sleep - Belly (posture-specific)
        AssignClip(so, "lieBellySleepStartClip", FindBestMatch("Lie_belly_sleep_start"));
        var bellySleepLoops = FindMatches("Lie_belly_sleep").ToList();
        AssignArray(so, "lieBellySleepClips", bellySleepLoops);
        AssignClip(so, "lieBellySleepEndClip", FindBestMatch("Lie_belly_sleep_end"));
        
        // Sleep - Side (posture-specific)
        AssignClip(so, "lieSideSleepStartClip", FindBestMatch("Lie_side_sleep_start"));
        var sideSleepLoops = FindMatches("Lie_side_sleep").ToList();
        AssignArray(so, "lieSideSleepClips", sideSleepLoops);
        AssignClip(so, "lieSideSleepEndClip", FindBestMatch("Lie_side_sleep_end"));
        
        AssignClip(so, "groomClip", FindBestMatch("Lick"));
        AssignClip(so, "stretchClip", FindBestMatch("Idle_2")); // Often has stretch
        AssignClip(so, "jumpClip", FindBestMatch("JumpStart.*F_IP"));
        AssignClip(so, "landClip", FindBestMatch("JumpLand"));
        AssignClip(so, "eatClip", FindBestMatch("Eating"));
        AssignClip(so, "drinkClip", FindBestMatch("Drinking"));

        // Petting/Caress animations
        AssignClip(so, "caressIdleClip", FindBestMatch("Caress_idle"));
        AssignClip(so, "caressSitClip", FindBestMatch("Caress_sit"));
        AssignClip(so, "caressLieClip", FindBestMatch("Caress_lie"));

        // Directional Movement
        AssignClip(so, "walkLeftClip", FindBestMatch("Walk_L_IP"));
        AssignClip(so, "walkRightClip", FindBestMatch("Walk_R_IP"));
        AssignClip(so, "walkBackClip", FindBestMatch("Walk_B_IP"));
        AssignClip(so, "runLeftClip", FindBestMatch("Run_L_IP"));
        AssignClip(so, "runRightClip", FindBestMatch("Run_R_IP"));

        // Scratching animations
        AssignClip(so, "scratchHorizClip", FindBestMatch("SharpenClaws_Horiz"));
        AssignClip(so, "scratchVertClip", FindBestMatch("SharpenClaws_Vert"));

        // Transition animations
        AssignClip(so, "transSitToLieBellyClip", FindBestMatch("Trans_Sit_LieBelly"));
        AssignClip(so, "transSitToLieSideClip", FindBestMatch("Trans_Sit_LieSide"));
        AssignClip(so, "transLieBellyToSitClip", FindBestMatch("Trans_LieBelly_Sit"));
        AssignClip(so, "transLieSideToSitClip", FindBestMatch("Trans_LieSide_Sit"));

        // Idle variations
        var idleVars = FindMatches("Idle_[0-9]").Take(7).ToList();
        AssignArray(so, "idleVariations", idleVars);

        // Sleep variations are now posture-specific (belly/side) - handled above

        // Play clips
        var playClips = FindMatches("Attack|Scratching|SharpenClaws").Take(5).ToList();
        AssignArray(so, "playClips", playClips);

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(targetController);
        AssetDatabase.SaveAssets();

        Debug.Log("✅ Auto-assigned animations to AnimalController!");
        EditorUtility.DisplayDialog("Success", $"Assigned animations to {targetController.name}", "OK");
    }

    void AssignClip(SerializedObject so, string propertyName, AnimationClip clip)
    {
        if (clip == null) return;

        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = clip;
            Debug.Log($"  ✓ {propertyName} → {clip.name}");
        }
    }

    void AssignArray(SerializedObject so, string propertyName, List<AnimationClip> clips)
    {
        if (clips == null || clips.Count == 0) return;

        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                Debug.Log($"  ✓ {propertyName}[{i}] → {clips[i].name}");
            }
        }
    }
}

