using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;


/// <summary>
/// Holds information about each category in the game
/// </summary>
[System.Serializable]
public class CategoryInfo
{
	public string			name;			// Name of the category, should be unique
	public string			description;	// Short description, can be anything
	public Sprite			icon;			// An icon that goes with the category
	public List<LevelInfo>	levelInfos;		// The list of levels in this category
}

/// <summary>
/// Holds information about each level in the game.
/// </summary>
[System.Serializable]
public class LevelInfo
{
	public string[]	words = null;
}

public class GameManager : SingletonComponent<GameManager>
{


	/// <summary>
	/// Holds infomation about the state of a board that is being play.
	/// </summary>
	[System.Serializable]
	public class BoardState
	{
		public enum TileState
		{
			NotUsed,
			Found,
			UsedButNotFound
		}

		public string wordBoardId;
		public int wordBoardSize;
		public string[] words;
		public bool[] foundWords;
		public TileState[] tileStates;
		public char[] tileLetters;
		public int nextHintIndex;
		public List<int[]> hintLettersShown;
		public float startTime;
		public float elapsedTime;
	}



	[Tooltip("The number of hints that a player gets when they first start the game.")]
	[SerializeField] private int				startingHints;

	[Tooltip("The GameObject from the Hierarchy that has the LetterBoard component attached to it.")]
	[SerializeField] private LetterBoard		letterBoard;

	[Tooltip("The GameObject from the Hierarchy that has the WordGrid component attached to it.")]
	[SerializeField] private WordGrid			wordGrid;

	[Tooltip("The prefab from the Project folder that has the LetterTile component attached to it.")]
	[SerializeField] private LetterTile			letterTilePrefab;

	[Tooltip("All the categories that are in the game. Levels are assigned to categories.")]
	[SerializeField] private List<CategoryInfo>	categoryInfos;

	[Tooltip("A list of levels which will be randomly choosen from when a daily puzzle starts.")]
	[SerializeField] private List<LevelInfo>	dailyPuzzles;

	[Tooltip("The sprite that appears in the top bar when a daily puzzle is being played.")]
	[SerializeField] private Sprite				dailyPuzzleIcon;

    public RewardedButton rewardedButton;



	public static string dailyPuzzleId = "Daily Puzzle";

	private CategoryInfo dailyPuzzleInfo;



	public ObjectPool						LetterTilePool				{ get; private set; }
	public int								CurrentHints				{ get; set; }
	public string							ActiveCategory				{ get; private set; }
	public int								ActiveLevelIndex			{ get; private set; }
	public int								ActiveDailyPuzzleIndex		{ get; private set; }
	public BoardState						ActiveBoardState			{ get; private set; }
	public Dictionary<string, BoardState>	SavedBoardStates			{ get; private set; }
	public Dictionary<string, bool>			CompletedLevels				{ get; private set; }
	public bool								AnimatingWord				{ get; private set; }
	public System.DateTime					NextDailyPuzzleAt			{ get; private set; }
	public bool								IsMultiplayer				{ get; set; }
	public string							CurrentRoomId				{ get; set; }
	public string							PlayerId					{ get; set; }

	public List<CategoryInfo> CategoryInfos
	{
		get
		{
			// If the list of Category Infos doesn't already contain a daily puzzle info then add it
			if (!this.categoryInfos.Contains(this.DailyPuzzleInfo))
			{
				this.categoryInfos.Add(this.DailyPuzzleInfo);
			}

			return this.categoryInfos;
		}
	}

	public CategoryInfo DailyPuzzleInfo
	{
		get
		{
			// Create a new CategoryInfo for the daily puzzles
			if (this.dailyPuzzleInfo == null)
			{
				this.dailyPuzzleInfo            = new CategoryInfo();
				this.dailyPuzzleInfo.name       = dailyPuzzleId;
				this.dailyPuzzleInfo.icon       = this.dailyPuzzleIcon;
				this.dailyPuzzleInfo.levelInfos = this.dailyPuzzles;
			}

			return this.dailyPuzzleInfo;
		}

		set => this.dailyPuzzleInfo = value;
	}



	protected override void Awake()
	{
		base.Awake();

		this.LetterTilePool   = new ObjectPool(this.letterTilePrefab.gameObject, 16, this.transform);
		this.SavedBoardStates = new Dictionary<string, BoardState>();
		this.CompletedLevels  = new Dictionary<string, bool>();

		// Initialize runtime data
		this.CurrentHints = this.startingHints;
		this.ActiveDailyPuzzleIndex = -1;
		this.NextDailyPuzzleAt = System.DateTime.Now;

		// Initialize all our important things
		this.letterBoard.Initialize();
		this.wordGrid.Initialize();

		// Setup events
		this.letterBoard.OnWordFound += this.OnWordFound;
	}



