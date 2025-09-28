using System;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;

namespace WordGame.UI
{
    public class UIScreenMultiplayerMenu : UIScreen
    {
        [Header("UI References")]
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField topicInput;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;

        public event Action<string, string> OnCreateRoomRequest;
        public event Action<string, string> OnJoinRoomRequest;

        public override void Initialize()
        {
            base.Initialize();
            createRoomButton.onClick.AddListener(HandleCreateRoom);
            joinRoomButton.onClick.AddListener(HandleJoinRoom);
        }

        private void OnDestroy()
        {
            createRoomButton.onClick.RemoveListener(HandleCreateRoom);
            joinRoomButton.onClick.RemoveListener(HandleJoinRoom);
        }

        private void HandleCreateRoom()
        {
            var username = usernameInput.text.Trim();
            var topic = topicInput.text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(topic))
            {
                SetStatus("Please enter username and topic");
                return;
            }

            OnCreateRoomRequest?.Invoke(username, topic);
        }

        private void HandleJoinRoom()
        {
            var username = usernameInput.text.Trim();
            var roomCode = roomCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roomCode))
            {
                SetStatus("Please enter username and room code");
                return;
            }

            OnJoinRoomRequest?.Invoke(username, roomCode);
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
            topicInput.text = "";
            roomCodeInput.text = "";
        }
    }
}