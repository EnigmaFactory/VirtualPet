using UnityEngine;
using UnityEditor;

/// <summary>
/// Dev tools for spawning waypoints and watch points in the scene
/// </summary>
public class DevToolsSpawner : EditorWindow
{
    private Vector3 spawnPosition = Vector3.zero;
    private string waypointName = "Waypoint";
    private string watchPointName = "Watch Point";
    
    [MenuItem("Window/Virtual Pet/Spawn Dev Objects")]
    public static void ShowWindow()
    {
        var window = GetWindow<DevToolsSpawner>("Spawn Dev Objects");
        window.minSize = new Vector2(300, 200);
    }

    void OnGUI()
    {
        GUILayout.Label("🎯 Spawn Dev Objects", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to spawn objects", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Spawn Position:", EditorStyles.miniLabel);
        spawnPosition = EditorGUILayout.Vector3Field("Position", spawnPosition);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Scene View Camera"))
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                spawnPosition = sceneView.camera.transform.position;
                spawnPosition.y = 0f; // Put on ground
            }
        }
        if (GUILayout.Button("Use Mouse Position"))
        {
            // Get mouse position in scene view
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                spawnPosition = hit.point;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Waypoint (Cat will go here):", EditorStyles.miniLabel);
        waypointName = EditorGUILayout.TextField("Name:", waypointName);
        
        if (GUILayout.Button("Spawn Waypoint"))
        {
            SpawnWaypoint();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Watch Point (Cat will watch this):", EditorStyles.miniLabel);
        watchPointName = EditorGUILayout.TextField("Name:", watchPointName);
        
        if (GUILayout.Button("Spawn Watch Point"))
        {
            SpawnWatchPoint();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        if (GUILayout.Button("Clear All Dev Objects"))
        {
            ClearAllDevObjects();
        }
    }

    void SpawnWaypoint()
    {
        GameObject waypoint = new GameObject(waypointName);
        waypoint.transform.position = spawnPosition;
        waypoint.tag = "Waypoint";
        
        // Add visual marker
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(waypoint.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.3f;
        sphere.name = "Visual";
        
        // Make it a different color
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.cyan;
        renderer.material = mat;
        
        // Remove collider (we don't need it for waypoints)
        Collider col = sphere.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        
        // Add InteractionPoint component so cats can go to it
        InteractionPoint interactionPoint = waypoint.AddComponent<InteractionPoint>();
        // Set interaction type using SerializedObject (since it's private)
        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(interactionPoint);
        so.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Play;
        so.ApplyModifiedProperties();
        
        // Make it selectable in scene
        Selection.activeGameObject = waypoint;
        
        Debug.Log($"✅ Spawned waypoint '{waypointName}' at {spawnPosition}");
    }

    void SpawnWatchPoint()
    {
        GameObject watchPoint = new GameObject(watchPointName);
        watchPoint.transform.position = spawnPosition;
        watchPoint.tag = "WatchPoint";
        
        // Add visual marker (different from waypoint)
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(watchPoint.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        cube.name = "Visual";
        
        // Make it a different color
        Renderer renderer = cube.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.yellow;
        renderer.material = mat;
        
        // Remove collider
        Collider col = cube.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        
        // Add InteractionPoint component
        InteractionPoint interactionPoint = watchPoint.AddComponent<InteractionPoint>();
        // Set interaction type using SerializedObject (since it's private)
        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(interactionPoint);
        so.FindProperty("interactionType").enumValueIndex = (int)InteractionType.Watch;
        so.ApplyModifiedProperties();
        
        // Make it selectable
        Selection.activeGameObject = watchPoint;
        
        Debug.Log($"✅ Spawned watch point '{watchPointName}' at {spawnPosition}");
    }

    void ClearAllDevObjects()
    {
        int count = 0;
        
        // Find all waypoints and watch points
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
        GameObject[] watchPoints = GameObject.FindGameObjectsWithTag("WatchPoint");
        
        foreach (var obj in waypoints)
        {
            DestroyImmediate(obj);
            count++;
        }
        
        foreach (var obj in watchPoints)
        {
            DestroyImmediate(obj);
            count++;
        }
        
        Debug.Log($"🗑️ Cleared {count} dev objects");
    }
}

