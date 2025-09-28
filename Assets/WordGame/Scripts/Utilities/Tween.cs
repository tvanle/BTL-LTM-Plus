using UnityEngine;
using System.Collections;

/// <summary>
/// Simple Tween class that provides the ability to quickly add dynamic animations to GameObjects.
/// </summary>
public class Tween : MonoBehaviour
{

	public enum TweenType
	{
		PositionX,
		PositionY,
		PositionZ,
		ScaleX,
		ScaleY,
		ScaleZ,
		Rotation,
		RotationPoint,
		ColourImage,
		ColourText,
		ColourMaterial
	}

	public enum TweenStyle
	{
		Linear,
		EaseIn,
		EaseOut
	}

	public enum LoopType
	{
		None,	// When the Tween finishes it removes itself from the GameObject
		Reset,	// When the Tween finishes it sets the values back to the fromValue and starts over
		Reverse	// When the Tween finishes it "reverses" the Tween by setting the fromValue to equal the toValue and vis-versa
	}



	public delegate void OnTweenFinished(GameObject tweenedObject, object[] bundleObjects);



	// Used by all tween types
	private TweenType		tweenType;
	private TweenStyle		tweenStyle;
	private float			duration;
	private double			startTime;
	private double			endTime;
	private LoopType		loopType;
	private bool			useRectTransform;
	private OnTweenFinished	finishCallback;
	private bool			isDestroyed;
	private object[] 		bundleObjects;

	// Used by position and scale tweens
	private float	fromValue;
	private float	toValue;
	private bool	useLocal;

	// Used by rotation tweens
	private Vector3		point;
	private Transform	pointT;
	private Vector3		axis;
	private float		angleSoFar;

	// Used by rotation point tweens
	private Vector3 fromPoint;
	private Vector3 toPoint;

	// Used by colour tweens
	private Color	fromColour;
	private Color	toColour;



	public Vector3 Point { get { return this.point; } set { this.point = value; } }



	private void Start()
	{
		this.SetTimes();
	}

	private void Update()
	{
		// Check if the tween has finished
		if (Utilities.SystemTimeInMilliseconds >= this.endTime)
		{
			switch (this.loopType)
			{
			case LoopType.None:
				this.SetToValue();		// Set the value to the toValue
				this.DestroyTween();
				break;
			case LoopType.Reset:
				this.SetTimes(); // Reset the startTime and endTime
				this.Reset();       // Set the values be the the fromValue
				break;
			case LoopType.Reverse:
				this.SetTimes();   // Reset the startTime and endTime
				this.SetToValue(); // Set the values to the toValue
				this.Reverse();       // Swap the from and to values so the tween plays in reverse
				break;
			}

			// Call the finish callback if one was set
			if (this.finishCallback != null)
			{
				this.finishCallback(this.gameObject, this.bundleObjects);
			}
		}
		else
		{
			// Update the values
			switch (this.tweenType)
			{
			case TweenType.PositionX:
			case TweenType.PositionY:
			case TweenType.PositionZ:
				this.UpdatePosition(Mathf.Lerp(this.fromValue, this.toValue, this.GetLerpT()));
				break;
			case TweenType.ScaleX:
			case TweenType.ScaleY:
			case TweenType.ScaleZ:
				this.UpdateScale(Mathf.Lerp(this.fromValue, this.toValue, this.GetLerpT()));
				break;
			case TweenType.Rotation:
				this.UpdateRotation();
				break;
			case TweenType.RotationPoint:
				this.UpdateRotationPoint(Vector3.Lerp(this.fromPoint, this.toPoint, this.GetLerpT()));
				break;
			case TweenType.ColourImage:
			case TweenType.ColourText:
			case TweenType.ColourMaterial:
				this.UpdateColour(Color.Lerp(this.fromColour, this.toColour, this.GetLerpT()));
				break;
			}
		}
	}



