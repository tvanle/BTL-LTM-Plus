using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class UIScreen : MonoBehaviour
{

	public string			id;
	public List<GameObject>	worldObjects;



	public RectTransform RectT { get { return this.gameObject.GetComponent<RectTransform>(); } }



	public virtual void Initialize()
	{

	}

	public virtual void OnShowing(object data)
	{

	}

    public virtual void OnBackClicked()
    {

    }

}
