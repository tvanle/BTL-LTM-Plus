using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
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
	[SerializeField] private TextMeshProUGUI timerText;

	private bool isMultiplayer = false;
	private float levelTimer;
	private bool isLevelActive;
	private NetworkManager networkManager;
	


	private void Update()
	{
		this.hintBtnText.text = $"HINT ({GameManager.Instance.CurrentHints})";

		// Update multiplayer timer
		if (this.isMultiplayer && this.isLevelActive && this.levelTimer > 0)
		{
			this.levelTimer -= Time.deltaTime;
			if (this.timerText != null && this.timerText.gameObject.activeSelf)
			{
				this.timerText.text = $"Time: {Mathf.RoundToInt(this.levelTimer)}";
			}

			if (this.levelTimer <= 0)
			{
				this.levelTimer = 0;
				this.isLevelActive = false;
				if (this.timerText != null)
				{
					this.timerText.text = "Time's up!";
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
		this.networkManager = NetworkManager.Instance;
	}

	public override void OnShowing(object data)
	{
		// Check if this is multiplayer game data
		if (data is GameStartData gameData)
		{
			this.StartMultiplayerLevel(gameData);
			return;
		}

		// Normal single player flow
		var categoryInfo = GameManager.Instance.GetCategoryInfo(GameManager.Instance.ActiveCategory);

		this.categoryText.text = GameManager.Instance.ActiveCategory.ToUpper();
		this.hintBtnText.text  = $"HINT ({GameManager.Instance.CurrentHints})";

		// Only set icon if categoryInfo exists
		if (categoryInfo != null && categoryInfo.icon != null)
		{
			this.iconImage.sprite = categoryInfo.icon;
		}

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
			if (this.isMultiplayer)
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
		this.isMultiplayer = true;
		this.isLevelActive = true;
		this.levelTimer       = 60; //Default

		// Show timer for multiplayer
		if (this.timerText != null)
		{
			this.timerText.gameObject.SetActive(true);
			this.timerText.text = $"Time: {this.levelTimer}";
		}

		this.levelText.text = $"Level {gameData.level}";
		this.categoryText.text = gameData.category?.ToUpper() ?? "MULTIPLAYER";

		// The board is already loaded by GameManager.StartLevel()
	}

	public void ResetMultiplayer()
	{
		this.isMultiplayer = false;
		this.isLevelActive = false;
		this.levelTimer       = 0;

		if (this.timerText != null)
		{
			this.timerText.gameObject.SetActive(false);
		}
	}

}
