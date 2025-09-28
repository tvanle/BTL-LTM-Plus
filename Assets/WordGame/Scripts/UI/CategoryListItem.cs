using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CategoryListItem : MonoBehaviour
{

	[SerializeField] private Text	categoryText;
	[SerializeField] private Text	infoText;
	[SerializeField] private Image	iconImage;
	[SerializeField] private Image	completedImage;



	private string categoryName;



	public void Setup(CategoryInfo categoryInfo)
	{
		this.categoryName = categoryInfo.name;

		float numberOfLevels			= categoryInfo.levelInfos.Count;
		float numberOfCompletedLevels	= GameManager.Instance.GetCompletedLevelCount(categoryInfo);

		this.categoryText.text = categoryInfo.name.ToUpper();
		this.infoText.text     = $"SIZE: {categoryInfo.description} - LEVELS: {numberOfCompletedLevels}/{numberOfLevels}";
		this.iconImage.sprite  = categoryInfo.icon;

		this.completedImage.enabled = (numberOfLevels == numberOfCompletedLevels);
	}

	public void OnClick()
	{
		// Show the category levels screen
		UIScreenController.Instance.Show(UIScreenController.CategoryLevelsScreenId, false, true, false, Tween.TweenStyle.EaseOut, null, this.categoryName);
	}

}
