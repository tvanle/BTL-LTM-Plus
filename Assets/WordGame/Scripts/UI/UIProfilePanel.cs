using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using WordGame.Utilities;
using WordGame.Network;

namespace WordGame.UI
{
    public class UIProfilePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelContainer;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Button editProfileButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button matchHistoryButton;
        [SerializeField] private Button rankingButton;
        [SerializeField] private Button closeButton;

        public event Action OnEditProfileClicked;
        public event Action OnLogoutClicked;
        public event Action OnMatchHistoryClicked;

        private void Start()
        {
            this.editProfileButton.onClick.AddListener(this.HandleEditProfile);
            this.logoutButton.onClick.AddListener(this.HandleLogout);
            this.matchHistoryButton.onClick.AddListener(this.HandleMatchHistory);
            if (this.rankingButton != null)
            {
                this.rankingButton.onClick.AddListener(this.HandleRanking);
            }
            this.closeButton.onClick.AddListener(this.Hide);

            // Hide panel by default
            this.Hide();
        }

        private void OnDestroy()
        {
            this.editProfileButton.onClick.RemoveListener(this.HandleEditProfile);
            this.logoutButton.onClick.RemoveListener(this.HandleLogout);
            this.matchHistoryButton.onClick.RemoveListener(this.HandleMatchHistory);
            if (this.rankingButton != null)
            {
                this.rankingButton.onClick.RemoveListener(this.HandleRanking);
            }
            this.closeButton.onClick.RemoveListener(this.Hide);
        }

        public void Show()
        {
            // Load user data from PlayerPrefs
            var username = PlayerPrefs.GetString("username", "Guest");
            var avatarUrl = PlayerPrefs.GetString("avatar_url", "");

            this.usernameText.text = username;

            // Load avatar if URL exists
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                this.LoadAvatar(avatarUrl);
            }
            else
            {
                // Use default avatar
                this.SetDefaultAvatar();
            }

            this.panelContainer.SetActive(true);
        }

        public void Hide()
        {
            this.panelContainer.SetActive(false);
        }

        private void HandleEditProfile()
        {
            // Open image picker
            ImagePicker.PickImage(
                onImagePicked: async (texture, bytes) =>
                {
                    // Convert to base64
                    var base64 = ImagePicker.ConvertToBase64(bytes);

                    // Update avatar on server
                    await this.UpdateAvatarAsync(base64, texture);
                },
                onCancelled: () =>
                {
                    Debug.Log("Image picker cancelled");
                }
            );
        }

        private void HandleLogout()
        {
            this.Hide();
            this.OnLogoutClicked?.Invoke();
        }

        private void HandleMatchHistory()
        {
            this.Hide();
            this.OnMatchHistoryClicked?.Invoke();
        }

        private void HandleRanking()
        {
            this.Hide();
            UIScreenController.Instance.Show(UIScreenController.GlobalRankingScreenId, false, true);
        }

        private void LoadAvatar(string avatarData)
        {
            try
            {
                // Try to load from base64
                if (!string.IsNullOrEmpty(avatarData))
                {
                    var texture = ImagePicker.LoadTextureFromBase64(avatarData);
                    if (texture != null)
                    {
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        this.SetAvatar(sprite);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load avatar: {ex.Message}");
            }

            // Fallback to default avatar
            this.SetDefaultAvatar();
        }

        private void SetDefaultAvatar()
        {
            // Set a default color/sprite for avatar
            if (this.avatarImage != null)
            {
                this.avatarImage.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public void SetAvatar(Sprite sprite)
        {
            if (this.avatarImage != null && sprite != null)
            {
                this.avatarImage.sprite = sprite;
                this.avatarImage.color = Color.white;
            }
        }

        private async System.Threading.Tasks.Task UpdateAvatarAsync(string base64Avatar, Texture2D texture)
        {
            try
            {
                // displayName can be null (we're only updating avatar)
                var success = await NetworkManager.Instance.UpdateProfile(null, base64Avatar);

                if (success)
                {
                    // Save to PlayerPrefs
                    PlayerPrefs.SetString("avatar_url", base64Avatar);
                    PlayerPrefs.Save();

                    // Update UI
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    this.SetAvatar(sprite);

                    Toast.instance?.ShowMessage("Avatar updated successfully");
                    Debug.Log("Avatar updated successfully");
                }
                else
                {
                    Toast.instance?.ShowMessage("Failed to update avatar");
                    Debug.LogError("Failed to update avatar");
                }
            }
            catch (Exception ex)
            {
                Toast.instance?.ShowMessage($"Error: {ex.Message}");
                Debug.LogError($"Error updating avatar: {ex.Message}");
            }
        }
    }
}