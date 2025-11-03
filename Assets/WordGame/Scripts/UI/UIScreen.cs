using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class UIScreen : MonoBehaviour
{

	public string			id;
	public List<GameObject>	worldObjects;



	public RectTransform RectT { get { return this.gameObject.GetComponent<RectTransform>(); } }



	public virtual void Initialize()
	{

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
		// Default animation implementation
		// Can be overridden by child screens for custom animations
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
