using UnityEditor;
using UnityEngine;

public class PetStatusWindow : EditorWindow
{
    private GameManager gameManager;
    private VirtualPet activePet;

    [MenuItem("Window/Pet Status")]
    public static void ShowWindow()
    {
        GetWindow<PetStatusWindow>("Pet Status");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            activePet = gameManager.activePet;
        }

        if (activePet != null)
        {
            EditorGUILayout.LabelField("Name", activePet.Name);
            EditorGUILayout.LabelField("Age", activePet.Age.ToString("F2"));
            EditorGUILayout.LabelField("Mood", activePet.Mood.ToString());
            EditorGUILayout.LabelField("Hunger", activePet.Hunger.ToString("F2"));
            EditorGUILayout.LabelField("Happiness", activePet.Happiness.ToString("F2"));
            EditorGUILayout.LabelField("Energy", activePet.Energy.ToString("F2"));
            EditorGUILayout.LabelField("Health", activePet.Health.ToString("F2"));
            EditorGUILayout.LabelField("Is Sleeping", activePet.IsSleeping.ToString());
        }
        else
        {
            EditorGUILayout.LabelField("No active pet found.");
        }

        EditorGUILayout.EndVertical();
    }
}
