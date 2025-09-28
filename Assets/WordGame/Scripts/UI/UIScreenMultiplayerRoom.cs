using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;

namespace WordGame.UI
{
    public class UIScreenMultiplayerRoom : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text roomCodeText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveRoomButton;
        [SerializeField] private Text statusText;

        public event Action OnReadyPressed;
        public event Action OnStartGamePressed;
        public event Action OnLeaveRoomPressed;

        private Dictionary<string, GameObject> _playerListItems = new Dictionary<string, GameObject>();
        private bool _isHost;

        private void Awake()
        {
            readyButton.onClick.AddListener(() => OnReadyPressed?.Invoke());
            startGameButton.onClick.AddListener(() => OnStartGamePressed?.Invoke());
            leaveRoomButton.onClick.AddListener(() => OnLeaveRoomPressed?.Invoke());
        }

        private void OnDestroy()
        {
            readyButton.onClick.RemoveAllListeners();
            startGameButton.onClick.RemoveAllListeners();
            leaveRoomButton.onClick.RemoveAllListeners();
        }

        public void Initialize(string roomCode, bool isHost)
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

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Reset()
        {
            readyButton.interactable = true;
            SetStatus("");
        }
    }
}