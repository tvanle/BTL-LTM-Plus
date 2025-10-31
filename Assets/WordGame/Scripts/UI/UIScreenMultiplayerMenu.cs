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

        [Header("UI References")]
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;

        public override void Initialize()
        {
            base.Initialize();

            this.networkManager = NetworkManager.Instance;
            if (this.networkManager != null)
            {
                this.networkManager.OnConnected       += this.OnConnected;
                this.networkManager.OnDisconnected    += this.OnDisconnected;
                this.networkManager.OnError           += this.OnError;
                this.networkManager.OnMessageReceived += this.OnMessageReceived;
            }

            this.createRoomButton.onClick.AddListener(this.HandleCreateRoom);
            this.joinRoomButton.onClick.AddListener(this.HandleJoinRoom);

            // Populate category dropdown
            this.PopulateCategoryDropdown();
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
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, true);
                    break;

                case "ROOM_JOINED":
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, false);
                    break;
            }
        }

        private void OnDestroy()
        {
            this.createRoomButton.onClick.RemoveListener(this.HandleCreateRoom);
            this.joinRoomButton.onClick.RemoveListener(this.HandleJoinRoom);

            if (this.networkManager != null)
            {
                this.networkManager.OnConnected       -= this.OnConnected;
                this.networkManager.OnDisconnected    -= this.OnDisconnected;
                this.networkManager.OnError           -= this.OnError;
                this.networkManager.OnMessageReceived -= this.OnMessageReceived;
            }
        }

        private string GetCurrentUsername()
        {
            return PlayerPrefs.GetString("username", "");
        }

        private async void HandleCreateRoom()
        {
            var username = this.GetCurrentUsername();
            var category = this.categoryDropdown != null && this.categoryDropdown.options.Count > 0
                ? this.categoryDropdown.options[this.categoryDropdown.value].text
                : "ANIMALS";

            if (string.IsNullOrEmpty(username))
            {
                Toast.instance?.ShowMessage("User not logged in");
                return;
            }

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
            if (this.categoryDropdown != null && this.categoryDropdown.options.Count > 0)
            {
                this.categoryDropdown.value = 0;
            }
        }

        private void PopulateCategoryDropdown()
        {
            if (this.categoryDropdown == null) return;

            this.categoryDropdown.ClearOptions();

            // Get categories from GameManager if available
            var categories = new List<string>();
            foreach (var categoryInfo in GameManager.Instance.CategoryInfos)
            {
                if (categoryInfo.name != GameManager.dailyPuzzleId)
                {
                    categories.Add(categoryInfo.name);
                }
            }

            this.categoryDropdown.AddOptions(categories);
        }
    }
}