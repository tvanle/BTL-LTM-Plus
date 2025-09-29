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
    [SerializeField] private Button continueButton;

    private List<GameObject> leaderboardItems = new List<GameObject>();

    public override void Initialize()
    {
        base.Initialize();

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() =>
            {
                // Return to multiplayer room
                UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, true, true);
            });
        }
    }

    public override void OnShowing(object data)
    {
        if (data is LevelEndData levelEndData)
        {
            DisplayLeaderboard(levelEndData.results);

            if (titleText != null)
            {
                titleText.text = $"LEVEL {levelEndData.level} COMPLETE";
            }

            // Auto continue to next level after countdown
            if (levelEndData.hasNextLevel)
            {
                StartCoroutine(AutoContinueCountdown(levelEndData.nextLevelStartTime));
            }
        }
        else if (data is List<PlayerResult> results)
        {
            DisplayLeaderboard(results);
        }
    }

    private System.Collections.IEnumerator AutoContinueCountdown(int seconds)
    {
        var countdown = seconds;
        while (countdown > 0)
        {
            if (continueButton != null)
            {
                var buttonText = continueButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"Next Level in {countdown}...";
                }
            }
            yield return new WaitForSeconds(1);
            countdown--;
        }

        // Auto continue to next level
        if (continueButton != null)
        {
            continueButton.onClick.Invoke();
        }
    }

    private void DisplayLeaderboard(List<PlayerResult> results)
    {
        // Clear existing items
        foreach (var item in leaderboardItems)
        {
            Destroy(item);
        }
        leaderboardItems.Clear();

        // Sort results by score (descending)
        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Add new leaderboard items
        var rank = 1;
        foreach (var result in results)
        {
            if (leaderboardItemPrefab != null && leaderboardContainer != null)
            {
                var item = Instantiate(leaderboardItemPrefab, leaderboardContainer);
                var texts = item.GetComponentsInChildren<Text>();

                if (texts.Length >= 3)
                {
                    texts[0].text = rank.ToString(); // Rank
                    texts[1].text = result.Username; // Name
                    texts[2].text = result.Score.ToString(); // Score
                }
                else if (texts.Length > 0)
                {
                    texts[0].text = $"#{rank} {result.Username}: {result.Score}";
                }

                leaderboardItems.Add(item);
                rank++;
            }
        }

        if (titleText != null)
        {
            titleText.text = "LEADERBOARD";
        }
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