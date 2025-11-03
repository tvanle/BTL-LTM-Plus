using System;
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

    [Header("Result Colors")]
    [SerializeField] private Color winColor = new Color(0.3f, 0.8f, 0.3f, 0.3f);
    [SerializeField] private Color loseColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);

    public void Setup(MatchHistoryData data)
    {
        // Format date
        if (this.dateText != null)
        {
            var date = DateTime.Parse(data.completedAt);
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