	/// <summary>
	/// Gets the Tween component on the given GameObject with the given TweenType
	/// </summary>
	public static Tween GetTween(GameObject obj, TweenType tweenType)
	{
		if (obj == null)
		{
			return null;
		}

		var tweens = obj.GetComponents<Tween>();

		for (var i = 0; i < tweens.Length; i++)
		{
			if (tweens[i].tweenType == tweenType)
			{
				return !tweens[i].isDestroyed ? tweens[i] : null;
			}
		}

		return null;
	}

	/// <summary>
	/// Removes the Tween component on the given GameObject with the given TweenType
	/// </summary>
	public static void RemoveTween(GameObject obj, TweenType tweenType)
	{
		var tweenObject = GetTween(obj, tweenType);

		if (tweenObject != null)
		{
			Destroy(tweenObject);
		}
	}

	/// <summary>
	/// Tweens the X position of the GameObject
	/// </summary>
	public static Tween PositionX(Transform transform, TweenStyle tweenStyle, float fromValue, float toValue, float duration, bool transformLocal = false, LoopType loopType = LoopType.None)
	{
		return CreateTween(transform.gameObject, TweenType.PositionX, tweenStyle, fromValue, toValue, duration, transformLocal, loopType);
	}

	/// <summary>
	/// Tweens the Y position of the GameObject
	/// </summary>
	public static Tween PositionY(Transform transform, TweenStyle tweenStyle, float fromValue, float toValue, float duration, bool transformLocal = false, LoopType loopType = LoopType.None)
	{
		return CreateTween(transform.gameObject, TweenType.PositionY, tweenStyle, fromValue, toValue, duration, transformLocal, loopType);
	}

	/// <summary>
	/// Tweens the Z position of the GameObject
	/// </summary>
	public static Tween PositionZ(Transform transform, TweenStyle tweenStyle, float fromValue, float toValue, float duration, bool transformLocal = false, LoopType loopType = LoopType.None)
	{
		return CreateTween(transform.gameObject, TweenType.PositionZ, tweenStyle, fromValue, toValue, duration, transformLocal, loopType);
	}

	/// <summary>
	/// Tweens the X scale of the GameObject
	/// </summary>
	public static Tween ScaleX(Transform transform, TweenStyle tweenStyle, float fromValue, float toValue, float duration, LoopType loopType = LoopType.None)
	{
		return CreateTween(transform.gameObject, TweenType.ScaleX, tweenStyle, fromValue, toValue, duration, true, loopType);
	}
	
	/// <summary>
	/// Tweens the Y scale of the GameObject
	/// </summary>
	public static Tween ScaleY(Transform transform, TweenStyle tweenStyle, float fromValue, float toValue, float duration, LoopType loopType = LoopType.None)
	{
		return CreateTween(transform.gameObject, TweenType.ScaleY, tweenStyle, fromValue, toValue, duration, true, loopType);
	}
	
	/// <summary>
	/// Tweens the Z scale of the GameObject
	/// </summary>
	public static Tween ScaleZ(Transform transform, TweenStyle tweenStyle, float fromValue, float toValue, float duration, LoopType loopType = LoopType.None)
	{
		return CreateTween(transform.gameObject, TweenType.ScaleZ, tweenStyle, fromValue, toValue, duration, true, loopType);
	}

	/// <summary>
	/// Rotates the GameObject around a point on the axis by the given angle
	/// </summary>
	public static Tween RotateAround(Transform transform, TweenStyle tweenStyle, Vector3 point, Vector3 axis, float angle, float duration, LoopType loopType = LoopType.None)
	{
		return CreateRotationTween(transform.gameObject, TweenType.Rotation, tweenStyle, point, axis, angle, duration, loopType);
	}

	/// <summary>
	/// Rotates the GameObject around a point on the axis by the given angle
	/// </summary>
	public static Tween RotateAround(Transform transform, TweenStyle tweenStyle, Transform point, Vector3 axis, float angle, float duration, LoopType loopType = LoopType.None)
	{
		return CreateRotationTween(transform.gameObject, TweenType.Rotation, tweenStyle, point, axis, angle, duration, loopType);
	}

