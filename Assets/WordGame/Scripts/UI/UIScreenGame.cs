using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using WordGame.Network;
using WordGame.Network.Models;

public class UIScreenGame : UIScreen
{

	[SerializeField] private Text 			categoryText;
	[SerializeField] private Text 			levelText;
	[SerializeField] private Image			iconImage;
	[SerializeField] private Text 			hintBtnText;
	[SerializeField] private Text 			selectedWordText;
	[SerializeField] private LetterBoard	letterBoard;
	[SerializeField] private Text timerText;

	private bool isMultiplayer = false;
	private float levelTimer;
	private bool isLevelActive;
	private bool hasCompletedLevel = false;
	private NetworkManager networkManager;
	


	private void Update()
	{
		this.hintBtnText.text = $"HINT ({GameManager.Instance.CurrentHints})";

		// Update multiplayer timer
		if (isMultiplayer && isLevelActive && levelTimer > 0)
		{
			levelTimer -= Time.deltaTime;
			if (timerText != null && timerText.gameObject.activeSelf)
			{
				timerText.text = $"Time: {Mathf.RoundToInt(levelTimer)}";
			}

			if (levelTimer <= 0)
			{
				levelTimer = 0;
				isLevelActive = false;
				if (timerText != null)
				{
					timerText.text = "Time's up!";
				}
				// Send timeout to server if haven't completed
				if (!hasCompletedLevel)
				{
					SendTimeoutToServer();
				}
			}
		}
	}



	public override void Initialize()
	{
		this.selectedWordText.text = "";

		this.letterBoard.OnSelectedWordChanged += (string word) =>
		{
			this.selectedWordText.text = word;
		};

		// Get reference to NetworkManager
		networkManager = NetworkManager.Instance;
	}

	public override void OnShowing(object data)
	{
		var categoryInfo = GameManager.Instance.GetCategoryInfo(GameManager.Instance.ActiveCategory);

		this.categoryText.text = GameManager.Instance.ActiveCategory.ToUpper();
		this.hintBtnText.text  = $"HINT ({GameManager.Instance.CurrentHints})";
		this.iconImage.sprite  = categoryInfo.icon;

		if (GameManager.Instance.ActiveCategory == GameManager.dailyPuzzleId)
		{
			this.levelText.text = $"COMPLETE TO GAIN {GameConfig.instance.completeDailyPuzzleAward} HINT";
		}
		else
		{
			this.levelText.text = $"LEVEL {GameManager.Instance.ActiveLevelIndex + 1}";
		}

	}
	
	public override void OnBackClicked()
	{
		if (!GameManager.Instance.AnimatingWord)
		{
			if (isMultiplayer)
			{
				// Don't allow back during multiplayer game
				return;
			}

			if (GameManager.Instance.ActiveCategory == GameManager.dailyPuzzleId)
			{
				UIScreenController.Instance.Show(UIScreenController.MainScreenId, true);
			}
			else
			{
				UIScreenController.Instance.Show(UIScreenController.CategoryLevelsScreenId, true, true, false, Tween.TweenStyle.EaseOut, null, GameManager.Instance.ActiveCategory);
			}
		}
	}

	// Multiplayer methods
	public void StartMultiplayerLevel(GameStartData gameData)
	{
		isMultiplayer = true;
		isLevelActive = true;
		hasCompletedLevel = false;
		levelTimer = gameData.duration;

		// Show timer for multiplayer
		if (timerText != null)
		{
			timerText.gameObject.SetActive(true);
			timerText.text = $"Time: {gameData.duration}";
		}

		levelText.text = $"Level {gameData.level}";
		categoryText.text = "MULTIPLAYER";

		// TODO: Setup the letter board with multiplayer grid data
		// This will require integration with the existing WordRegion/LetterBoard system
	}

	public async void OnMultiplayerLevelCompleted()
	{
		if (!isMultiplayer || hasCompletedLevel) return;

		hasCompletedLevel = true;
		isLevelActive = false;

		// Calculate time taken
		int timeTaken = Mathf.RoundToInt(30f - levelTimer);

		// Send completion to server
		if (networkManager != null)
		{
			await networkManager.SubmitAnswer("COMPLETED", timeTaken);
		}

		// Show complete overlay screen
		if (UIScreenController.Instance != null)
		{
			UIScreenController.Instance.Show(UIScreenController.CompleteScreenId, false, false, true);
		}
	}

	private async void SendTimeoutToServer()
	{
		if (!isMultiplayer || hasCompletedLevel) return;

		hasCompletedLevel = true;

		// Send timeout signal to server
		if (networkManager != null)
		{
			await networkManager.SubmitAnswer("TIMEOUT", 30);
		}

		// Show complete overlay screen
		if (UIScreenController.Instance != null)
		{
			UIScreenController.Instance.Show(UIScreenController.CompleteScreenId, false, false, true);
		}
	}

	public void ResetMultiplayer()
	{
		isMultiplayer = false;
		isLevelActive = false;
		hasCompletedLevel = false;
		levelTimer = 0;

		if (timerText != null)
		{
			timerText.gameObject.SetActive(false);
		}
	}

}
