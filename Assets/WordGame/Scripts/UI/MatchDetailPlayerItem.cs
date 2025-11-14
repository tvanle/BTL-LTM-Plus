using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchDetailPlayerItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI wordsFoundText;
    [SerializeField] private TextMeshProUGUI avgTimeText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image crownIcon;

    [Header("Colors")]
    [SerializeField] private Color winnerColor = new Color(1f, 0.84f, 0f, 0.3f); // Gold
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    public void Setup(MatchPlayerData data)
    {
        // Rank
        if (this.rankText != null)
        {
            this.rankText.text = $"#{data.rankPosition}";

            // Gold color for rank 1
            if (data.rankPosition == 1)
            {
                this.rankText.color = new Color(1f, 0.84f, 0f); // Gold
            }
        }

        // Username
        if (this.usernameText != null)
        {
            var displayName = !string.IsNullOrEmpty(data.displayName) ? data.displayName : data.username;
            this.usernameText.text = displayName;
        }

        // Score
        if (this.scoreText != null)
        {
            this.scoreText.text = $"{data.finalScore} pts";
        }

        // Streak
        if (this.streakText != null)
        {
            this.streakText.text = $"Streak: {data.bestStreak}";
        }

        // Words found
        if (this.wordsFoundText != null)
        {
            this.wordsFoundText.text = $"Words: {data.totalWordsFound}";
        }

        // Average time
        if (this.avgTimeText != null)
        {
            this.avgTimeText.text = $"Avg: {data.averageTimePerLevel:F1}s";
        }

        // XP gained
        if (this.xpText != null)
        {
            this.xpText.text = $"+{data.xpGained} XP";
        }

        // Background color
        if (this.backgroundImage != null)
        {
            this.backgroundImage.color = data.isWinner ? this.winnerColor : this.normalColor;
        }

        // Crown icon for winner
        if (this.crownIcon != null)
        {
            this.crownIcon.gameObject.SetActive(data.isWinner);
        }
    }
}
