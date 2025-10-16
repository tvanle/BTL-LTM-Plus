using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIScreenController : SingletonComponent<UIScreenController>
{
	[SerializeField] private List<UIScreen> uiScreens;



	// The UIScreen Ids currently used in the game
	public const string MainScreenId			= "main";
	public const string CategoriesScreenId		= "categories";
	public const string CategoryLevelsScreenId	= "category_levels";
	public const string GameScreenId			= "game";
	public const string CompleteScreenId		= "complete";

	public const string MultiplayerMenuScreenId	= "multiplayer_menu";
	public const string MultiplayerRoomScreenId	= "multiplayer_room";
	public const string LeaderboardScreenId		= "leaderboard";

	// The screen that is currently being shown
	private UIScreen	currentUIScreen;



	private void Start()
	{
		// Initialize and hide all the screens
		for (var i = 0; i < this.uiScreens.Count; i++)
		{
			this.uiScreens[i].Initialize();
			this.uiScreens[i].gameObject.SetActive(false);
		}

		// Show the multiplayer menu screen when the app starts up
		this.Show(MultiplayerMenuScreenId, false, false);
	}



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
		var uiScreen = this.GetScreenInfo(id);

		if (uiScreen != null)
		{
			// If not overlay, deactivate all other screens
			if (!overlay)
			{
				for (var i = 0; i < this.uiScreens.Count; i++)
				{
					if (this.uiScreens[i] != uiScreen)
					{
						this.uiScreens[i].gameObject.SetActive(false);
					}
				}

				this.currentUIScreen = uiScreen;
			}

			// Activate and show the target screen
			uiScreen.gameObject.SetActive(true);
			this.ShowUIScreen(uiScreen, animate, fromLeft, style, onTweenFinished, data);
		}
	}

	/// <summary>
	/// Hides the UI screen that was shown as an overlay
	/// </summary>
	public void HideOverlay(string id, bool fromLeft, Tween.TweenStyle style, System.Action onTweenFinished = null)
	{
		var screen = this.GetScreenInfo(id);
		if (screen != null)
		{
			this.HideUIScreen(screen, true, fromLeft, style, () =>
			{
				screen.gameObject.SetActive(false);
				onTweenFinished?.Invoke();
			});
		}

		if (this.currentUIScreen != null)
		{
			this.currentUIScreen.OnShowing(null);
		}
	}

	/// <summary>
	/// Hides the overlay instantly without animation
	/// </summary>
	public void HideOverlayInstant(string id)
	{
		var screen = this.GetScreenInfo(id);
		if (screen != null)
		{
			this.HideUIScreen(screen, false, false, Tween.TweenStyle.EaseOut, null);
			screen.gameObject.SetActive(false);
		}

		if (this.currentUIScreen != null)
		{
			this.currentUIScreen.OnShowing(null);
		}
	}



	private void ShowUIScreen(UIScreen uiScreen, bool animate, bool fromLeft, Tween.TweenStyle style, System.Action onTweenFinished, object data)
	{
		if (uiScreen == null)
		{
			return;
		}

		uiScreen.OnShowing(data);

		// Reset position to center (0,0)
		uiScreen.RectT.anchoredPosition = new Vector2(0, uiScreen.RectT.anchoredPosition.y);

		// Reset world objects position
		for (var i = 0; i < uiScreen.worldObjects.Count; i++)
		{
			uiScreen.worldObjects[i].transform.position = new Vector3(0, uiScreen.worldObjects[i].transform.position.y, uiScreen.worldObjects[i].transform.position.z);
		}

		onTweenFinished?.Invoke();
	}

	private void HideUIScreen(UIScreen uiScreen, bool animate, bool fromBack, Tween.TweenStyle style, System.Action onTweenFinished)
	{
		if (uiScreen == null)
		{
			return;
		}

		// No animation needed, just invoke callback
		onTweenFinished?.Invoke();
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
   //      if (this.isAnimating) return;
   //      for (var i = 0; i < this.uiScreens.Count; i++)
   //      {
   //          float direction = this.uiScreens[i].RectT.anchoredPosition.x == 0 ? 0 :
			// 	this.uiScreens[i].RectT.anchoredPosition.x < 0                   ? -1 : 1;
   //          if (direction == 0) continue;
   //
			// this.uiScreens[i].RectT.anchoredPosition = new Vector2(this.uiScreens[i].RectT.rect.width * direction, this.uiScreens[i].RectT.anchoredPosition.y);
   //      }

#if UNITY_ANDROID || UNITY_WSA
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            for (var i = 0; i < this.uiScreens.Count; i++)
            {
                if (this.uiScreens[i].gameObject.activeSelf)
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
}
