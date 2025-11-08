using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;
using WordGame.Utilities;

namespace WordGame.UI
{
    public class InvitationPopup : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI inviterNameText;
        [SerializeField] private Image inviterAvatarImage;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;

        [Header("Settings")]
        [SerializeField] private float autoCloseTime = 15f;

        private RoomInviteData inviteData;
        private CancellationTokenSource cancellationTokenSource;
        private float timeRemaining;

        private void Awake()
        {
            if (this.acceptButton != null)
            {
                this.acceptButton.onClick.AddListener(this.OnAcceptClicked);
            }

            if (this.declineButton != null)
            {
                this.declineButton.onClick.AddListener(this.OnDeclineClicked);
            }
        }

        public void Setup(RoomInviteData data)
        {
            this.inviteData = data;

            if (this.inviterNameText != null)
            {
                this.inviterNameText.text = data.inviterName;
            }

            if (this.messageText != null)
            {
                this.messageText.text = $"{data.inviterName} invited you to join their room!";
            }

            if (this.inviterAvatarImage != null)
            {
                this.LoadAvatar(data.inviterAvatar);
            }

            this.timeRemaining = this.autoCloseTime;
            this.cancellationTokenSource = new CancellationTokenSource();
            _ = this.StartTimerAsync();
        }

        private async Task StartTimerAsync()
        {
            try
            {
                while (this.timeRemaining > 0 && !this.cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, this.cancellationTokenSource.Token);
                    this.timeRemaining--;

                    if (this.timerText != null)
                    {
                        UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                        {
                            this.timerText.text = $"{Mathf.CeilToInt(this.timeRemaining)}s";
                        });
                    }
                }

                if (this.timeRemaining <= 0)
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        this.Close();
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Timer was cancelled, this is expected
            }
        }

        private async void OnAcceptClicked()
        {
            if (this.inviteData == null)
                return;

            Debug.Log($"[INVITE] Accepting invite from {this.inviteData.inviterName}, joining room {this.inviteData.roomCode}");

            this.cancellationTokenSource?.Cancel();

            // Join the room
            var username = PlayerPrefs.GetString("username", "Player");
            await NetworkManager.Instance?.JoinRoom(this.inviteData.roomCode, username);

            // Show room screen
            UIScreenController.Instance?.Show(UIScreenController.MultiplayerRoomScreenId);

            this.Close();
        }

        private void OnDeclineClicked()
        {
            Debug.Log($"[INVITE] Declined invite from {this.inviteData?.inviterName}");
            this.cancellationTokenSource?.Cancel();
            this.Close();
        }

        private void Close()
        {
            Destroy(this.gameObject);
        }

        private void LoadAvatar(string avatarUrl)
        {
            if (this.inviterAvatarImage == null)
                return;

            if (string.IsNullOrEmpty(avatarUrl))
            {
                this.inviterAvatarImage.color = new Color(0.7f, 0.7f, 0.7f);
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
                    this.inviterAvatarImage.sprite = sprite;
                    this.inviterAvatarImage.color = Color.white;
                }
                else
                {
                    this.inviterAvatarImage.color = new Color(0.7f, 0.7f, 0.7f);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load avatar: {ex.Message}");
                this.inviterAvatarImage.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private void OnDestroy()
        {
            this.cancellationTokenSource?.Cancel();
            this.cancellationTokenSource?.Dispose();

            if (this.acceptButton != null)
            {
                this.acceptButton.onClick.RemoveListener(this.OnAcceptClicked);
            }

            if (this.declineButton != null)
            {
                this.declineButton.onClick.RemoveListener(this.OnDeclineClicked);
            }
        }
    }
}
