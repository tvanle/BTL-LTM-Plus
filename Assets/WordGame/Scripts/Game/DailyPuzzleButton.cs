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

			System.TimeSpan timeLeft = GameManager.Instance.NextDailyPuzzleAt - System.DateTime.Now;

			string hours	= string.Format("{0}{1}", (timeLeft.Hours < 10 ? "0" : ""), timeLeft.Hours);
			string mins		= string.Format("{0}{1}", (timeLeft.Minutes < 10 ? "0" : ""), timeLeft.Minutes);
			string secs		= string.Format("{0}{1}", (timeLeft.Seconds < 10 ? "0" : ""), timeLeft.Seconds);

			this.timeText.text = string.Format("{0}:{1}:{2}", hours, mins, secs);
		}
	}

	#endregion
}