using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIScreenCompleteOverlay : UIScreen
{

	[SerializeField] private Image		categoryIconImage;
	[SerializeField] private Text		categoryNameText;
	[SerializeField] private Text		categoryLevelText;
	[SerializeField] private Text	    plusHintText;






	public override void OnShowing(object data)
	{
		var categoryInfo = GameManager.Instance.GetCategoryInfo(GameManager.Instance.ActiveCategory);

		this.categoryIconImage.sprite = categoryInfo.icon;
		this.categoryNameText.text       = GameManager.Instance.ActiveCategory;

		if (GameManager.Instance.ActiveCategory == GameManager.dailyPuzzleId)
		{
			this.categoryLevelText.gameObject.SetActive(false);
		}
		else
		{
			this.categoryLevelText.gameObject.SetActive(true);
			this.categoryLevelText.text = "Level " + (GameManager.Instance.ActiveLevelIndex + 1).ToString();
		}

        var number = (int)data;
		this.plusHintText.gameObject.SetActive(number > 0);
		this.plusHintText.text = "+ " + number + (number == 1 ? " Hint" : " Hints");
	}

}
