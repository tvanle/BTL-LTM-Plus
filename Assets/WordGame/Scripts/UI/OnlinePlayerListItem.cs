using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;
using WordGame.Utilities;

namespace WordGame.UI
{
    public class OnlinePlayerListItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Button inviteButton;

        [Header("Status Colors")]
        [SerializeField] private Color idleColor = Color.green;
        [SerializeField] private Color busyColor = Color.red;

        private OnlinePlayerData playerData;

        public void Setup(OnlinePlayerData data)
        {
            this.playerData = data;

            if (this.usernameText != null)
            {
                this.usernameText.text = data.Username;
            }

            if (this.statusIndicator != null)
            {
                this.statusIndicator.color = data.Status == "idle" ? this.idleColor : this.busyColor;
            }

            if (this.inviteButton != null)
            {
                this.inviteButton.onClick.RemoveAllListeners();
                this.inviteButton.onClick.AddListener(this.OnInviteClicked);
                // Disable button if player is busy
                this.inviteButton.interactable = data.Status == "idle";
            }

            if (this.avatarImage != null)
            {
                this.LoadAvatar(data.AvatarUrl);
            }
        }

        private void OnInviteClicked()
        {
            if (this.playerData == null)
                return;

            Debug.Log($"[INVITE] Sending invite to {this.playerData.Username}");
            NetworkManager.Instance?.SendInvite(this.playerData.PlayerId);
            Toast.instance?.ShowMessage($"Invited {this.playerData.Username}!");
        }

        private void LoadAvatar(string avatarUrl)
        {
            if (this.avatarImage == null)
                return;

            if (string.IsNullOrEmpty(avatarUrl))
            {
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
}
