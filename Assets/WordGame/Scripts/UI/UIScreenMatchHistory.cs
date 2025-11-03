using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;

public class UIScreenMatchHistory : UIScreen
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform historyContainer;
    [SerializeField] private GameObject matchHistoryItemPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Stats Summary")]
    [SerializeField] private TextMeshProUGUI totalGamesText;
    [SerializeField] private TextMeshProUGUI totalWinsText;
    [SerializeField] private TextMeshProUGUI winRateText;

    private List<GameObject> historyItems = new List<GameObject>();
    private NetworkManager networkManager;

    public override void Initialize()
    {
        base.Initialize();

        if (this.backButton != null)
        {
            this.backButton.onClick.AddListener(this.OnBackButtonClicked);
        }

        this.networkManager = NetworkManager.Instance;
    }

    protected override void OnShowingContent(object data)
    {
        if (this.titleText != null)
        {
            this.titleText.text = "Match History";
        }

        // Load match history
        this.LoadMatchHistory();
    }

    private async void LoadMatchHistory()
    {
        if (this.networkManager == null)
        {
            Debug.LogError("[MATCH_HISTORY] NetworkManager not found");
            this.ShowEmptyState("Network error");
            return;
        }

        // Show loading
        this.ShowLoading(true);
        this.ShowEmptyState(null);

        try
        {
            // Request match history from server
            var history = await this.networkManager.GetMatchHistory();

            if (history == null || history.Count == 0)
            {
                this.ShowEmptyState("No match history yet.\nPlay some games to see your history!");
                this.ShowLoading(false);
                return;
            }

            // Display history
            this.DisplayMatchHistory(history);

            // Update stats summary
            this.UpdateStatsSummary(history);

            this.ShowLoading(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MATCH_HISTORY] Error loading history: {ex.Message}");
            this.ShowEmptyState("Failed to load match history");
            this.ShowLoading(false);
        }
    }

    private void DisplayMatchHistory(List<MatchHistoryData> history)
    {
        // Clear existing items
        foreach (var item in this.historyItems)
        {
            Destroy(item);
        }
        this.historyItems.Clear();

        if (history == null || history.Count == 0)
        {
            return;
        }

        // Sort by date (newest first)
        history.Sort((a, b) =>
            DateTime.Parse(b.completedAt).CompareTo(DateTime.Parse(a.completedAt)));

        // Create items
        foreach (var match in history)
        {
            var itemGO = Instantiate(this.matchHistoryItemPrefab, this.historyContainer);
            itemGO.SetActive(true);

            var matchItem = itemGO.GetComponent<MatchHistoryItem>();
            if (matchItem != null)
            {
                matchItem.Setup(match);
            }
            else
            {
                Debug.LogWarning("[MATCH_HISTORY] MatchHistoryItem component not found on prefab");
            }

            this.historyItems.Add(itemGO);
        }

        Debug.Log($"[MATCH_HISTORY] Displayed {this.historyItems.Count} matches");
    }

    private void UpdateStatsSummary(List<MatchHistoryData> history)
    {
        if (history == null || history.Count == 0)
        {
            return;
        }

        var totalGames = history.Count;
        var totalWins = 0;

        foreach (var match in history)
        {
            if (match.isWinner)
            {
                totalWins++;
            }
        }

        var winRate = totalGames > 0 ? (totalWins * 100.0f / totalGames) : 0;

        if (this.totalGamesText != null)
        {
            this.totalGamesText.text = totalGames.ToString();
        }

        if (this.totalWinsText != null)
        {
            this.totalWinsText.text = totalWins.ToString();
        }

        if (this.winRateText != null)
        {
            this.winRateText.text = $"{winRate:F1}%";
        }
    }

    private void ShowLoading(bool show)
    {
        if (this.loadingPanel != null)
        {
            this.loadingPanel.SetActive(show);
        }
    }

    private void ShowEmptyState(string message)
    {
        if (this.emptyStatePanel != null)
        {
            this.emptyStatePanel.SetActive(!string.IsNullOrEmpty(message));
        }

        if (this.emptyStateText != null && !string.IsNullOrEmpty(message))
        {
            this.emptyStateText.text = message;
        }
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("[MATCH_HISTORY] Back button clicked");

        // Return to multiplayer menu
        UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId);
    }

    private void OnDisable()
    {
        // Clear items when screen is disabled
        foreach (var item in this.historyItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        this.historyItems.Clear();
    }
}