	public void StartLevel(string category, int levelIndex)
	{
		this.ActiveCategory = category;
		this.ActiveLevelIndex  = levelIndex;

		// Get the board id for the level and load the WordBoard from Resources
		var    boardId   = Utilities.FormatBoardId(this.ActiveCategory, this.ActiveLevelIndex);
		var wordBoard = Utilities.LoadWordBoard(boardId);

		if (wordBoard == null)
		{
			Debug.LogError("Could not load WordBoard with the boardId: " + boardId);
			return;
		}

		// If a saved BoardState does not already exist then create one
		if (!this.SavedBoardStates.ContainsKey(boardId))
		{
			this.SavedBoardStates.Add(boardId, this.CreateNewBoardState(wordBoard));
		}

		// Try and get a saved board state if one exists
		this.ActiveBoardState = this.SavedBoardStates[boardId];

		// Setup the display using the assigned activeBoardState
		this.SetupActiveBoard();
	}

	/// <summary>
	/// Starts the daily puzzle.
	/// </summary>
	public void StartDailyPuzzle()
	{
		if (this.dailyPuzzles.Count == 0)
		{
			return;
		}

		// Check if we need to pick a new daily puzzle
		if (this.ActiveDailyPuzzleIndex == -1 || System.DateTime.Now >= this.NextDailyPuzzleAt)
		{
			if (this.ActiveDailyPuzzleIndex != -1)
			{
				var boardId = Utilities.FormatBoardId(dailyPuzzleId, this.ActiveDailyPuzzleIndex);

				// Remove any save data for the previous daily puzzle
				this.SavedBoardStates.Remove(boardId);
				this.CompletedLevels.Remove(boardId);
			}

			// Get a new random daily puzzle level index to use
			this.ActiveDailyPuzzleIndex = Random.Range(0, this.dailyPuzzles.Count);
		}

		// Start the daily puzzle
		this.StartLevel(dailyPuzzleId, this.ActiveDailyPuzzleIndex);
	}

	/// <summary>
	/// Displays one letter in the WordGrid as a hint.
	/// </summary>
	public void DisplayNextHint()
	{
        if (this.CurrentHints == 0)
        {
            if (this.rewardedButton.IsAvailableToShow())
            {
				this.rewardedButton.OnClick();
            }
            else if (!this.rewardedButton.IsActionAvailable())
            {
                var remainTime = (int)(GameConfig.instance.rewardedVideoPeriod - CUtils.GetActionDeltaTime("rewarded_video"));
                Toast.instance.ShowMessage("Ad is not available now. Please wait " + remainTime + " seconds");
            }
            else
            {
                Toast.instance.ShowMessage("Ad is not available now. Please wait");
            }
        }
		else if (this.ActiveBoardState != null && this.CurrentHints > 0)
		{
			// Call DisplayNextHint in wordGrid, giving it the last hint index that was displayed. DisplayNextHint will return the word and letter that was displayed
			int		hintWordIndex;
			int		hintLetterIndex;
			var	hintDisplayed = this.wordGrid.DisplayNextHint(ref this.ActiveBoardState.nextHintIndex, out hintWordIndex, out hintLetterIndex);

			// Check if a hint was actually displayed
			if (hintDisplayed)
			{
				// Decrement the amount of hints
				this.CurrentHints--;

				// Update the board state so we know what letters where shown because of hints (so if the board is loaded from save we can lpace the hint words)
				this.ActiveBoardState.hintLettersShown.Add(new int[] { hintWordIndex, hintLetterIndex });

			}
		}
	}

	/// <summary>
	/// Resets the board so all the letters are back on the GameBoard and the WordGrid is only showing the hints.
	/// </summary>
	public void RestartBoard()
	{
		if (this.ActiveBoardState != null)
		{
			// Set all the words to not found on the BoardState
			this.ActiveBoardState.foundWords = this.ActiveBoardState.foundWords.Select(_ => false).ToArray();

			// Set all Found tile states back to UsedButNotFound
			this.ActiveBoardState.tileStates = this.ActiveBoardState.tileStates
				.Select(state => state == BoardState.TileState.Found ? BoardState.TileState.UsedButNotFound : state)
				.ToArray();

			this.SetupActiveBoard();
		}
	}

