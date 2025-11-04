using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;

namespace WordGame.UI
{
    public class UIScreenMultiplayerMenu : UIScreen
    {
        private NetworkManager networkManager;

        [Header("UI References")] [SerializeField]
        private TMP_InputField roomCodeInput;

        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button profileButton;
        [SerializeField] private UIProfilePanel profilePanel;

        public override void Initialize()
        {
            base.Initialize();

            this.networkManager = NetworkManager.Instance;
            if (this.networkManager != null)
            {
                this.networkManager.OnConnected += this.OnConnected;
                this.networkManager.OnDisconnected += this.OnDisconnected;
                this.networkManager.OnError += this.OnError;
                this.networkManager.OnMessageReceived += this.OnMessageReceived;
            }

            this.createRoomButton.onClick.AddListener(this.HandleCreateRoom);
            this.joinRoomButton.onClick.AddListener(this.HandleJoinRoom);

            if (this.profileButton != null)
            {
                this.profileButton.onClick.AddListener(this.HandleProfileButton);
            }

            if (this.profilePanel != null)
            {
                this.profilePanel.OnEditProfileClicked += this.HandleEditProfile;
                this.profilePanel.OnLogoutClicked += this.HandleLogout;
                this.profilePanel.OnMatchHistoryClicked += this.HandleMatchHistory;
            }
        }


        private void OnConnected()
        {
            Toast.instance?.ShowMessage("Connected to server");
        }

        private void OnDisconnected()
        {
            Toast.instance?.ShowMessage("Disconnected from server");
        }

        private void OnError(string error)
        {
            Toast.instance?.ShowMessage($"Error: {error}", 3f);
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            switch (message.Type)
            {
                case "ROOM_CREATED":
                    // Play room created sound
                    AudioManager.Instance.PlayRoomCreated();
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, true);
                    break;

                case "ROOM_JOINED":
                    // Play room joined sound
                    AudioManager.Instance.PlayRoomCreated();
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, false);
                    break;
            }
        }

        private void OnDestroy()
        {
            this.createRoomButton.onClick.RemoveListener(this.HandleCreateRoom);
            this.joinRoomButton.onClick.RemoveListener(this.HandleJoinRoom);

            if (this.profileButton != null)
            {
                this.profileButton.onClick.RemoveListener(this.HandleProfileButton);
            }

            if (this.profilePanel != null)
            {
                this.profilePanel.OnEditProfileClicked -= this.HandleEditProfile;
                this.profilePanel.OnLogoutClicked -= this.HandleLogout;
                this.profilePanel.OnMatchHistoryClicked -= this.HandleMatchHistory;
            }

            if (this.networkManager != null)
            {
                this.networkManager.OnConnected -= this.OnConnected;
                this.networkManager.OnDisconnected -= this.OnDisconnected;
                this.networkManager.OnError -= this.OnError;
                this.networkManager.OnMessageReceived -= this.OnMessageReceived;
            }
        }

        private string GetCurrentUsername()
        {
            return PlayerPrefs.GetString("username", "");
        }

        private void HandleCreateRoom()
        {
            var username = this.GetCurrentUsername();

            if (string.IsNullOrEmpty(username))
            {
                Toast.instance?.ShowMessage("User not logged in");
                return;
            }

            // Show category selection screen with callback
            UIScreenController.Instance.Show(
                UIScreenController.CategoriesScreenId,
                false,
                true,
                false,
                Tween.TweenStyle.EaseOut,
                null,
                (Action<string>)this.CreateRoomWithCategory
            );
        }

        private async void CreateRoomWithCategory(string category)
        {
            var username = this.GetCurrentUsername();

            if (this.networkManager != null)
            {
                await this.networkManager.CreateRoom(username, category);
            }
        }

        private async void HandleJoinRoom()
        {
            var username = this.GetCurrentUsername();
            var roomCode = this.roomCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(username))
            {
                Toast.instance?.ShowMessage("User not logged in");
                return;
            }

            if (string.IsNullOrEmpty(roomCode))
            {
                Toast.instance?.ShowMessage("Please enter room code");
                return;
            }

            if (this.networkManager != null)
            {
                await this.networkManager.JoinRoom(roomCode, username);
            }
        }


        public void ResetInputs()
        {
            this.roomCodeInput.text = "";
        }

        private void HandleProfileButton()
        {
            if (this.profilePanel != null)
            {
                this.profilePanel.Show();
            }
        }

        private void HandleEditProfile()
        {
            // TODO: Show Edit Profile screen
            Toast.instance?.ShowMessage("Edit Profile - Coming soon");
        }

        private async void HandleLogout()
        {
            // Send logout message to server to invalidate session
            if (this.networkManager != null)
            {
                await this.networkManager.Logout();
            }

            // Clear stored credentials
            PlayerPrefs.DeleteKey("auth_token");
            PlayerPrefs.DeleteKey("user_id");
            PlayerPrefs.DeleteKey("username");
            PlayerPrefs.DeleteKey("avatar_url");
            PlayerPrefs.Save();

            // Note: Connection stays alive for faster re-login
            // Server invalidates session token but keeps connection

            // Go back to login screen
            UIScreenController.Instance.Show(UIScreenController.LoginScreenId, true);

            Toast.instance?.ShowMessage("Logged out successfully");
        }

        private void HandleMatchHistory()
        {
            UIScreenController.Instance.Show(UIScreenController.MatchHistoryScreenId);
        }
    }
}