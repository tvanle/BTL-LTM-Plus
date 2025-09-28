using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool
{

	private GameObject			objectPrefab		= null;
	private List<GameObject>	instantiatedObjects = new List<GameObject>();
	private Transform			parent				= null;



	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectPooler"/> class.
	/// </summary>
	/// <param name="objectPrefab">The GameObject to instantiate.</param>
	/// <param name="initialSize">Initial amount of objects to instantiate.</param>
	public ObjectPool(GameObject objectPrefab, int initialSize, Transform parent = null)
	{
		this.objectPrefab	= objectPrefab;
		this.parent			= parent;

		for (var i = 0; i < initialSize; i++)
		{
			var obj = this.CreateObject();
			obj.SetActive(false);
		}
	}

	/// <summary>
	/// Returns an object, it there is no object that can be returned from instantiatedObjects then it creates a new one.
	/// Objects are returned to the pool by setting their active state to false.
	/// </summary>
	public GameObject GetObject()
	{
		for (var i = 0; i < this.instantiatedObjects.Count; i++)
		{
			if (!this.instantiatedObjects[i].activeSelf)
			{
				return this.instantiatedObjects[i];
			}
		}

		return this.CreateObject();
	}

	/// <summary>
	/// Sets all instantiated GameObjects to de-active
	/// </summary>
	public void ReturnAllObjectsToPool()
	{
		for (var i = 0; i < this.instantiatedObjects.Count; i++)
		{
			this.instantiatedObjects[i].SetActive(false);
		}
	}



	private GameObject CreateObject()
	{
		var obj = GameObject.Instantiate(this.objectPrefab);
		obj.transform.SetParent(this.parent, false);
		this.instantiatedObjects.Add(obj);
		return obj;
	}

}
