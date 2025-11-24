using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;

public class MatchDetailPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;

    [Header("Match Info")]
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI roomCodeText;

    [Header("Players List")]
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject playerItemPrefab;

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI errorText;

    private NetworkManager networkManager;
    private List<GameObject> playerItems = new List<GameObject>();

    private void Awake()
    {
        this.networkManager = NetworkManager.Instance;

        if (this.closeButton != null)
        {
            this.closeButton.onClick.AddListener(this.Hide);
        }

        if (this.backgroundButton != null)
        {
            this.backgroundButton.onClick.AddListener(this.Hide);
        }

        this.Hide();
    }

    public async void ShowMatchDetail(string matchId)
    {
        if (string.IsNullOrEmpty(matchId))
        {
            Debug.LogError("[MATCH_DETAIL] Invalid match ID");
            return;
        }

        // Show popup and loading
        if (this.popupPanel != null)
        {
            this.popupPanel.SetActive(true);
        }

        this.ShowLoading(true);
        this.ShowError(null);

        // Clear previous players
        this.ClearPlayerItems();

        try
        {
            // Request match details from server
            var matchDetail = await this.networkManager.GetMatchDetails(matchId);

            if (matchDetail == null)
            {
                this.ShowError("Failed to load match details");
                return;
            }

            // Display match info
            this.DisplayMatchInfo(matchDetail);

            // Display players
            this.DisplayPlayers(matchDetail.players);

            this.ShowLoading(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MATCH_DETAIL] Error loading details: {ex.Message}");
            this.ShowError($"Error: {ex.Message}");
            this.ShowLoading(false);
        }
    }

    private void DisplayMatchInfo(MatchDetailData matchDetail)
    {
        if (this.titleText != null)
        {
            this.titleText.text = "Match Details";
        }

        if (this.categoryText != null)
        {
            this.categoryText.text = matchDetail.category;
        }

        if (this.dateText != null && !string.IsNullOrEmpty(matchDetail.completedAt))
        {
            var date = DateTime.Parse(matchDetail.completedAt, null, System.Globalization.DateTimeStyles.RoundtripKind);
            if (date.Kind == DateTimeKind.Local)
            {
                date = date.ToUniversalTime();
            }
            this.dateText.text = date.ToString("MMM dd, yyyy HH:mm");
        }

        if (this.durationText != null)
        {
            var minutes = matchDetail.totalDurationSeconds / 60;
            var seconds = matchDetail.totalDurationSeconds % 60;
            this.durationText.text = $"{minutes:D2}:{seconds:D2}";
        }

        if (this.roomCodeText != null)
        {
            this.roomCodeText.text = $"Room: {matchDetail.roomCode}";
        }
    }

    private void DisplayPlayers(List<MatchPlayerData> players)
    {
        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("[MATCH_DETAIL] No players data");
            return;
        }

        // Sort by rank
        players.Sort((a, b) => a.rankPosition.CompareTo(b.rankPosition));

        foreach (var player in players)
        {
            var itemGO = Instantiate(this.playerItemPrefab, this.playersContainer);
            itemGO.SetActive(true);

            var playerItem = itemGO.GetComponent<MatchDetailPlayerItem>();
            if (playerItem != null)
            {
                playerItem.Setup(player);
            }

            this.playerItems.Add(itemGO);
        }
    }

    private void ClearPlayerItems()
    {
        foreach (var item in this.playerItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        this.playerItems.Clear();
    }

    private void ShowLoading(bool show)
    {
        if (this.loadingPanel != null)
        {
            this.loadingPanel.SetActive(show);
        }
    }

    private void ShowError(string message)
    {
        if (this.errorText != null)
        {
            this.errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            if (!string.IsNullOrEmpty(message))
            {
                this.errorText.text = message;
            }
        }
    }

    public void Hide()
    {
        if (this.popupPanel != null)
        {
            this.popupPanel.SetActive(false);
        }

        this.ClearPlayerItems();
    }

    private void OnDestroy()
    {
        this.ClearPlayerItems();
    }
}

[Serializable]
public class MatchDetailData
{
    public string matchId;
    public string roomCode;
    public string category;
    public int totalDurationSeconds;
    public string completedAt;
    public List<MatchPlayerData> players;
}

[Serializable]
public class MatchPlayerData
{
    public string userId;
    public string username;
    public string displayName;
    public string avatarUrl;
    public int finalScore;
    public int rankPosition;
    public int bestStreak;
    public int totalWordsFound;
    public int completedLevels;
    public float averageTimePerLevel;
    public int xpGained;
    public bool isWinner;
}