	/// <summary>
	/// Tweens the color of the UI Image
	/// </summary>
	public static Tween Colour(UnityEngine.UI.Image uiImage, TweenStyle tweenStyle, Color fromValue, Color toValue, float duration, LoopType loopType = LoopType.None)
	{
		return CreateColourTween(uiImage.gameObject, TweenType.ColourImage, tweenStyle, fromValue, toValue, duration, loopType);
	}
	
	/// <summary>
	/// Tweens the color of the UI Text
	/// </summary>
	public static Tween Colour(UnityEngine.UI.Text uiText, TweenStyle tweenStyle, Color fromValue, Color toValue, float duration, LoopType loopType = LoopType.None)
	{
		return CreateColourTween(uiText.gameObject, TweenType.ColourText, tweenStyle, fromValue, toValue, duration, loopType);
	}
	
	/// <summary>
	/// Tweens the color of the Material
	/// </summary>
	public static Tween Colour(Renderer renderer, TweenStyle tweenStyle, Color fromValue, Color toValue, float duration, LoopType loopType = LoopType.None)
	{
		return CreateColourTween(renderer.gameObject, TweenType.ColourMaterial, tweenStyle, fromValue, toValue, duration, loopType);
	}

	/// <summary>
	/// Sets the method to call when the Tween has finished (If the tween loops then this callback will be called at the end of each loop).
	/// </summary>
	public void SetFinishCallback(OnTweenFinished finishCallback, params object[] bundleObjects)
	{
		this.finishCallback = finishCallback;
		this.bundleObjects = bundleObjects;
	}

	/// <summary>
	/// If set to true then the transform on the object will be cast to a RectTransform and anchorPosition will be used (If the TweenType is a Position tween)
	/// </summary>
	public void SetUseRectTransform(bool useRectTransform)
	{
		this.useRectTransform = useRectTransform;
	}

	/// <summary>
	/// This will tween the "point" value on a Rotation Tween. Use this if you have a Rotation tween on a GameObject that is moving.
	/// </summary>
	public Tween TweenRotationPoint(TweenStyle tweenStyle, Vector3 fromPoint, Vector3 toPoint, float duration, LoopType loopType = LoopType.None)
	{
		// If the TweenType for the current Tween is not a Rotation then do nothing
		if (this.tweenType != TweenType.Rotation)
		{
			Debug.LogWarning("Cannot set a TweenType.RotationPoint on a Tween that is not a TweenType.Rotation.");
			return null;
		}

		var tween = GetTween(this.gameObject, TweenType.RotationPoint);

		if (tween == null)
		{
			tween = this.gameObject.AddComponent<Tween>();
		}

		tween.tweenType		= TweenType.RotationPoint;
		tween.tweenStyle	= tweenStyle;
		tween.fromPoint		= fromPoint;
		tween.toPoint		= toPoint;
		tween.duration		= duration;
		tween.loopType		= loopType;

		return tween;
	}

	public void DestroyTween()
	{
		Destroy(this);        // Remove the Tween component
		this.isDestroyed = true; // Set destroy flag
	}



	private static Tween CreateTween(GameObject obj, TweenType tweenType, TweenStyle tweenStyle, float fromValue, float toValue, float duration, bool transformLocal, LoopType loopType)
	{
		var tween = GetTween(obj, tweenType);

		if (tween == null)
		{
			tween = obj.AddComponent<Tween>();
		}

		tween.tweenType			= tweenType;
		tween.tweenStyle		= tweenStyle;
		tween.fromValue			= fromValue;
		tween.toValue			= toValue;
		tween.duration			= duration;
		tween.useLocal			= transformLocal;
		tween.loopType			= loopType;

		return tween;
	}

