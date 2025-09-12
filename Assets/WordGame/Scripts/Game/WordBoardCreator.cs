using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

/// <summary>
/// WordBoardCreator handles taking a array of string words and creating a WordBoard out of them where all letters of the words
/// are connected either horizontally, vertically, or diagnoally from end to end.
///
/// When one of the StartCreatingBoard methods is called, a new Thread is created in order to process the board in the background
/// and not cause the game to lag. The Board could end up taking a couple seconds to successfully complete depending on where it
/// chooses to randomly place the words. When it is done, it sets the boardState enum to either DoneSuccess (indicating the
/// Board has finished processing and successfully placed all words on the board) or DoneFailed (indicating that it is impossible
/// to place the given words on the Board with the given settings). In practise I have never gotten a DoneFailed state, but I
/// have to put it in.
///
/// Once the board enters the DoneComplete or DoneFailed state the OnBoardFinished callback will be called on the Main Thread with
/// the finished Board.
/// </summary>
public class WordBoardCreator : MonoBehaviour
{
	#region Data Classes

	private class ActiveThread
	{
		public string			id;
		public Thread			thread;
		public WordBoard		board;
		public OnBoardFinished	callback;
	}

	#endregion

	#region Delegates

	public delegate void OnBoardFinished(WordBoard board);

	#endregion

	#region Member Variables

	private readonly List<ActiveThread> activeThreads = new List<ActiveThread>();

	#endregion

	#region Properties

	/// <summary>
	/// Gets an array of strings which are the currently generating board ids.
	/// </summary>
	public string[] CurrentlyCreatingBoards
	{
		get
		{
			var boardIds = new string[this.activeThreads.Count];

			for (var i = 0; i < this.activeThreads.Count; i++)
			{
				boardIds[i] = this.activeThreads[i].id;
			}

			return boardIds;
		}
	}

	#endregion

	#region Unity Methods

	public void Update()
	{
		// Checks for completed boards
		for (var i = this.activeThreads.Count - 1; i >= 0; i--)
		{
			switch (this.activeThreads[i].board.boardState)
			{
				case WordBoard.BoardState.DoneSuccess:
				case WordBoard.BoardState.DoneFailed:
				{
					var       wordBoard = this.activeThreads[i].board;
					var callback  = this.activeThreads[i].callback;

					this.activeThreads.RemoveAt(i);

					callback(wordBoard);

					break;
				}
				case WordBoard.BoardState.Restart:
				{
					var          id           = this.activeThreads[i].id;
					var        words        = this.activeThreads[i].board.words;
					var            restartTimer = this.activeThreads[i].board.restartTime;
					var callback     = this.activeThreads[i].callback;

					this.AbortBoardCreation(id);

					this.StartCreatingBoard(id, words, callback, restartTimer);

					break;
				}
			}
		}
	}

