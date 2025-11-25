using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility to fix animation clips - marks them as Legacy for Animation component
/// Run once after importing Red Deer animations
/// </summary>
public class FixAnimationLegacy
{
    [MenuItem("Window/Virtual Pet/Fix Animation Legacy Settings")]
    public static void FixAllAnimations()
    {
        // Find the IP animation FBX
        string[] guids = AssetDatabase.FindAssets("Cat_Simple_anim_IP t:Model");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("Could not find Cat_Simple_anim_IP.fbx. Make sure it's imported.");
            return;
        }

        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        int fixedCount = 0;
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                // Set legacy mode
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                clip.legacy = true;
                
                // Mark as dirty and save
                EditorUtility.SetDirty(clip);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Fixed {fixedCount} animation clips - marked as Legacy");
        EditorUtility.DisplayDialog("Success", $"Fixed {fixedCount} animation clips!", "OK");
    }

    [MenuItem("Window/Virtual Pet/Fix Animation Legacy Settings (Selected)")]
    public static void FixSelectedAnimations()
    {
        int fixedCount = 0;
        
        foreach (Object obj in Selection.objects)
        {
            if (obj is AnimationClip clip)
            {
                clip.legacy = true;
                EditorUtility.SetDirty(clip);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Fixed {fixedCount} selected animation clips");
        }
        else
        {
            Debug.LogWarning("No AnimationClips selected!");
        }
    }
}

