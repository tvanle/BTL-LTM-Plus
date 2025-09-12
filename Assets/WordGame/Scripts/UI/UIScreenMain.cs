using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIScreenMain : UIScreen
{
	#region Inspector Variables

	[SerializeField] private ProgressRing	progressRing;
	[SerializeField] private Text			continueBtnTopText;
	[SerializeField] private Text			continueBtnBottomText;
	[SerializeField] private Image			continueBtnImage;

	#endregion

	#region Member Variables

	private string	continueBtnCategory;
	private int		continueBtnLevelIndex;

	#endregion

	#region Public Methods

	public override void OnShowing(object data)
	{
		// Set the progress rings percentage to the number of completed levels from all categories
		var totalNumberOfLevels				= 0;
		var totalNumberOfCompletedLevels	= 0;

		for (var i = 0; i < GameManager.Instance.CategoryInfos.Count; i++)
		{
			var categoryInfo = GameManager.Instance.CategoryInfos[i];

			// Only include levels that are not part of the paily puzzle category
			if (categoryInfo.name != GameManager.dailyPuzzleId)
			{
				totalNumberOfLevels				+= categoryInfo.levelInfos.Count;
				totalNumberOfCompletedLevels	+= GameManager.Instance.GetCompletedLevelCount(categoryInfo);
			}
		}

		this.progressRing.SetProgress((float)totalNumberOfCompletedLevels / (float)totalNumberOfLevels);

		// Set the Continue button to the active category
		if (string.IsNullOrEmpty(GameManager.Instance.ActiveCategory) || GameManager.Instance.ActiveCategory == GameManager.dailyPuzzleId)
		{
			var foundUncompletedLevel = false;

			for (var i = 0; i < GameManager.Instance.CategoryInfos.Count; i++)
			{
				var categoryInfo = GameManager.Instance.CategoryInfos[i];

				if (categoryInfo.name == GameManager.dailyPuzzleId)
				{
					continue;
				}

				for (var j = 0; j < categoryInfo.levelInfos.Count; j++)
				{
					if (!GameManager.Instance.IsLevelCompleted(categoryInfo, j))
					{
						this.continueBtnCategory = categoryInfo.name;
						this.continueBtnLevelIndex  = j;
						foundUncompletedLevel    = true;

						break;
					}
				}

				if (foundUncompletedLevel)
				{
					break;
				}
			}

			// If all levels are completed then set the button to the first category and first level
			if (!foundUncompletedLevel)
			{
				this.continueBtnCategory = GameManager.Instance.CategoryInfos[0].name;
				this.continueBtnLevelIndex  = 0;
			}

			this.continueBtnTopText.text    = "PLAY";
			this.continueBtnBottomText.text = $"{this.continueBtnCategory.ToUpper()} LEVEL {this.continueBtnLevelIndex + 1}";
			this.continueBtnImage.sprite    = GameManager.Instance.GetCategoryInfo(this.continueBtnCategory).icon;
		}
		else
		{
			this.continueBtnCategory = GameManager.Instance.ActiveCategory;
			this.continueBtnLevelIndex  = GameManager.Instance.ActiveLevelIndex;

			this.continueBtnTopText.text    = "CONTINUE";
			this.continueBtnBottomText.text = $"{this.continueBtnCategory.ToUpper()} LEVEL {this.continueBtnLevelIndex + 1}";
			this.continueBtnImage.sprite    = GameManager.Instance.GetCategoryInfo(this.continueBtnCategory).icon;
		}
	}

	public void OnCategoryButtonClicked()
	{
		// Show the main screen
		UIScreenController.Instance.Show(UIScreenController.CategoriesScreenId);
	}

	public void OnContinueButtonClicked()
	{
		// Start the level the button is tied to
		GameManager.Instance.StartLevel(this.continueBtnCategory, this.continueBtnLevelIndex);

		// Show the game screen
		UIScreenController.Instance.Show(UIScreenController.GameScreenId);
	}

	#endregion
}
