using static UnityEngine.EventSystems.EventTrigger;
using UnityEngine;

public enum PetMood { Happy, Sad, Hungry, Tired }

public class VirtualPet
{

    // Properties
    public string Name { get; set; }
    public float Age { get; set; }
    public PetMood Mood { get; set; }
    public float Hunger { get; set; }
    public float Happiness { get; set; }
    public float Energy { get; set; }
    public float Health { get; set; }
    public bool IsSleeping { get; set; }

    // Constructor
    public VirtualPet(string name)
    {
        Name = name;
        Age = 0;
        Hunger = 50;
        Happiness = 50;
        Health = 100;
        Energy = 100;
        IsSleeping = false;
        UpdateMood();
    }

    // Methods
    public void Feed(float foodAmount)
    {
        Hunger = Mathf.Clamp(Hunger - foodAmount, 0, 100);
        UpdateMood();
    }

    public void Play(float playAmount)
    {
        Happiness = Mathf.Clamp(Happiness + playAmount, 0, 100);
        Energy = Mathf.Clamp(Energy - playAmount * 0.5f, 0, 100);
        UpdateMood();
    }

    public void StartSleeping()
    {
        IsSleeping = true;
    }

    public void EndSleeping()
    {
        IsSleeping = false;
        Energy = 100;
        UpdateMood();
    }

    public void UpdatePet(float deltaTime)
    {
        // Decrease happiness over time
        Happiness -= 0.5f * deltaTime;
        Happiness = Mathf.Clamp(Happiness, 0, 100);

        // Increase hunger over time
        Hunger += 0.5f * deltaTime;
        Hunger = Mathf.Clamp(Hunger, 0, 100);

        // Update health based on hunger and happiness
        Health = Mathf.Lerp(0, 100, (100 - Hunger) * 0.5f + Happiness * 0.5f);
        Health = Mathf.Clamp(Health, 0, 100);

        // Update mood
        UpdateMood();

        // Age the pet every day (assuming updates every 1 second)
        Age += deltaTime / (24 * 60 * 60);
    }

    private void UpdateMood()
    {
        if (Hunger > 80)
        {
            Mood = PetMood.Hungry;
        }
        else if (Energy < 20)
        {
            Mood = PetMood.Tired;
        }
        else if (Happiness < 40)
        {
            Mood = PetMood.Sad;
        }
        else
        {
            Mood = PetMood.Happy;
        }
    }
}