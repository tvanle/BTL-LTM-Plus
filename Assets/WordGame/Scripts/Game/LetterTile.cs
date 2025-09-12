using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LetterTile : MonoBehaviour
{
	#region Inspector Variables

	[SerializeField] private Image 	backgroundImage;
	[SerializeField] private Text 	letterText;
	[SerializeField] private Color 	backgroundNormalColor;
	[SerializeField] private Color 	backgroundSelectedColor;
	[SerializeField] private Color 	letterNormalColor;
	[SerializeField] private Color 	letterSelectedColor;
	[SerializeField] private Sprite normalSprite;
	[SerializeField] private Sprite selectedSprite;

	#endregion

	#region Properties

	public Text LetterText => this.letterText;
	public int  TileIndex  { get; set; }
	public bool Selected   { get; set; }
	public bool Found      { get; set; }
	public char Letter     { get; set; }

	#endregion

	#region Public Methods

	public void SetSelected(bool selected)
	{
		this.Selected = selected;

		this.backgroundImage.sprite = selected ? this.selectedSprite : this.normalSprite;
		this.backgroundImage.color  = selected ? this.backgroundSelectedColor : this.backgroundNormalColor;
		this.letterText.color          = selected ? this.letterSelectedColor : this.letterNormalColor;
	}

	#endregion
}