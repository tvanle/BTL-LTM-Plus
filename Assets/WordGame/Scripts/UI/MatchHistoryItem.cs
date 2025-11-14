using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchHistoryItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image resultIcon;
    [SerializeField] private Button itemButton;

    [Header("Result Colors")]
    [SerializeField] private Color winColor = new Color(0.3f, 0.8f, 0.3f, 0.3f);
    [SerializeField] private Color loseColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);

    private MatchHistoryData matchData;
    private MatchDetailPopup detailPopup;

    private void Awake()
    {
        // Add button listener
        if (this.itemButton != null)
        {
            this.itemButton.onClick.AddListener(this.OnItemClicked);
        }
        else
        {
            // If no button assigned, add it to this GameObject
            var button = this.GetComponent<Button>();
            if (button == null)
            {
                button = this.gameObject.AddComponent<Button>();
            }
            button.onClick.AddListener(this.OnItemClicked);
        }
    }

    public void Setup(MatchHistoryData data)
    {
        this.matchData = data;

        // Find detail popup if not cached
        if (this.detailPopup == null)
        {
            this.detailPopup = FindFirstObjectByType<MatchDetailPopup>();
        }

        // Format date
        if (this.dateText != null)
        {
            // Parse as UTC time (server sends UTC time in ISO 8601 format)
            var date = DateTime.Parse(data.completedAt, null, System.Globalization.DateTimeStyles.RoundtripKind);

            // Convert to UTC if not already
            if (date.Kind == DateTimeKind.Local)
            {
                date = date.ToUniversalTime();
            }

            var timeSince = DateTime.UtcNow - date;

            if (timeSince.TotalDays < 1)
            {
                this.dateText.text = $"{(int)timeSince.TotalHours}h ago";
            }
            else if (timeSince.TotalDays < 7)
            {
                this.dateText.text = $"{(int)timeSince.TotalDays}d ago";
            }
            else
            {
                this.dateText.text = date.ToString("MM/dd");
            }
        }

        // Category
        if (this.categoryText != null)
        {
            this.categoryText.text = data.category;
        }

        // Result
        bool isWinner = data.isWinner;
        if (this.resultText != null)
        {
            this.resultText.text = isWinner ? $"#{data.rank} WIN" : $"#{data.rank}";
            this.resultText.color = isWinner ? Color.green : Color.white;
        }

        // Score
        if (this.scoreText != null)
        {
            this.scoreText.text = $"{data.finalScore} pts";
        }

        // XP
        if (this.xpText != null)
        {
            this.xpText.text = $"+{data.xpGained} XP";
        }

        // Background color
        if (this.backgroundImage != null)
        {
            this.backgroundImage.color = isWinner ? this.winColor : this.loseColor;
        }

        // Result icon (optional)
        if (this.resultIcon != null)
        {
            this.resultIcon.gameObject.SetActive(isWinner);
        }
    }

    private void OnItemClicked()
    {
        if (this.matchData == null || string.IsNullOrEmpty(this.matchData.matchId))
        {
            Debug.LogWarning("[MATCH_HISTORY_ITEM] No match data to show");
            return;
        }

        if (this.detailPopup == null)
        {
            Debug.LogError("[MATCH_HISTORY_ITEM] MatchDetailPopup not found in scene");
            return;
        }

        Debug.Log($"[MATCH_HISTORY_ITEM] Opening details for match {this.matchData.matchId}");
        this.detailPopup.ShowMatchDetail(this.matchData.matchId);
    }
}

[Serializable]
public class MatchHistoryData
{
    public string matchId;
    public string category;
    public int finalScore;
    public int rank;
    public bool isWinner;
    public int xpGained;
    public string completedAt;
}