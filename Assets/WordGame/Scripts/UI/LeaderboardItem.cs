using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network.Models;
using WordGame.Utilities;

public class LeaderboardItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI pointText;

    [Header("Optional Highlight")]
    [SerializeField] private Image backgroundImage;

    private PlayerResult playerData;

    public void Setup(int rank, PlayerResult result, bool isCurrentPlayer = false)
    {
        this.playerData = result;

        // Set rank
        if (this.rankText != null)
        {
            this.rankText.text = rank.ToString();
        }

        // Set name
        if (this.nameText != null)
        {
            this.nameText.text = result.Username;
        }

        // Set points
        if (this.pointText != null)
        {
            this.pointText.text = result.Score.ToString();
        }

        // Load avatar
        if (this.avatarImage != null)
        {
            this.LoadAvatar(result.AvatarUrl);
        }

        // Highlight current player
        if (isCurrentPlayer && this.backgroundImage != null)
        {
            this.backgroundImage.color = new Color(1f, 1f, 0.7f, 0.3f); // Light yellow
        }
        else if (this.backgroundImage != null)
        {
            this.backgroundImage.color = new Color(1f, 1f, 1f, 0.1f); // Default
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

    public PlayerResult GetPlayerData()
    {
        return this.playerData;
    }
}
