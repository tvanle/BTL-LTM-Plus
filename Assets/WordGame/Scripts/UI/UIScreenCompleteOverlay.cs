using UnityEngine;
using TMPro;
using WordGame.Network;
using System.Collections;

public class UIScreenCompleteOverlay : UIScreen
{
	[Header("Multiplayer Score Display")]
	[SerializeField] private TextMeshProUGUI scoreEarnedText;
	[SerializeField] private TextMeshProUGUI totalScoreText;
	[SerializeField] private TextMeshProUGUI streakText;

	[Header("Animation Settings")]
	[SerializeField] private float fadeDuration = 0.3f;
	[SerializeField] private float scaleDuration = 0.5f;
	[SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

	private CanvasGroup canvasGroup;
	private RectTransform containerTransform;

	public override void Initialize()
	{
		// Get or add CanvasGroup for fade effect
		this.canvasGroup = this.GetComponent<CanvasGroup>();
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}

		// Find the Container transform for scale animation
		Transform container = this.transform.Find("Container");
		if (container != null)
		{
			this.containerTransform = container.GetComponent<RectTransform>();
		}
	}

	public override void OnShowing(object data)
	{
		if (data is NetworkManager.ScoreUpdateData scoreData)
		{
			this.ShowScore(scoreData);
			this.StartCoroutine(this.PlayShowAnimation());
		}
	}

	private void ShowScore(NetworkManager.ScoreUpdateData scoreData)
	{
		// Display score information
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
			this.streakText.text = $"COMPLETED Streak: {scoreData.streak}x";
		}

		Debug.Log($"[Complete Overlay] Score: +{scoreData.scoreGained} | Total: {scoreData.totalScore} | Streak: {scoreData.streak}");
	}

	private IEnumerator PlayShowAnimation()
	{
		// Initialize CanvasGroup if not already done
		if (this.canvasGroup == null)
		{
			this.Initialize();
		}

		// Start with invisible and scaled down
		this.canvasGroup.alpha = 0f;
		if (this.containerTransform != null)
		{
			this.containerTransform.localScale = Vector3.zero;
		}

		float elapsedTime = 0f;

		// Animate both fade and scale simultaneously
		while (elapsedTime < Mathf.Max(this.fadeDuration, this.scaleDuration))
		{
			elapsedTime += Time.deltaTime;

			// Fade in
			if (elapsedTime < this.fadeDuration)
			{
				float fadeProgress = elapsedTime / this.fadeDuration;
				this.canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeProgress);
			}
			else
			{
				this.canvasGroup.alpha = 1f;
			}

			// Scale up with bounce effect
			if (this.containerTransform != null && elapsedTime < this.scaleDuration)
			{
				float scaleProgress = elapsedTime / this.scaleDuration;
				float curveValue = this.scaleCurve.Evaluate(scaleProgress);

				// Add elastic overshoot effect
				float elasticValue = curveValue;
				if (scaleProgress > 0.7f)
				{
					float overshoot = Mathf.Sin((scaleProgress - 0.7f) * Mathf.PI * 3f) * 0.1f;
					elasticValue += overshoot;
				}

				this.containerTransform.localScale = Vector3.one * Mathf.Lerp(0f, 1f, elasticValue);
			}
			else if (this.containerTransform != null)
			{
				this.containerTransform.localScale = Vector3.one;
			}

			yield return null;
		}

		// Ensure final state
		this.canvasGroup.alpha = 1f;
		if (this.containerTransform != null)
		{
			this.containerTransform.localScale = Vector3.one;
		}
	}
}