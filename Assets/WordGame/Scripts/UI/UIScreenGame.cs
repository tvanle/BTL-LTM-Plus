using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using WordGame.Network;
using WordGame.Network.Models;

public class UIScreenGame : UIScreen
{

	[SerializeField] private Text 			levelText;
	[SerializeField] private Image			iconImage;
	[SerializeField] private Text 			hintBtnText;
	[SerializeField] private Text 			selectedWordText;
	[SerializeField] private LetterBoard	letterBoard;
	[SerializeField] private TextMeshProUGUI timerText;

	private float levelTimer;
	private bool isLevelActive;
	private NetworkManager networkManager;



	private void Update()
	{
		this.hintBtnText.text = $"HINT ({GameManager.Instance.CurrentHints})";

		// Update multiplayer timer
		if (this.isLevelActive && this.levelTimer > 0)
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

	}

	public override void OnBackClicked()
	{
		if (!GameManager.Instance.AnimatingWord)
		{
			this.LeaveMultiplayerGame();
		}
	}

	private async void LeaveMultiplayerGame()
	{
		await this.networkManager.LeaveRoom();
		this.ResetMultiplayer();
		UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, true);
	}

	// Multiplayer methods
	public void StartMultiplayerLevel(GameStartData gameData)
	{
		this.isLevelActive = true;
		this.levelTimer       = 60; //Default

		// Show timer for multiplayer
		if (this.timerText != null)
		{
			this.timerText.gameObject.SetActive(true);
			this.timerText.text = $"Time: {this.levelTimer}";
		}

		this.levelText.text = $"Level {gameData.level}";
		var categoryInfo = GameManager.Instance.GetCategoryInfo(gameData.category);

		// Only set icon if categoryInfo exists
		if (categoryInfo != null && categoryInfo.icon != null)
		{
			this.iconImage.sprite = categoryInfo.icon;
		}

		// The board is already loaded by GameManager.StartLevel()
	}

	public void ResetMultiplayer()
	{
		this.isLevelActive = false;
		this.levelTimer       = 0;

		if (this.timerText != null)
		{
			this.timerText.gameObject.SetActive(false);
		}
	}

}