using UnityEngine;
using System.Collections;

public class UIScreenCategories : UIScreen
{
	#region Inspector Variables

	[SerializeField] private Transform			categoriesListContainer;
	[SerializeField] private CategoryListItem	categoryListItemPrefab;

	#endregion

	#region Member Variables

	private ObjectPool categoryItemObjectPool;

	#endregion

	#region Public Methods

	public override void Initialize()
	{
		this.categoryItemObjectPool = new ObjectPool(this.categoryListItemPrefab.gameObject, 10, this.categoriesListContainer);
	}

	public override void OnShowing(object data)
	{
		this.categoryItemObjectPool.ReturnAllObjectsToPool();

		for (var i = 0; i < GameManager.Instance.CategoryInfos.Count; i++)
		{
			var categoryInfo = GameManager.Instance.CategoryInfos[i];

			// If its the daily puzzle category the don't show it in the list of categories
			if (categoryInfo.name == GameManager.dailyPuzzleId)
			{
				continue;
			}

			var categoryListItem = this.categoryItemObjectPool.GetObject().GetComponent<CategoryListItem>();

			categoryListItem.Setup(categoryInfo);
			categoryListItem.gameObject.SetActive(true);
		}
	}

	public override void OnBackClicked()
	{
		// Go back to main screen
		UIScreenController.Instance.Show(UIScreenController.MainScreenId, true);
	}

	#endregion
}
