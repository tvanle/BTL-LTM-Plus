using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LetterTile : MonoBehaviour
{

	[SerializeField] private Image 	backgroundImage;
	[SerializeField] private Text 	letterText;
	[SerializeField] private Color 	backgroundNormalColor;
	[SerializeField] private Color 	backgroundSelectedColor;
	[SerializeField] private Color 	letterNormalColor;
	[SerializeField] private Color 	letterSelectedColor;
	[SerializeField] private Sprite normalSprite;
	[SerializeField] private Sprite selectedSprite;
	[SerializeField] private float scaleAmount = 1.15f;
	[SerializeField] private float scaleDuration = 0.15f;

	private Coroutine scaleCoroutine;
	private Vector3 originalScale;
	private bool isOriginalScaleCaptured;



	public Text LetterText => this.letterText;
	public int  TileIndex  { get; set; }
	public bool Selected   { get; set; }
	public bool Found      { get; set; }
	public char Letter     { get; set; }

	private void OnDisable()
	{
		// Reset the flag when tile is returned to pool so next time it's reused,
		// it will capture the new scale set by LetterBoard
		isOriginalScaleCaptured = false;
	}

	public void SetSelected(bool selected)
	{
		this.Selected = selected;

		// Capture the original scale on first selection (after LetterBoard has set the scale)
		if (!isOriginalScaleCaptured && selected)
		{
			originalScale = transform.localScale;
			isOriginalScaleCaptured = true;
		}

		this.backgroundImage.sprite = selected ? this.selectedSprite : this.normalSprite;
		this.backgroundImage.color  = selected ? this.backgroundSelectedColor : this.backgroundNormalColor;
		this.letterText.color          = selected ? this.letterSelectedColor : this.letterNormalColor;

		if (selected)
		{
			if (scaleCoroutine != null)
			{
				StopCoroutine(scaleCoroutine);
			}
			scaleCoroutine = StartCoroutine(ScaleAnimation());
		}
		else
		{
			if (scaleCoroutine != null)
			{
				StopCoroutine(scaleCoroutine);
			}

			// Only reset to original scale if we've captured it
			if (isOriginalScaleCaptured)
			{
				transform.localScale = originalScale;
			}
		}
	}

	private IEnumerator ScaleAnimation()
	{
		float halfDuration = scaleDuration / 2f;
		float elapsed = 0f;
		Vector3 targetScale = originalScale * scaleAmount;

		// Scale up
		while (elapsed < halfDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / halfDuration;
			transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
			yield return null;
		}

		elapsed = 0f;

		// Scale down back to origin
		while (elapsed < halfDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / halfDuration;
			transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
			yield return null;
		}

		transform.localScale = originalScale;
	}

}