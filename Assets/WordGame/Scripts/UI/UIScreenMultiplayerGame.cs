using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordGame.Network.Models;

namespace WordGame.UI
{
    public class UIScreenMultiplayerGame : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text timerText;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Transform targetWordsContainer;
        [SerializeField] private InputField answerInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text statusText;

        public event Action<string, int> OnAnswerSubmit;

        private float _levelTimer;
        private bool _isPlaying;
        private int _currentLevel;

        private void Awake()
        {
            submitButton.onClick.AddListener(HandleSubmitAnswer);
        }

        private void OnDestroy()
        {
            submitButton.onClick.RemoveListener(HandleSubmitAnswer);
        }

        private void Update()
        {
            if (_isPlaying && _levelTimer > 0)
            {
                _levelTimer -= Time.deltaTime;
                timerText.text = $"Time: {Mathf.RoundToInt(_levelTimer)}";

                if (_levelTimer <= 0)
                {
                    _levelTimer = 0;
                    timerText.text = "Time's up!";
                    _isPlaying = false;
                }
            }
        }

        private void HandleSubmitAnswer()
        {
            var answer = answerInput.text.Trim();
            if (string.IsNullOrEmpty(answer))
                return;

            var timeTaken = Mathf.RoundToInt(30f - _levelTimer);
            OnAnswerSubmit?.Invoke(answer, timeTaken);
            answerInput.text = "";
        }

        public void StartLevel(GameStartData gameData)
        {
            _currentLevel = gameData.level;
            _levelTimer = gameData.duration;
            _isPlaying = true;
            UpdateGameUI(gameData);
        }

        public void UpdateLevel(GameStartData gameData)
        {
            _currentLevel = gameData.level;
            _levelTimer = gameData.duration;
            UpdateGameUI(gameData);
        }

        private void UpdateGameUI(GameStartData gameData)
        {
            levelText.text = $"Level {gameData.level}";

            // Clear existing grid and words
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in targetWordsContainer)
            {
                Destroy(child.gameObject);
            }

            // Display grid
            var gridText = gridContainer.GetComponent<Text>();
            if (gridText != null)
            {
                gridText.text = gameData.grid;
            }

            // Display target words
            foreach (var word in gameData.words)
            {
                var wordObj = new GameObject(word);
                wordObj.transform.SetParent(targetWordsContainer);
                var text = wordObj.AddComponent<Text>();
                text.text = word;
            }
        }

        public void UpdateScore(int score)
        {
            scoreText.text = $"Score: {score}";
        }

        public void ShowAnswerResult(bool isCorrect)
        {
            statusText.text = isCorrect ? "Correct!" : "Wrong answer";
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void StopGame()
        {
            _isPlaying = false;
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
            _isPlaying = false;
            _levelTimer = 0;
            answerInput.text = "";
            SetStatus("");
        }
    }
}