	private static Tween CreateRotationTween(GameObject obj, TweenType tweenType, TweenStyle tweenStyle, Vector3 point, Vector3 axis, float angle, float duration, LoopType loopType)
	{
		var tween = GetTween(obj, tweenType);

		if (tween == null)
		{
			tween = obj.AddComponent<Tween>();
		}

		tween.angleSoFar = 0;

		tween.tweenType			= tweenType;
		tween.tweenStyle		= tweenStyle;
		tween.point				= point;
		tween.pointT			= null;
		tween.axis				= axis;
		tween.fromValue			= 0;
		tween.toValue			= angle;
		tween.duration			= duration;
		tween.loopType			= loopType;

		return tween;
	}

	private static Tween CreateRotationTween(GameObject obj, TweenType tweenType, TweenStyle tweenStyle, Transform point, Vector3 axis, float angle, float duration, LoopType loopType)
	{
		var tween = GetTween(obj, tweenType);

		if (tween == null)
		{
			tween = obj.AddComponent<Tween>();
		}

		tween.angleSoFar = 0;

		tween.tweenType			= tweenType;
		tween.tweenStyle		= tweenStyle;
		tween.pointT			= point;
		tween.axis				= axis;
		tween.fromValue			= 0;
		tween.toValue			= angle;
		tween.duration			= duration;
		tween.loopType			= loopType;

		return tween;
	}

	private static Tween CreateColourTween(GameObject obj, TweenType tweenType, TweenStyle tweenStyle, Color fromValue, Color toValue, float duration, LoopType loopType)
	{
		var tween = GetTween(obj, tweenType);

		if (tween == null)
		{
			tween = obj.AddComponent<Tween>();
		}

		tween.tweenType			= tweenType;
		tween.tweenStyle		= tweenStyle;
		tween.fromColour		= fromValue;
		tween.toColour			= toValue;
		tween.duration			= duration;
		tween.loopType			= loopType;

		return tween;
	}

	private void SetTimes()
	{
		this.startTime = Utilities.SystemTimeInMilliseconds;
		this.endTime   = this.startTime + this.duration;;
	}

	private void Reset()
	{
		switch (this.tweenType)
		{
		case TweenType.PositionX:
		case TweenType.PositionY:
		case TweenType.PositionZ:
			this.UpdatePosition(this.fromValue);
			break;
		case TweenType.ScaleX:
		case TweenType.ScaleY:
		case TweenType.ScaleZ:
			this.UpdateScale(this.fromValue);
			break;
		case TweenType.Rotation:
			this.transform.RotateAround(this.pointT == null ? this.point : this.pointT.position, this.axis, -this.toValue);
			this.angleSoFar = 0;
			break;
		case TweenType.RotationPoint:
			this.UpdateRotationPoint(this.fromPoint);
			break;
		case TweenType.ColourImage:
		case TweenType.ColourText:
		case TweenType.ColourMaterial:
			this.UpdateColour(this.fromColour);
			break;
		}
	}

	private void SetToValue()
	{
		switch (this.tweenType)
		{
		case TweenType.PositionX:
		case TweenType.PositionY:
		case TweenType.PositionZ:
			this.UpdatePosition(this.toValue);
			break;
		case TweenType.ScaleX:
		case TweenType.ScaleY:
		case TweenType.ScaleZ:
			this.UpdateScale(this.toValue);
			break;
		case TweenType.Rotation:
			this.transform.RotateAround(this.pointT == null ? this.point : this.pointT.position, this.axis, this.toValue - this.angleSoFar);
			this.angleSoFar = 0;
			break;
		case TweenType.RotationPoint:
			this.UpdateRotationPoint(this.toPoint);
			break;
		case TweenType.ColourImage:
		case TweenType.ColourText:
		case TweenType.ColourMaterial:
			this.UpdateColour(this.toColour);
			break;
		}
	}

