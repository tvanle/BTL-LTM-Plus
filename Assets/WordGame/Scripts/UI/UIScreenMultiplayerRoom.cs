using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;
using WordGame.Network.Models;
using WordGame.Utilities;

namespace WordGame.UI
{
    public class UIScreenMultiplayerRoom : UIScreen
    {
        private NetworkManager networkManager;

        [Header("UI References")] [SerializeField]
        private TextMeshProUGUI roomCodeText;

        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject roomPlayerListItemPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;

        [Header("Online Players Panel")]
        [SerializeField] private GameObject onlinePlayersPanel;
        [SerializeField] private Button toggleOnlinePlayersButton;
        [SerializeField] private Transform onlinePlayersContainer;
        [SerializeField] private GameObject onlinePlayerListItemPrefab;
        [SerializeField] private GameObject invitationPopupPrefab;
        [SerializeField] private Canvas mainCanvas;

        [Header("Room Info Display")] [SerializeField]
        private Image categoryIconImage;

        [SerializeField] private TextMeshProUGUI categoryNameText;
        [SerializeField] private TextMeshProUGUI categoryNoteText;
        [SerializeField] private TextMeshProUGUI numPlayersText;
        [SerializeField] private TextMeshProUGUI numQuestionsText;

        [Header("Category Icons (Optional - Assign in Inspector)")] [SerializeField]
        private Sprite[] categoryIconSprites = new Sprite[15];

        private Dictionary<string, GameObject> _playerListItems = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _onlinePlayerListItems = new Dictionary<string, GameObject>();
        private bool _isHost;
        private Dictionary<string, Sprite> _categoryIcons;
        private bool _isOnlinePlayersPanelOpen;

        // Random notes for categories
        private readonly string[] _categoryNotes = new[]
        {
            "Find all the hidden words!",
            "Challenge your vocabulary",
            "How many words can you find?",
            "Test your word skills",
            "Discover the words within",
            "Unleash your inner wordsmith",
            "Time to prove your vocabulary",
            "Let the word hunt begin!",
            "Words are waiting to be found",
            "Ready to solve the puzzle?"
        };

        public override void Initialize()
        {
            base.Initialize();

            this.networkManager = NetworkManager.Instance;
            if (this.networkManager != null)
            {
                this.networkManager.OnMessageReceived += this.OnMessageReceived;
                this.networkManager.OnOnlinePlayersReceived += this.OnOnlinePlayersReceived;
                this.networkManager.OnRoomInviteReceived += this.OnRoomInviteReceived;
            }

            this.startGameButton.onClick.AddListener(this.HandleStartGame);
            this.leaveRoomButton.onClick.AddListener(this.HandleLeaveRoom);

            if (this.toggleOnlinePlayersButton != null)
            {
                this.toggleOnlinePlayersButton.onClick.AddListener(this.ToggleOnlinePlayersPanel);
            }

            if (this.onlinePlayersPanel != null)
            {
                this.onlinePlayersPanel.SetActive(false);
                this._isOnlinePlayersPanelOpen = false;
            }

            this.LoadCategoryIcons();
        }

        private void LoadCategoryIcons()
        {
            this._categoryIcons = new Dictionary<string, Sprite>();

            // Option 1: Try loading from Resources first
            var sprites = Resources.LoadAll<Sprite>("Sprites/CategoryIcons");

            if (sprites != null && sprites.Length > 0)
            {
                // Map icon names to category names
                // Format: icon_[animal].png -> Category [number]
                var iconMapping = new Dictionary<string, string>
                {
                    { "icon_duck", "Category 1" },
                    { "icon_elephant", "Category 2" },
                    { "icon_fish", "Category 3" },
                    { "icon_hedgehog", "Category 4" },
                    { "icon_horse", "Category 5" },
                    { "icon_lion", "Category 6" },
                    { "icon_monkey", "Category 7" },
                    { "icon_pangolin", "Category 8" },
                    { "icon_peacock", "Category 9" },
                    { "icon_rabbit", "Category 10" },
                    { "icon_rhino", "Category 11" },
                    { "icon_sheep", "Category 12" },
                    { "icon_snail", "Category 13" },
                    { "icon_snake", "Category 14" },
                    { "icon_tiger", "Category 15" }
                };

                foreach (var sprite in sprites)
                {
                    string spriteName = sprite.name;
                    if (iconMapping.ContainsKey(spriteName))
                    {
                        this._categoryIcons[iconMapping[spriteName]] = sprite;
                    }
                }

                Debug.Log(
                    $"[UIScreenMultiplayerRoom] Loaded {this._categoryIcons.Count} category icons from Resources");
            }
            // Option 2: Fallback to Inspector-assigned sprites
            else if (this.categoryIconSprites != null && this.categoryIconSprites.Length >= 15)
            {
                for (int i = 0; i < 15 && i < this.categoryIconSprites.Length; i++)
                {
                    if (this.categoryIconSprites[i] != null)
                    {
                        this._categoryIcons[$"Category {i + 1}"] = this.categoryIconSprites[i];
                    }
                }

                Debug.Log(
                    $"[UIScreenMultiplayerRoom] Loaded {this._categoryIcons.Count} category icons from Inspector");
            }
            else
            {
                Debug.LogWarning(
                    "[UIScreenMultiplayerRoom] No category icons found. Please either:\n" +
                    "1. Create folder 'Assets/WordGame/Resources/Sprites/CategoryIcons' and copy icons there, OR\n" +
                    "2. Assign sprites in Inspector under 'Category Icons' array");
            }
        }

