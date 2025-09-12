using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordGame.Network
{
    public class MultiplayerGameController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private GameObject gamePanel;

        [Header("Menu UI")]
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField topicInput;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;

        [Header("Room UI")]
        [SerializeField] private Text roomCodeText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;

        [Header("Game UI")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text timerText;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Transform targetWordsContainer;
        [SerializeField] private InputField answerInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text scoreText;

        private NetworkManager _networkManager;
        private bool _isHost;
        private int _currentLevel;
        private float _levelTimer;
        private bool _isPlaying;
        private Dictionary<string, GameObject> _playerListItems = new Dictionary<string, GameObject>();

        private async void Start()
        {
            this._networkManager = NetworkManager.Instance;

            if (this._networkManager == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }

            this._networkManager.OnConnected       += this.OnConnected;
            this._networkManager.OnDisconnected    += this.OnDisconnected;
            this._networkManager.OnMessageReceived += this.OnMessageReceived;
            this._networkManager.OnError           += this.OnError;

            this.createRoomButton.onClick.AddListener(this.CreateRoom);
            this.joinRoomButton.onClick.AddListener(this.JoinRoom);
            this.readyButton.onClick.AddListener(this.SetReady);
            this.startGameButton.onClick.AddListener(this.StartGame);
            this.leaveRoomButton.onClick.AddListener(this.LeaveRoom);
            this.submitButton.onClick.AddListener(this.SubmitAnswer);

            this.ShowMenu();

            var connected = await this._networkManager.ConnectAsync();
            if (!connected)
            {
                this.statusText.text = "Failed to connect to server";
            }
        }

        private void OnConnected()
        {
            this.statusText.text = "Connected to server";
        }

        private void OnDisconnected()
        {
            this.statusText.text = "Disconnected from server";
            this.ShowMenu();
        }

        private void OnError(string error)
        {
            this.statusText.text = $"Error: {error}";
            Debug.LogError($"Network Error: {error}");
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            switch (message.Type)
            {
                case "ROOM_CREATED":
                    this._isHost = true;
                    this.ShowRoom();
                    this.UpdateRoomUI();
                    break;

                case "ROOM_JOINED":
                    this._isHost = false;
                    this.ShowRoom();
                    this.UpdateRoomUI();
                    break;

                case "PLAYER_JOINED":
                case "PLAYER_LEFT":
                case "PLAYER_READY":
                    this.UpdatePlayerList();
                    break;

                case "GAME_STARTED":
                    this.HandleGameStarted(message.Data);
                    break;

                case "NEXT_LEVEL":
                    this.HandleNextLevel(message.Data);
                    break;

                case "ANSWER_RESULT":
                    this.HandleAnswerResult(message.Data);
                    break;

                case "GAME_ENDED":
                    this.HandleGameEnded(message.Data);
                    break;
            }
        }

        private async void CreateRoom()
        {
            var username = this.usernameInput.text.Trim();
            var topic    = this.topicInput.text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(topic))
            {
                this.statusText.text = "Please enter username and topic";
                return;
            }

            await this._networkManager.CreateRoom(username, topic);
        }

        private async void JoinRoom()
        {
            var username = this.usernameInput.text.Trim();
            var roomCode = this.roomCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roomCode))
            {
                this.statusText.text = "Please enter username and room code";
                return;
            }

            await this._networkManager.JoinRoom(roomCode, username);
        }

        private async void SetReady()
        {
            await this._networkManager.SetReady();
            this.readyButton.interactable = false;
        }

        private async void StartGame()
        {
            if (this._isHost)
            {
                await this._networkManager.StartGame();
            }
        }

        private async void LeaveRoom()
        {
            await this._networkManager.LeaveRoom();
            this.ShowMenu();
        }

        private async void SubmitAnswer()
        {
            var answer = this.answerInput.text.Trim();
            if (string.IsNullOrEmpty(answer))
                return;

            var timeTaken = Mathf.RoundToInt(30f - this._levelTimer);
            await this._networkManager.SubmitAnswer(answer, timeTaken);
            this.answerInput.text = "";
        }

        private void ShowMenu()
        {
            this.menuPanel.SetActive(true);
            this.roomPanel.SetActive(false);
            this.gamePanel.SetActive(false);
        }

        private void ShowRoom()
        {
            this.menuPanel.SetActive(false);
            this.roomPanel.SetActive(true);
            this.gamePanel.SetActive(false);
        }

        private void ShowGame()
        {
            this.menuPanel.SetActive(false);
            this.roomPanel.SetActive(false);
            this.gamePanel.SetActive(true);
        }

        private void UpdateRoomUI()
        {
            this.roomCodeText.text = $"Room Code: {this._networkManager.RoomCode}";
            this.startGameButton.gameObject.SetActive(this._isHost);
            this.UpdatePlayerList();
        }

        private void UpdatePlayerList()
        {
            foreach (var kvp in this._playerListItems)
            {
                Destroy(kvp.Value);
            }
            this._playerListItems.Clear();

            foreach (var player in this._networkManager.RoomPlayers)
            {
                var item = Instantiate(this.playerListItemPrefab, this.playerListContainer);
                var text = item.GetComponentInChildren<Text>();
                text.text                     = $"{player.Username} {(player.IsReady ? "✓" : "")}";
                this._playerListItems[player.Id] = item;
            }
        }

        private void HandleGameStarted(string data)
        {
            var gameData = JsonUtility.FromJson<GameStartData>(data);
            this._currentLevel = gameData.level;
            this._levelTimer   = gameData.duration;
            this._isPlaying       = true;

            this.ShowGame();
            this.UpdateGameUI(gameData);
        }

        private void HandleNextLevel(string data)
        {
            var gameData = JsonUtility.FromJson<GameStartData>(data);
            this._currentLevel = gameData.level;
            this._levelTimer      = gameData.duration;

            this.UpdateGameUI(gameData);
        }

        private void HandleAnswerResult(string data)
        {
            var result = JsonUtility.FromJson<AnswerResultData>(data);

            if (result.playerId == this._networkManager.PlayerId)
            {
                this.scoreText.text = $"Score: {result.score}";

                if (result.isCorrect)
                {
                    this.statusText.text = "Correct!";
                }
                else
                {
                    this.statusText.text = "Wrong answer";
                }
            }
        }

        private void HandleGameEnded(string data)
        {
            var endData = JsonUtility.FromJson<GameEndData>(data);
            this._isPlaying = false;

            this.ShowRoom();
            this.readyButton.interactable = true;

            // Show results
            var resultsText = "Game Over!\n\nResults:\n";
            foreach (var result in endData.results)
            {
                resultsText += $"{result.Username}: {result.Score}\n";
            }
            this.statusText.text = resultsText;
        }

        private void UpdateGameUI(GameStartData gameData)
        {
            this.levelText.text = $"Level {gameData.level}";

            // Clear existing grid and words
            foreach (Transform child in this.gridContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in this.targetWordsContainer)
            {
                Destroy(child.gameObject);
            }

            // Display grid (simplified)
            var gridText = this.gridContainer.GetComponent<Text>();
            if (gridText != null)
            {
                gridText.text = gameData.grid;
            }

            // Display target words
            foreach (var word in gameData.words)
            {
                var wordObj = new GameObject(word);
                wordObj.transform.SetParent(this.targetWordsContainer);
                var text = wordObj.AddComponent<Text>();
                text.text = word;
            }
        }

        private void Update()
        {
            if (this._isPlaying && this._levelTimer > 0)
            {
                this._levelTimer -= Time.deltaTime;
                this.timerText.text =  $"Time: {Mathf.RoundToInt(this._levelTimer)}";

                if (this._levelTimer <= 0)
                {
                    this._levelTimer = 0;
                    this.timerText.text = "Time's up!";
                }
            }
        }

        private void OnDestroy()
        {
            if (this._networkManager != null)
            {
                this._networkManager.OnConnected       -= this.OnConnected;
                this._networkManager.OnDisconnected    -= this.OnDisconnected;
                this._networkManager.OnMessageReceived -= this.OnMessageReceived;
                this._networkManager.OnError           -= this.OnError;
            }
        }

        [Serializable]
        private class GameStartData
        {
            public int level;
            public string grid;
            public List<string> words;
            public int duration;
        }

        [Serializable]
        private class AnswerResultData
        {
            public string playerId;
            public string answer;
            public bool isCorrect;
            public int score;
        }

        [Serializable]
        private class GameEndData
        {
            public List<PlayerResult> results;
        }

        [Serializable]
        private class PlayerResult
        {
            public string Id;
            public string Username;
            public int Score;
        }
    }
}