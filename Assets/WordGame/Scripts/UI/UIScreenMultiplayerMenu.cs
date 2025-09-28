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
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;

        public override void Initialize()
        {
            base.Initialize();

            networkManager = NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.OnConnected += OnConnected;
                networkManager.OnDisconnected += OnDisconnected;
                networkManager.OnError += OnError;
                networkManager.OnMessageReceived += OnMessageReceived;

                // Start connection
                ConnectToServer();
            }

            createRoomButton.onClick.AddListener(HandleCreateRoom);
            joinRoomButton.onClick.AddListener(HandleJoinRoom);

            // Populate category dropdown
            PopulateCategoryDropdown();
        }

        private async void ConnectToServer()
        {
            if (networkManager != null)
            {
                var connected = await networkManager.ConnectAsync();
                if (!connected)
                {
                    SetStatus("Failed to connect to server");
                }
            }
        }

        private void OnConnected()
        {
            SetStatus("Connected to server");
        }

        private void OnDisconnected()
        {
            SetStatus("Disconnected from server");
        }

        private void OnError(string error)
        {
            SetStatus($"Error: {error}");
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            switch (message.Type)
            {
                case NetworkMessageType.ROOM_CREATED:
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false, Tween.TweenStyle.EaseOut, null, true);
                    break;

                case NetworkMessageType.ROOM_JOINED:
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false, Tween.TweenStyle.EaseOut, null, false);
                    break;
            }
        }

        private void OnDestroy()
        {
            createRoomButton.onClick.RemoveListener(HandleCreateRoom);
            joinRoomButton.onClick.RemoveListener(HandleJoinRoom);

            if (networkManager != null)
            {
                networkManager.OnConnected -= OnConnected;
                networkManager.OnDisconnected -= OnDisconnected;
                networkManager.OnError -= OnError;
                networkManager.OnMessageReceived -= OnMessageReceived;
            }
        }

        private async void HandleCreateRoom()
        {
            var username = usernameInput.text.Trim();
            var category = categoryDropdown != null && categoryDropdown.options.Count > 0
                ? categoryDropdown.options[categoryDropdown.value].text
                : "ANIMALS";

            if (string.IsNullOrEmpty(username))
            {
                SetStatus("Please enter username");
                return;
            }

            if (networkManager != null)
            {
                await networkManager.CreateRoom(username, category);
            }
        }

        private async void HandleJoinRoom()
        {
            var username = usernameInput.text.Trim();
            var roomCode = roomCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roomCode))
            {
                SetStatus("Please enter username and room code");
                return;
            }

            if (networkManager != null)
            {
                await networkManager.JoinRoom(roomCode, username);
            }
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }


        public void ResetInputs()
        {
            usernameInput.text = "";
            roomCodeInput.text = "";
            if (categoryDropdown != null && categoryDropdown.options.Count > 0)
            {
                categoryDropdown.value = 0;
            }
        }

        private void PopulateCategoryDropdown()
        {
            if (categoryDropdown == null) return;

            categoryDropdown.ClearOptions();

            // Get categories from GameManager if available
            if (GameManager.Instance != null)
            {
                var categories = new List<string>();
                foreach (var categoryInfo in GameManager.Instance.CategoryInfos)
                {
                    if (categoryInfo.name != GameManager.dailyPuzzleId)
                    {
                        categories.Add(categoryInfo.name.ToUpper());
                    }
                }
                categoryDropdown.AddOptions(categories);
            }
            else
            {
                // Default categories if GameManager not available
                var defaultCategories = new List<string> { "ANIMALS", "FOOD", "SPORTS", "SCIENCE" };
                categoryDropdown.AddOptions(defaultCategories);
            }
        }
    }
}