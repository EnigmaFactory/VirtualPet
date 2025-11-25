using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractionPointAuthoring))]
public class InteractionPointAuthoringEditor : Editor
{
    SerializedProperty anchorsProp;

    void OnEnable()
    {
        anchorsProp = serializedObject.FindProperty("anchors");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(anchorsProp, true);

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Anchors To Children"))
        {
            foreach (var targetObject in targets)
            {
                var authoring = targetObject as InteractionPointAuthoring;
                authoring?.ApplyAnchorsToChildren();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        var authoring = (InteractionPointAuthoring)target;
        if (authoring == null || authoring.Anchors == null) return;

        var transform = authoring.transform;
        var anchors = authoring.Anchors;
        for (int i = 0; i < anchors.Count; i++)
        {
            var anchor = anchors[i];
            if (anchor == null) continue;

            EditorGUI.BeginChangeCheck();
            Vector3 worldPos = transform.TransformPoint(anchor.localPosition);
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, transform.rotation * Quaternion.Euler(anchor.localEuler));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(authoring, "Move Interaction Anchor");
                anchor.localPosition = transform.InverseTransformPoint(newWorldPos);
                EditorUtility.SetDirty(authoring);
            }
        }
    }
}

