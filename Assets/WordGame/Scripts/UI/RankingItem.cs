using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network.Models;
using WordGame.Utilities;

public class RankingItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private Image backgroundImage;

    [Header("Highlight Settings")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.3f);

    public void Setup(RankingEntry entry, bool isCurrentUser = false)
    {
        // Set rank
        if (this.rankText != null)
        {
            this.rankText.text = $"#{entry.rank}";
        }

        // Set username
        if (this.usernameText != null)
        {
            var displayName = !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : entry.username;
            this.usernameText.text = displayName;

            // Highlight current user
            if (isCurrentUser)
            {
                this.usernameText.color = Color.yellow;
                this.usernameText.fontStyle = FontStyles.Bold;
            }
        }

        // Set XP
        if (this.xpText != null)
        {
            this.xpText.text = $"{entry.totalXP:N0} XP";
        }

        // Load avatar
        if (this.avatarImage != null)
        {
            this.LoadAvatar(entry.avatarUrl);
        }

        // Highlight background for current user
        if (this.backgroundImage != null)
        {
            this.backgroundImage.color = isCurrentUser ? this.highlightColor : this.normalColor;
        }
    }

    private void LoadAvatar(string avatarUrl)
    {
        if (this.avatarImage == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(avatarUrl))
        {
            // Set default avatar color
            this.avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
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
                this.avatarImage.sprite = sprite;
                this.avatarImage.color = Color.white;
            }
            else
            {
                // Fallback to default
                this.avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load avatar: {ex.Message}");
            this.avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
        }
    }
}
