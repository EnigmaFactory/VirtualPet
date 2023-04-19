using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Pet Stats Panel")]
    public TextMeshProUGUI statusText;
    public Slider hungerSlider;
    public Slider happinessSlider;
    public Slider energySlider;
    public Slider healthSlider;
    public TextMeshProUGUI moodText;
    public TextMeshProUGUI petNameText;
    public TextMeshProUGUI petAgeText;
    public Image petImage;

    [Header("Interaction Buttons")]
    public Button feedButton;
    public Button playButton;
    public Button sleepButton;


    private void Start()
    {
        // Add listener methods to button clicks
        feedButton.onClick.AddListener(FeedPet);
        playButton.onClick.AddListener(PlayWithPet);
        sleepButton.onClick.AddListener(SendPetToBed);
    }

    public void UpdateUI(VirtualPet pet)
    {
        petNameText.text = pet.Name;
        petAgeText.text = $"{pet.Age:F1}"; //$"Age: {pet.Age:F1} days";
        hungerSlider.value = pet.Hunger;
        happinessSlider.value = pet.Happiness;
        energySlider.value = pet.Energy;
        healthSlider.value = pet.Health;
        moodText.text = pet.Mood.ToString(); //$"Mood: {pet.Mood}";
    }

    public void UpdateStatus(string message)
    {
        statusText.text = message;
    }

    public void FeedPet()
    {
        GameManager.instance.FeedPet(10f);
        UpdateUI(GameManager.instance.activePet);
    }

    public void PlayWithPet()
    {
        GameManager.instance.PlayWithPet(10);
        UpdateUI(GameManager.instance.activePet);
    }

    public void SendPetToBed()
    {
        GameManager.instance.BedTimeRoutine(10f);
        UpdateUI(GameManager.instance.activePet);
    }

    private void OnDestroy()
    {
        // Remove listener methods from button clicks when UIManager is destroyed
        feedButton.onClick.RemoveListener(FeedPet);
        playButton.onClick.RemoveListener(PlayWithPet);
        sleepButton.onClick.RemoveListener(SendPetToBed);
    }
}
