using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;

public class UIScreenLeaderboard : UIScreen
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private GameObject leaderboardItemPrefab;

    private List<GameObject> leaderboardItems = new List<GameObject>();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void OnShowing(object data)
    {
        Debug.Log($"[LEADERBOARD] OnShowing called with data: {data?.GetType().Name ?? "null"}");

        if (data is LevelEndData levelEndData)
        {
            Debug.Log($"[LEADERBOARD] Level {levelEndData.level}, Results count: {levelEndData.results?.Count ?? 0}");
            DisplayLeaderboard(levelEndData.results);

            if (titleText != null)
            {
                titleText.text = $"Leaderboard - Level {levelEndData.level}";
            }
        }
        else if (data is List<PlayerResult> results)
        {
            Debug.Log($"[LEADERBOARD] Direct results list, count: {results?.Count ?? 0}");
            DisplayLeaderboard(results);
        }
        else
        {
            Debug.LogWarning($"[LEADERBOARD] Unknown data type: {data?.GetType().Name}");
        }
    }
    

    private void DisplayLeaderboard(List<PlayerResult> results)
    {
        Debug.Log($"[LEADERBOARD] DisplayLeaderboard called with {results?.Count ?? 0} results");

        // Clear existing items
        foreach (var item in leaderboardItems)
        {
            Destroy(item);
        }
        leaderboardItems.Clear();

        if (results == null || results.Count == 0)
        {
            Debug.LogWarning("[LEADERBOARD] No results to display");
            return;
        }

        // Sort results by score (descending)
        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Add new leaderboard items
        var rank = 1;
        foreach (var result in results)
        {
            Debug.Log($"[LEADERBOARD] Processing rank {rank}: {result.Username} - {result.Score}");

            var item = Instantiate(leaderboardItemPrefab, leaderboardContainer);
            item.SetActive(true);

            // Try TextMeshProUGUI first
            var tmpTexts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmpTexts.Length >= 3)
            {
                tmpTexts[0].text = rank.ToString(); // Rank
                tmpTexts[1].text = result.Username; // Name
                tmpTexts[2].text = result.Score.ToString() + " points"; // Score
            }

            leaderboardItems.Add(item);
            rank++;
        }

        Debug.Log($"[LEADERBOARD] Created {leaderboardItems.Count} leaderboard items");
    }

    private void OnDisable()
    {
        // Clear items when screen is disabled
        foreach (var item in leaderboardItems)
        {
            Destroy(item);
        }
        leaderboardItems.Clear();
    }
}