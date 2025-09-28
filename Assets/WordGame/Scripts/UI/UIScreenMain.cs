using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIScreenMain : UIScreen
{

	[SerializeField] private ProgressRing	progressRing;
	[SerializeField] private Text			continueBtnTopText;
	[SerializeField] private Text			continueBtnBottomText;
	[SerializeField] private Image			continueBtnImage;



	private string	continueBtnCategory;
	private int		continueBtnLevelIndex;



	public override void OnShowing(object data)
	{
		// Set progress to 100% as all levels are now unlocked
		this.progressRing.SetProgress(1.0f);

		// Always show the first category and first level on the Play button
		if (GameManager.Instance.CategoryInfos.Count > 0)
		{
			var firstNonDailyCategory = GameManager.Instance.CategoryInfos
				.FirstOrDefault(c => c.name != GameManager.dailyPuzzleId);

			if (firstNonDailyCategory != null)
			{
				this.continueBtnCategory = firstNonDailyCategory.name;
				this.continueBtnLevelIndex = 0;

				this.continueBtnTopText.text = "PLAY";
				this.continueBtnBottomText.text = $"{this.continueBtnCategory.ToUpper()} LEVEL 1";
				this.continueBtnImage.sprite = firstNonDailyCategory.icon;
			}
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

}