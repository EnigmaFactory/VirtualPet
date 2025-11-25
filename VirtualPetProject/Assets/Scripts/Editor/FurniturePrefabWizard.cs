using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FurniturePrefabWizard : EditorWindow
{
    private GameObject sourcePrefab;
    private string outputFolder = "Assets/Furniture/Prefabs";
    private FurnitureCatalog catalog;
    private string furnitureId = "new_furniture";
    private string displayName = "New Furniture";
    private FurnitureType furnitureType = FurnitureType.Bed;
    private string defaultSetName;
    private float defaultGenerationBonus;
    private bool includeInStarter;

    private List<FurnitureStarterPlacement> starterPlacements = new List<FurnitureStarterPlacement>();
    private List<InteractionPointDefinition> interactionPoints = new List<InteractionPointDefinition>();

    private bool markAsAddressable = true;
    private string addressableGroup = "Furniture";

    [MenuItem("Window/Virtual Pet/Furniture Prefab Wizard")]
    public static void ShowWindow()
    {
        var window = GetWindow<FurniturePrefabWizard>("Furniture Prefab Wizard");
        window.minSize = new Vector2(420, 540);
        window.Initialize();
    }

    void Initialize()
    {
        if (catalog == null)
        {
            catalog = FurnitureCatalogProvider.Catalog;
            if (catalog == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:FurnitureCatalog");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(path);
                }
            }
        }

        if (starterPlacements.Count == 0)
        {
            starterPlacements.Add(new FurnitureStarterPlacement());
        }

        if (interactionPoints.Count == 0)
        {
            interactionPoints.Add(new InteractionPointDefinition());
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Source Prefab", EditorStyles.boldLabel);
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", sourcePrefab, typeof(GameObject), false);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Furniture Definition", EditorStyles.boldLabel);
        catalog = (FurnitureCatalog)EditorGUILayout.ObjectField("Catalog", catalog, typeof(FurnitureCatalog), false);
        furnitureId = EditorGUILayout.TextField("Furniture ID", furnitureId);
        displayName = EditorGUILayout.TextField("Display Name", displayName);
        furnitureType = (FurnitureType)EditorGUILayout.EnumPopup("Furniture Type", furnitureType);
        defaultSetName = EditorGUILayout.TextField("Set Name", defaultSetName);
        defaultGenerationBonus = EditorGUILayout.FloatField("Generation Bonus", defaultGenerationBonus);

        includeInStarter = EditorGUILayout.Toggle("Include in Starter Set", includeInStarter);
        DrawStarterPlacementList();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interaction Points", EditorStyles.boldLabel);
        DrawInteractionPointsList();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Addressables", EditorStyles.boldLabel);
        markAsAddressable = EditorGUILayout.Toggle("Mark as Addressable", markAsAddressable);
        using (new EditorGUI.DisabledScope(!markAsAddressable))
        {
            addressableGroup = EditorGUILayout.TextField("Group Name", addressableGroup);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Create / Update Furniture Prefab", GUILayout.Height(40)))
        {
            CreateFurniturePrefab();
        }
    }

    void DrawStarterPlacementList()
    {
        using (new EditorGUI.DisabledScope(!includeInStarter))
        {
            for (int i = 0; i < starterPlacements.Count; i++)
            {
                var placement = starterPlacements[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Placement #{i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    starterPlacements.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                placement.roomType = (RoomType)EditorGUILayout.EnumPopup("Room Type", placement.roomType);
                placement.localPosition = EditorGUILayout.Vector3Field("Local Position", placement.localPosition);
                placement.localEuler = EditorGUILayout.Vector3Field("Local Rotation", placement.localEuler);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Placement"))
            {
                starterPlacements.Add(new FurnitureStarterPlacement());
            }
        }
    }

    void DrawInteractionPointsList()
    {
        for (int i = 0; i < interactionPoints.Count; i++)
        {
            var point = interactionPoints[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Point #{i + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                interactionPoints.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            point.anchorName = EditorGUILayout.TextField("Name", point.anchorName);
            point.interactionType = (InteractionType)EditorGUILayout.EnumPopup("Type", point.interactionType);
            point.localPosition = EditorGUILayout.Vector3Field("Local Position", point.localPosition);
            point.localEuler = EditorGUILayout.Vector3Field("Local Rotation", point.localEuler);
            point.snapToPosition = EditorGUILayout.Toggle("Snap To Position", point.snapToPosition);
            point.matchRotation = EditorGUILayout.Toggle("Match Rotation", point.matchRotation);
            point.maxOccupants = EditorGUILayout.IntSlider("Max Occupants", point.maxOccupants, 1, 4);
            point.isToy = EditorGUILayout.Toggle("Is Toy", point.isToy);
            point.playDuration = EditorGUILayout.FloatField("Play Duration", point.playDuration);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Interaction Point"))
        {
            interactionPoints.Add(new InteractionPointDefinition());
        }
    }

    void CreateFurniturePrefab()
    {
        if (sourcePrefab == null)
        {
            Debug.LogError("Select a source prefab first.");
            return;
        }

        if (catalog == null)
        {
            Debug.LogError("Catalog asset not assigned.");
            return;
        }

        if (string.IsNullOrEmpty(furnitureId))
        {
            Debug.LogError("Furniture ID cannot be empty.");
            return;
        }

        Directory.CreateDirectory(outputFolder);
        string prefabPath = Path.Combine(outputFolder, $"{furnitureId}.prefab");
        prefabPath = prefabPath.Replace("\\", "/");

        GameObject workingInstance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (workingInstance == null)
        {
            Debug.LogError("Failed to instantiate source prefab.");
            return;
        }

        workingInstance.name = displayName;

        var authoring = workingInstance.GetComponent<InteractionPointAuthoring>();
        if (authoring == null)
        {
            authoring = workingInstance.AddComponent<InteractionPointAuthoring>();
        }
        authoring.SetFromDefinitions(new List<InteractionPointDefinition>(interactionPoints));

        PrefabUtility.SaveAsPrefabAsset(workingInstance, prefabPath, out bool success);
        DestroyImmediate(workingInstance);

        if (!success)
        {
            Debug.LogError("Failed to save prefab asset.");
            return;
        }

        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (markAsAddressable)
        {
            EditorAddressablesUtil.MarkPrefabAddressable(prefabAsset, furnitureId, addressableGroup);
        }

        UpdateCatalogEntry(prefabAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        FurnitureCatalogProvider.SetCatalog(catalog);

        Debug.Log($"✅ Furniture prefab '{displayName}' created at {prefabPath}");
    }

    void UpdateCatalogEntry(GameObject prefabAsset)
    {
        var entry = catalog.GetOrCreateDefinition(furnitureId);

        entry.furnitureId = furnitureId;
        entry.displayName = displayName;
        entry.furnitureType = furnitureType;
        entry.defaultSetName = defaultSetName;
        entry.defaultGenerationBonus = defaultGenerationBonus;
        entry.prefabFallback = prefabAsset;
        entry.addressableKey = furnitureId;
        entry.includeInStarterSet = includeInStarter;

        entry.starterPlacements = includeInStarter ? new List<FurnitureStarterPlacement>(starterPlacements) : new List<FurnitureStarterPlacement>();
        entry.interactionPoints = new List<InteractionPointDefinition>(interactionPoints);

        EditorUtility.SetDirty(catalog);
    }
}

