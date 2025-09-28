using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProgressRing : MonoBehaviour
{

	[SerializeField] private RectTransform	firstHalf;
	[SerializeField] private RectTransform	secondHalf;
	[SerializeField] private Text 			percentText;





	private void Awake()
	{
		this.SetProgress(0f);
	}



	public void SetProgress(float percent)
	{
		this.percentText.text = Mathf.RoundToInt(percent * 100f) + "%";

		var z1 = Mathf.Lerp(180f, 0f, Mathf.Clamp01(percent * 2f));
		var z2 = Mathf.Lerp(180f, 0f, Mathf.Clamp01((percent - 0.5f) * 2f));

		this.firstHalf.localEulerAngles = new Vector3(this.firstHalf.localEulerAngles.x, this.firstHalf.localEulerAngles.y, z1);
		this.secondHalf.localEulerAngles   = new Vector3(this.secondHalf.localEulerAngles.x, this.secondHalf.localEulerAngles.y, z2);
	}



}