        protected override void OnShowingContent(object data)
        {
            if (data is bool isHost)
            {
                this._isHost = isHost;
                if (this.networkManager != null)
                {
                    this.InitializeRoom(this.networkManager.RoomCode, this._isHost);
                    this.UpdatePlayerList(this.networkManager.RoomPlayers);
                    // Update room info with category and numQuestions from NetworkManager
                    string category = !string.IsNullOrEmpty(this.networkManager.Category)
                        ? this.networkManager.Category
                        : "Category 1";
                    this.UpdateRoomInfo(category, this.networkManager.RoomPlayers?.Count ?? 1,
                        this.networkManager.NumQuestions);

                    // Request online players when showing the room
                    _ = this.networkManager.GetOnlinePlayers();
                }
            }
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            switch (message.Type)
            {
                case "PLAYER_JOINED":
                    // Play sound for player joined
                    AudioManager.Instance.PlayPlayerJoined();
                    if (this.networkManager != null)
                    {
                        this.UpdatePlayerList(this.networkManager.RoomPlayers);
                        // Update player count in room info with category and numQuestions from NetworkManager
                        string category = !string.IsNullOrEmpty(this.networkManager.Category)
                            ? this.networkManager.Category
                            : "Category 1";
                        this.UpdateRoomInfo(category, this.networkManager.RoomPlayers?.Count ?? 1,
                            this.networkManager.NumQuestions);
                    }

                    break;

                case "PLAYER_LEFT":
                    // Play sound for player left
                    AudioManager.Instance.PlayPlayerLeft();
                    if (this.networkManager != null)
                    {
                        this.UpdatePlayerList(this.networkManager.RoomPlayers);
                        // Update player count in room info with category and numQuestions from NetworkManager
                        string category = !string.IsNullOrEmpty(this.networkManager.Category)
                            ? this.networkManager.Category
                            : "Category 1";
                        this.UpdateRoomInfo(category, this.networkManager.RoomPlayers?.Count ?? 1,
                            this.networkManager.NumQuestions);
                    }

                    break;

                case "GAME_STARTED":
                    // Play game started sound
                    AudioManager.Instance.PlayGameStarted();
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
                    Debug.Log($"[MULTIPLAYER_ROOM] Game ended with {endData.results?.Count ?? 0} results");
                    this.Reset();
                    // Show game result screen with final leaderboard and XP
                    UIScreenController.Instance.Show(UIScreenController.GameResultScreenId, false, true, false,
                        Tween.TweenStyle.EaseOut, null, endData);
                    break;
            }
        }

        private async void HandleStartGame()
        {
            if (this._isHost && this.networkManager != null)
            {
                await this.networkManager.StartGame();
            }
        }

        private async void HandleLeaveRoom()
        {
            if (this.networkManager != null)
            {
                await this.networkManager.LeaveRoom();
            }

            UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, true, true);
        }

        private void OnDestroy()
        {
            this.startGameButton.onClick.RemoveAllListeners();
            this.leaveRoomButton.onClick.RemoveAllListeners();

            if (this.toggleOnlinePlayersButton != null)
            {
                this.toggleOnlinePlayersButton.onClick.RemoveAllListeners();
            }

            if (this.networkManager != null)
            {
                this.networkManager.OnMessageReceived -= this.OnMessageReceived;
                this.networkManager.OnOnlinePlayersReceived -= this.OnOnlinePlayersReceived;
                this.networkManager.OnRoomInviteReceived -= this.OnRoomInviteReceived;
            }
        }

