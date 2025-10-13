using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using WordGame.Network;


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

	// Scoring system properties
	public int								CurrentScore				{ get; private set; }
	public int								CurrentStreak				{ get; private set; }
	private bool							levelCompleted;

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

		// Check if previous level was completed, if not reset streak
		if (this.ActiveBoardState != null && !this.levelCompleted)
		{
			this.CurrentStreak = 0;
			Debug.Log("[Scoring] Previous level not completed. Streak reset!");
		}

		// Reset scoring variables for new game (not for multiplayer levels)
		if (this.ActiveBoardState == null)
		{
			this.CurrentScore = 0;
			this.CurrentStreak = 0;
		}

		// Mark level as not completed yet
		this.levelCompleted = false;

		// Get the board id for the level and load the WordBoard from Resources
		var boardId = Utilities.FormatBoardId(this.ActiveCategory, this.ActiveLevelIndex);
		var wordBoard = Utilities.LoadWordBoard(boardId);

		if (wordBoard == null)
		{
			Debug.LogError("Could not load WordBoard with the boardId: " + boardId);
			return;
		}

		// Always create a fresh board state for multiplayer
		this.ActiveBoardState = this.CreateNewBoardState(wordBoard);

		// Setup the display using the assigned activeBoardState
		this.SetupActiveBoard();
	}

	/// <summary>
	/// Gets a random category and level for the server to use when "Random" is selected
	/// </summary>
	public (string category, int level) GetRandomLevel()
	{
		// Get all categories except daily puzzle
		var validCategories = this.categoryInfos.Where(c => c.name != dailyPuzzleId).ToList();
		if (validCategories.Count == 0)
			return ("Category 1", 0); // Default fallback

		var randomCategory = validCategories[Random.Range(0, validCategories.Count)];
		var randomLevelIndex = Random.Range(0, randomCategory.levelInfos.Count);

		return (randomCategory.name, randomLevelIndex);
	}

	/// <summary>
	/// For backward compatibility - redirects to GetRandomLevel
	/// </summary>
	public void StartDailyPuzzle()
	{
		var (category, level) = GetRandomLevel();
		this.StartLevel(category, level);
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
	/// Returns the CategoryInfo with the given category name (case-insensitive).
	/// </summary>
	public CategoryInfo GetCategoryInfo(string categoryName)
	{
		// Case-insensitive comparison to handle "Category 1" vs "CATEGORY 1"
		return this.CategoryInfos.FirstOrDefault(category =>
			string.Equals(categoryName, category.name, System.StringComparison.OrdinalIgnoreCase));
	}



	/// <summary>
	/// Called when the player finds a word
	/// </summary>
	private void OnWordFound(string word, List<LetterTile> letterTile, bool foundAllWords)
	{
		// Handle correct answer scoring
		this.HandleCorrectAnswer();

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
	private async void BoardComplete()
	{
		// Mark level as completed (so streak won't be reset on next level)
		this.levelCompleted = true;

		// Calculate time taken
		var timeTaken = this.GetElapsedTime();

		// Send level completed to server
		await NetworkManager.Instance.LevelCompleted((int)timeTaken);

		// Use the calculated score from our scoring system

		Debug.Log($"[Level Complete] Final Score: {this.CurrentScore} | Time: {timeTaken}s | Streak: {this.CurrentStreak}");

		// Show complete overlay with score - wait for server instruction
		UIScreenController.Instance.Show(UIScreenController.CompleteScreenId, false, true, true, Tween.TweenStyle.EaseOut, null, this.CurrentScore);

		// Clear board state
		this.ActiveBoardState = null;
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

	/// <summary>
	/// Calculates score for a correct answer based on speed and streak
	/// Formula: Base * (Speed factor + Streak multiplier)
	/// </summary>
	private int CalculateCorrectAnswerScore(float levelDuration = 60f)
	{
		const int BASE_SCORE = 1000;
		const float MIN_SPEED_FACTOR = 0.5f;
		const float MAX_SPEED_FACTOR = 1.0f;
		const float STREAK_BONUS_PER_STREAK = 0.1f;
		const float MAX_STREAK_MULTIPLIER = 1.5f;

		// Calculate speed factor based on time remaining (0.5 to 1.0)
		var elapsedTime = this.GetElapsedTime();
		var timeRemaining = Mathf.Max(0, levelDuration - elapsedTime);
		var percentTimeRemaining = timeRemaining / levelDuration;
		var speedFactor = Mathf.Lerp(MIN_SPEED_FACTOR, MAX_SPEED_FACTOR, percentTimeRemaining);

		// Calculate streak multiplier (0.1 per streak, max 1.5)
		var streakMultiplier = Mathf.Min(this.CurrentStreak * STREAK_BONUS_PER_STREAK, MAX_STREAK_MULTIPLIER);

		// Calculate total score
		var totalMultiplier = speedFactor + streakMultiplier;
		var scoreGained = Mathf.RoundToInt(BASE_SCORE * totalMultiplier);

		return scoreGained;
	}

	/// <summary>
	/// Handles correct answer: adds score and increases streak
	/// </summary>
	private void HandleCorrectAnswer()
	{
		var scoreGained = this.CalculateCorrectAnswerScore();
		this.CurrentScore += scoreGained;
		this.CurrentStreak++;

		Debug.Log($"[Scoring] Correct! +{scoreGained} points | Streak: {this.CurrentStreak} | Total Score: {this.CurrentScore}");
	}


    private void OnApplicationPause(bool pause)
    {
        // Ads removed - no interstitial on app resume
    }
}