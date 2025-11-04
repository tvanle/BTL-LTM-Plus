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
