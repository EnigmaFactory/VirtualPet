using System.Collections;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class GameManager : MonoBehaviour
{ 
    [Header("Routine Controls")]
    [SerializeField] private bool routineActive = true;
    [SerializeField] private float updateInterval = 1f;

    [Header("Managers")]
    [SerializeField] private UIManager uiManager;

    [Header("Pet Parameters")]
    [SerializeField] private string[] petNames = { "Fluffy", "Buddy", "Max", "Charlie", "Luna", "Cooper", "Rocky", "Oliver", "Daisy", "Molly" };

    [Header("Runtime Intances")]
    // Singleton instance of GameManager
    public static GameManager instance;
    public VirtualPet activePet;


    // Awake method is called when the script instance is being loaded
    private void Awake()
    {
        // If the instance is not set yet, set it to this GameManager
        if (instance == null)
        {
            instance = this;
        }
        // If the instance is set to another GameManager, destroy this one
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Start method is called before the first frame update
    private void Start()
    {
        // Create a new VirtualPet instance
        StartCoroutine(GameStartRoutine());
        
    }

    private IEnumerator GameStartRoutine()
    {
        BirthPet();
        yield return new WaitForSeconds(1f);
        uiManager.UpdateStatus($"{activePet.Name} has been born!");

        // Start the UpdatePetRoutine coroutine
        StartCoroutine(UpdatePetRoutine());
    }

    private void BirthPet()
    {
        string petName = GenerateRandomName();
        activePet = new VirtualPet(petName);
        uiManager.UpdateUI(activePet);
    }

    public string GenerateRandomName()
    {
        int index = Random.Range(0, petNames.Length);
        return petNames[index];
    }

    public void FeedPet(float foodAmount)
    {
        if (activePet.IsSleeping)
        {
            uiManager.UpdateStatus(Messages.GetPetIsSleepingMessage(activePet.Name));
            return;
        }

        activePet.Feed(foodAmount);
        uiManager.UpdateStatus(Messages.GetPetEatMessage(activePet.Name));
    }

    public void PlayWithPet(float playAmount)
    {
        if (activePet.IsSleeping)
        {
            uiManager.UpdateStatus(Messages.GetPetIsSleepingMessage(activePet.Name));
            return;
        }

        activePet.Play(playAmount);
        uiManager.UpdateStatus(Messages.GetPetPlayMessage(activePet.Name));
    }

    public void BedTimeRoutine(float sleepDuration)
    {
        if (!activePet.IsSleeping)
        {
            activePet.StartSleeping();
            StartCoroutine(SleepRoutine(sleepDuration));
            uiManager.UpdateStatus(Messages.GetPetBedTimeMessage(activePet.Name));
        }
        else
        {
            uiManager.UpdateStatus(Messages.GetPetIsSleepingMessage(activePet.Name));
        }
    }

    private IEnumerator SleepRoutine(float sleepDuration)
    {
        yield return new WaitForSeconds(sleepDuration);
        activePet.EndSleeping();
        uiManager.UpdateStatus(Messages.GetPetWakeUpMessage(activePet.Name));
        uiManager.UpdateUI(activePet);
    }

    private IEnumerator UpdatePetRoutine()
    {
        while (routineActive)
        {
            activePet.UpdatePet(updateInterval);
            uiManager.UpdateUI(activePet);
            yield return new WaitForSeconds(updateInterval);
        }
    }
}


// Messages class
public static class Messages
{
    public static string GetPetIsSleepingMessage(string petName)
    {
        return $"{petName} is sleeping and cannot be fed or played with right now.";
    }

    public static string GetPetIsAlreadyHappyMessage(string petName)
    {
        return $"{petName} is already happy and does not want to play right now.";
    }

    internal static string GetPetWakeUpMessage(string petName)
    {
        return $"Good morning {petName}! Rise and shine.";
    }

    internal static string GetPetBedTimeMessage(string petName)
    {
        return $"It's time for bed {petName}! See you in the morning!";
    }

    internal static string GetPetPlayMessage(string petName)
    {
        return $"I have so much fun playing with you {petName}!";
    }

    internal static string GetPetEatMessage(string petName)
    {
        return $"I hop you enjoy this yummy treat {petName}!";
    }
}