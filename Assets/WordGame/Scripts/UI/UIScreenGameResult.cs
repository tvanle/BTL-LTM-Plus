using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;
using WordGame.Utilities;

public class UIScreenGameResult : UIScreen
{
    [Header("Summary Section - Top Row")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI xpGainedText;

    [Header("Summary Section - Stats Row")]
    [SerializeField] private TextMeshProUGUI wordsCompleteText;
    [SerializeField] private TextMeshProUGUI yourScoreText;

    [Header("Leaderboard Section")]
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private GameObject leaderboardItemPrefab;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private List<GameObject> leaderboardItems = new List<GameObject>();
    private GameEndData currentGameData;

    public override void Initialize()
    {
        base.Initialize();

        if (this.backButton != null)
        {
            this.backButton.onClick.AddListener(this.OnBackButtonClicked);
        }
    }

    public override void OnShowing(object data)
    {
        Debug.Log($"[GAME_RESULT] OnShowing called with data: {data?.GetType().Name ?? "null"}");

        if (data is GameEndData gameEndData)
        {
            Debug.Log($"[GAME_RESULT] Results count: {gameEndData.results?.Count ?? 0}");
            this.currentGameData = gameEndData;
            this.DisplayResults(gameEndData);
        }
        else
        {
            Debug.LogWarning($"[GAME_RESULT] Unknown data type: {data?.GetType().Name}");
        }
    }

    private void DisplayResults(GameEndData gameData)
    {
        if (gameData?.results == null || gameData.results.Count == 0)
        {
            Debug.LogWarning("[GAME_RESULT] No results to display");
            return;
        }

        // Find current player's result
        var currentPlayerId = NetworkManager.Instance?.PlayerId;
        PlayerResult myResult = null;

        if (!string.IsNullOrEmpty(currentPlayerId))
        {
            myResult = gameData.results.Find(r => r.Id == currentPlayerId);
        }

        // If not found by ID, assume first player for single player or use first result
        if (myResult == null && gameData.results.Count > 0)
        {
            myResult = gameData.results[0];
        }

        // Display summary section
        if (myResult != null)
        {
            // Find rank
            var sortedResults = new List<PlayerResult>(gameData.results);
            sortedResults.Sort((a, b) => b.Score.CompareTo(a.Score));
            var myRank = sortedResults.FindIndex(r => r.Id == myResult.Id) + 1;

            // Top row
            if (this.rankText != null)
            {
                this.rankText.text = $"#{myRank}";
            }

            if (this.usernameText != null)
            {
                this.usernameText.text = myResult.Username;
            }

            if (this.xpGainedText != null)
            {
                this.xpGainedText.text = $"+{myResult.XPGained} XP";
            }

            // Stats row
            if (this.wordsCompleteText != null)
            {
                this.wordsCompleteText.text = $"{myResult.WordsFound}/{myResult.TotalWords}";
            }

            if (this.yourScoreText != null)
            {
                this.yourScoreText.text = $"+{myResult.Score.ToString()}";
            }
        }

        // Display leaderboard
        this.DisplayLeaderboard(gameData.results);
    }

    private void DisplayLeaderboard(List<PlayerResult> results)
    {
        Debug.Log($"[GAME_RESULT] DisplayLeaderboard called with {results?.Count ?? 0} results");

        // Clear existing items
        foreach (var item in this.leaderboardItems)
        {
            Destroy(item);
        }
        this.leaderboardItems.Clear();

        if (results == null || results.Count == 0)
        {
            Debug.LogWarning("[GAME_RESULT] No results to display");
            return;
        }

        // Results should already be sorted by server, but sort again to be safe
        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Get current player ID for highlighting
        var currentPlayerId = NetworkManager.Instance?.PlayerId;

        // Add new leaderboard items
        var rank = 1;
        foreach (var result in results)
        {
            Debug.Log($"[GAME_RESULT] Processing rank {rank}: {result.Username} - {result.Score} points, +{result.XPGained} XP");

            var itemGO = Instantiate(this.leaderboardItemPrefab, this.leaderboardContainer);
            itemGO.SetActive(true);

            // Setup using LeaderboardItem script
            var leaderboardItem = itemGO.GetComponent<LeaderboardItem>();
            if (leaderboardItem != null)
            {
                var isCurrentPlayer = !string.IsNullOrEmpty(currentPlayerId) && result.Id == currentPlayerId;
                leaderboardItem.Setup(rank, result, isCurrentPlayer);
            }
            else
            {
                Debug.LogWarning("[GAME_RESULT] LeaderboardItem component not found on prefab");
            }

            this.leaderboardItems.Add(itemGO);
            rank++;
        }

        Debug.Log($"[GAME_RESULT] Created {this.leaderboardItems.Count} leaderboard items");
    }

    private void OnBackButtonClicked()
    {
        // Return to menu
        UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId);
    }

    private void OnDisable()
    {
        // Clear items when screen is disabled
        foreach (var item in this.leaderboardItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        this.leaderboardItems.Clear();
    }
}