	private void Reverse()
	{
		switch (this.tweenType)
		{
		case TweenType.PositionX:
		case TweenType.PositionY:
		case TweenType.PositionZ:
		case TweenType.ScaleX:
		case TweenType.ScaleY:
		case TweenType.ScaleZ:
		case TweenType.Rotation:
			var temp	= this.fromValue;
			this.fromValue = this.toValue;
			this.toValue      = temp;
			break;
		case TweenType.RotationPoint:
			var tempV	= this.fromPoint;
			this.fromPoint = this.toPoint;
			this.toPoint      = tempV;
			break;
		case TweenType.ColourImage:
		case TweenType.ColourText:
		case TweenType.ColourMaterial:
			var tempC	= this.fromColour;
			this.fromColour = this.toColour;
			this.toColour      = tempC;
			break;
		}
	}

	private void UpdatePosition(float pos)
	{
		switch (this.tweenType)
		{
		case TweenType.PositionX:
			if (this.useLocal)
			{
				this.transform.localPosition = new Vector3(pos, this.transform.localPosition.y, this.transform.localPosition.z);
			}
			else if (this.useRectTransform)
			{
				(this.transform as RectTransform).anchoredPosition = new Vector2(pos, (this.transform as RectTransform).anchoredPosition.y);
			}
			else
			{
				this.transform.position = new Vector3(pos, this.transform.position.y, this.transform.position.z);
			}

			break;
		case TweenType.PositionY:
			if (this.useLocal)
			{
				this.transform.localPosition = new Vector3(this.transform.localPosition.x, pos, this.transform.localPosition.z);
			}
			else if (this.useRectTransform)
			{
				(this.transform as RectTransform).anchoredPosition = new Vector2((this.transform as RectTransform).anchoredPosition.x, pos);
			}
			else
			{
				this.transform.position = new Vector3(this.transform.position.x, pos, this.transform.position.z);
			}

			break;
		case TweenType.PositionZ:
			if (this.useLocal)
			{
				this.transform.localPosition = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, pos);
			}
			else
			{
				this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, pos);
			}

			break;
		}
	}

	private void UpdateScale(float scale)
	{
		switch (this.tweenType)
		{
		case TweenType.ScaleX:
			this.transform.localScale = new Vector3(scale, this.transform.localScale.y, this.transform.localScale.z);
			break;
		case TweenType.ScaleY:
			this.transform.localScale = new Vector3(this.transform.localScale.x, scale, this.transform.localScale.z);
			break;
		case TweenType.ScaleZ:
			this.transform.localScale = new Vector3(this.transform.localScale.x, this.transform.localScale.y, scale);
			break;
		}
	}

	private void UpdateRotation()
	{
		var angle  = Mathf.Lerp(this.fromValue, this.toValue, this.GetLerpT());
		var amount = this.angleSoFar - angle;

		this.transform.RotateAround(this.pointT == null ? this.point : this.pointT.position, this.axis, amount);

		this.angleSoFar = angle;
	}

	private void UpdateRotationPoint(Vector3 point)
	{
		var rotationTween = GetTween(this.gameObject, TweenType.Rotation);

		if (rotationTween == null)
		{
			this.DestroyTween();
		}
		else
		{
			rotationTween.Point = point;
		}
	}

	private void UpdateColour(Color colour)
	{
		switch (this.tweenType)
		{
		case TweenType.ColourImage:
			this.gameObject.GetComponent<UnityEngine.UI.Image>().color = colour;
			break;
		case TweenType.ColourText:
			this.gameObject.GetComponent<UnityEngine.UI.Text>().color = colour;
			break;
		case TweenType.ColourMaterial:
			this.gameObject.GetComponent<Renderer>().material.color = colour;
			break;
		}
	}

	private float GetLerpT()
	{
		var lerpT = (float)(Utilities.SystemTimeInMilliseconds - this.startTime) / this.duration;

		switch (this.tweenStyle)
		{
		case TweenStyle.EaseIn:
			lerpT = lerpT * lerpT * lerpT;
			break;
		case TweenStyle.EaseOut:
			lerpT = 1.0f - (1.0f - lerpT) * (1.0f - lerpT) * (1.0f - lerpT);
			break;
		}

		return lerpT;
	}

}
