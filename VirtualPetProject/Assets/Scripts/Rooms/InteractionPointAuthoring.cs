using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class InteractionPointAuthoring : MonoBehaviour
{
    [SerializeField] private List<InteractionPointAnchor> anchors = new List<InteractionPointAnchor>();
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.6f);

    public IReadOnlyList<InteractionPointAnchor> Anchors => anchors;

#if UNITY_EDITOR
    public void SetFromDefinitions(List<InteractionPointDefinition> definitions)
    {
        anchors.Clear();
        if (definitions == null) return;

        foreach (var def in definitions)
        {
            anchors.Add(new InteractionPointAnchor
            {
                anchorName = def.anchorName,
                interactionType = def.interactionType,
                localPosition = def.localPosition,
                localEuler = def.localEuler,
                snapToPosition = def.snapToPosition,
                matchRotation = def.matchRotation,
                maxOccupants = def.maxOccupants,
                isToy = def.isToy,
                playDuration = def.playDuration
            });
        }

        ApplyAnchorsToChildren();
    }

    public List<InteractionPointDefinition> ToDefinitions()
    {
        var list = new List<InteractionPointDefinition>();
        foreach (var anchor in anchors)
        {
            list.Add(new InteractionPointDefinition
            {
                anchorName = anchor.anchorName,
                interactionType = anchor.interactionType,
                localPosition = anchor.localPosition,
                localEuler = anchor.localEuler,
                snapToPosition = anchor.snapToPosition,
                matchRotation = anchor.matchRotation,
                maxOccupants = anchor.maxOccupants,
                isToy = anchor.isToy,
                playDuration = anchor.playDuration
            });
        }
        return list;
    }

    public void ApplyAnchorsToChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("IP_", StringComparison.Ordinal))
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }

        foreach (var anchor in anchors)
        {
            if (anchor == null) continue;

            var child = new GameObject($"IP_{anchor.anchorName}");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = anchor.localPosition;
            child.transform.localRotation = Quaternion.Euler(anchor.localEuler);

            var point = child.AddComponent<InteractionPoint>();
            var serialized = new UnityEditor.SerializedObject(point);
            serialized.FindProperty("interactionType").enumValueIndex = (int)anchor.interactionType;
            serialized.FindProperty("snapToPosition").boolValue = anchor.snapToPosition;
            serialized.FindProperty("matchRotation").boolValue = anchor.matchRotation;
            serialized.FindProperty("maxOccupants").intValue = anchor.maxOccupants;
            serialized.FindProperty("isToy").boolValue = anchor.isToy;
            serialized.FindProperty("playDuration").floatValue = anchor.playDuration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
#endif

    void OnDrawGizmosSelected()
    {
        if (anchors == null) return;

        Gizmos.color = gizmoColor;
        foreach (var anchor in anchors)
        {
            if (anchor == null) continue;
            Vector3 worldPos = transform.TransformPoint(anchor.localPosition);
            Gizmos.DrawSphere(worldPos, 0.075f);

            Vector3 forward = transform.TransformDirection(Quaternion.Euler(anchor.localEuler) * Vector3.forward);
            Gizmos.DrawLine(worldPos, worldPos + forward * 0.3f);
        }
    }
}

[Serializable]
public class InteractionPointAnchor
{
    public string anchorName = "Anchor";
    public InteractionType interactionType = InteractionType.Sit;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEuler = Vector3.zero;
    public bool snapToPosition = true;
    public bool matchRotation = true;
    public int maxOccupants = 1;
    public bool isToy;
    public float playDuration = 5f;
}

