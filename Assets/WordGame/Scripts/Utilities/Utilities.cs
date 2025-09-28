using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class Utilities
{

	public const string BoardFilesDirectory = "WordGameBoardFiles"; // This should be a non-empty string with no leading or trailing slashes (ie. /)



	public static double SystemTimeInMilliseconds { get { return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds; } }

	public static float WorldWidth  => 2f * Camera.main.orthographicSize * Camera.main.aspect;
	public static float WorldHeight => 2f * Camera.main.orthographicSize;



	public static void SaveWordBoard(WordBoard wordBoard, string directoryInResources)
	{
		#if !UNITY_EDITOR
		Debug.LogError("Can only save WordBoards in the Unity Editor.");
		#else
		/* Important things we need to save in order to create a game board: id, size, words, and wordTiles */

		// Get the words as one long string seperated by _ to save space
		var wordsStr = string.Join("_", wordBoard.words);

		// Get the word tile states as one long string to save space
		var wordTilesStr = string.Join("", wordBoard.wordTiles.Select(tile =>
		{
			var usedStr = tile.used ? "1" : "0";
			var hasLetterStr = tile.hasLetter ? "1" : "0";
			return $"{usedStr}{hasLetterStr}{(tile.hasLetter ? tile.letter : '-')}";
		}));

		// Create the text that defines a WordBoard
		var text = "";
		text += $"{wordBoard.id},";
		text += $"{wordBoard.size},";
		text += $"{wordsStr},";
		text += $"{wordTilesStr}";

		// Get the full path
		var directoyPath	= Application.dataPath + "/WordGame/Resources/" + BoardFilesDirectory;
		var ioPath		= directoyPath + "/" + wordBoard.id + ".csv";

		if (!System.IO.Directory.Exists(directoyPath))
		{
			System.IO.Directory.CreateDirectory(directoyPath);
		}

		// If there is already a board, then delete it to create a new one
		if (System.IO.File.Exists(ioPath))
		{
			System.IO.File.Delete(ioPath);
		}

		// Open a StreamWriter and write the text to the file
		var stream = System.IO.File.CreateText(ioPath);
		stream.WriteLine(text);
		stream.Close();
		#endif
	}

	public static WordBoard LoadWordBoard(string boardId)
	{
		var textAsset = Resources.Load<TextAsset>(Utilities.BoardFilesDirectory + "/" + boardId);

		if (textAsset != null)
		{
			var text = textAsset.text.Split(',');

			var wordBoard = new WordBoard();
			wordBoard.id		= text[0];
			wordBoard.size		= System.Convert.ToInt32(text[1]);
			wordBoard.words		= text[2].Split('_');

			wordBoard.wordTiles = new WordBoard.WordTile[text[3].Length / 3];

			for (var i = 0; i < wordBoard.wordTiles.Length; i++)
			{
				var wordTile = new WordBoard.WordTile();

				wordTile.used		= text[3][0] == '1';
				wordTile.hasLetter	= text[3][1] == '1';
				wordTile.letter		= text[3][2];

				wordBoard.wordTiles[i] = wordTile;

				// Remove the first 3 characters of text[3]
				text[3] = text[3].Substring(3, text[3].Length - 3);
			}

			return wordBoard;
		}

		return null;
	}

	/// <summary>
	/// Creates a board id using a category and level name
	/// </summary>
	public static string FormatBoardId(string category, int index)
	{
		return $"{category}_{index}".Replace(" ", "_");
	}
}