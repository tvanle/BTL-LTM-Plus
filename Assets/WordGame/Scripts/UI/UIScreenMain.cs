using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class UIScreenMain : UIScreen
{

	[SerializeField] private ProgressRing	progressRing;
	[SerializeField] private Text			continueBtnTopText;
	[SerializeField] private Text			continueBtnBottomText;
	[SerializeField] private Image			continueBtnImage;

	[Header("Main Screen Animation")]
	[SerializeField] private float slideDistance = 100f;
	[SerializeField] private float slideAndFadeDuration = 0.25f;

	private string	continueBtnCategory;
	private int		continueBtnLevelIndex;



	protected override void PlayShowAnimation()
	{
		// Custom slide up + fade animation
		this.StartCoroutine(this.SlideAndFadeIn());
	}

	private IEnumerator SlideAndFadeIn()
	{
		var canvasGroup = this.GetComponent<CanvasGroup>();
		if (canvasGroup == null)
		{
			canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}

		var rectT = this.RectT;
		var startY = rectT.anchoredPosition.y - this.slideDistance;
		var targetY = rectT.anchoredPosition.y;
		var elapsedTime = 0f;

		// Start invisible and below
		canvasGroup.alpha = 0f;
		rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x, startY);

		// Slide up and fade in
		while (elapsedTime < this.slideAndFadeDuration)
		{
			elapsedTime += Time.deltaTime;
			var progress = Mathf.Clamp01(elapsedTime / this.slideAndFadeDuration);

			// Ease out curve
			var smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

			canvasGroup.alpha = progress;
			var currentY = Mathf.Lerp(startY, targetY, smoothProgress);
			rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x, currentY);

			yield return null;
		}

		// Ensure final state
		canvasGroup.alpha = 1f;
		rectT.anchoredPosition = new Vector2(rectT.anchoredPosition.x, targetY);
	}

	protected override void OnShowingContent(object data)
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