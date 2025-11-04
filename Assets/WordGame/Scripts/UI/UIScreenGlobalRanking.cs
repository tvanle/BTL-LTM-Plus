using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;

public class UIScreenGlobalRanking : UIScreen
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform rankingContainer;
    [SerializeField] private GameObject rankingItemPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("User Info Section")]
    [SerializeField] private GameObject userInfoPanel;
    [SerializeField] private TextMeshProUGUI userRankText;
    [SerializeField] private TextMeshProUGUI userXPText;
    [SerializeField] private TextMeshProUGUI userNameText;

    private readonly List<GameObject> rankingItems = new List<GameObject>();
    private          RankingData      currentRankingData;

    public override void Initialize()
    {
        base.Initialize();

        if (this.closeButton != null)
        {
            this.closeButton.onClick.AddListener(this.OnCloseClicked);
        }

        if (this.refreshButton != null)
        {
            this.refreshButton.onClick.AddListener(this.OnRefreshClicked);
        }

        // Subscribe to network events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRankingReceived += this.HandleRankingData;
        }
    }

    protected override void OnShowingContent(object data)
    {
        Debug.Log($"[GLOBAL RANKING] OnShowing called");

        if (this.titleText != null)
        {
            this.titleText.text = "Global Ranking";
        }

        if (this.loadingText != null)
        {
            this.loadingText.gameObject.SetActive(true);
            this.loadingText.text = "Loading...";
        }

        // Request ranking from server
        this.RequestRankingData();
    }

    private async void RequestRankingData()
    {
        if (NetworkManager.Instance != null)
        {
            await NetworkManager.Instance.RequestRanking(limit: 100, offset: 0);
        }
    }

    private void HandleRankingData(RankingData data)
    {
        Debug.Log($"[GLOBAL RANKING] Received {data?.ranking?.Count ?? 0} entries");

        this.currentRankingData = data;

        if (this.loadingText != null)
        {
            this.loadingText.gameObject.SetActive(false);
        }

        // Display user's own rank info
        this.DisplayUserInfo(data?.userRank);

        // Display full ranking list
        this.DisplayRanking(data);
    }

    private void DisplayUserInfo(RankingEntry userRank)
    {
        if (this.userInfoPanel != null)
        {
            if (userRank != null)
            {
                this.userInfoPanel.SetActive(true);

                if (this.userRankText != null)
                {
                    this.userRankText.text = $"Your Rank: #{userRank.rank}";
                }

                if (this.userXPText != null)
                {
                    this.userXPText.text = $"{userRank.totalXP:N0} XP";
                }

                if (this.userNameText != null)
                {
                    var displayName = !string.IsNullOrEmpty(userRank.displayName)
                        ? userRank.displayName
                        : userRank.username;
                    this.userNameText.text = displayName;
                }
            }
            else
            {
                this.userInfoPanel.SetActive(false);
            }
        }
    }

    private void DisplayRanking(RankingData data)
    {
        // Clear existing items
        foreach (var item in this.rankingItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        this.rankingItems.Clear();

        if (data?.ranking == null || data.ranking.Count == 0)
        {
            Debug.LogWarning("[GLOBAL RANKING] No entries to display");
            if (this.loadingText != null)
            {
                this.loadingText.gameObject.SetActive(true);
                this.loadingText.text = "No players found";
            }
            return;
        }

        // Get current user ID
        var currentUserId = PlayerPrefs.GetString("user_id", "");

        // Display ranking entries
        foreach (var entry in data.ranking)
        {
            var itemGO = Instantiate(this.rankingItemPrefab, this.rankingContainer);
            itemGO.SetActive(true);

            var rankingItem = itemGO.GetComponent<RankingItem>();
            if (rankingItem != null)
            {
                var isCurrentUser = !string.IsNullOrEmpty(currentUserId) && entry.userId == currentUserId;
                rankingItem.Setup(entry, isCurrentUser);
            }
            else
            {
                Debug.LogWarning("[GLOBAL RANKING] RankingItem component not found on prefab");
            }

            this.rankingItems.Add(itemGO);
        }

        Debug.Log($"[GLOBAL RANKING] Created {this.rankingItems.Count} ranking items");
    }

    private void OnCloseClicked()
    {
        //show menu
        UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, false, true);
    }

    private void OnRefreshClicked()
    {
        if (this.loadingText != null)
        {
            this.loadingText.gameObject.SetActive(true);
            this.loadingText.text = "Refreshing...";
        }

        // Clear current items
        foreach (var item in this.rankingItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        this.rankingItems.Clear();

        // Request fresh data
        this.RequestRankingData();
    }

    private void OnDisable()
    {
        // Clear items when screen is disabled
        foreach (var item in this.rankingItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        this.rankingItems.Clear();
    }

    private void OnDestroy()
    {
        if (this.closeButton != null)
        {
            this.closeButton.onClick.RemoveListener(this.OnCloseClicked);
        }

        if (this.refreshButton != null)
        {
            this.refreshButton.onClick.RemoveListener(this.OnRefreshClicked);
        }

        // Unsubscribe from events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRankingReceived -= this.HandleRankingData;
        }
    }
}