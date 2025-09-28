using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIScreenGame : UIScreen
{

	[SerializeField] private Text 			categoryText;
	[SerializeField] private Text 			levelText;
	[SerializeField] private Image			iconImage;
	[SerializeField] private Text 			hintBtnText;
	[SerializeField] private Text 			selectedWordText;
	[SerializeField] private LetterBoard	letterBoard;
	


	private void Update()
	{
		this.hintBtnText.text = $"HINT ({GameManager.Instance.CurrentHints})";
	}



	public override void Initialize()
	{
		this.selectedWordText.text = "";

		this.letterBoard.OnSelectedWordChanged += (string word) => 
		{
			this.selectedWordText.text = word;
		};
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

}