	private void OnDestroy()
	{
		for (var i = this.activeThreads.Count - 1; i >= 0; i--)
		{
			this.AbortBoardCreation(this.activeThreads[i].id);
		}
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Starts the creation of a new board.
	/// </summary>
	public bool StartCreatingBoard(string id, string[] words, OnBoardFinished callback)
	{
		return this.StartCreatingBoard(id, words, callback, Random.Range(0, int.MaxValue), long.MaxValue);
	}

	/// <summary>
	/// Starts the creation of a new board, if it takes longer than restartTimer (in milliseconds) then it will abort and
	/// try again. Most of the time board creation takes less than a couple seconds but if there are alot of long words then the
	/// board can get itself in a state that takes a long time to find a fit for all the words. The easiest way to reslove
	/// this issue is to restart the board creation.
	/// </summary>
	public bool StartCreatingBoard(string id, string[] words, OnBoardFinished callback, long restartTimer)
	{
		return this.StartCreatingBoard(id, words, callback, Random.Range(0, int.MaxValue), restartTimer);
	}

	/// <summary>
	/// Starts the creation of a board using the given random seed number. Passing the same seed will generate the same
	/// board every time.
	/// </summary>
	public bool StartCreatingBoard(string id, string[] words, int randomNumberSeed, OnBoardFinished callback)
	{
		return this.StartCreatingBoard(id, words, callback, randomNumberSeed, long.MaxValue);
	}

	/// <summary>
	/// Determines whether this instance is creating a board the specified id.
	/// </summary>
	public bool IsCreatingBoard(string id)
	{
		for (var i = 0; i < this.activeThreads.Count; i++)
		{
			if (id == this.activeThreads[i].id)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Aborts the board creation for board with the specified id.
	/// </summary>
	public void AbortBoardCreation(string id)
	{
		for (var i = 0; i < this.activeThreads.Count; i++)
		{
			if (id == this.activeThreads[i].id)
			{
				this.activeThreads[i].thread.Abort();
				this.activeThreads.RemoveAt(i);

				return;
			}
		}

		UnityEngine.Debug.LogWarningFormat("Could not abort board creation because the id \"{0}\" does not exist.", id);
	}

	/// <summary>
	/// Aborts all board creation threads.
	/// </summary>
	public void AbortAll()
	{
		for (var i = 0; i < this.activeThreads.Count; i++)
		{
			this.AbortBoardCreation(this.activeThreads[i].id);
		}
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Starts a new thread that will process a board in the background. The OnBoardCreated callback will be called when the board is complete
	/// </summary>
	private bool StartCreatingBoard(string id, string[] words, OnBoardFinished callback, int randomNumberSeed, long restartTime)
	{
		if (this.IsCreatingBoard(id))
		{
			UnityEngine.Debug.LogErrorFormat("Could not start board creation because the id \"{0}\" already exist.", id);

			return false;
		}

		// Create a new Board object
		var board		= new WordBoard();
		board.id			= id;
		board.words			= words;
		board.randSeed		= randomNumberSeed;
		board.rand			= new System.Random(randomNumberSeed);
		board.boardState	= WordBoard.BoardState.Processing;
		board.stopwatch		= new System.Diagnostics.Stopwatch();
		board.restartTime	= restartTime;

		// Create a new ActiveThread object that will hold information about the thread/board
		var activeThread	= new ActiveThread
		{
			id       = id,
			board    = board,
			callback = callback,
			// Create the new Thread to start processing the Board
			thread   = new Thread(new ThreadStart(() => this.ProcessBoard(board, words))),
		};

		activeThread.thread.Start();

		this.activeThreads.Add(activeThread);

		return true;
	}

	/// <summary>
	/// Creates a new Board and initializes it using the array of words, then calls the methods that starts the board creation algorithm.
	/// </summary>
	private void ProcessBoard(WordBoard board, string[] words)
	{
		var letterCount = 0;

		// Get the total number of letters from all words
		for (var i = 0; i < words.Length; i++)
		{
			letterCount += words[i].Length;
		}

		// Get the core board size and actual board size.
		var coreBoardSize	= Mathf.FloorToInt(Mathf.Sqrt(letterCount));
		var boardSize		= Mathf.CeilToInt(Mathf.Sqrt(letterCount));

		// Create a new Board, set its size, and intialize a new array of WordTiles
		board.size		= boardSize;
		board.wordTiles	= new WordBoard.WordTile[boardSize * boardSize];

		// Instantiate all the WordTiles
		for (var i = 0; i < boardSize * boardSize; i++)
		{
			board.wordTiles[i] = new WordBoard.WordTile();
		}

		// Set all the core board indexes to used
		for (var i = 0; i < coreBoardSize; i++)
		{
			for (var j = 0; j < coreBoardSize; j++)
			{
				board.wordTiles[i * boardSize + j].used = true;
			}
		}

		board.stopwatch.Start();

		// Start the algorithm
		if (words.Length == 0 || this.CompleteBoard(board, letterCount - coreBoardSize * coreBoardSize, words))
		{
			board.boardState = WordBoard.BoardState.DoneSuccess;
		}
		else
		{
			board.boardState = WordBoard.BoardState.DoneFailed;
		}

		board.stopwatch.Stop();
	}

	/// <summary>
	/// Assigns any extra tiles that need to be assigned to "used" on the board then tries to placed the words on the board.
	/// This is part of a backtracking algorithm that picks extraTilesToPick number of un-used tiles and makes sets them as used,
	/// it then calls PlaceLetters in an attempt to place all the words on the board. If PlaceLetters returns false then the
	/// assignment of "used" tiles is invalid (ie. cannot place all words on the board) so it picks a different arrangement of
	/// "used" tiles.
	/// </summary>
	private bool CompleteBoard(WordBoard board, int extraTilesToPick, string[] words)
	{
		// If there are no more tiles we need to assigned as used then start placing words on the board
		if (extraTilesToPick == 0)
		{
			var validIndexes = new List<int>();

			for (var i = 0; i < board.wordTiles.Length; i++)
			{
				validIndexes.Add(i);
			}

			return this.PlaceLetters(board, validIndexes, words, 0, -1);
		}

		var unusedBottomIndexes	= new List<int>();
		var unusedRightIndexes	= new List<int>();

		// Gather all the indexes on the bottom most row and right most column that are not being used. We need to place a tile in one of those positions.
		for (var i = 0; i < board.size - 1; i++)
		{
			var bottomIndex	= board.size * (board.size - 1) + i;
			var rightIndex	= i * board.size + board.size - 1;

			if (!board.wordTiles[bottomIndex].used)
			{
				unusedBottomIndexes.Add(bottomIndex);
			}

			if (!board.wordTiles[rightIndex].used)
			{
				unusedRightIndexes.Add(rightIndex);
			}
		}

		var numberOfIndexes	= unusedBottomIndexes.Count + unusedRightIndexes.Count;

		for (var i = 0; i < numberOfIndexes; i++)
		{
			// Get a random index to try from the list of possible indexes
			var randIndex	= 0;
			var indexToTry	= 0;

			if (unusedBottomIndexes.Count > 0)
			{
				randIndex	= board.rand.Next(unusedBottomIndexes.Count);
				indexToTry	= unusedBottomIndexes[randIndex];

				unusedBottomIndexes.RemoveAt(randIndex);
			}
			else
			{
				randIndex	= board.rand.Next(unusedRightIndexes.Count);
				indexToTry	= unusedRightIndexes[randIndex];

				unusedRightIndexes.RemoveAt(randIndex);
			}

			// Set the bool on the board to say we are going to use this tile
			board.wordTiles[indexToTry].used = true;

			// If CompleteBoard returns true then we found a completed board
			if (this.CompleteBoard(board, extraTilesToPick - 1, words))
			{
				return true;
			}

			// We did not find a completed board with this board piece so set it back to false so we can pick another one
			board.wordTiles[indexToTry].used = false;
		}

		// Non of the pissible indexes lead to a completed board
		return false;
	}

	/// <summary>
	/// Attempts to place all letters from the first word in "words" on the board. The list "validIndexes" contains all the
	/// indexes of board.wordTiles that a letter can be placed on. The int "letterIndex" is the character in the word at words[0]
	/// that is next to be placed. The int "lastPlacedIndex" is the index in board.wordTiles of the last letter that was placed
	/// on the board (-1 if letterIndex is 0, ie. we are placing the first letter).
	///
	/// This part of the algorithm works by getting all possible places for the letter at words[0][letterIndex] can go then
	/// trying each on. If it runs out of possible places then it returns false indicating to the letter before it that it needs
	/// to pick another place. Once the full word is place on the board it calls FillRestOfWords method to continue to the next
	/// part of the algorithm.
	/// </summary>
	private bool PlaceLetters(WordBoard board, List<int> validIndexes, string[] words, int letterIndex, int lastPlacedIndex)
	{
		// If letterIndex is greater than the number of letters in the word then we are done placing this word on the board
		if (letterIndex >= words[0].Length)
		{
			// If this was the last word to place then we completed the board
			if (words.Length == 1)
			{
				return true;
			}

			// Now we try to fill in the rest of with words using the board
			return this.FillRestOfWords(board, validIndexes, words);
		}

		// Check if the amount of time that has elapsed is grater than board.restartTimer
		if (board.stopwatch.ElapsedMilliseconds >= board.restartTime)
		{
			// Signals the main thread that the thread that is processing this board needs to abort and restart
			board.boardState = WordBoard.BoardState.Restart;
		}

		// Get the list of possible board indexes where we can place the next letter of the word
		var possibleIndexes = this.GetPossibleLetterPositions(board, validIndexes, lastPlacedIndex);
		var       numberOfIndexes = possibleIndexes.Count;

		// Loop through each index, trying each one
		for (var i = 0; i < numberOfIndexes; i++)
		{
			// Get a random index to try from the list of possible indexes
			var randIndex	= board.rand.Next(possibleIndexes.Count);
			var indexToTry	= possibleIndexes[board.rand.Next(possibleIndexes.Count)];

			// Remove that index from the list of possibilities so we don't pick it again if it fails
			possibleIndexes.RemoveAt(randIndex);

			// Set the bool on the board to say we are going to use this tile
			board.wordTiles[indexToTry].hasLetter	= true;
			board.wordTiles[indexToTry].letter		= words[0][letterIndex];

			// Try and place the remaining letters on the board
			if (this.PlaceLetters(board, validIndexes, words, letterIndex + 1, indexToTry))
			{
				return true;
			}

			// We did not find a completed board with this board piece so set it back to false so we can pick another one
			board.wordTiles[indexToTry].hasLetter = false;
		}

		return false;
	}

	/// <summary>
	/// Returns all indexes of board.wordTiles that we can place a letter at from the index specified by fromTile. This will
	/// only return indexes that are in the validIndexes list.
	/// </summary>
	private List<int> GetPossibleLetterPositions(WordBoard board, List<int> validIndexes, int fromTile)
	{
		var possibleIndexes = new List<int>();

		// If fromTile is -1 then we return all indexes from validIndexes that is used and doesn't already have a letter
		if (fromTile == -1)
		{
			for (var i = 0; i < validIndexes.Count; i++)
			{
				if (board.wordTiles[validIndexes[i]].used && !board.wordTiles[validIndexes[i]].hasLetter)
				{
					possibleIndexes.Add(validIndexes[i]);
				}
			}
		}
		else
		{
			// Now we need to check the indexes that "surround" the index at fromTile so for instance we have a board:
			// _ _ _ _ _
			// _ i i i _   'f' is the tile represented by fromTile and we want to return all 'i' indexes that are
			// _ i f i _    used, don't have a letter, and exist in validIndexes.
			// _ i i i _
			// _ _ _ _ _

			var iStart	= Mathf.FloorToInt((float)fromTile / (float)board.size) - 1;
			var jStart	= (fromTile % board.size) - 1;
			var iEnd	= iStart + 3;
			var jEnd	= jStart + 3;

			// Clamp the indexes so we don't go off the board
			iStart	= (iStart < 0) ? 0 : iStart;
			jStart	= (jStart < 0) ? 0 : jStart;
			iEnd	= (iEnd > board.size) ? board.size : iEnd;
			jEnd	= (jEnd > board.size) ? board.size : jEnd;

			for (var i = iStart; i < iEnd; i++)
			{
				for (var j = jStart; j < jEnd; j++)
				{
					var tileIndex = i * board.size + j;

					if (tileIndex != fromTile && 					// Don't want to return the fromTile index
						board.wordTiles[tileIndex].used && 			// The tile needs to be used
						!board.wordTiles[tileIndex].hasLetter && 	// The tile cannot have a letter already on it
						validIndexes.Contains(tileIndex))			// The index must exist in validIndexes
					{
						possibleIndexes.Add(tileIndex);
					}
				}
			}
		}

		return possibleIndexes;
	}

	/// <summary>
	/// This part of the algorithm is called after a full word is placed on the board by PlaceLetters. It sets in motion the
	/// "sub-algorithm" that splits the board into "regions" where words can be placed then attempts to place the remaining
	/// words in those regions. If it fails to do so then that means the word that was just placed in PlaceLetters created a
	/// board in which its impossible to place the remain letters so it returns false to indicate to PlaceLetters that it needs
	/// to find a new arrangment of letters for the word it just placed.
	/// </summary>
	private bool FillRestOfWords(WordBoard board, List<int> validIndexes, string[] words)
	{
		// Make sure we set all regions to 0 and regionLocked to false on the board
		this.ResetRegions(board);

		// Fill the regions with numbers
		this.FillRegions(board, validIndexes);

		var possibleBoards = this.GetAllBoardRegionCombinations(board);

		for (var i = 0; i < possibleBoards.Count; i++)
		{
			var regionIndexes = this.GetRegionIndexes(board);

			if (regionIndexes.Count > board.words.Length - 1)
			{
				continue;
			}

			var wordRegionAssignments = new Dictionary<int, List<int>>();

			var regionSizes	= new List<int>();
			var wordSizes		= new List<int>();
			var regions		= new List<int>();
			var wordIndexes	= new List<int>();

			foreach (var pair in regionIndexes)
			{
				wordRegionAssignments.Add(pair.Key, new List<int>());
				regions.Add(pair.Key);
				regionSizes.Add(pair.Value.Count);
			}

			for (var j = 1; j < words.Length; j++)
			{
				wordIndexes.Add(j);
				wordSizes.Add(words[j].Length);
			}

			if (this.TryFitWordsIntoRegions(regionSizes, wordSizes, regions, wordIndexes, wordRegionAssignments))
			{
				var allWordsPlaced = true;

				var savedWordTiles = board.wordTiles;
				board.wordTiles = possibleBoards[i].wordTiles;

				foreach (var pair in wordRegionAssignments)
				{
					var regionWords = new string[pair.Value.Count];

					for (var j = 0; j < pair.Value.Count; j++)
					{
						regionWords[j] = words[pair.Value[j]];
					}

					if (!this.PlaceLetters(board, regionIndexes[pair.Key], regionWords, 0, -1))
					{
						allWordsPlaced = false;
						break;
					}
				}

				if (allWordsPlaced)
				{
					return true;
				}

				board.wordTiles = savedWordTiles;
			}
		}

		return false;
	}

	/// <summary>
	/// Resets the regions back to 0 and the regionLocked back to false.
	/// </summary>
	private void ResetRegions(WordBoard board)
	{
		for (var i = 0; i < board.wordTiles.Length; i++)
		{
			board.wordTiles[i].region		= 0;
			board.wordTiles[i].regionLocked	= false;
		}
	}

	/// <summary>
	/// Assignes all tiles that are used and have no letters to a region. A region is just an integer assigned to the WordTile
	/// region field. A region is "filled" by finding all tiles that connect to each other either vertically or horizontally but
	/// not diagonally.
	/// </summary>
	public void FillRegions(WordBoard board, List<int> validIndexes)
	{
		var regionCount = 1;

		for (var i = 0; i < board.wordTiles.Length; i++)
		{
			if (validIndexes.Contains(i) && this.IsUnassignedRegion(board, i))
			{
				this.FillRegion(board, i, regionCount);
				regionCount++;
			}
		}
	}

	/// <summary>
	/// Helper method for FillRegions
	/// </summary>
	private void FillRegion(WordBoard board, int index, int region)
	{
		board.wordTiles[index].region = region;

		var i = Mathf.FloorToInt((float)index / (float)board.size);
		var j = (index % board.size);

		if (i != 0)
		{
			var topIndex = (i - 1) * board.size + j;

			if (this.IsUnassignedRegion(board, topIndex))
			{
				this.FillRegion(board, topIndex, region);
			}
		}

		if (i != board.size - 1)
		{
			var bottomIndex	= (i + 1) * board.size + j;

			if (this.IsUnassignedRegion(board, bottomIndex))
			{
				this.FillRegion(board, bottomIndex, region);
			}
		}

		if (j != 0)
		{
			var leftIndex = i * board.size + (j - 1);

			if (this.IsUnassignedRegion(board, leftIndex))
			{
				this.FillRegion(board, leftIndex, region);
			}
		}

		if (j != board.size - 1)
		{
			var rightIndex = i * board.size + (j + 1);

			if (this.IsUnassignedRegion(board, rightIndex))
			{
				this.FillRegion(board, rightIndex, region);
			}
		}
	}

	/// <summary>
	/// Determines if the wordTile at index 'i' is used, has no letter, and has no region (ie region == 0)
	/// </summary>
	private bool IsUnassignedRegion(WordBoard board, int i)
	{
		return board.wordTiles[i].used && !board.wordTiles[i].hasLetter && board.wordTiles[i].region == 0;
	}

	/// <summary>
	/// Takes a single Board that has a number of regions defined in the wordTiles then returns a List of Boards whose
	/// regions have been orgninsed in such a way that they define all the ways words could be laid out.
	/// </summary>
	private List<WordBoard> GetAllBoardRegionCombinations(WordBoard board)
	{
		var possibleBoards = new List<WordBoard>();

		// Merge any regions that can be merged
		this.MergeRegions(board);

		// Get any crossroad index. A crossroad is an index where a region could split off into 2 - 4 other regions
		var crossroadIndex = this.GetAnyCrossroad(board);

		// If its -1 then no crossroads exist and this Board is complete and can be returned
		if (crossroadIndex == -1)
		{
			possibleBoards.Add(board);
		}
		else
		{
			// If there is a crossroad we need to split the board
			var cornerIndexes = this.GetCornerRegionIndexes(board, crossroadIndex);

			// If there is only 2 distinct crossroad regions
			if (cornerIndexes.Count == 2)
			{
				var crossroadRegion = board.wordTiles[crossroadIndex].region;
				var corner1Region	= board.wordTiles[cornerIndexes[0]].region;
				var corner2Region	= board.wordTiles[cornerIndexes[1]].region;

				var newBoard1 = board.Copy();
				var newBoard2 = board.Copy();
				var newBoard3 = board.Copy();

				// Setup new board 1
				newBoard1.wordTiles[crossroadIndex].region			= corner1Region;
				newBoard1.wordTiles[crossroadIndex].regionLocked	= true;
				this.ConvertRegionForCrossroad(newBoard1, corner1Region, corner1Region);		// Makes region un-mergable
				this.ConvertRegionForCrossroad(newBoard1, corner2Region, corner1Region);

				// Setup new board 2
				this.ConvertRegionForCrossroad(newBoard2, crossroadRegion, crossroadRegion);	// Makes region un-mergable
				this.ConvertRegionForCrossroad(newBoard2, corner1Region, crossroadRegion);

				// Setup new board 3
				this.ConvertRegionForCrossroad(newBoard3, crossroadRegion, crossroadRegion);	// Makes region un-mergable
				this.ConvertRegionForCrossroad(newBoard3, corner2Region, crossroadRegion);

				// Get all possible boards that can be made from the 3 new boards
				possibleBoards.AddRange(this.GetAllBoardRegionCombinations(newBoard1));
				possibleBoards.AddRange(this.GetAllBoardRegionCombinations(newBoard2));
				possibleBoards.AddRange(this.GetAllBoardRegionCombinations(newBoard3));
			}
			else
			{
				for (var i = 0; i < cornerIndexes.Count; i++)
				{
					var corner1Region = board.wordTiles[cornerIndexes[i]].region;

					for (var j = i + 1; j < cornerIndexes.Count; j++)
					{
						var corner2Region = board.wordTiles[cornerIndexes[j]].region;

						var newBoard = board.Copy();

						newBoard.wordTiles[crossroadIndex].region		= corner1Region;
						newBoard.wordTiles[crossroadIndex].regionLocked	= true;

						this.ConvertRegionForCrossroad(newBoard, corner1Region, corner1Region);	// Makes region un-mergable
						this.ConvertRegionForCrossroad(newBoard, corner2Region, corner1Region);

						possibleBoards.AddRange(this.GetAllBoardRegionCombinations(newBoard));
					}
				}
			}
		}

		return possibleBoards;
	}

	/// <summary>
	/// Merges all regions that can be merged.
	/// </summary>
	private void MergeRegions(WordBoard board)
	{
		for (var i = board.size + 1; i < board.wordTiles.Length - board.size; i++)
		{
			if (i % board.size == board.size - 1)
			{
				i += 1;

				continue;
			}

			this.MergeRegion(board, i);
		}
	}

	/// <summary>
	/// Merges the region at index i.
	/// </summary>
	private void MergeRegion(WordBoard board, int i)
	{
		var thisRegion = board.wordTiles[i].region;

		if (thisRegion == 0 || board.wordTiles[i].regionLocked)
		{
			return;
		}

		var thisI = Mathf.FloorToInt((float)i / (float)board.size);
		var thisJ = (i % board.size);

		var cornerIndexes = this.GetCornerRegionIndexes(board, i);

		// A region can be merged with another if the region has only 1 corner region or it has 2 corner regions and it is
		// a single region (only one tile in the region)
		if (cornerIndexes.Count == 2)
		{
			// Get the left, right, top, and bottom regions
			var left 	= thisI * board.size + (thisJ - 1);
			var right 	= thisI * board.size + (thisJ + 1);
			var top 	= (thisI + 1) * board.size + thisJ;
			var bottom	= (thisI - 1) * board.size + thisJ;

			// If none of those regions equal this region then it is a single region
			if (!((thisRegion == board.wordTiles[left].region) ||
				(thisRegion == board.wordTiles[right].region) ||
				(thisRegion == board.wordTiles[top].region) ||
				(thisRegion == board.wordTiles[bottom].region)))
			{
				// Need to check if the index for the region we are about to merge with is a crossroads becuase if it is
				// then we cannot merge with it. Also check if the region is locked (ie. it cannot be changed)
				if (!this.IsCrossroad(board, cornerIndexes[0]) && !board.wordTiles[cornerIndexes[0]].regionLocked)
				{
					// Convert the first corner region to this region
					this.ConvertRegion(board, board.wordTiles[cornerIndexes[0]].region, thisRegion);
				}

				// Need to check if the index for the region we are about to merge with is a crossroads becuase if it is
				// then we cannot merge with it. Also check if the region is locked (ie. it cannot be changed
				if (!this.IsCrossroad(board, cornerIndexes[1]) && !board.wordTiles[cornerIndexes[1]].regionLocked)
				{
					// Convert the second corner region to this region
					this.ConvertRegion(board, board.wordTiles[cornerIndexes[1]].region, thisRegion);
				}
			}
		}
		else if (cornerIndexes.Count == 1 && !this.IsCrossroad(board, cornerIndexes[0]) && !board.wordTiles[cornerIndexes[0]].regionLocked)
		{
			this.ConvertRegion(board, board.wordTiles[cornerIndexes[0]].region, thisRegion);
		}
	}

	/// <summary>
	/// Determines if the tile at index is a crossroad tile.
	/// </summary>
	private bool IsCrossroad(WordBoard board, int index)
	{
		// It cannot be a crossroad tile if it is on one of the edges of the board
		if ((index < board.size) ||								// Top edge
			(index >= board.wordTiles.Length - board.size) ||	// Bottom edge
			(index % board.size == 0) ||						// Left edge
			(index % board.size == board.size - 1))				// Right edge
		{
			return false;
		}

		// Get the region number
		var thisRegion = board.wordTiles[index].region;

		// If its 0 then this is not a region and therefore cannot be a crossroad
		if (thisRegion == 0)
		{
			return false;
		}

		var thisI = Mathf.FloorToInt((float)index / (float)board.size);
		var thisJ = (index % board.size);

		var cornerRegions = this.GetCornerRegionIndexes(board, index);

		// If there are 3 or 4 cornering regions then this is definitly a crossroad region
		if (cornerRegions.Count > 2)
		{
			return true;
		}

		// If there are only 2 corner regions then we need to check if thisRegion is a single region (only 1 tile in the region)
		if (cornerRegions.Count == 2)
		{
			var left 	= thisI * board.size + (thisJ - 1);
			var right 	= thisI * board.size + (thisJ + 1);
			var top 	= (thisI + 1) * board.size + thisJ;
			var bottom	= (thisI - 1) * board.size + thisJ;

			if ((thisRegion == board.wordTiles[left].region) ||
				(thisRegion == board.wordTiles[right].region) ||
				(thisRegion == board.wordTiles[top].region) ||
				(thisRegion == board.wordTiles[bottom].region))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Get a List of all the unique regions that corner the region at the specified index.
	/// </summary>
	private List<int> GetCornerRegionIndexes(WordBoard board, int index)
	{
		var thisI = Mathf.FloorToInt((float)index / (float)board.size);
		var thisJ = (index % board.size);

		var corner1Index = (thisI - 1) * board.size + (thisJ - 1);
		var corner2Index = (thisI - 1) * board.size + (thisJ + 1);
		var corner3Index = (thisI + 1) * board.size + (thisJ - 1);
		var corner4Index = (thisI + 1) * board.size + (thisJ + 1);

		var corner1Region = board.wordTiles[corner1Index].region;
		var corner2Region = board.wordTiles[corner2Index].region;
		var corner3Region = board.wordTiles[corner3Index].region;
		var corner4Region = board.wordTiles[corner4Index].region;

		var			thisRegion		= board.wordTiles[index].region;
		var	cornerRegions	= new List<int>();
		var	cornerIndexes	= new List<int>();

		if (corner1Region != 0 && !board.wordTiles[corner1Index].regionLocked && thisRegion != corner1Region)
		{
			cornerIndexes.Add(corner1Index);
			cornerRegions.Add(corner1Region);
		}

		if (corner2Region != 0 && !board.wordTiles[corner2Index].regionLocked && thisRegion != corner2Region && !cornerRegions.Contains(corner2Region))
		{
			cornerIndexes.Add(corner2Index);
			cornerRegions.Add(corner2Region);
		}

		if (corner3Region != 0 && !board.wordTiles[corner3Index].regionLocked && thisRegion != corner3Region && !cornerRegions.Contains(corner3Region))
		{
			cornerIndexes.Add(corner3Index);
			cornerRegions.Add(corner3Region);
		}

		if (corner4Region != 0 && !board.wordTiles[corner4Index].regionLocked && thisRegion != corner4Region && !cornerRegions.Contains(corner4Region))
		{
			cornerIndexes.Add(corner4Index);
			cornerRegions.Add(corner4Region);
		}

		return cornerIndexes;
	}

	/// <summary>
	/// Converts one region to another region
	/// </summary>
	private void ConvertRegion(WordBoard board, int fromRegion, int toRegion)
	{
		var covertedCrossroads = new List<int>();

		for (var i = 0; i < board.wordTiles.Length; i++)
		{
			if (board.wordTiles[i].region == fromRegion)
			{
				// If the region we are about to convert is a crossroad region then we need to remember it because
				// when we are done converting it we need to attempt to merge it since it might have become mergeable
				if (this.IsCrossroad(board, i))
				{
					covertedCrossroads.Add(i);
				}

				board.wordTiles[i].region = toRegion;
			}
		}

		for (var i = 0; i < covertedCrossroads.Count; i++)
		{
			this.MergeRegion(board, covertedCrossroads[i]);
		}
	}

	/// <summary>
	/// This will convert one regoin to another region. It will not attempt to merge any crossroad regions and will also
	/// lock the tile to the converted region.
	/// </summary>
	private void ConvertRegionForCrossroad(WordBoard board, int fromRegion, int toRegion)
	{
		for (var i = 0; i < board.wordTiles.Length; i++)
		{
			if (board.wordTiles[i].region == fromRegion)
			{
				board.wordTiles[i].region		= toRegion;
				board.wordTiles[i].regionLocked	= true;
			}
		}
	}

	/// <summary>
	/// Returns the index of the first crossroad region we come across, -1 if there are no corssroads
	/// </summary>
	private int GetAnyCrossroad(WordBoard board)
	{
		for (var i = board.size + 1; i < board.wordTiles.Length - board.size; i++)
		{
			if (i % board.size == board.size - 1)
			{
				i += 1;

				continue;
			}

			if (this.IsCrossroad(board, i))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Returns a Dictionary of List<int> where the key is a region and the list is all the indexes of tiles that belong
	/// to that region.
	/// </summary>
	private Dictionary<int, List<int>> GetRegionIndexes(WordBoard board)
	{
		var regionIndexes = new Dictionary<int, List<int>>();

		for (var i = 0; i < board.wordTiles.Length; i++)
		{
			var region = board.wordTiles[i].region;

			if (board.wordTiles[i].region != 0)
			{
				if (!regionIndexes.ContainsKey(region))
				{
					regionIndexes.Add(region, new List<int>());
				}

				regionIndexes[region].Add(i);
			}
		}

		return regionIndexes;
	}

	/// <summary>
	/// This method attempts to fit words into regions. It returns true if all the words successfully fit into all the
	/// regions, false otherwise. If it returns true then wordRegionAssignments will contain what words go into what regions.
	/// </summary>
	public bool TryFitWordsIntoRegions(List<int> regionSizes, List<int> wordSizes, List<int> regions, List<int> wordIndexes, Dictionary<int, List<int>> wordRegionAssignments)
	{
		if (regionSizes.Count == 0 && wordSizes.Count == 0)
		{
			return true;
		}

		if (regionSizes.Count == 0)
		{
			return false;
		}

		var regionSize	= regionSizes[regionSizes.Count - 1];
		var region		= regions[regions.Count - 1];

		for (var i = 0; i < wordSizes.Count; i++)
		{
			if (wordSizes[i] <= regionSize)
			{
				var	removed		= false;
				var		wordSize	= wordSizes[i];
				var		wordIndex	= wordIndexes[i];

				regionSizes[regionSizes.Count - 1] -= wordSize;

				if (regionSizes[regionSizes.Count - 1] == 0)
				{
					regionSizes.RemoveAt(regionSizes.Count - 1);
					regions.RemoveAt(regions.Count - 1);
					removed = true;
				}

				wordSizes.RemoveAt(i);
				wordIndexes.RemoveAt(i);

				if (this.TryFitWordsIntoRegions(regionSizes, wordSizes, regions, wordIndexes, wordRegionAssignments))
				{
					wordRegionAssignments[region].Add(wordIndex);
					return true;
				}

				wordSizes.Insert(i, wordSize);
				wordIndexes.Insert(i, wordIndex);

				if (removed)
				{
					regionSizes.Add(0);
					regions.Add(region);
				}

				regionSizes[regionSizes.Count - 1] += wordSize;
			}
		}

		return false;
	}

	#endregion
}