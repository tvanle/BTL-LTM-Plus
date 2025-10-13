using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;

namespace WordGame.UI
{
    public class UIScreenMultiplayerRoom : UIScreen
    {
        private NetworkManager networkManager;

        [Header("UI References")] [SerializeField]
        private TextMeshProUGUI roomCodeText;

        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;

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
                    if (networkManager != null)
                    {
                        UpdatePlayerList(networkManager.RoomPlayers);
                    }

                    break;

                case "GAME_STARTED":
                    try
                    {
                        // Parse game start data from server
                        var gameData = JsonUtility.FromJson<GameStartData>(message.Data);

                        // Default category if not provided - use "Category 1" (with space) format
                        string category = !string.IsNullOrEmpty(gameData.category) ? gameData.category : "Category 1";
                        int level = gameData.level > 0
                            ? gameData.level - 1
                            : 0; // Convert to 0-based index (level 1 = index 0)

                        Debug.Log($"[MULTIPLAYER] Starting level - Category: {category}, Level: {level + 1}");
                        GameManager.Instance.StartLevel(category, level);

                        // Show game screen - pass GameStartData so UIScreenGame knows it's multiplayer
                        UIScreenController.Instance.Show(UIScreenController.GameScreenId, false, true, false,
                            Tween.TweenStyle.EaseOut, null, gameData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MULTIPLAYER] Error handling GAME_STARTED: {ex.Message}\n{ex.StackTrace}");
                    }

                    break;

                case "NEXT_LEVEL":
                    // Handle next level in multiplayer
                    var nextLevelData = JsonUtility.FromJson<GameStartData>(message.Data);

                    if (GameManager.Instance != null && nextLevelData != null)
                    {
                        string category = !string.IsNullOrEmpty(nextLevelData.category)
                            ? nextLevelData.category
                            : "Category 1";
                        int level = nextLevelData.level > 0 ? nextLevelData.level - 1 : 0; // Convert to 0-based index

                        Debug.Log($"[MULTIPLAYER] Next level - Category: {category}, Level: {level}");

                        // IMPORTANT: Hide all overlays instantly (no animation to avoid glitches)
                        UIScreenController.Instance.HideOverlayInstant(UIScreenController.CompleteScreenId);
                        UIScreenController.Instance.HideOverlayInstant(UIScreenController.LeaderboardScreenId);

                        // Start next level
                        GameManager.Instance.StartLevel(category, level);
                        UIScreenController.Instance.Show(UIScreenController.GameScreenId, false, true, false,
                            Tween.TweenStyle.EaseOut, null, nextLevelData);
                    }

                    break;

                case "LEVEL_ENDED":
                    var levelEndData = JsonUtility.FromJson<LevelEndData>(message.Data);
                    UIScreenController.Instance.Show(UIScreenController.LeaderboardScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, levelEndData);
                    break;

                case "GAME_ENDED":
                    var endData = JsonUtility.FromJson<GameEndData>(message.Data);
                    Reset();
                    // Return to room after game ends
                    UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, false);
                    break;
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
            roomCodeText.text = $"{roomCode}";
            startGameButton.gameObject.SetActive(isHost);
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
                item.SetActive(true);

                // Find PlayerName child
                var playerNameTransform = item.transform.Find("PlayerName");
                if (playerNameTransform != null)
                {
                    var nameText = playerNameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = player.Username;
                    }
                }

                _playerListItems[player.Id] = item;
            }
        }

        public void Reset()
        {
            // Reset any room state if needed
        }
    }
}