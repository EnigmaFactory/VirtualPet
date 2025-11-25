using System.IO;
using UnityEditor;
using UnityEngine;

public class FurnitureCatalogWindow : EditorWindow
{
    private const string CatalogAssetDefaultPath = "Assets/Resources/GameData/FurnitureCatalog.asset";

    private FurnitureCatalog catalog;
    private SerializedObject serializedCatalog;
    private SerializedProperty entriesProperty;
    private Vector2 scroll;

    [MenuItem("Window/Virtual Pet/Furniture Catalog")]
    public static void ShowWindow()
    {
        var window = GetWindow<FurnitureCatalogWindow>("Furniture Catalog");
        window.minSize = new Vector2(400, 400);
        window.Initialize();
    }

    void OnEnable()
    {
        Initialize();
    }

    void Initialize()
    {
        if (catalog == null)
        {
            catalog = FindCatalogAsset();
        }

        if (catalog != null)
        {
            serializedCatalog = new SerializedObject(catalog);
            entriesProperty = serializedCatalog.FindProperty("entries");
        }
    }

    void OnGUI()
    {
        DrawCatalogSelector();

        if (catalog == null || serializedCatalog == null)
        {
            DrawCreateCatalogButton();
            return;
        }

        serializedCatalog.Update();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
            DrawEntry(entry, i);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Furniture Definition"))
        {
            entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
            SerializedProperty newEntry = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
            InitializeEntry(newEntry);
        }

        serializedCatalog.ApplyModifiedProperties();
    }

    void DrawCatalogSelector()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        catalog = (FurnitureCatalog)EditorGUILayout.ObjectField("Catalog Asset", catalog, typeof(FurnitureCatalog), false);
        if (GUILayout.Button("Ping", GUILayout.Width(60)) && catalog != null)
        {
            EditorGUIUtility.PingObject(catalog);
        }
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
        {
            Initialize();
        }
    }

    void DrawCreateCatalogButton()
    {
        EditorGUILayout.HelpBox("No FurnitureCatalog asset assigned. Create one to continue.", MessageType.Info);
        if (GUILayout.Button("Create Furniture Catalog Asset"))
        {
            CreateCatalogAsset();
            Initialize();
        }
    }

    void DrawEntry(SerializedProperty entry, int index)
    {
        if (entry == null) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Definition #{index + 1}", EditorStyles.boldLabel);
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            entriesProperty.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(entry.FindPropertyRelative("furnitureId"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("furnitureType"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("addressableKey"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("prefabFallback"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("defaultPositionOffset"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("defaultEulerRotation"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("defaultScale"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("defaultGenerationBonus"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("defaultSetName"));

        EditorGUILayout.PropertyField(entry.FindPropertyRelative("includeInStarterSet"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("starterPlacements"), true);
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("interactionPoints"), true);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    void InitializeEntry(SerializedProperty entry)
    {
        if (entry == null) return;

        entry.FindPropertyRelative("furnitureId").stringValue = "new_furniture";
        entry.FindPropertyRelative("displayName").stringValue = "New Furniture";
        entry.FindPropertyRelative("defaultScale").vector3Value = Vector3.one;
    }

    FurnitureCatalog FindCatalogAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:FurnitureCatalog");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(path);
        }

        return null;
    }

    void CreateCatalogAsset()
    {
        string directory = Path.GetDirectoryName(CatalogAssetDefaultPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var asset = ScriptableObject.CreateInstance<FurnitureCatalog>();
        AssetDatabase.CreateAsset(asset, CatalogAssetDefaultPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        catalog = asset;
    }
}

