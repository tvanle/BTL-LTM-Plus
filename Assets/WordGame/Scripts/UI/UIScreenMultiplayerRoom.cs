using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;

namespace WordGame.UI
{
    public class UIScreenMultiplayerRoom : UIScreen
    {
        private NetworkManager networkManager;
        [Header("UI References")]
        [SerializeField] private Text roomCodeText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private Text statusText;

        private Dictionary<string, GameObject> _playerListItems = new Dictionary<string, GameObject>();
        private bool _isHost;

        public override void Initialize()
        {
            base.Initialize();

            networkManager = NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.OnMessageReceived += OnMessageReceived;
            }

            readyButton.onClick.AddListener(HandleReady);
            startGameButton.onClick.AddListener(HandleStartGame);
            leaveRoomButton.onClick.AddListener(HandleLeaveRoom);
        }

        public override void OnShowing(object data)
        {
            base.OnShowing(data);

            if (data is bool isHost)
            {
                _isHost = isHost;
                if (networkManager != null)
                {
                    InitializeRoom(networkManager.RoomCode, _isHost);
                    UpdatePlayerList(networkManager.RoomPlayers);
                }
            }
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            switch (message.Type)
            {
                case "PLAYER_JOINED":
                case "PLAYER_LEFT":
                case "PLAYER_READY":
                    if (networkManager != null)
                    {
                        UpdatePlayerList(networkManager.RoomPlayers);
                    }
                    break;

                case "GAME_STARTED":
                    var gameData = JsonUtility.FromJson<GameStartData>(message.Data);
                    UIScreenController.Instance.Show(UIScreenController.GameScreenId, false, true, false, Tween.TweenStyle.EaseOut, null, gameData);
                    break;

                case "GAME_ENDED":
                    var endData = JsonUtility.FromJson<GameEndData>(message.Data);
                    Reset();
                    UIScreenController.Instance.Show(UIScreenController.LeaderboardScreenId, false, true, false, Tween.TweenStyle.EaseOut, null, endData.results);
                    break;
            }
        }

        private async void HandleReady()
        {
            if (networkManager != null)
            {
                await networkManager.SetReady();
                SetReadyButtonInteractable(false);
            }
        }

        private async void HandleStartGame()
        {
            if (_isHost && networkManager != null)
            {
                await networkManager.StartGame();
            }
        }

        private async void HandleLeaveRoom()
        {
            if (networkManager != null)
            {
                await networkManager.LeaveRoom();
            }
            UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, true, true);
        }

        private void OnDestroy()
        {
            readyButton.onClick.RemoveAllListeners();
            startGameButton.onClick.RemoveAllListeners();
            leaveRoomButton.onClick.RemoveAllListeners();

            if (networkManager != null)
            {
                networkManager.OnMessageReceived -= OnMessageReceived;
            }
        }

        public void InitializeRoom(string roomCode, bool isHost)
        {
            _isHost = isHost;
            roomCodeText.text = $"Room Code: {roomCode}";
            startGameButton.gameObject.SetActive(isHost);
            readyButton.interactable = true;
        }

        public void UpdatePlayerList(List<NetworkManager.PlayerInfo> players)
        {
            foreach (var kvp in _playerListItems)
            {
                Destroy(kvp.Value);
            }
            _playerListItems.Clear();

            foreach (var player in players)
            {
                var item = Instantiate(playerListItemPrefab, playerListContainer);
                var text = item.GetComponentInChildren<Text>();
                text.text = $"{player.Username} {(player.IsReady ? "✓" : "")}";
                _playerListItems[player.Id] = item;
            }
        }

        public void SetReadyButtonInteractable(bool interactable)
        {
            readyButton.interactable = interactable;
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void ShowResults(string resultsText)
        {
            SetStatus(resultsText);
        }


        public void Reset()
        {
            readyButton.interactable = true;
            SetStatus("");
        }
    }
}