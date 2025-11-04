using UnityEngine;
using System.Collections;
using System;

public class UIScreenCategories : UIScreen
{

	[SerializeField] private Transform			categoriesListContainer;
	[SerializeField] private CategoryListItem	categoryListItemPrefab;

	[Header("Categories Animation")]
	[SerializeField] private float fadeInDuration = 0.2f;

	private ObjectPool categoryItemObjectPool;

	public Action<string> OnCategorySelected { get; set; }



	public override void Initialize()
	{
		base.Initialize();
		this.categoryItemObjectPool = new ObjectPool(this.categoryListItemPrefab.gameObject, 10, this.categoriesListContainer);
	}

	protected override void PlayShowAnimation()
	{
		// Simple quick fade for category list
		this.StartCoroutine(this.QuickFadeIn());
	}

	private IEnumerator QuickFadeIn()
	{
		var canvasGroup = this.GetComponent<CanvasGroup>();
		if (canvasGroup == null)
		{
			canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}

		var elapsedTime = 0f;

		// Start invisible
		canvasGroup.alpha = 0f;

		// Quick fade in
		while (elapsedTime < this.fadeInDuration)
		{
			elapsedTime += Time.deltaTime;
			var progress = Mathf.Clamp01(elapsedTime / this.fadeInDuration);
			canvasGroup.alpha = progress;

			yield return null;
		}

		// Ensure final state
		canvasGroup.alpha = 1f;
	}

	protected override void OnShowingContent(object data)
	{
		this.categoryItemObjectPool.ReturnAllObjectsToPool();

		// Reset callback if not provided in data
		if (data is Action<string> callback)
		{
			this.OnCategorySelected = callback;
		}
		else
		{
			this.OnCategorySelected = null;
		}

		for (var i = 0; i < GameManager.Instance.CategoryInfos.Count; i++)
		{
			var categoryInfo = GameManager.Instance.CategoryInfos[i];

			// If its the daily puzzle category the don't show it in the list of categories
			if (categoryInfo.name == GameManager.dailyPuzzleId)
			{
				continue;
			}

			var categoryListItem = this.categoryItemObjectPool.GetObject().GetComponent<CategoryListItem>();

			categoryListItem.Setup(categoryInfo, this);
			categoryListItem.gameObject.SetActive(true);
		}
	}

	public override void OnBackClicked()
	{
		UIScreenController.Instance.Show(UIScreenController.MultiplayerMenuScreenId, true);
	}

}