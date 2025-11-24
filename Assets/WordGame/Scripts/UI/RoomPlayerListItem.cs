using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Utilities;

namespace WordGame.UI
{
    public class RoomPlayerListItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Image avatarImage;

        public void Setup(string username, string avatarUrl)
        {
            if (this.usernameText != null)
            {
                this.usernameText.text = username;
            }

            if (this.avatarImage != null)
            {
                this.LoadAvatar(avatarUrl);
            }
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