	/// <summary>
	/// Adds one hint to the current number of hints
	/// </summary>
	public void AddHint(int number = 1)
	{
		this.CurrentHints += number;
	}

	/// <summary>
	/// Returns true if the given category and level is completed
	/// </summary>
	public bool IsLevelCompleted(CategoryInfo categoryInfo, int levelIndex)
	{
		var boardId = Utilities.FormatBoardId(categoryInfo.name, levelIndex);
		return this.CompletedLevels.ContainsKey(boardId) && this.CompletedLevels[boardId];
	}

	/// <summary>
	/// Returns the number of completed levels for the given category
	/// </summary>
	public int GetCompletedLevelCount(CategoryInfo categoryInfo)
	{
		return categoryInfo.levelInfos
			.Select((_, index) => index)
			.Count(i => this.IsLevelCompleted(categoryInfo, i));
	}

	/// <summary>
	/// Returns the CategoryInfo with the given category name.
	/// </summary>
	public CategoryInfo GetCategoryInfo(string categoryName)
	{
		return this.CategoryInfos.FirstOrDefault(category => categoryName == category.name);
	}



	/// <summary>
	/// Called when the player finds a word
	/// </summary>
	private void OnWordFound(string word, List<LetterTile> letterTile, bool foundAllWords)
	{
		// Set all the tileStates for the found game tiles to BoardState.TileState.Found to indicate the tile has been found
		foreach (var t in letterTile)
		{
			this.ActiveBoardState.tileStates[t.TileIndex] = BoardState.TileState.Found;
		}

		// Set the flag of the word to found
		var wordIndex = this.ActiveBoardState.words.ToList().IndexOf(word);
		if (wordIndex >= 0)
		{
			this.ActiveBoardState.foundWords[wordIndex] = true;
		}

		// Cannot transition screens while a word is animating or bad things happen
		this.AnimatingWord = true;

		// Trasition the LetterTiles from the board to the word grid
		if (foundAllWords)
		{
			// If we found all the words then when the tile animation is finished call BoardComplete
			this.wordGrid.FoundWord(word, letterTile, (GameObject obj, object[] objs) => {
				this.BoardComplete();
				this.AnimatingWord = false;
			});
		}
		else
		{
			this.wordGrid.FoundWord(word, letterTile, (GameObject obj, object[] objs) => {
				this.AnimatingWord = false;
			});
		}

        // Ads removed - no interstitial on word found
	}

	/// <summary>
	/// Sets up the GameBoard and WordGrid using the current active BoardState
	/// </summary>
	private void SetupActiveBoard()
	{
		if (this.ActiveBoardState == null)
		{
			Debug.LogError("[GameManager] No activeBoardState when SetupActiveBoard was called.");

			return;
		}

		// Setup the GameBoard and WordGrid
		this.letterBoard.Setup(this.ActiveBoardState);
		this.wordGrid.Setup(this.ActiveBoardState);
	}

	/// <summary>
	/// Creates a new BoardState object using the values defined in the given WordBoard
	/// </summary>
	private BoardState CreateNewBoardState(WordBoard wordBoard)
	{
		var boardState = new BoardState();

		boardState.wordBoardId		= wordBoard.id;
		boardState.wordBoardSize	= wordBoard.size;
		boardState.words			= wordBoard.words;
		boardState.nextHintIndex	= 0;
		boardState.startTime		= Time.time;
		boardState.elapsedTime		= 0;

		boardState.foundWords = new bool[wordBoard.words.Length];
		boardState.hintLettersShown = new List<int[]>();

		boardState.tileLetters = wordBoard.wordTiles
			.Select(tile => tile.hasLetter ? tile.letter : (char)0)
			.ToArray();
		
		boardState.tileStates = wordBoard.wordTiles
			.Select(tile => tile.hasLetter ? BoardState.TileState.UsedButNotFound : BoardState.TileState.NotUsed)
			.ToArray();

		return boardState;
	}

	/// <summary>
	/// Called when the current active board is completed by the player (ie. they found the last word)
	/// </summary>
	private void BoardComplete()
	{
		var boardId     = Utilities.FormatBoardId(this.ActiveCategory, this.ActiveLevelIndex);
		var    awardNumber = 0;

		// Check if the completed category was a daily puzzle, if so check if we want to award a hint
		if (this.ActiveCategory != dailyPuzzleId)
		{
            var awardHint = !this.CompletedLevels.ContainsKey(boardId) || !this.CompletedLevels[boardId];
            awardNumber = awardHint ? GameConfig.instance.completeNormalLevelAward : 0;
		}
		else
		{
            awardNumber = GameConfig.instance.completeDailyPuzzleAward;
			// Set the next daily puzzle to start at the start of the next day
			this.NextDailyPuzzleAt = new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, System.DateTime.Now.Day).AddDays(1);
		}

