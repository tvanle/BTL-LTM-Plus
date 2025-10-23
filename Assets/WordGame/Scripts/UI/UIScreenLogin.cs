using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network;

namespace WordGame.UI
{
    public class UIScreenLogin : UIScreen
    {
        [Header("Login Panel")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button switchToRegisterButton;

        [Header("Register Panel")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button switchToLoginButton;

        [Header("Common")]
        [SerializeField] private GameObject loadingIndicator;

        private NetworkManager networkManager;
        private bool isConnecting = false;

        public override void Initialize()
        {
            base.Initialize();

            this.networkManager = NetworkManager.Instance;
            if (this.networkManager != null)
            {
                this.networkManager.OnConnected       += this.OnConnected;
                this.networkManager.OnDisconnected    += this.OnDisconnected;
                this.networkManager.OnError           += this.OnError;
                this.networkManager.OnMessageReceived += this.OnMessageReceived;

                // Auto connect to server
                this.ConnectToServer();
            }

            // Setup button listeners
            this.loginButton.onClick.AddListener(this.HandleLogin);
            this.registerButton.onClick.AddListener(this.HandleRegister);
            this.switchToRegisterButton.onClick.AddListener(this.ShowRegisterPanel);
            this.switchToLoginButton.onClick.AddListener(this.ShowLoginPanel);

            // Show login panel by default
            this.ShowLoginPanel();
        }

        private async void ConnectToServer()
        {
            if (this.networkManager != null && !this.isConnecting)
            {
                this.isConnecting = true;
                Toast.instance?.ShowMessage("Connecting to server...");
                this.ShowLoading(true);

                var connected = await this.networkManager.ConnectAsync();
                this.isConnecting = false;

                if (!connected)
                {
                    Toast.instance?.ShowMessage("Failed to connect to server. Please check your connection.", 3f);
                    this.ShowLoading(false);
                }
            }
        }

        private void OnConnected()
        {
            Toast.instance?.ShowMessage("Connected to server");
            this.ShowLoading(false);
        }

        private void OnDisconnected()
        {
            Toast.instance?.ShowMessage("Disconnected from server");
            this.ShowLoading(false);
        }

        private void OnError(string error)
        {
            Toast.instance?.ShowMessage($"Error: {error}", 3f);
            this.ShowLoading(false);
        }

        private void OnMessageReceived(NetworkManager.GameMessage message)
        {
            switch (message.Type)
            {
                case "LOGIN_SUCCESS":
                    this.HandleLoginSuccess(message.Data);
                    break;
                case "LOGIN_FAILED":
                    this.HandleLoginFailed(message.Data);
                    break;
                case "REGISTER_SUCCESS":
                    this.HandleRegisterSuccess(message.Data);
                    break;
                case "REGISTER_FAILED":
                    this.HandleRegisterFailed(message.Data);
                    break;
            }
        }

        private void HandleLogin()
        {
            var username = this.loginUsernameInput.text.Trim();
            var password = this.loginPasswordInput.text;

            // Validation
            if (string.IsNullOrWhiteSpace(username))
            {
                Toast.instance?.ShowMessage("Please enter username or email");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Toast.instance?.ShowMessage("Please enter password");
                return;
            }

            // Send login request
            Toast.instance?.ShowMessage("Logging in...");
            this.ShowLoading(true);
            this.networkManager.Login(username, password);
        }

        private void HandleRegister()
        {
            var username        = this.registerUsernameInput.text.Trim();
            var email           = this.registerEmailInput.text.Trim();
            var password        = this.registerPasswordInput.text;
            var confirmPassword = this.registerConfirmPasswordInput.text;

            // Validation
            if (string.IsNullOrWhiteSpace(username))
            {
                Toast.instance?.ShowMessage("Please enter username");
                return;
            }

            if (username.Length < 3)
            {
                Toast.instance?.ShowMessage("Username must be at least 3 characters");
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                Toast.instance?.ShowMessage("Please enter a valid email");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Toast.instance?.ShowMessage("Please enter password");
                return;
            }

            if (password.Length < 6)
            {
                Toast.instance?.ShowMessage("Password must be at least 6 characters");
                return;
            }

            if (password != confirmPassword)
            {
                Toast.instance?.ShowMessage("Passwords do not match");
                return;
            }

            // Send register request
            Toast.instance?.ShowMessage("Registering...");
            this.ShowLoading(true);
            this.networkManager.Register(username, email, password);
        }

        private void HandleLoginSuccess(string data)
        {
            this.ShowLoading(false);
            Toast.instance?.ShowMessage("Login successful!");

            // Parse user data
            var responseData = JsonUtility.FromJson<LoginResponse>(data);
            if (responseData != null)
            {
                // Store user data in PlayerPrefs
                PlayerPrefs.SetString("auth_token", responseData.token);
                PlayerPrefs.SetString("user_id", responseData.user.id);
                PlayerPrefs.SetString("username", responseData.user.username);
                PlayerPrefs.Save();

                Debug.Log($"Logged in as: {responseData.user.username}");

                // Navigate to multiplayer menu
                UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, false, true, false,
                    Tween.TweenStyle.EaseOut, null, true);
            }
        }

        private void HandleLoginFailed(string data)
        {
            this.ShowLoading(false);

            var errorData = JsonUtility.FromJson<ErrorResponse>(data);
            if (errorData != null)
            {
                Toast.instance?.ShowMessage($"Login failed: {errorData.error}", 3f);
            }
            else
            {
                Toast.instance?.ShowMessage("Login failed. Please try again.", 3f);
            }
        }

        private void HandleRegisterSuccess(string data)
        {
            this.ShowLoading(false);
            Toast.instance?.ShowMessage("Registration successful!");

            // Parse user data
            var responseData = JsonUtility.FromJson<LoginResponse>(data);
            if (responseData != null)
            {
                // Store user data in PlayerPrefs
                PlayerPrefs.SetString("auth_token", responseData.token);
                PlayerPrefs.SetString("user_id", responseData.user.id);
                PlayerPrefs.SetString("username", responseData.user.username);
                PlayerPrefs.Save();

                Debug.Log($"Registered as: {responseData.user.username}");

                // Navigate to multiplayer menu
                UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, false, true, false,
                    Tween.TweenStyle.EaseOut, null, true);
            }
        }

