using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class DailyPuzzleButton : MonoBehaviour
{
	#region Inspector Variables

	[SerializeField] private Text timeText;

	#endregion

	#region Member Variables

	private Button button;

	#endregion

	#region Unity Methods

	private void Start()
	{
		this.button = this.gameObject.GetComponent<Button>();

		this.button.onClick.AddListener(() =>
		{
			GameManager.Instance.StartDailyPuzzle();
			UIScreenController.Instance.Show(UIScreenController.GameScreenId);
		});

		this.Update();
	}

	private void Update()
	{
		if (System.DateTime.Now >= GameManager.Instance.NextDailyPuzzleAt)
		{
			this.timeText.gameObject.SetActive(false);
			this.button.interactable = true;
		}
		else
		{
			this.timeText.gameObject.SetActive(true);
			this.button.interactable = false;

			var timeLeft = GameManager.Instance.NextDailyPuzzleAt - System.DateTime.Now;

			var hours = $"{(timeLeft.Hours < 10 ? "0" : "")}{timeLeft.Hours}";
			var mins  = $"{(timeLeft.Minutes < 10 ? "0" : "")}{timeLeft.Minutes}";
			var secs  = $"{(timeLeft.Seconds < 10 ? "0" : "")}{timeLeft.Seconds}";

			this.timeText.text = $"{hours}:{mins}:{secs}";
		}
	}

	#endregion
}