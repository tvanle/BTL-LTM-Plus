using System;
using UnityEngine;
using WordGame.Network.Models;
using WordGame.UI;

namespace WordGame.Network
{
    public class MultiplayerGameController : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private UIScreenMultiplayerMenu menuScreen;
        [SerializeField] private UIScreenMultiplayerRoom roomScreen;
        [SerializeField] private UIScreenMultiplayerGame gameScreen;

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
                menuScreen.SetStatus("Failed to connect to server");
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
            // Menu Screen Events
            menuScreen.OnCreateRoomRequest += async (username, topic) =>
            {
                await _networkManager.CreateRoom(username, topic);
            };

            menuScreen.OnJoinRoomRequest += async (username, roomCode) =>
            {
                await _networkManager.JoinRoom(roomCode, username);
            };

            // Room Screen Events
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

            // Game Screen Events
            gameScreen.OnAnswerSubmit += async (answer, timeTaken) =>
            {
                await _networkManager.SubmitAnswer(answer, timeTaken);
            };
        }

        private void OnConnected()
        {
            menuScreen.SetStatus("Connected to server");
        }

        private void OnDisconnected()
        {
            menuScreen.SetStatus("Disconnected from server");
            ShowMenu();
        }

        private void OnError(string error)
        {
            menuScreen.SetStatus($"Error: {error}");
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
            roomScreen.Initialize(_networkManager.RoomCode, _isHost);
            UpdatePlayerList();
        }

        private void HandleRoomJoined()
        {
            _isHost = false;
            ShowRoom();
            roomScreen.Initialize(_networkManager.RoomCode, _isHost);
            UpdatePlayerList();
        }

        private void UpdatePlayerList()
        {
            roomScreen.UpdatePlayerList(_networkManager.RoomPlayers);
        }

        private void HandleGameStarted(string data)
        {
            var gameData = JsonUtility.FromJson<GameStartData>(data);
            ShowGame();
            gameScreen.StartLevel(gameData);
        }

        private void HandleNextLevel(string data)
        {
            var gameData = JsonUtility.FromJson<GameStartData>(data);
            gameScreen.UpdateLevel(gameData);
        }

        private void HandleAnswerResult(string data)
        {
            var result = JsonUtility.FromJson<AnswerResultData>(data);

            if (result.playerId == _networkManager.PlayerId)
            {
                gameScreen.UpdateScore(result.score);
                gameScreen.ShowAnswerResult(result.isCorrect);
            }
        }

        private void HandleGameEnded(string data)
        {
            var endData = JsonUtility.FromJson<GameEndData>(data);

            gameScreen.StopGame();
            ShowRoom();
            roomScreen.Reset();

            // Show results
            var resultsText = "Game Over!\n\nResults:\n";
            foreach (var result in endData.results)
            {
                resultsText += $"{result.Username}: {result.Score}\n";
            }
            roomScreen.ShowResults(resultsText);
        }

        private void ShowMenu()
        {
            menuScreen.Show();
            roomScreen.Hide();
            gameScreen.Hide();
        }

        private void ShowRoom()
        {
            menuScreen.Hide();
            roomScreen.Show();
            gameScreen.Hide();
        }

        private void ShowGame()
        {
            menuScreen.Hide();
            roomScreen.Hide();
            gameScreen.Show();
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