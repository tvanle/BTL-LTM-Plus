using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace WordGame.UI
{
    /// <summary>
    /// Simple loading screen for Firebase config and server connection
    /// </summary>
    public class UILoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image progressBar;
        [SerializeField] private GameObject spinnerImage;

        [Header("Messages")]
        [SerializeField] private string initializingMessage = "Initializing...";
        [SerializeField] private string fetchingConfigMessage = "Loading configuration...";
        [SerializeField] private string connectingMessage = "Connecting to server...";
        [SerializeField] private string authenticatingMessage = "Authenticating...";

        private Coroutine spinnerCoroutine;

        private void Awake()
        {
            if (this.loadingPanel != null)
            {
                this.loadingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show loading screen with a message
        /// </summary>
        public void Show(string message = null)
        {
            if (this.loadingPanel != null)
            {
                this.loadingPanel.SetActive(true);
            }

            if (this.statusText != null)
            {
                this.statusText.text = message ?? this.initializingMessage;
            }

            if (this.progressBar != null)
            {
                this.progressBar.fillAmount = 0f;
            }

            // Start spinner animation
            if (this.spinnerImage != null && this.spinnerCoroutine == null)
            {
                this.spinnerImage.SetActive(true);
                this.spinnerCoroutine = StartCoroutine(this.RotateSpinner());
            }
        }

        /// <summary>
        /// Hide loading screen
        /// </summary>
        public void Hide()
        {
            if (this.loadingPanel != null)
            {
                this.loadingPanel.SetActive(false);
            }

            // Stop spinner animation
            if (this.spinnerCoroutine != null)
            {
                StopCoroutine(this.spinnerCoroutine);
                this.spinnerCoroutine = null;
            }

            if (this.spinnerImage != null)
            {
                this.spinnerImage.SetActive(false);
            }
        }

        /// <summary>
        /// Update status text
        /// </summary>
        public void SetStatus(string message)
        {
            if (this.statusText != null)
            {
                this.statusText.text = message;
            }
        }

        /// <summary>
        /// Update progress bar (0-1)
        /// </summary>
        public void SetProgress(float progress)
        {
            if (this.progressBar != null)
            {
                this.progressBar.fillAmount = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Show with step progression
        /// </summary>
        public void ShowStep(LoadingStep step)
        {
            string message = step switch
            {
                LoadingStep.Initializing => this.initializingMessage,
                LoadingStep.FetchingConfig => this.fetchingConfigMessage,
                LoadingStep.Connecting => this.connectingMessage,
                LoadingStep.Authenticating => this.authenticatingMessage,
                _ => this.initializingMessage
            };

            this.Show(message);

            float progress = step switch
            {
                LoadingStep.Initializing => 0.25f,
                LoadingStep.FetchingConfig => 0.5f,
                LoadingStep.Connecting => 0.75f,
                LoadingStep.Authenticating => 0.9f,
                _ => 0f
            };

            this.SetProgress(progress);
        }

        private IEnumerator RotateSpinner()
        {
            if (this.spinnerImage == null) yield break;

            var rectTransform = this.spinnerImage.GetComponent<RectTransform>();
            if (rectTransform == null) yield break;

            while (true)
            {
                rectTransform.Rotate(0f, 0f, -360f * Time.deltaTime);
                yield return null;
            }
        }

        public enum LoadingStep
        {
            Initializing,
            FetchingConfig,
            Connecting,
            Authenticating
        }
    }
}
