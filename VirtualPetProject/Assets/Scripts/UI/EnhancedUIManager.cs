using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Enhanced UI Manager for cat virtual pet with Firebase integration
/// Displays player stats, cat stats, and handles user interactions
/// </summary>
public class EnhancedUIManager : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI heartsText;
    [SerializeField] private TextMeshProUGUI prestigeText;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Selected Cat Display")]
    [SerializeField] private GameObject catStatsPanel;
    [SerializeField] private TextMeshProUGUI catNameText;
    [SerializeField] private TextMeshProUGUI catPersonalityText;
    [SerializeField] private TextMeshProUGUI catStateText;
    [SerializeField] private Slider affectionSlider;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TextMeshProUGUI affectionValueText;
    [SerializeField] private TextMeshProUGUI hungerValueText;
    [SerializeField] private TextMeshProUGUI energyValueText;
    [SerializeField] private Image catPortrait;

    [Header("Status Messages")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float statusDisplayDuration = 3f;
    private float statusTimer = 0f;

    [Header("Interaction Buttons")]
    [SerializeField] private Button feedButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button petButton;
    [SerializeField] private Button wakeButton;
    [SerializeField] private Button releaseButton;

    [Header("Login Screen")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private Button signInButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;

    [Header("Offline Reward Popup")]
    [SerializeField] private GameObject offlineRewardPanel;
    [SerializeField] private TextMeshProUGUI offlineRewardText;
    [SerializeField] private Button claimRewardButton;

    [Header("Adoption UI")]
    [SerializeField] private GameObject adoptionPanel;
    [SerializeField] private Transform adoptionCatGrid;
    [SerializeField] private GameObject adoptionCatCardPrefab;

    // References
    private CatGameManager gameManager;
    private FirebaseManager firebaseManager;

    // Currently selected cat
    private string selectedCatId;

    void Start()
    {
        // Get references
        gameManager = CatGameManager.Instance;
        firebaseManager = FirebaseManager.Instance;

        // Subscribe to events
        if (gameManager != null)
        {
            gameManager.OnCoinsChanged += UpdateCoins;
            gameManager.OnHeartsChanged += UpdateHearts;
            gameManager.OnCatAdopted += OnCatAdopted;
        }

        if (firebaseManager != null)
        {
            firebaseManager.OnAuthenticationChanged += OnAuthChanged;
            firebaseManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }

        // Setup buttons
        if (signInButton != null)
            signInButton.onClick.AddListener(OnSignInClicked);

        if (feedButton != null)
            feedButton.onClick.AddListener(OnFeedClicked);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (petButton != null)
            petButton.onClick.AddListener(OnPetClicked);

        if (wakeButton != null)
            wakeButton.onClick.AddListener(OnWakeClicked);

        if (releaseButton != null)
            releaseButton.onClick.AddListener(OnReleaseClicked);

        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(OnClaimRewardClicked);

        // Show login screen initially
        ShowLoginScreen();
    }

    void Update()
    {
        // Update status text timer
        if (statusTimer > 0)
        {
            statusTimer -= Time.deltaTime;
            if (statusTimer <= 0 && statusText != null)
            {
                statusText.text = "";
            }
        }

        // Update selected cat display
        if (!string.IsNullOrEmpty(selectedCatId) && gameManager != null && gameManager.PlayerProfile != null)
        {
            if (gameManager.PlayerProfile.cats.ContainsKey(selectedCatId))
            {
                UpdateCatStatsDisplay(gameManager.PlayerProfile.cats[selectedCatId]);
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (gameManager != null)
        {
            gameManager.OnCoinsChanged -= UpdateCoins;
            gameManager.OnHeartsChanged -= UpdateHearts;
            gameManager.OnCatAdopted -= OnCatAdopted;
        }

        if (firebaseManager != null)
        {
            firebaseManager.OnAuthenticationChanged -= OnAuthChanged;
            firebaseManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        }
    }

    #region Display Updates

    /// <summary>
    /// Update player stats display
    /// </summary>
    public void UpdatePlayerStats(PlayerProfile profile)
    {
        if (profile == null) return;

        if (playerNameText != null)
            playerNameText.text = profile.displayName;

        if (coinsText != null)
            coinsText.text = profile.coins.ToString();

        if (heartsText != null)
            heartsText.text = profile.hearts.ToString();

        if (prestigeText != null)
            prestigeText.text = $"Prestige: {profile.prestigePoints}";
    }

    /// <summary>
    /// Update cat stats panel for selected cat
    /// </summary>
    public void UpdateCatStatsDisplay(CatData cat)
    {
        if (cat == null || catStatsPanel == null) return;

        catStatsPanel.SetActive(true);

        if (catNameText != null)
            catNameText.text = cat.name;

        if (catPersonalityText != null)
            catPersonalityText.text = $"{cat.personality} {cat.rarity}";

        if (catStateText != null)
        {
            string stateDisplay = cat.isSleeping ? "😴 Sleeping" : GetStateEmoji(cat.currentState);
            catStateText.text = stateDisplay;
        }

        // Sliders
        if (affectionSlider != null)
        {
            affectionSlider.value = cat.affection / 100f;
            if (affectionValueText != null)
                affectionValueText.text = $"{cat.affection:F0}/100";
        }

        if (hungerSlider != null)
        {
            hungerSlider.value = cat.hunger / 100f;
            if (hungerValueText != null)
                hungerValueText.text = $"{cat.hunger:F0}/100";
        }

        if (energySlider != null)
        {
            energySlider.value = cat.energy / 100f;
            if (energyValueText != null)
                energyValueText.text = $"{cat.energy:F0}/100";
        }

        // Update button states
        UpdateInteractionButtons(cat);
    }

    /// <summary>
    /// Update interaction buttons based on cat state
    /// </summary>
    void UpdateInteractionButtons(CatData cat)
    {
        bool isSleeping = cat.isSleeping;

        if (feedButton != null)
            feedButton.interactable = !isSleeping;

        if (playButton != null)
            playButton.interactable = !isSleeping && cat.energy > 20f;

        if (petButton != null)
            petButton.interactable = !isSleeping;

        if (wakeButton != null)
            wakeButton.interactable = isSleeping;
    }

    /// <summary>
    /// Get emoji for cat state
    /// </summary>
    string GetStateEmoji(CatState state)
    {
        return state switch
        {
            CatState.Idle => "🐱 Idle",
            CatState.Playing => "🎾 Playing",
            CatState.Eating => "🍽️ Eating",
            CatState.Grooming => "✨ Grooming",
            CatState.Exploring => "🔍 Exploring",
            CatState.Watching => "👀 Watching",
            CatState.BeingPetted => "🤗 Being Petted",
            _ => "🐾 " + state.ToString()
        };
    }

    /// <summary>
    /// Update coins display
    /// </summary>
    void UpdateCoins(int newAmount)
    {
        if (coinsText != null)
            coinsText.text = newAmount.ToString();
    }

    /// <summary>
    /// Update hearts display
    /// </summary>
    void UpdateHearts(int newAmount)
    {
        if (heartsText != null)
            heartsText.text = newAmount.ToString();
    }

    /// <summary>
    /// Show status message temporarily
    /// </summary>
    public void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusTimer = statusDisplayDuration;
        }

        Debug.Log($"💬 {message}");
    }

    #endregion

    #region UI Screens

    /// <summary>
    /// Show login screen
    /// </summary>
    void ShowLoginScreen()
    {
        if (loginPanel != null)
            loginPanel.SetActive(true);

        if (catStatsPanel != null)
            catStatsPanel.SetActive(false);

        if (loginStatusText != null)
            loginStatusText.text = "Welcome! Sign in to start caring for cats.";
    }

    /// <summary>
    /// Hide login screen, show game UI
    /// </summary>
    void ShowGameUI()
    {
        if (loginPanel != null)
            loginPanel.SetActive(false);

        if (catStatsPanel != null)
            catStatsPanel.SetActive(true);
    }

    /// <summary>
    /// Show offline reward popup
    /// </summary>
    public void ShowOfflineReward(OfflineReward reward)
    {
        if (offlineRewardPanel == null) return;

        offlineRewardPanel.SetActive(true);

        if (offlineRewardText != null)
        {
            string message = $"Welcome back!\n\n" +
                           $"You were away for {reward.hoursAway:F1} hours\n" +
                           $"Your cats earned {reward.heartsEarned} hearts!\n\n";

            if (reward.wasCapped)
            {
                message += "(Capped at 24 hours)";
            }

            offlineRewardText.text = message;
        }
    }

    /// <summary>
    /// Show adoption center UI
    /// </summary>
    public async void ShowAdoptionCenter()
    {
        if (adoptionPanel == null) return;

        adoptionPanel.SetActive(true);

        // Get adoption batch from Firebase
        var cats = await firebaseManager.GetAdoptionCenterBatch();

        // Clear existing cards
        foreach (Transform child in adoptionCatGrid)
        {
            Destroy(child.gameObject);
        }

        // Create card for each cat
        foreach (var cat in cats)
        {
            CreateAdoptionCard(cat);
        }
    }

    /// <summary>
    /// Create adoption card for a cat
    /// </summary>
    void CreateAdoptionCard(CatData cat)
    {
        if (adoptionCatCardPrefab == null) return;

        GameObject card = Instantiate(adoptionCatCardPrefab, adoptionCatGrid);

        // TODO: Populate card with cat data
        // This will be implemented when we create the adoption card prefab
    }

    #endregion

    #region Button Handlers

    void OnSignInClicked()
    {
        if (loginStatusText != null)
            loginStatusText.text = "Signing in...";

        // Firebase will handle sign-in and trigger OnAuthChanged event
    }

    void OnFeedClicked()
    {
        if (string.IsNullOrEmpty(selectedCatId)) return;

        // Use basic food for now (10 coins)
        gameManager.FeedCat(selectedCatId, "dry_food", 10);
    }

    void OnPlayClicked()
    {
        if (string.IsNullOrEmpty(selectedCatId)) return;

        // TODO: Show mini-game UI
        // For now, just auto-play
        UpdateStatus("Playing with cat... (mini-game coming soon!)");
    }

    void OnPetClicked()
    {
        if (string.IsNullOrEmpty(selectedCatId)) return;

        // Pet for 3 seconds (earns 6 coins)
        gameManager.PetCat(selectedCatId, 3f);
    }

    void OnWakeClicked()
    {
        if (string.IsNullOrEmpty(selectedCatId)) return;

        gameManager.WakeUpCat(selectedCatId);
    }

    void OnReleaseClicked()
    {
        if (string.IsNullOrEmpty(selectedCatId)) return;

        // TODO: Show confirmation dialog
        // For now, warn in console
        Debug.LogWarning("Release button clicked - need confirmation dialog!");
    }

    void OnClaimRewardClicked()
    {
        if (offlineRewardPanel != null)
            offlineRewardPanel.SetActive(false);
    }

    #endregion

    #region Event Handlers

    void OnAuthChanged(string userId)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            Debug.Log("✅ UI: User authenticated");
            ShowGameUI();
        }
        else
        {
            Debug.Log("❌ UI: User signed out");
            ShowLoginScreen();
        }
    }

    void OnPlayerDataLoaded(PlayerProfile profile)
    {
        Debug.Log("📊 UI: Player data loaded");
        UpdatePlayerStats(profile);

        // Select first cat if available
        if (profile.cats.Count > 0)
        {
            selectedCatId = profile.cats.Keys.GetEnumerator().Current;
        }
    }

    void OnCatAdopted(CatData cat)
    {
        Debug.Log($"🎉 UI: Cat adopted - {cat.name}");

        // Select newly adopted cat
        selectedCatId = cat.id;

        UpdateStatus($"Welcome home, {cat.name}! 🐾");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Select a cat to display
    /// </summary>
    public void SelectCat(string catId)
    {
        selectedCatId = catId;

        if (gameManager != null && gameManager.PlayerProfile != null)
        {
            if (gameManager.PlayerProfile.cats.ContainsKey(catId))
            {
                UpdateCatStatsDisplay(gameManager.PlayerProfile.cats[catId]);
            }
        }
    }

    #endregion
}
