using UnityEngine;
using TMPro;
using WordGame.Network;

public class UIScreenCompleteOverlay : UIScreen
{
	[Header("Multiplayer Score Display")]
	[SerializeField] private GameObject multiplayerScorePanel;
	[SerializeField] private TextMeshProUGUI scoreEarnedText;
	[SerializeField] private TextMeshProUGUI totalScoreText;
	[SerializeField] private TextMeshProUGUI streakText;

	public override void OnShowing(object data)
	{
		if (data is NetworkManager.ScoreUpdateData scoreData)
		{
			this.ShowScore(scoreData);
		}
	}

	private void ShowScore(NetworkManager.ScoreUpdateData scoreData)
	{
		// Show multiplayer score panel
		if (this.multiplayerScorePanel != null)
		{
			this.multiplayerScorePanel.SetActive(true);
		}

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
			this.streakText.text = $"Streak: {scoreData.streak}x";
		}

		Debug.Log($"[Complete Overlay] Score: +{scoreData.scoreGained} | Total: {scoreData.totalScore} | Streak: {scoreData.streak}");
	}
}