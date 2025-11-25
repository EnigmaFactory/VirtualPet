using System;
using UnityEditor;
using UnityEngine;

public static class EditorAddressablesUtil
{
    const string SettingsTypeName = "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor";
    const string SettingsDefaultObjectTypeName = "UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor";
    const string EntryTypeName = "UnityEditor.AddressableAssets.Settings.AddressableAssetEntry, Unity.Addressables.Editor";

    public static bool MarkPrefabAddressable(GameObject prefab, string addressKey, string groupName = "Furniture")
    {
        if (prefab == null || string.IsNullOrEmpty(addressKey))
        {
            Debug.LogWarning("Cannot mark Addressable. Prefab or key missing.");
            return false;
        }

        Type settingsType = Type.GetType(SettingsTypeName);
        Type defaultObjectType = Type.GetType(SettingsDefaultObjectTypeName);
        if (settingsType == null || defaultObjectType == null)
        {
            Debug.LogWarning("Addressables package not installed. Skipping addressable setup.");
            return false;
        }

        var defaultSettingsProperty = defaultObjectType.GetProperty("Settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var settings = defaultSettingsProperty?.GetValue(null);
        if (settings == null)
        {
            Debug.LogWarning("Addressables settings not found. Create them via Window > Asset Management > Addressables.");
            return false;
        }

        var FindGroup = settingsType.GetMethod("FindGroup", new[] { typeof(string) });
        var CreateGroup = settingsType.GetMethod(
            "CreateGroup",
            new[]
            {
                typeof(string),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(System.Collections.Generic.IEnumerable<Type>),
                typeof(Type)
            });

        var group = FindGroup?.Invoke(settings, new object[] { groupName });
        if (group == null)
        {
            group = CreateGroup?.Invoke(settings, new object[] { groupName, false, false, false, null, null });
        }

        if (group == null)
        {
            Debug.LogWarning("Failed to create/find Addressable group.");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefab);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning("Failed to resolve prefab GUID for Addressables.");
            return false;
        }

        var CreateOrMoveEntry = settingsType.GetMethod("CreateOrMoveEntry", new[] { typeof(string), group.GetType(), typeof(bool), typeof(bool) });
        var entry = CreateOrMoveEntry?.Invoke(settings, new object[] { guid, group, false, false });
        if (entry == null)
        {
            Debug.LogWarning("Failed to create Addressable entry.");
            return false;
        }

        var addressProp = entry.GetType().GetProperty("address");
        addressProp?.SetValue(entry, addressKey);

        var SetDirty = settingsType.GetMethod("SetDirty");
        SetDirty?.Invoke(settings, null);

        Debug.Log($"Marked prefab '{prefab.name}' as Addressable ({addressKey}) in group '{groupName}'.");
        return true;
    }
}

