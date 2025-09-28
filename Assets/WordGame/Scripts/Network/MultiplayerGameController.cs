using System;
using System.Collections.Generic;
using UnityEngine;
using WordGame.Network.Models;
using WordGame.UI;

namespace WordGame.Network
{
    public class MultiplayerGameController : MonoBehaviour
    {

        private NetworkManager _networkManager;
        private bool _isHost;

        private async void Start()
        {
            _networkManager = NetworkManager.Instance;

            if (_networkManager == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }

            InitializeNetworkEvents();
            InitializeUIEvents();

            ShowMenu();

            var connected = await _networkManager.ConnectAsync();
            if (!connected)
            {
                var menuScreen = GetScreen<UIScreenMultiplayerMenu>(UIScreenController.MultiplayerMenuScreenId);
                if (menuScreen != null)
                {
                    menuScreen.SetStatus("Failed to connect to server");
                }
            }
        }

        private void InitializeNetworkEvents()
        {
            _networkManager.OnConnected += OnConnected;
            _networkManager.OnDisconnected += OnDisconnected;
            _networkManager.OnMessageReceived += OnMessageReceived;
            _networkManager.OnError += OnError;
        }

        private void InitializeUIEvents()
        {
            // Get screen references
            var menuScreen = GetScreen<UIScreenMultiplayerMenu>(UIScreenController.MultiplayerMenuScreenId);
            var roomScreen = GetScreen<UIScreenMultiplayerRoom>(UIScreenController.MultiplayerRoomScreenId);

            // Menu Screen Events
            if (menuScreen != null)
            {
                menuScreen.OnCreateRoomRequest += async (username, topic) =>
                {
                    await _networkManager.CreateRoom(username, topic);
                };

                menuScreen.OnJoinRoomRequest += async (username, roomCode) =>
                {
                    await _networkManager.JoinRoom(roomCode, username);
                };
            }

            // Room Screen Events
            if (roomScreen != null)
            {
                roomScreen.OnReadyPressed += async () =>
                {
                    await _networkManager.SetReady();
                    roomScreen.SetReadyButtonInteractable(false);
                };

                roomScreen.OnStartGamePressed += async () =>
                {
                    if (_isHost)
                    {
                        await _networkManager.StartGame();
                    }
                };

                roomScreen.OnLeaveRoomPressed += async () =>
                {
                    await _networkManager.LeaveRoom();
                    ShowMenu();
                };
            }
        }

        private void OnConnected()
        {
            var menuScreen = GetScreen<UIScreenMultiplayerMenu>(UIScreenController.MultiplayerMenuScreenId);
            if (menuScreen != null)
            {
                menuScreen.SetStatus("Connected to server");
            }
        }

        private void OnDisconnected()
        {
            var menuScreen = GetScreen<UIScreenMultiplayerMenu>(UIScreenController.MultiplayerMenuScreenId);
            if (menuScreen != null)
            {
                menuScreen.SetStatus("Disconnected from server");
            }
            ShowMenu();
        }

        private void OnError(string error)
        {
            var menuScreen = GetScreen<UIScreenMultiplayerMenu>(UIScreenController.MultiplayerMenuScreenId);
            if (menuScreen != null)
            {
                menuScreen.SetStatus($"Error: {error}");
            }
            Debug.LogError($"Network Error: {error}");
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            var messageType = ParseMessageType(message.Type);

            switch (messageType)
            {
                case NetworkMessageType.ROOM_CREATED:
                    HandleRoomCreated();
                    break;

                case NetworkMessageType.ROOM_JOINED:
                    HandleRoomJoined();
                    break;

                case NetworkMessageType.PLAYER_JOINED:
                case NetworkMessageType.PLAYER_LEFT:
                case NetworkMessageType.PLAYER_READY:
                    UpdatePlayerList();
                    break;

                case NetworkMessageType.GAME_STARTED:
                    HandleGameStarted(message.Data);
                    break;

                case NetworkMessageType.NEXT_LEVEL:
                    HandleNextLevel(message.Data);
                    break;

                case NetworkMessageType.ANSWER_RESULT:
                    HandleAnswerResult(message.Data);
                    break;

                case NetworkMessageType.GAME_ENDED:
                    HandleGameEnded(message.Data);
                    break;
            }
        }

