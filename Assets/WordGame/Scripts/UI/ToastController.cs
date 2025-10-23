using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordGame.UI
{
    public class ToastController : SingletonComponent<ToastController>
    {
        [Header("Toast UI")]
        [SerializeField] private GameObject toastContainer;
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private Image toastBackground;

        [Header("Toast Colors")]
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color errorColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color infoColor = new Color(0.2f, 0.5f, 0.9f, 0.9f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.7f, 0.2f, 0.9f);

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private Queue<ToastMessage> toastQueue = new Queue<ToastMessage>();
        private bool isShowingToast = false;
        private Coroutine currentToastCoroutine;

        protected override void Awake()
        {
            base.Awake();

            if (this.toastContainer != null)
            {
                this.toastContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Show a success toast message (green)
        /// </summary>
        public void ShowSuccess(string message, float duration = 2.5f)
        {
            this.Show(message, ToastType.Success, duration);
        }

        /// <summary>
        /// Show an error toast message (red)
        /// </summary>
        public void ShowError(string message, float duration = 3f)
        {
            this.Show(message, ToastType.Error, duration);
        }

        /// <summary>
        /// Show an info toast message (blue)
        /// </summary>
        public void ShowInfo(string message, float duration = 2.5f)
        {
            this.Show(message, ToastType.Info, duration);
        }

        /// <summary>
        /// Show a warning toast message (orange)
        /// </summary>
        public void ShowWarning(string message, float duration = 2.5f)
        {
            this.Show(message, ToastType.Warning, duration);
        }

        /// <summary>
        /// Show a toast message with custom type
        /// </summary>
        public void Show(string message, ToastType type = ToastType.Info, float duration = 2.5f)
        {
            var toast = new ToastMessage
            {
                message = message,
                type = type,
                duration = duration
            };

            this.toastQueue.Enqueue(toast);

            if (!this.isShowingToast)
            {
                this.ShowNextToast();
            }
        }

        private void ShowNextToast()
        {
            if (this.toastQueue.Count == 0)
            {
                this.isShowingToast = false;
                return;
            }

            this.isShowingToast = true;
            var toast = this.toastQueue.Dequeue();

            if (this.currentToastCoroutine != null)
            {
                this.StopCoroutine(this.currentToastCoroutine);
            }

            this.currentToastCoroutine = this.StartCoroutine(this.ShowToastCoroutine(toast));
        }

        private IEnumerator ShowToastCoroutine(ToastMessage toast)
        {
            // Setup toast
            if (this.toastText != null)
            {
                this.toastText.text = toast.message;
            }

            if (this.toastBackground != null)
            {
                this.toastBackground.color = this.GetColorForType(toast.type);
            }

            // Show container
            if (this.toastContainer != null)
            {
                this.toastContainer.SetActive(true);

                // Fade in animation
                yield return this.StartCoroutine(this.FadeToast(0f, 1f, this.fadeInDuration));

                // Display duration
                yield return new WaitForSeconds(toast.duration);

                // Fade out animation
                yield return this.StartCoroutine(this.FadeToast(1f, 0f, this.fadeOutDuration));

                // Hide container
                this.toastContainer.SetActive(false);
            }

            // Show next toast in queue
            this.ShowNextToast();
        }

        private IEnumerator FadeToast(float fromAlpha, float toAlpha, float duration)
        {
            if (this.toastContainer == null) yield break;

            var canvasGroup = this.toastContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = this.toastContainer.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
        }

        private Color GetColorForType(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success:
                    return this.successColor;
                case ToastType.Error:
                    return this.errorColor;
                case ToastType.Warning:
                    return this.warningColor;
                case ToastType.Info:
                default:
                    return this.infoColor;
            }
        }

        /// <summary>
        /// Clear all pending toasts and hide current one
        /// </summary>
        public void ClearAll()
        {
            this.toastQueue.Clear();

            if (this.currentToastCoroutine != null)
            {
                this.StopCoroutine(this.currentToastCoroutine);
                this.currentToastCoroutine = null;
            }

            if (this.toastContainer != null)
            {
                this.toastContainer.SetActive(false);
            }

            this.isShowingToast = false;
        }

        private class ToastMessage
        {
            public string message;
            public ToastType type;
            public float duration;
        }

        public enum ToastType
        {
            Info,
            Success,
            Error,
            Warning
        }
    }
}
