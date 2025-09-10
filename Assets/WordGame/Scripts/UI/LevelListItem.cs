using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LevelListItem : MonoBehaviour
{
	#region Enums

	public enum Type
	{
		Normal,
		Locked,
		Completed
	}

	#endregion

	#region Inspector Variables
	
	[SerializeField] private Text		levelText;
	[SerializeField] private Image		iconImage;
	[SerializeField] private GameObject	completedImage;
	[SerializeField] private GameObject	lockedImage;
	
	#endregion
	
	#region Member Variables
	
	private string	categoryName;
	private int		levelIndex;
	private Type	type;
	
	#endregion
	
	#region Public Methods
	
	public void Setup(CategoryInfo categoryInfo, int levelIndex, Type type)
	{
		this.categoryName	= categoryInfo.name;
		this.levelIndex		= levelIndex;
		this.type			= type;

		this.levelText.text = string.Format("{0} - LEVEL {1}", this.categoryName.ToUpper(), levelIndex + 1);
		this.iconImage.sprite  = categoryInfo.icon;

		this.completedImage.gameObject.SetActive(type == Type.Completed);
		this.lockedImage.gameObject.SetActive(type == Type.Locked);
	}
	
	public void OnClick()
	{
		if (this.type != Type.Locked)
		{
			// Start the level the button is tied to
			GameManager.Instance.StartLevel(this.categoryName, this.levelIndex);
			
			// Show the game screen
			UIScreenController.Instance.Show(UIScreenController.GameScreenId);
		}
	}
	
	#endregion
}