		// Award hints for completing the level or daily puzzle
		this.AddHint(awardNumber);

		// Set the completed flag on the level
		this.CompletedLevels[boardId] = true;

		// The board has been completed, we no longer need to save it
		this.ActiveBoardState = null;

		// Remove the BoardState from the list of saved BoardStates
		this.SavedBoardStates.Remove(boardId);

		UIScreenController.Instance.Show(UIScreenController.CompleteScreenId, false, true, true, Tween.TweenStyle.EaseOut, this.OnCompleteScreenShown, awardNumber);
	}

	private void OnCompleteScreenShown()
	{
		var categoryInfo   = this.GetCategoryInfo(this.ActiveCategory);
		var          nextLevelIndex = this.ActiveLevelIndex + 1;

		// Check if the category has been completed or it was the daily puzzle
		if (this.ActiveCategory == dailyPuzzleId || nextLevelIndex >= categoryInfo.levelInfos.Count)
		{

			// If we completed the daily puzzle then move back to the main screen else move to the categories screen
			var screenToShow = (this.ActiveCategory == dailyPuzzleId) ? UIScreenController.MainScreenId : UIScreenController.CategoriesScreenId;

			// Set the active category to nothing
			this.ActiveCategory = "";
			this.ActiveLevelIndex  = -1;

			// Force the category screen to show right away (behind the now showing overlay)
			UIScreenController.Instance.Show(screenToShow, true, false);
		}
		else
		{
			// Start the next level
			this.StartLevel(this.ActiveCategory, nextLevelIndex);
		}

		this.WaitThenHideCompleteScreen();
	}

	protected virtual async void WaitThenHideCompleteScreen()
	{
		await Task.Delay(1000);

		UIScreenController.Instance.HideOverlay(UIScreenController.CompleteScreenId, true, Tween.TweenStyle.EaseIn);
	}



	/// <summary>
	/// Resets all game data for a new session
	/// </summary>
	public void ResetGameData()
	{
		this.CurrentHints = this.startingHints;
		this.ActiveCategory = "";
		this.ActiveLevelIndex = -1;
		this.ActiveDailyPuzzleIndex = -1;
		this.ActiveBoardState = null;
		this.SavedBoardStates.Clear();
		this.CompletedLevels.Clear();
		this.NextDailyPuzzleAt = System.DateTime.Now;
	}

	/// <summary>
	/// Gets the current game state for multiplayer synchronization
	/// </summary>
	public Dictionary<string, object> GetGameStateForSync()
	{
		var gameState = new Dictionary<string, object>();

		if (this.ActiveBoardState != null)
		{
			gameState["boardId"] = this.ActiveBoardState.wordBoardId;
			gameState["foundWords"] = this.ActiveBoardState.foundWords;
			gameState["tileStates"] = this.ActiveBoardState.tileStates;
			gameState["hints"] = this.CurrentHints;
			gameState["elapsedTime"] = Time.time - this.ActiveBoardState.startTime;
		}

		return gameState;
	}

	/// <summary>
	/// Updates game state from multiplayer sync data
	/// </summary>
	public void UpdateGameStateFromSync(Dictionary<string, object> syncData)
	{
		if (syncData.ContainsKey("hints"))
		{
			this.CurrentHints = (int)syncData["hints"];
		}

		if (this.ActiveBoardState != null && syncData.ContainsKey("foundWords"))
		{
			this.ActiveBoardState.foundWords = (bool[])syncData["foundWords"];
			this.ActiveBoardState.tileStates = (BoardState.TileState[])syncData["tileStates"];

			if (syncData.ContainsKey("elapsedTime"))
			{
				this.ActiveBoardState.elapsedTime = (float)syncData["elapsedTime"];
			}

			// Refresh the display
			this.SetupActiveBoard();
		}
	}

	/// <summary>
	/// Gets the elapsed time for the current board
	/// </summary>
	public float GetElapsedTime()
	{
		if (this.ActiveBoardState != null)
		{
			return Time.time - this.ActiveBoardState.startTime + this.ActiveBoardState.elapsedTime;
		}
		return 0f;
	}


    private void OnApplicationPause(bool pause)
    {
        // Ads removed - no interstitial on app resume
    }
}