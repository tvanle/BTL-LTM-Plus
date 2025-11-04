using UnityEngine;
using TMPro;
using WordGame.Network;
using System.Collections;

public class UIScreenCompleteOverlay : UIScreen
{
	[Header("Multiplayer Score Display")] 
	[SerializeField] private Transform brainIcon;
	[SerializeField] private TextMeshProUGUI scoreEarnedText;
	[SerializeField] private TextMeshProUGUI totalScoreText;
	[SerializeField] private TextMeshProUGUI streakText;

	[Header("Animation Settings")]
	[SerializeField] private float scaleBounceDuration = 0.6f;
	[SerializeField] private float itemDelay = 0.1f; // Delay giữa các items
	[SerializeField] private AnimationCurve scaleBounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1.2f);

	private CanvasGroup canvasGroup;
	private RectTransform rectTransform;

	public override void Initialize()
	{
		base.Initialize();

		this.canvasGroup = this.GetComponent<CanvasGroup>();
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}

		this.rectTransform = this.GetComponent<RectTransform>();
	}

	protected override void PlayShowAnimation()
	{
		// Custom staggered scale animation for complete overlay
		this.StartCoroutine(this.StaggeredScaleAnimation());
	}

	private IEnumerator StaggeredScaleAnimation()
	{
		// Ensure canvas group exists
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.GetComponent<CanvasGroup>();
			if (this.canvasGroup == null)
			{
				this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
			}
		}

		// Start visible
		this.canvasGroup.alpha = 1f;

		// Collect all elements to animate
		var elementsToAnimate = new Transform[]
		{
			this.brainIcon,
			this.scoreEarnedText?.transform,
			this.totalScoreText?.transform,
			this.streakText?.transform
		};

		// Hide all elements initially
		foreach (var element in elementsToAnimate)
		{
			if (element != null)
			{
				element.localScale = Vector3.zero;
			}
		}

		// Animate each element with stagger
		foreach (var element in elementsToAnimate)
		{
			if (element != null)
			{
				this.StartCoroutine(this.ScaleBounceElement(element));
				yield return new WaitForSeconds(this.itemDelay);
			}
		}
	}

	private IEnumerator ScaleBounceElement(Transform element)
	{
		var elapsedTime = 0f;

		while (elapsedTime < this.scaleBounceDuration)
		{
			elapsedTime += Time.deltaTime;
			var progress = Mathf.Clamp01(elapsedTime / this.scaleBounceDuration);

			// Use animation curve for bounce effect
			var curveValue = this.scaleBounceCurve.Evaluate(progress);

			// Overshoot then settle
			float scale;
			if (progress < 0.7f)
			{
				// Bounce up to 1.2
				scale = Mathf.Lerp(0f, 1.2f, progress / 0.7f);
			}
			else
			{
				// Settle to 1.0
				scale = Mathf.Lerp(1.2f, 1.0f, (progress - 0.7f) / 0.3f);
			}

			element.localScale = Vector3.one * scale;

			yield return null;
		}

		// Ensure final scale
		element.localScale = Vector3.one;
	}

	protected override void OnShowingContent(object data)
	{
		if (data is NetworkManager.ScoreUpdateData scoreData)
		{
			this.ShowScore(scoreData);
		}
	}

	private void ShowScore(NetworkManager.ScoreUpdateData scoreData)
	{
		if (this.scoreEarnedText != null)
		{
			this.scoreEarnedText.text = $"+{scoreData.scoreGained}";
		}

		if (this.totalScoreText != null)
		{
			this.totalScoreText.text = $"Total Score: {scoreData.totalScore}";
		}

		if (this.streakText != null)
		{
			this.streakText.text = $"Streak: {scoreData.streak}x";
		}

		Debug.Log($"[Complete Overlay] Score: +{scoreData.scoreGained} | Total: {scoreData.totalScore} | Streak: {scoreData.streak}");
	}
}
