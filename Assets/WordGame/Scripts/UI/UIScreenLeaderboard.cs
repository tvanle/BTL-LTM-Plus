using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;
using WordGame.Utilities;

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
            this.DisplayLeaderboard(levelEndData.results);

            if (this.titleText != null)
            {
                this.titleText.text = $"Leaderboard - Level {levelEndData.level}";
            }
        }
        else if (data is List<PlayerResult> results)
        {
            Debug.Log($"[LEADERBOARD] Direct results list, count: {results?.Count ?? 0}");
            this.DisplayLeaderboard(results);
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
        foreach (var item in this.leaderboardItems)
        {
            Destroy(item);
        }
        this.leaderboardItems.Clear();

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

            var item = Instantiate(this.leaderboardItemPrefab, this.leaderboardContainer);
            item.SetActive(true);

            // Try TextMeshProUGUI first
            var tmpTexts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (tmpTexts.Length >= 3)
            {
                tmpTexts[0].text = rank.ToString(); // Rank
                tmpTexts[1].text = result.Username; // Name
                tmpTexts[2].text = result.Score.ToString() + " points"; // Score
            }

            // Load player avatar
            var avatarTransform = item.transform.Find("Avatar");
            if (avatarTransform != null)
            {
                var avatarImage = avatarTransform.GetComponent<Image>();
                if (avatarImage != null)
                {
                    this.LoadPlayerAvatar(avatarImage, result.AvatarUrl);
                }
            }

            this.leaderboardItems.Add(item);
            rank++;
        }

        Debug.Log($"[LEADERBOARD] Created {this.leaderboardItems.Count} leaderboard items");
    }

    private void LoadPlayerAvatar(Image avatarImage, string avatarUrl)
    {
        if (string.IsNullOrEmpty(avatarUrl))
        {
            // Set default avatar
            avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
            return;
        }

        try
        {
            var texture = ImagePicker.LoadTextureFromBase64(avatarUrl);
            if (texture != null)
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                avatarImage.sprite = sprite;
                avatarImage.color = Color.white;
            }
            else
            {
                // Fallback to default
                avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load avatar: {ex.Message}");
            avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }

    private void OnDisable()
    {
        // Clear items when screen is disabled
        foreach (var item in this.leaderboardItems)
        {
            Destroy(item);
        }
        this.leaderboardItems.Clear();
    }
}