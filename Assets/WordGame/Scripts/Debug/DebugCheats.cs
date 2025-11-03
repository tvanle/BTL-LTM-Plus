using UnityEngine;
using WordGame.Network;
using System.Threading.Tasks;

/// <summary>
/// Debug cheats for testing game flow quickly
/// Press F9: Complete current level instantly
/// Press F10: Complete ALL remaining levels → Jump to Game Result screen
/// Press F11: Test complete overlay animation (fake data)
/// </summary>
public class DebugCheats : MonoBehaviour
{
	[Header("Debug Settings")]
	[SerializeField] private bool enableCheats = true;
	[SerializeField] private KeyCode completeOneKey = KeyCode.F9;
	[SerializeField] private KeyCode jumpToEndKey = KeyCode.F10;
	[SerializeField] private KeyCode showOverlayKey = KeyCode.F11;
	[SerializeField] private int maxLevelsToComplete = 20; // Safety limit

	private NetworkManager networkManager;
	private bool isProcessing = false;

	private void Start()
	{
		this.networkManager = NetworkManager.Instance;

		if (this.enableCheats)
		{
			Debug.Log($"[DebugCheats] Enabled! Controls:");
			Debug.Log($"  {this.completeOneKey}: Complete current level");
			Debug.Log($"  {this.jumpToEndKey}: Complete ALL levels → Game Result");
			Debug.Log($"  {this.showOverlayKey}: Test complete overlay animation");
		}
	}

	private void Update()
	{
		if (!this.enableCheats || this.isProcessing)
		{
			return;
		}

		// F9: Complete one level
		if (Input.GetKeyDown(this.completeOneKey))
		{
			this.CompleteCurrentLevel();
		}

		// F10: Jump to game end (complete all remaining levels)
		if (Input.GetKeyDown(this.jumpToEndKey))
		{
			this.JumpToGameEnd();
		}

		// F11: Test complete overlay
		if (Input.GetKeyDown(this.showOverlayKey))
		{
			this.TestCompleteOverlay();
		}
	}

	private async void CompleteCurrentLevel()
	{
		if (this.networkManager == null)
		{
			Debug.LogWarning("[DebugCheats] Not connected to server!");
			return;
		}

		this.isProcessing = true;

		Debug.Log("[DebugCheats] Completing current level...");

		try
		{
			// Send level completed with fake time (5 seconds)
			await this.networkManager.LevelCompleted(5);
			Debug.Log("[DebugCheats] Level completed!");
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[DebugCheats] Error completing level: {e.Message}");
		}

		this.isProcessing = false;
	}

	private async void JumpToGameEnd()
	{
		if (this.networkManager == null)
		{
			Debug.LogWarning("[DebugCheats] Not connected to server!");
			return;
		}

		this.isProcessing = true;

		Debug.Log($"[DebugCheats] 🚀 JUMPING TO GAME END! Completing all remaining levels...");

		try
		{
			// Complete levels rapidly until game ends
			// Server will send GAME_ENDED when CurrentLevel >= TotalLevels
			for (int i = 0; i < this.maxLevelsToComplete; i++)
			{
				// Send level completed with random fast time
				int randomTime = Random.Range(3, 10);
				await this.networkManager.LevelCompleted(randomTime);

				Debug.Log($"[DebugCheats] ✅ Completed level {i + 1} (time: {randomTime}s)");

				// Small delay between levels
				await Task.Delay(150);

				// Note: Server will automatically trigger GAME_ENDED when we reach the final level
				// Client will handle GAME_ENDED message and show result screen
			}

			Debug.Log("[DebugCheats] 🎮 Sent max level completions! Waiting for GAME_ENDED from server...");
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[DebugCheats] ❌ Error completing levels: {e.Message}");
		}

		this.isProcessing = false;
	}

	private void TestCompleteOverlay()
	{
		Debug.Log("[DebugCheats] Testing complete overlay animation...");

		// Create fake score data to test overlay
		var fakeScoreData = new NetworkManager.ScoreUpdateData
		{
			scoreGained = Random.Range(50, 150),
			totalScore = Random.Range(200, 500),
			streak = Random.Range(1, 5)
		};

		// Show the overlay screen as overlay (don't hide current screen)
		UIScreenController screenController = FindObjectOfType<UIScreenController>();
		if (screenController != null)
		{
			// Show(id, fromLeft, animate, overlay, style, onFinished, data)
			screenController.Show("complete", fromLeft: false, animate: true, overlay: true,
			                     style: Tween.TweenStyle.EaseOut, onTweenFinished: null,
			                     data: fakeScoreData);
			Debug.Log($"[DebugCheats] Showing complete overlay with fake data: +{fakeScoreData.scoreGained} score");
		}
		else
		{
			Debug.LogWarning("[DebugCheats] UIScreenController not found!");
		}
	}

	private void OnGUI()
	{
		if (!this.enableCheats)
		{
			return;
		}

		// Show cheat info on screen
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.fontSize = 14;
		style.normal.textColor = Color.yellow;

		string info = $"DEBUG CHEATS:\n" +
		              $"{this.completeOneKey}: Complete 1 level\n" +
		              $"{this.jumpToEndKey}: Jump to Game Result\n" +
		              $"{this.showOverlayKey}: Test overlay";

		GUI.Label(new Rect(10, 10, 300, 100), info, style);

		if (this.isProcessing)
		{
			style.normal.textColor = Color.red;
			GUI.Label(new Rect(10, 120, 200, 30), "Processing...", style);
		}
	}
}