        private NetworkMessageType ParseMessageType(string type)
        {
            if (Enum.TryParse<NetworkMessageType>(type, out var result))
            {
                return result;
            }
            Debug.LogWarning($"Unknown message type: {type}");
            return default;
        }

        private void HandleRoomCreated()
        {
            _isHost = true;
            ShowRoom();
            var roomScreen = GetScreen<UIScreenMultiplayerRoom>(UIScreenController.MultiplayerRoomScreenId);
            if (roomScreen != null)
            {
                roomScreen.InitializeRoom(_networkManager.RoomCode, _isHost);
            }
            UpdatePlayerList();
        }

        private void HandleRoomJoined()
        {
            _isHost = false;
            ShowRoom();
            var roomScreen = GetScreen<UIScreenMultiplayerRoom>(UIScreenController.MultiplayerRoomScreenId);
            if (roomScreen != null)
            {
                roomScreen.InitializeRoom(_networkManager.RoomCode, _isHost);
            }
            UpdatePlayerList();
        }

        private void UpdatePlayerList()
        {
            var roomScreen = GetScreen<UIScreenMultiplayerRoom>(UIScreenController.MultiplayerRoomScreenId);
            if (roomScreen != null)
            {
                roomScreen.UpdatePlayerList(_networkManager.RoomPlayers);
            }
        }

        private void HandleGameStarted(string data)
        {
            var gameData = JsonUtility.FromJson<GameStartData>(data);
            ShowGame();
            var gameScreen = GetScreen<UIScreenGame>(UIScreenController.GameScreenId);
            if (gameScreen != null)
            {
                gameScreen.StartMultiplayerLevel(gameData);
            }
        }

        private void HandleNextLevel(string data)
        {
            var gameData = JsonUtility.FromJson<GameStartData>(data);
            var gameScreen = GetScreen<UIScreenGame>(UIScreenController.GameScreenId);
            if (gameScreen != null)
            {
                gameScreen.StartMultiplayerLevel(gameData);
            }
        }

        private void HandleAnswerResult(string data)
        {
            var result = JsonUtility.FromJson<AnswerResultData>(data);

            // The game screen will handle level completion through GameManager
            // This is just for tracking answer results
            Debug.Log($"Answer result for {result.playerId}: {result.isCorrect}, Score: {result.score}");
        }

        private void HandleGameEnded(string data)
        {
            var endData = JsonUtility.FromJson<GameEndData>(data);

            // Reset game screen multiplayer state
            var gameScreen = GetScreen<UIScreenGame>(UIScreenController.GameScreenId);
            if (gameScreen != null)
            {
                gameScreen.ResetMultiplayer();
            }

            // Show leaderboard
            ShowLeaderboard(endData.results);
        }


        private void ShowLeaderboard(List<PlayerResult> results)
        {
            UIScreenController.Instance.Show(UIScreenController.LeaderboardScreenId, false, true, false, Tween.TweenStyle.EaseOut, null, results);
        }

        private void ShowMenu()
        {
            UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, false, true);

            var gameScreen = GetScreen<UIScreenGame>(UIScreenController.GameScreenId);
            if (gameScreen != null)
            {
                gameScreen.ResetMultiplayer();
            }
        }

        private void ShowRoom()
        {
            UIScreenController.Instance.Show(UIScreenController.MultiplayerRoomScreenId, false, true);
        }

        private void ShowGame()
        {
            UIScreenController.Instance.Show(UIScreenController.GameScreenId, false, true);
        }

        private T GetScreen<T>(string screenId) where T : UIScreen
        {
            var screens = FindObjectsOfType<UIScreen>();
            foreach (var screen in screens)
            {
                if (screen.id == screenId && screen is T)
                {
                    return screen as T;
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnConnected -= OnConnected;
                _networkManager.OnDisconnected -= OnDisconnected;
                _networkManager.OnMessageReceived -= OnMessageReceived;
                _networkManager.OnError -= OnError;
            }
        }
    }
}