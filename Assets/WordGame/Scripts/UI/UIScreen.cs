using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class UIScreen : MonoBehaviour
{

	public string			id;
	public List<GameObject>	worldObjects;

	[Header("Show Animation Settings")]
	[SerializeField] protected bool enableShowAnimation = true;
	[SerializeField] protected float fadeDuration = 0.15f;

	private Coroutine currentAnimationCoroutine;
	protected CanvasGroup canvasGroup;


	public RectTransform RectT { get { return this.gameObject.GetComponent<RectTransform>(); } }



	public virtual void Initialize()
	{
		// Ensure CanvasGroup exists for fade animation
		this.canvasGroup = this.GetComponent<CanvasGroup>();
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
		}
	}

	// Template Method Pattern: Sealed method controls the flow
	public void OnShowing(object data)
	{
		// 1. Play show animation first (common for all screens)
		this.PlayShowAnimation();

		// 2. Then execute screen-specific logic
		this.OnShowingContent(data);
	}

	// Virtual method for show animation - screens can override to customize
	protected virtual void PlayShowAnimation()
	{
		AudioManager.Instance.PlayScreenTransition();
		// Stop any previous animation
		if (this.currentAnimationCoroutine != null)
		{
			this.StopCoroutine(this.currentAnimationCoroutine);
		}

		if (this.enableShowAnimation)
		{
			this.currentAnimationCoroutine = this.StartCoroutine(this.AnimateFade());
		}
		else
		{
			// Ensure alpha is at target value
			if (this.canvasGroup != null)
			{
				this.canvasGroup.alpha = 1f;
			}
		}
	}

	private IEnumerator AnimateFade()
	{
		if (this.canvasGroup == null)
		{
			this.canvasGroup = this.GetComponent<CanvasGroup>();
			if (this.canvasGroup == null)
			{
				this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
			}
		}

		var elapsedTime = 0f;

		// Start invisible
		this.canvasGroup.alpha = 0f;

		// Fade in quickly
		while (elapsedTime < this.fadeDuration)
		{
			elapsedTime += Time.deltaTime;
			var progress = Mathf.Clamp01(elapsedTime / this.fadeDuration);
			this.canvasGroup.alpha = progress;

			yield return null;
		}

		// Ensure final alpha is exact
		this.canvasGroup.alpha = 1f;

		this.currentAnimationCoroutine = null;
	}

	// Virtual method for screen-specific content initialization
	// Screens should override this instead of OnShowing
	protected virtual void OnShowingContent(object data)
	{

	}

    public virtual void OnBackClicked()
    {

    }

}
