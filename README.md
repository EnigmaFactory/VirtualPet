# 🐾 Virtual Pet Unity Project 🐾

Welcome to the Virtual Pet Unity project! This project is a fun and retro virtual pet game where you can take care of your very own adorable pet. The project is built using Unity 2021.3.22f1.

## 🚀 Getting Started

### 🖥️ Clone the Repository

1. Open [GitHub Desktop](https://desktop.github.com/).
2. Click on `File` > `Clone Repository...`.
3. Select the `URL` tab and paste the repository URL.
4. Choose a local path to clone the repository.
5. Click `Clone`.

### 🎮 Open the Project in Unity

1. Launch Unity Hub.
2. Click `Add` and navigate to the `VirtualPetProject` folder inside the cloned repository.
3. Select the `VirtualPetProject` folder and click `Open`.
4. The project should now appear in your Unity Hub project list. Open it with Unity 2021.3.22f1.

## 📁 Project Structure

* `Assets` - Contains all the assets for the project, such as scenes, scripts, sprites, and prefabs.
  * `Scenes` - Contains the Unity scene files.
  * `Scripts` - Contains the C# script files.
    * `Pet` - Contains the scripts related to the virtual pet mechanics.
    * `UI` - Contains the scripts for managing user interface elements.
  * `Sprites` - Contains the sprite assets for the game.
  * `Prefabs` - Contains the prefab assets for the game.
* `ProjectSettings` - Contains the Unity project settings.

## 🎨 Customization

You can easily customize and expand this project to create your own unique virtual pet experience. Feel free to modify the existing assets or add new ones to make the game truly yours!

Happy coding and enjoy your virtual pet journey! 🥳

## 🎲 GameManager Class 🎲

The `GameManager` is the central controller for the game's logic and coordinates the interactions between the virtual pet and the player. It manages the game loop and ensures that the virtual pet's stats are updated at the correct interval.

### 🕒 Update Interval

The update interval in the `GameManager` is set to a range between 0.5 and 3 seconds. This interval determines how frequently the virtual pet's stats are updated. It strikes a balance between responsiveness and performance, ensuring that the game runs smoothly on a variety of devices while still providing a dynamic and engaging experience.

<details>
<summary>Click to expand the Table of Contents</summary>

- [InitializePet](#initializepet)
- [UpdatePet](#updatepet)
- [FeedPet](#feedpet)
- [PlayWithPet](#playwithpet)
- [PetSleep](#petsleep)
- [OnPetStatChanged](#onpetstatchanged)
</details>

### 🐾 Methods

#### <a name="initializepet"></a> InitializePet

`InitializePet` is responsible for creating a new virtual pet instance and initializing its stats. It also starts the main game loop by invoking the `UpdatePet` method at the specified update interval.

#### <a name="updatepet"></a> UpdatePet

`UpdatePet` is the main game loop that updates the virtual pet's stats and triggers any associated events. This method is called at the specified update interval, which ensures that the pet's stats are updated at a consistent rate regardless of the device's performance.

#### <a name="feedpet"></a> FeedPet

`FeedPet` allows the player to feed the virtual pet with a specific food item. It updates the pet's hunger stat based on the nutritional value of the food and triggers the `OnPetStatChanged` event.

#### <a name="playwithpet"></a> PlayWithPet

`PlayWithPet` lets the player play with the virtual pet, increasing its happiness and decreasing its energy. It updates the pet's happiness and energy stats based on the play session and triggers the `OnPetStatChanged` event.

#### <a name="petsleep"></a> PetSleep

`PetSleep` makes the virtual pet go to sleep for a specified duration. It increases the pet's energy stat and triggers the `OnPetStatChanged` event once the pet wakes up.

#### <a name="onpetstatchanged"></a> OnPetStatChanged

`OnPetStatChanged` is an event that is triggered whenever a pet's stat changes. This event allows other scripts, such as UI components, to react to changes in the pet's stats and update their visuals or behavior accordingly.

### 🎯 Future Enhancements

The `GameManager` class can be easily extended to include additional functionality, such as multiple pets, more complex pet interactions, or even a pet store and currency system. The class is designed to be flexible and easily adaptable to new requirements.

## 🐾 VirtualPet Class 🐾

The `VirtualPet` class represents the virtual pet in the game and defines its properties and behaviors. It provides methods for interacting with the pet, such as feeding, playing, and sleeping, and manages the pet's stats, such as hunger, happiness, and energy.

<details>
<summary>Click to expand the Table of Contents</summary>

- [Properties](#properties)
- [Constructor](#constructor)
- [Feed](#feed)
- [Play](#play)
- [Sleep](#sleep)
- [UpdateMood](#updatemood)
- [PetUpdateRoutine](#petupdateroutine)
- [SleepRoutine](#sleeproutine)
</details>

### 🏠 Properties

The `VirtualPet` class has several properties that store the pet's current state and stats:

- Name
- Age
- Mood
- Hunger
- Happiness
- Energy
- Health
- IsSleeping

### 🛠️ Methods

#### <a name="constructor"></a> Constructor

The constructor initializes the virtual pet's properties with default values and starts the `PetUpdateRoutine` coroutine, which updates the pet's stats and mood over time.

#### <a name="feed"></a> Feed

`Feed` allows the player to feed the virtual pet, reducing its hunger stat by a specified food amount. This method also ensures that the pet's hunger stat stays within a valid range.

#### <a name="play"></a> Play

`Play` lets the player play with the virtual pet, increasing its happiness stat and decreasing its energy stat by a specified play amount. This method ensures that the pet's happiness and energy stats stay within valid ranges.

#### <a name="sleep"></a> Sleep

`Sleep` makes the virtual pet go to sleep for a specified duration. The method sets the `IsSleeping` property to `true` and starts the `SleepRoutine` coroutine, which increases the pet's energy stat after the specified sleep duration.

#### <a name="updatemood"></a> UpdateMood

`UpdateMood` updates the pet's mood based on its current hunger, happiness, and energy stats. The method sets the `Mood` property to an appropriate value (e.g., `PetMood.Hungry`, `PetMood.Tired`, or `PetMood.Sad`) depending on the pet's stats.

#### <a name="petupdateroutine"></a> PetUpdateRoutine

`PetUpdateRoutine` is a coroutine that continuously updates the virtual pet's stats and mood at regular intervals. It adjusts the pet's hunger, happiness, and health stats, and then calls the `UpdateMood` method to update the pet's mood accordingly.

#### <a name="sleeproutine"></a> SleepRoutine

`SleepRoutine` is a coroutine that manages the virtual pet's sleep behavior. It waits for the specified sleep duration and then sets the `IsSleeping` property to `false` and fully restores the pet's energy stat.

### 🎯 Future Enhancements

The `VirtualPet` class can be easily extended to include additional functionality, such as:

- More complex pet interactions
- Different pet types with unique behaviors and stats
- Skill and experience systems for pets
- A customization system for the pet's appearance

## 🖥️ User Interface (UI) 🖥️

The User Interface (UI) in the virtual pet game provides players with essential information about their pet and allows them to interact with it. The UI includes various elements, such as buttons, sliders, and status bars, to display the pet's stats and provide access to pet interactions.

<details>
<summary>Click to expand the Table of Contents</summary>

- [UI Components](#uicomponents)
- [UIManager](#uimanager)
</details>

### 🎨 UI Components

The UI consists of several components that display the pet's stats and enable player interactions:

- PetStatsPanel: Displays the pet's current stats (hunger, happiness, energy, and health) and mood.
- InteractionButtons: Contains buttons for the player to interact with their pet (e.g., feed, play, sleep).
- PetName: Displays the pet's name.
- PetAge: Shows the pet's age.
- PetImage: Represents the pet visually and can change based on the pet's mood or actions.

### 📚 UIManager Class

The `UIManager` class is responsible for managing and updating the UI elements in response to changes in the pet's stats and actions. It also provides methods for handling player interactions, such as feeding, playing, and sleeping.

<details>
<summary>Click to expand the Table of Contents</summary>

- [Properties](#ui-properties)
- [Methods](#ui-methods)
</details>

#### <a name="ui-properties"></a> Properties

The `UIManager` class contains several properties that reference UI components in the scene:

- PetStatsPanel
- InteractionButtons
- PetName
- PetAge
- PetImage

#### <a name="ui-methods"></a> Methods

The `UIManager` class provides methods for handling player interactions and updating the UI components:

- UpdatePetStats: Updates the PetStatsPanel with the current pet's stats and mood.
- FeedPet: Calls the `Feed` method of the `VirtualPet` instance when the player clicks the feed button.
- PlayWithPet: Calls the `Play` method of the `VirtualPet` instance when the player clicks the play button.
- PutPetToSleep: Calls the `Sleep` method of the `VirtualPet` instance when the player clicks the sleep button.

### 💡 Future Enhancements

The UI can be improved and expanded with additional features, such as:

- Animations and visual effects for pet interactions.
- Customizable pet appearance and accessories.
- Notifications for important pet events or milestones.
- Mini-games and other interactive elements to engage the player.
- Social features, such as pet sharing or visiting friends' pets.

