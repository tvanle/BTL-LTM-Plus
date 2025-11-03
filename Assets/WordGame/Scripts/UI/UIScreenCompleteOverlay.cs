using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordGame.Network;
using System.Collections;
using System.Collections.Generic;

public class UIScreenCompleteOverlay : UIScreen
{
	[Header("Multiplayer Score Display")]
	[SerializeField] private TextMeshProUGUI scoreEarnedText;
	[SerializeField] private TextMeshProUGUI totalScoreText;
	[SerializeField] private TextMeshProUGUI streakText;

	[Header("Cross Dissolve Animation")]
	[SerializeField] private float dissolveDuration = 1.2f;
	[SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
	[SerializeField] private Color dissolveEdgeColor = new Color(1f, 0.8f, 0.2f, 1f);
	[SerializeField] private float dissolveGlowIntensity = 2.5f;
	[SerializeField] private float dissolveEdgeWidth = 0.08f;

	private Material dissolveMaterial;
	private List<Graphic> graphicsToDissolve = new List<Graphic>();
	private CanvasGroup canvasGroup;

	private static readonly int DissolveAmountProperty = Shader.PropertyToID("_DissolveAmount");
	private static readonly int DissolveColorProperty = Shader.PropertyToID("_DissolveColor");
	private static readonly int DissolveGlowProperty = Shader.PropertyToID("_DissolveGlow");
	private static readonly int DissolveEdgeWidthProperty = Shader.PropertyToID("_DissolveEdgeWidth");

	public override void Initialize()
	{
		// Create dissolve material from shader
		Shader dissolveShader = Shader.Find("UI/FadeCrossDissolve");
		if (dissolveShader != null)
		{
			this.dissolveMaterial = new Material(dissolveShader);
			this.dissolveMaterial.SetColor(DissolveColorProperty, this.dissolveEdgeColor);
			this.dissolveMaterial.SetFloat(DissolveGlowProperty, this.dissolveGlowIntensity);
			this.dissolveMaterial.SetFloat(DissolveEdgeWidthProperty, this.dissolveEdgeWidth);
		}
		else
		{
			Debug.LogError("[UIScreenCompleteOverlay] Shader 'UI/FadeCrossDissolve' not found!");
		}

		// Get or add CanvasGroup
		this.canvasGroup = this.GetComponent<CanvasGroup>();
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}

		// Find all UI graphics to apply dissolve effect
		this.FindGraphicsToDissolve();
	}

	private void FindGraphicsToDissolve()
	{
		this.graphicsToDissolve.Clear();

		// Get all Image and TextMeshProUGUI components
		Image[] images = this.GetComponentsInChildren<Image>(true);
		foreach (Image img in images)
		{
			this.graphicsToDissolve.Add(img);
		}

		// Note: TextMeshPro doesn't support material override the same way
		// So we'll use CanvasGroup alpha for overall fade
	}

	public override void OnShowing(object data)
	{
		if (data is NetworkManager.ScoreUpdateData scoreData)
		{
			this.ShowScore(scoreData);
			this.StartCoroutine(this.PlayDissolveAnimation());
		}
	}

	private void ShowScore(NetworkManager.ScoreUpdateData scoreData)
	{
		// Display score information
		if (this.scoreEarnedText != null)
		{
			this.scoreEarnedText.text = $"{scoreData.scoreGained}";
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

	private IEnumerator PlayDissolveAnimation()
	{
		// Initialize if not already done
		if (this.dissolveMaterial == null)
		{
			this.Initialize();
		}

		// Apply dissolve material to graphics
		if (this.dissolveMaterial != null)
		{
			foreach (Graphic graphic in this.graphicsToDissolve)
			{
				if (graphic != null)
				{
					graphic.material = this.dissolveMaterial;
				}
			}
		}

		// Start with fully dissolved
		if (this.dissolveMaterial != null)
		{
			this.dissolveMaterial.SetFloat(DissolveAmountProperty, 0f);
		}

		float elapsedTime = 0f;

		// Animate dissolve
		while (elapsedTime < this.dissolveDuration)
		{
			elapsedTime += Time.deltaTime;
			float progress = elapsedTime / this.dissolveDuration;
			float curvedProgress = this.dissolveCurve.Evaluate(progress);

			// Update dissolve amount (0 = fully dissolved, 1 = fully visible)
			if (this.dissolveMaterial != null)
			{
				this.dissolveMaterial.SetFloat(DissolveAmountProperty, curvedProgress);
			}

			// Also fade in the canvas group for text
			if (this.canvasGroup != null)
			{
				this.canvasGroup.alpha = curvedProgress;
			}

			yield return null;
		}

		// Ensure final state
		if (this.dissolveMaterial != null)
		{
			this.dissolveMaterial.SetFloat(DissolveAmountProperty, 1f);
		}

		if (this.canvasGroup != null)
		{
			this.canvasGroup.alpha = 1f;
		}
	}

	private void OnDestroy()
	{
		// Clean up material
		if (this.dissolveMaterial != null)
		{
			Destroy(this.dissolveMaterial);
		}
	}
}