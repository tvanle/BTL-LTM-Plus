using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WordGame.Network;
using WordGame.Network.Models;

public class UIScreenLeaderboard : UIScreen
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private GameObject leaderboardItemPrefab;
    [SerializeField] private TextMeshProUGUI levelEndText;

    [Header("Leaderboard Animation")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float popScale = 1.2f;

    private List<GameObject> leaderboardItems = new List<GameObject>();

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void PlayShowAnimation()
    {
        // Trophy pop effect
        this.StartCoroutine(this.PopAndBounce());
    }

    private IEnumerator PopAndBounce()
    {
        var canvasGroup = this.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
        }

        var rectT = this.RectT;
        var elapsedTime = 0f;

        // Start invisible and small
        canvasGroup.alpha = 0f;
        rectT.localScale = Vector3.zero;

        // Pop and bounce
        while (elapsedTime < this.popDuration)
        {
            elapsedTime += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsedTime / this.popDuration);

            // Bounce curve
            var scale = 0f;
            if (progress < 0.5f)
            {
                // Grow quickly to overshoot
                var t = progress / 0.5f;
                scale = Mathf.Lerp(0f, this.popScale, t * t);
            }
            else
            {
                // Bounce back to 1.0
                var t = (progress - 0.5f) / 0.5f;
                scale = Mathf.Lerp(this.popScale, 1.0f, t);
            }

            canvasGroup.alpha = Mathf.Min(1f, progress * 2f);
            rectT.localScale = Vector3.one * scale;

            yield return null;
        }

        // Ensure final state
        canvasGroup.alpha = 1f;
        rectT.localScale = Vector3.one;
    }

    protected override void OnShowingContent(object data)
    {
        Debug.Log($"[LEADERBOARD] OnShowing called with data: {data?.GetType().Name ?? "null"}");

        if (data is LevelEndData levelEndData)
        {
            Debug.Log($"[LEADERBOARD] Level {levelEndData.level}, Results count: {levelEndData.results?.Count ?? 0}");
            this.DisplayLeaderboard(levelEndData.results);

            if (this.titleText != null)
            {
                this.titleText.text = $"Leaderboard";
            }
            if (this.levelEndText != null)
            {
                this.levelEndText.text = $"Level {levelEndData.level} Completed!";
            }
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

        // Get current player ID for highlighting
        var currentPlayerId = NetworkManager.Instance?.PlayerId;

        // Add new leaderboard items
        var rank = 1;
        foreach (var result in results)
        {
            Debug.Log($"[LEADERBOARD] Processing rank {rank}: {result.Username} - {result.Score}");

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
                Debug.LogWarning("[LEADERBOARD] LeaderboardItem component not found on prefab");
            }

            this.leaderboardItems.Add(itemGO);
            rank++;
        }

        Debug.Log($"[LEADERBOARD] Created {this.leaderboardItems.Count} leaderboard items");
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