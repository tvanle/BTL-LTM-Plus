using UnityEngine;
using System.Collections;

/// <summary>
/// Gets a static instance of the Component that extends this class and makes it accessible through the Instance property.
/// </summary>
public class SingletonComponent<T> : MonoBehaviour where T : Object
{

	private static T instance;



	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				Debug.LogWarningFormat("[SingletonComponent] Returning null instance for component of type {0}.", typeof(T));
			}

			return instance;
		}
	}



	protected virtual void Awake()
	{
		this.SetInstance();
	}



	public static bool Exists()
	{
		return instance != null;
	}

	public void SetInstance()
	{
		if (instance != null && instance != this.gameObject.GetComponent<T>())
		{
			Debug.LogWarning("[SingletonComponent] Instance already set for type " + typeof(T));
			return;
		}

		instance = this.gameObject.GetComponent<T>();
	}

}
