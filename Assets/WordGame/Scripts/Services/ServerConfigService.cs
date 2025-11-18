using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.RemoteConfig;
using Firebase.Extensions;

namespace WordGame.Services
{
    /// <summary>
    /// Manages server configuration fetched from Firebase Remote Config
    /// </summary>
    public class ServerConfigService : MonoBehaviour
    {
        private static ServerConfigService instance;
        public static ServerConfigService Instance => instance;

        [Header("Default Configuration (Fallback)")]
        [SerializeField] private string defaultServerHost = "localhost";
        [SerializeField] private int defaultServerPort = 8080;
        [SerializeField] private bool useSSL = false;

        [Header("Firebase Settings")]
        [SerializeField] private float fetchTimeoutSeconds = 10f;
        [SerializeField] private bool enableDeveloperMode = false;

        // Public properties
        public string ServerHost { get; private set; }
        public int ServerPort { get; private set; }
        public bool UseSSL { get; private set; }
        public bool IsConfigLoaded { get; private set; }
        public string ConfigVersion { get; private set; }
        public bool MaintenanceMode { get; private set; }

        // Events
        public event Action OnConfigLoaded;
        public event Action<string> OnConfigError;

        private FirebaseApp firebaseApp;
        private bool isInitialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this.gameObject);

            // Set default values
            this.ServerHost = this.defaultServerHost;
            this.ServerPort = this.defaultServerPort;
            this.UseSSL = this.useSSL;
        }

        /// <summary>
        /// Initialize Firebase and fetch remote config
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (this.isInitialized)
            {
                Debug.Log("[ServerConfig] Already initialized");
                return true;
            }

            try
            {
                Debug.Log("[ServerConfig] ===== START FIREBASE INITIALIZATION =====");
                
                // Check Firebase dependencies
                Debug.Log("[ServerConfig] Checking Firebase dependencies...");
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

                Debug.Log($"[ServerConfig] Dependency check result: {dependencyStatus}");
                
                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError($"[ServerConfig] Could not resolve Firebase dependencies: {dependencyStatus}");
                    Debug.LogError("[ServerConfig] POSSIBLE CAUSES:");
                    Debug.LogError("[ServerConfig] 1. Missing google-services.json file in Assets/ folder");
                    Debug.LogError("[ServerConfig] 2. Firebase SDK not properly imported");
                    Debug.LogError("[ServerConfig] 3. Android/iOS build settings not configured");
                    this.OnConfigError?.Invoke($"Firebase initialization failed: {dependencyStatus}");
                    return false;
                }

                // Initialize Firebase
                Debug.Log("[ServerConfig] Dependencies OK! Initializing Firebase App...");
                this.firebaseApp = FirebaseApp.DefaultInstance;
                Debug.Log($"[ServerConfig] Firebase App initialized: {this.firebaseApp.Name}");

                // Set Remote Config settings
                Debug.Log("[ServerConfig] Setting up Remote Config...");
                var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                var configSettings = new ConfigSettings
                {
                    MinimumFetchIntervalInMilliseconds = (ulong)(this.enableDeveloperMode ? 0 : 3600000) // 1 hour in production
                };
                await remoteConfig.SetConfigSettingsAsync(configSettings);
                Debug.Log($"[ServerConfig] Config settings applied (Developer Mode: {this.enableDeveloperMode}, Fetch Interval: {configSettings.MinimumFetchIntervalInMilliseconds}ms)");

                // Set default values
                Debug.Log("[ServerConfig] Setting default config values...");
                var defaults = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "server_host", this.defaultServerHost },
                    { "server_port", this.defaultServerPort },
                    { "use_ssl", this.useSSL },
                    { "maintenance_mode", false },
                    { "config_version", "1.0.0" },
                    { "min_client_version", "1.0.0" }
                };
                await remoteConfig.SetDefaultsAsync(defaults);
                Debug.Log($"[ServerConfig] Default values set: host={this.defaultServerHost}, port={this.defaultServerPort}");

                this.isInitialized = true;
                Debug.Log("[ServerConfig] ===== FIREBASE INITIALIZED SUCCESSFULLY =====");

                // Fetch and activate config
                return await this.FetchAndActivateAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerConfig] ===== INITIALIZATION FAILED =====");
                Debug.LogError($"[ServerConfig] Error: {ex.Message}");
                Debug.LogError($"[ServerConfig] Stack Trace: {ex.StackTrace}");
                this.OnConfigError?.Invoke($"Initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fetch latest config from Firebase and activate it
        /// </summary>
        public async Task<bool> FetchAndActivateAsync()
        {
            if (!this.isInitialized)
            {
                Debug.LogWarning("[ServerConfig] Not initialized, using default values");
                this.IsConfigLoaded = true;
                this.OnConfigLoaded?.Invoke();
                return false;
            }

            try
            {
                Debug.Log("[ServerConfig] ===== FETCHING REMOTE CONFIG =====");
                var remoteConfig = FirebaseRemoteConfig.DefaultInstance;

                Debug.Log($"[ServerConfig] Fetch timeout: {this.fetchTimeoutSeconds} seconds");
                Debug.Log("[ServerConfig] Starting fetch from Firebase...");
                
                // Fetch with timeout
                var fetchTask = remoteConfig.FetchAsync(TimeSpan.FromSeconds(this.fetchTimeoutSeconds));
                await fetchTask;

                if (fetchTask.IsFaulted)
                {
                    Debug.LogError($"[ServerConfig] ===== FETCH FAILED =====");
                    Debug.LogError($"[ServerConfig] Exception: {fetchTask.Exception}");
                    if (fetchTask.Exception != null)
                    {
                        foreach (var innerEx in fetchTask.Exception.InnerExceptions)
                        {
                            Debug.LogError($"[ServerConfig] Inner Exception: {innerEx.Message}");
                        }
                    }
                    throw new Exception("Failed to fetch config");
                }

                Debug.Log("[ServerConfig] Fetch completed! Activating config...");
                
                // Activate fetched config
                var activateTask = remoteConfig.ActivateAsync();
                await activateTask;

                if (activateTask.Result)
                {
                    Debug.Log("[ServerConfig] ===== CONFIG ACTIVATED (NEW VALUES) =====");
                    this.LoadConfigValues();
                    return true;
                }
                else
                {
                    Debug.LogWarning("[ServerConfig] Config fetched but not activated (no changes detected)");
                    this.LoadConfigValues();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerConfig] ===== FETCH ERROR =====");
                Debug.LogError($"[ServerConfig] Error: {ex.Message}");
                Debug.LogError($"[ServerConfig] Stack Trace: {ex.StackTrace}");
                this.OnConfigError?.Invoke($"Failed to fetch config: {ex.Message}");

                // Use default/cached values
                Debug.LogWarning("[ServerConfig] Using fallback/cached values...");
                this.LoadConfigValues();
                return false;
            }
        }

        /// <summary>
        /// Load values from Remote Config into properties
        /// </summary>
        private void LoadConfigValues()
        {
            try
            {
                var remoteConfig = FirebaseRemoteConfig.DefaultInstance;

                // Load server configuration
                this.ServerHost = remoteConfig.GetValue("server_host").StringValue;
                this.ServerPort = (int)remoteConfig.GetValue("server_port").LongValue;
                this.UseSSL = remoteConfig.GetValue("use_ssl").BooleanValue;
                this.MaintenanceMode = remoteConfig.GetValue("maintenance_mode").BooleanValue;
                this.ConfigVersion = remoteConfig.GetValue("config_version").StringValue;

                Debug.Log($"[ServerConfig] Loaded config: {this.ServerHost}:{this.ServerPort} (SSL: {this.UseSSL}, Version: {this.ConfigVersion})");

                // Check maintenance mode
                if (this.MaintenanceMode)
                {
                    Debug.LogWarning("[ServerConfig] Server is in maintenance mode!");
                }

                this.IsConfigLoaded = true;
                this.OnConfigLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerConfig] Error loading config values: {ex.Message}");

                // Use fallback values
                this.ServerHost = this.defaultServerHost;
                this.ServerPort = this.defaultServerPort;
                this.UseSSL = this.useSSL;
                this.MaintenanceMode = false;
                this.ConfigVersion = "fallback";

                this.IsConfigLoaded = true;
                this.OnConfigLoaded?.Invoke();
            }
        }

        /// <summary>
        /// Get string value from Remote Config
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            if (!this.isInitialized) return defaultValue;

            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Get long value from Remote Config
        /// </summary>
        public long GetLong(string key, long defaultValue = 0)
        {
            if (!this.isInitialized) return defaultValue;

            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Get bool value from Remote Config
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (!this.isInitialized) return defaultValue;

            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Force refresh config from Firebase
        /// </summary>
        public async Task RefreshConfigAsync()
        {
            Debug.Log("[ServerConfig] Manually refreshing config...");
            await this.FetchAndActivateAsync();
        }

        /// <summary>
        /// Get full server URL with protocol
        /// </summary>
        public string GetServerUrl()
        {
            var protocol = this.UseSSL ? "https" : "http";
            return $"{protocol}://{this.ServerHost}:{this.ServerPort}";
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Fetch Config")]
        private async void TestFetchConfig()
        {
            Debug.Log("=== Testing Firebase Remote Config ===");
            var success = await this.InitializeAsync();
            Debug.Log($"Config loaded: {success}");
            Debug.Log($"Server: {this.ServerHost}:{this.ServerPort}");
            Debug.Log($"Maintenance: {this.MaintenanceMode}");
        }

        [ContextMenu("Print Current Config")]
        private void PrintCurrentConfig()
        {
            Debug.Log("=== Current Server Config ===");
            Debug.Log($"Host: {this.ServerHost}");
            Debug.Log($"Port: {this.ServerPort}");
            Debug.Log($"SSL: {this.UseSSL}");
            Debug.Log($"Maintenance: {this.MaintenanceMode}");
            Debug.Log($"Version: {this.ConfigVersion}");
            Debug.Log($"Loaded: {this.IsConfigLoaded}");
        }
#endif
    }
}
