using UnityEngine;

namespace PathologicalGames
{
	public static class InstanceHandler
	{
		public delegate GameObject InstantiateDelegate(GameObject prefab, Vector3 pos, Quaternion rot);

		public delegate void DestroyDelegate(GameObject instance);

		public static InstantiateDelegate InstantiateDelegates;

		public static DestroyDelegate DestroyDelegates;

		internal static GameObject InstantiatePrefab(GameObject prefab, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		internal static void DestroyInstance(GameObject instance)
		{
		}
	}
}
