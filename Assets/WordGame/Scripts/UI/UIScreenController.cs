using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIScreenController : SingletonComponent<UIScreenController>
{
	#region Inspector Variables

	[SerializeField] private float			animationSpeed;
	[SerializeField] private List<UIScreen> uiScreens;

	#endregion

	#region Member Variables

	// The UIScreen Ids currently used in the game
	public const string MainScreenId			= "main";
	public const string CategoriesScreenId		= "categories";
	public const string CategoryLevelsScreenId	= "category_levels";
	public const string GameScreenId			= "game";
	public const string CompleteScreenId		= "complete";

	// The screen that is currently being shown
	private UIScreen	currentUIScreen;
	private bool		isAnimating;

	#endregion

	#region Unity Methods

	private void Start()
	{
		// Initialize and hide all the screens
		for (var i = 0; i < this.uiScreens.Count; i++)
		{
			this.uiScreens[i].Initialize();
			this.uiScreens[i].gameObject.SetActive(true);

			this.HideUIScreen(this.uiScreens[i], false, false, Tween.TweenStyle.EaseOut, null);
		}

		// Show the main screen when the app starts up
		this.Show(MainScreenId, false, false);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Shows the screen with the specified id.
	/// </summary>
	/// <param name="id">Id of UIScreen to be shown.</param>
	/// <param name="back">If set to true back then the screens will animateleft to right on the screen, if false they animate right to left.</param>
	/// <param name="animate">If set to true animate the screens will animate, if false the screens will snap into place.</param>
	/// <param name="overlay">If set to true then the current screen will not hide.</param>
	/// <param name="onTweenFinished">Called when the screens finish animating.</param>
	public void Show(string id, bool fromLeft = false, bool animate = true, bool overlay = false, Tween.TweenStyle style = Tween.TweenStyle.EaseOut, System.Action onTweenFinished = null, object data = null)
	{
		if (this.isAnimating)
		{
			return;
		}

		var uiScreen = this.GetScreenInfo(id);

		if (uiScreen != null)
		{
			this.ShowUIScreen(uiScreen, animate, fromLeft, style, onTweenFinished, data);

			// If its not an overlay screen then hide the current screen
			if (!overlay)
			{
				this.HideUIScreen(this.currentUIScreen, animate, fromLeft, style, null);

				this.currentUIScreen = uiScreen;
			}
		}
	}

	/// <summary>
	/// Hides the UI screen that was shown as an overlay
	/// </summary>
	public void HideOverlay(string id, bool fromLeft, Tween.TweenStyle style, System.Action onTweenFinished = null)
	{
		this.HideUIScreen(this.GetScreenInfo(id), true, fromLeft, style, onTweenFinished);

		if (this.currentUIScreen != null)
		{
			this.currentUIScreen.OnShowing(null);
		}
	}

	#endregion

	#region Private Methods

	private void ShowUIScreen(UIScreen uiScreen, bool animate, bool fromLeft, Tween.TweenStyle style, System.Action onTweenFinished, object data)
	{
		if (uiScreen == null)
		{
			return;
		}

		uiScreen.OnShowing(data);

		var direction = (fromLeft ? -1f : 1f);

		var fromX			= uiScreen.RectT.rect.width * direction;
		float toX			= 0;
		var fromWorldX	= Utilities.WorldWidth * direction;
		float toWorldX		= 0;

		this.isAnimating = animate;

		this.TransitionUIScreen(uiScreen, fromX, toX, fromWorldX, toWorldX, animate, style, () =>
		{
			this.isAnimating = false;

			if (onTweenFinished != null)
			{
				onTweenFinished();
			}
		});
	}

	private void HideUIScreen(UIScreen uiScreen, bool animate, bool fromBack, Tween.TweenStyle style, System.Action onTweenFinished)
	{
		if (uiScreen == null)
		{
			return;
		}

		var direction = (fromBack ? 1f : -1f);

		float fromX			= 0;
		var toX			= uiScreen.RectT.rect.width * direction;
		float fromWorldX	= 0;
		var toWorldX		= Utilities.WorldWidth * direction;

		this.TransitionUIScreen(uiScreen, fromX, toX, fromWorldX, toWorldX, animate, style, onTweenFinished);
	}

	private void TransitionUIScreen(UIScreen uiScreen, float fromX, float toX, float worldFromX, float worldToX, bool animate, Tween.TweenStyle style, System.Action onTweenFinished)
	{
		uiScreen.RectT.anchoredPosition = new Vector2(fromX, uiScreen.RectT.anchoredPosition.y);

		if (animate)
		{
			var tween = Tween.PositionX(uiScreen.RectT, style, fromX, toX, this.animationSpeed);
			
			tween.SetUseRectTransform(true);

			if (onTweenFinished != null)
			{
				tween.SetFinishCallback((tweenedObject, bundleObjects) => { onTweenFinished(); });
			}
		}
		else
		{
			uiScreen.RectT.anchoredPosition = new Vector2(toX, uiScreen.RectT.anchoredPosition.y);
		}
		
		for (var i = 0; i < uiScreen.worldObjects.Count; i++)
		{
			uiScreen.worldObjects[i].transform.position = new Vector3(worldFromX, uiScreen.worldObjects[i].transform.position.y, uiScreen.worldObjects[i].transform.position.z);

			if (animate)
			{
				Tween.PositionX(uiScreen.worldObjects[i].transform, style, worldFromX, worldToX, this.animationSpeed);
			}
			else
			{
				uiScreen.worldObjects[i].transform.position = new Vector3(worldToX, uiScreen.worldObjects[i].transform.position.y, uiScreen.worldObjects[i].transform.position.z);
			}
		}
	}

	private UIScreen GetScreenInfo(string id)
	{
		var screen = this.uiScreens.FirstOrDefault(screen => id == screen.id);
		
		if (screen == null)
		{
			Debug.LogError("[UIScreenController] No UIScreen exists with the id " + id);
		}
		
		return screen;
	}


    private void Update()
    {
        if (this.isAnimating) return;
        for (var i = 0; i < this.uiScreens.Count; i++)
        {
            float direction = this.uiScreens[i].RectT.anchoredPosition.x == 0 ? 0 :
				this.uiScreens[i].RectT.anchoredPosition.x < 0                   ? -1 : 1;
            if (direction == 0) continue;

			this.uiScreens[i].RectT.anchoredPosition = new Vector2(this.uiScreens[i].RectT.rect.width * direction, this.uiScreens[i].RectT.anchoredPosition.y);
        }

#if UNITY_ANDROID || UNITY_WSA
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            for (var i = 0; i < this.uiScreens.Count; i++)
            {
                if (this.uiScreens[i].RectT.anchoredPosition.x == 0)
                {
                    if (i == 0)
                    {
                        Application.Quit();
                    }
                    else
                    {
						this.uiScreens[i].OnBackClicked();
                        break;
                    }
                }
            }
        }
#endif
    }
    #endregion
}
