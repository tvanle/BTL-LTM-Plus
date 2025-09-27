using UnityEngine;
using System.Collections;

public class UIScreenCategoryLevels : UIScreen
{
	#region Inspector Variables
	
	[SerializeField] private Transform		levelListContainer;
	[SerializeField] private LevelListItem	levelListItemPrefab;
	
	#endregion
	
	#region Member Variables
	
	private ObjectPool levelItemObjectPool;
	
	#endregion
	
	#region Public Methods
	
	public override void Initialize()
	{
		this.levelItemObjectPool = new ObjectPool(this.levelListItemPrefab.gameObject, 10, this.levelListContainer);
	}
	
	public override void OnShowing(object categoryName)
	{
		this.levelItemObjectPool.ReturnAllObjectsToPool();

		var	categoryInfo	= GameManager.Instance.GetCategoryInfo((string)categoryName);

		for (var i = 0; i < categoryInfo.levelInfos.Count; i++)
		{
			var levelListItem = this.levelItemObjectPool.GetObject().GetComponent<LevelListItem>();

			levelListItem.Setup(categoryInfo, i, LevelListItem.Type.Normal);
			levelListItem.gameObject.SetActive(true);
		}
	}
	
	public override void OnBackClicked()
	{
		// Go back to main screen
		UIScreenController.Instance.Show(UIScreenController.CategoriesScreenId, true);
	}
	
	#endregion
}