        private void HandleRegisterFailed(string data)
        {
            this.ShowLoading(false);

            var errorData = JsonUtility.FromJson<ErrorResponse>(data);
            if (errorData != null)
            {
                Toast.instance?.ShowMessage($"Registration failed: {errorData.error}", 3f);
            }
            else
            {
                Toast.instance?.ShowMessage("Registration failed. Please try again.", 3f);
            }
        }

        private void ShowLoginPanel()
        {
            this.loginPanel.SetActive(true);
            this.registerPanel.SetActive(false);
        }

        private void ShowRegisterPanel()
        {
            this.loginPanel.SetActive(false);
            this.registerPanel.SetActive(true);
        }

        private void ShowLoading(bool show)
        {
            if (this.loadingIndicator != null)
            {
                this.loadingIndicator.SetActive(show);
            }
        }

        private void OnDestroy()
        {
            if (this.networkManager != null)
            {
                this.networkManager.OnConnected       -= this.OnConnected;
                this.networkManager.OnDisconnected    -= this.OnDisconnected;
                this.networkManager.OnError           -= this.OnError;
                this.networkManager.OnMessageReceived -= this.OnMessageReceived;
            }
        }
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string token;
        public UserData user;
        public StatsData stats;
    }

    [System.Serializable]
    public class UserData
    {
        public string id;
        public string username;
        public string email;
        public string displayName;
        public string avatarUrl;
    }

    [System.Serializable]
    public class StatsData
    {
        public int totalScore;
        public int bestScore;
        public int bestStreak;
        public int gamesPlayed;
        public int gamesWon;
    }

    [System.Serializable]
    public class ErrorResponse
    {
        public string error;
    }
}