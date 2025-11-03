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
	[SerializeField] private float fadeInDuration = 0.5f;
	[SerializeField] private float scaleBounceDuration = 0.6f;
	[SerializeField] private AnimationCurve scaleBounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1.2f);

	private CanvasGroup canvasGroup;
	private RectTransform rectTransform;

	public override void Initialize()
	{
		this.canvasGroup = this.GetComponent<CanvasGroup>();
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}

		this.rectTransform = this.GetComponent<RectTransform>();
	}

	protected override void OnShowingContent(object data)
	{
		if (data is NetworkManager.ScoreUpdateData scoreData)
		{
			this.ShowScore(scoreData);
			this.StartCoroutine(this.PlayShowAnimation());
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

	private IEnumerator PlayShowAnimation()
	{
		if (this.canvasGroup == null || this.rectTransform == null)
		{
			this.Initialize();
		}

		// Start invisible and small
		this.canvasGroup.alpha = 0f;
		this.rectTransform.localScale = Vector3.zero;

		float elapsedTime = 0f;

		// Fade in and scale up with bounce
		while (elapsedTime < Mathf.Max(this.fadeInDuration, this.scaleBounceDuration))
		{
			elapsedTime += Time.deltaTime;

			// Fade in alpha
			if (elapsedTime < this.fadeInDuration)
			{
				this.canvasGroup.alpha = elapsedTime / this.fadeInDuration;
			}
			else
			{
				this.canvasGroup.alpha = 1f;
			}

			// Scale with bounce curve
			if (elapsedTime < this.scaleBounceDuration)
			{
				float scaleProgress = elapsedTime / this.scaleBounceDuration;
				float scaleValue = this.scaleBounceCurve.Evaluate(scaleProgress);
				this.rectTransform.localScale = Vector3.one * scaleValue;
			}
			else
			{
				this.rectTransform.localScale = Vector3.one;
			}

			yield return null;
		}

		// Ensure final state
		this.canvasGroup.alpha = 1f;
		this.rectTransform.localScale = Vector3.one;
	}
}