        public void InitializeRoom(string roomCode, bool isHost)
        {
            this._isHost = isHost;
            this.roomCodeText.text = $"{roomCode}";
            this.startGameButton.gameObject.SetActive(isHost);
        }

        public void UpdatePlayerList(List<NetworkManager.PlayerInfo> players)
        {
            foreach (var kvp in this._playerListItems)
            {
                Destroy(kvp.Value);
            }

            this._playerListItems.Clear();

            foreach (var player in players)
            {
                if (this.roomPlayerListItemPrefab != null && this.playerListContainer != null)
                {
                    var item = Instantiate(this.roomPlayerListItemPrefab, this.playerListContainer);
                    item.SetActive(true);

                    var roomPlayerListItem = item.GetComponent<RoomPlayerListItem>();
                    if (roomPlayerListItem != null)
                    {
                        roomPlayerListItem.Setup(player.Username, player.AvatarUrl);
                    }

                    this._playerListItems[player.Id] = item;
                }
            }
        }

        public void Reset()
        {
            // Reset any room state if needed
        }

        /// <summary>
        /// Update room information display (category, players, questions)
        /// </summary>
        /// <param name="category">Category name (e.g., "Category 1")</param>
        /// <param name="numPlayers">Current number of players in room</param>
        /// <param name="numQuestions">Number of questions/words in game</param>
        public void UpdateRoomInfo(string category = "Category 1", int numPlayers = 1, int numQuestions = 10)
        {
            // Update category icon
            if (this.categoryIconImage != null && this._categoryIcons != null &&
                this._categoryIcons.ContainsKey(category))
            {
                this.categoryIconImage.sprite = this._categoryIcons[category];
            }

            // Update category name
            if (this.categoryNameText != null)
            {
                this.categoryNameText.text = category;
            }

            // Update random note
            if (this.categoryNoteText != null && this._categoryNotes != null && this._categoryNotes.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, this._categoryNotes.Length);
                this.categoryNoteText.text = this._categoryNotes[randomIndex];
            }

            // Update number of players
            if (this.numPlayersText != null)
            {
                this.numPlayersText.text = $"{numPlayers} Players";
            }

            // Update number of questions
            if (this.numQuestionsText != null)
            {
                this.numQuestionsText.text = $"{numQuestions} Words";
            }
        }

        private async void ToggleOnlinePlayersPanel()
        {
            this._isOnlinePlayersPanelOpen = !this._isOnlinePlayersPanelOpen;

            if (this.onlinePlayersPanel != null)
            {
                this.onlinePlayersPanel.SetActive(this._isOnlinePlayersPanelOpen);
            }

            // Request updated online players list when opening
            if (this._isOnlinePlayersPanelOpen && this.networkManager != null)
            {
                await this.networkManager.GetOnlinePlayers();
            }
        }

        private void OnOnlinePlayersReceived(OnlinePlayersResponse response)
        {
            if (response?.players == null)
                return;

            // Clear existing items
            foreach (var kvp in this._onlinePlayerListItems)
            {
                Destroy(kvp.Value);
            }
            this._onlinePlayerListItems.Clear();

            // Create new items for each online player
            foreach (var player in response.players)
            {
                if (this.onlinePlayerListItemPrefab != null && this.onlinePlayersContainer != null)
                {
                    var item = Instantiate(this.onlinePlayerListItemPrefab, this.onlinePlayersContainer);
                    item.SetActive(true);

                    var onlinePlayerListItem = item.GetComponent<OnlinePlayerListItem>();
                    if (onlinePlayerListItem != null)
                    {
                        onlinePlayerListItem.Setup(player);
                    }

                    this._onlinePlayerListItems[player.PlayerId] = item;
                }
            }

            Debug.Log($"[ONLINE_PLAYERS] Updated list with {response.players.Count} players");
        }

        private void OnRoomInviteReceived(RoomInviteData inviteData)
        {
            if (inviteData == null || this.invitationPopupPrefab == null)
                return;

            Debug.Log($"[ROOM_INVITE] Received invite from {inviteData.inviterName}");

            // Spawn invitation popup
            Canvas targetCanvas = this.mainCanvas;
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }

            if (targetCanvas != null)
            {
                var popup = Instantiate(this.invitationPopupPrefab, targetCanvas.transform);
                var invitationPopup = popup.GetComponent<InvitationPopup>();
                if (invitationPopup != null)
                {
                    invitationPopup.Setup(inviteData);
                }
            }
            else
            {
                Debug.LogError("[ROOM_INVITE] No canvas found to spawn invitation popup!");
            }
        }
    }
}