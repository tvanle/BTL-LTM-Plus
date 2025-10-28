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
        private TMP_InputField usernameInput;

        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;

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
            }

            createRoomButton.onClick.AddListener(HandleCreateRoom);
            joinRoomButton.onClick.AddListener(HandleJoinRoom);

            // Populate category dropdown
            PopulateCategoryDropdown();
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
                Toast.instance?.ShowMessage("Please enter username");
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
                Toast.instance?.ShowMessage("Please enter username and room code");
                return;
            }

            if (networkManager != null)
            {
                await networkManager.JoinRoom(roomCode, username);
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
            var categories = new List<string>();
            foreach (var categoryInfo in GameManager.Instance.CategoryInfos)
            {
                if (categoryInfo.name != GameManager.dailyPuzzleId)
                {
                    categories.Add(categoryInfo.name);
                }
            }

            categoryDropdown.AddOptions(categories);
        }
    }
}