using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathologicalGames
{
	public class SpawnPoolsDict : IDictionary<string, SpawnPool>, ICollection<KeyValuePair<string, SpawnPool>>, IEnumerable<KeyValuePair<string, SpawnPool>>, IEnumerable
	{
		public delegate void OnCreatedDelegate(SpawnPool pool);

		internal Dictionary<string, OnCreatedDelegate> onCreatedDelegates;

		private Dictionary<string, SpawnPool> _pools;

		public int Count => 0;

		public SpawnPool this[string key]
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public ICollection<string> Keys => null;

		public ICollection<SpawnPool> Values => null;

		private bool IsReadOnly => false;

		bool ICollection<KeyValuePair<string, SpawnPool>>.IsReadOnly => false;

		public void AddOnCreatedDelegate(string poolName, OnCreatedDelegate createdDelegate)
		{
		}

		public void RemoveOnCreatedDelegate(string poolName, OnCreatedDelegate createdDelegate)
		{
		}

		public SpawnPool Create(string poolName)
		{
			return null;
		}

		public SpawnPool Create(string poolName, GameObject owner)
		{
			return null;
		}

		private bool assertValidPoolName(string poolName)
		{
			return false;
		}

		public override string ToString()
		{
			return null;
		}

		public bool Destroy(string poolName)
		{
			return false;
		}

		public void DestroyAll()
		{
		}

		internal void Add(SpawnPool spawnPool)
		{
		}

		public void Add(string key, SpawnPool value)
		{
		}

		internal bool Remove(SpawnPool spawnPool)
		{
			return false;
		}

		public bool Remove(string poolName)
		{
			return false;
		}

		public bool ContainsKey(string poolName)
		{
			return false;
		}

		public bool TryGetValue(string poolName, out SpawnPool spawnPool)
		{
			spawnPool = null;
			return false;
		}

		public bool Contains(KeyValuePair<string, SpawnPool> item)
		{
			return false;
		}

		public void Add(KeyValuePair<string, SpawnPool> item)
		{
		}

		public void Clear()
		{
		}

		private void CopyTo(KeyValuePair<string, SpawnPool>[] array, int arrayIndex)
		{
		}

		void ICollection<KeyValuePair<string, SpawnPool>>.CopyTo(KeyValuePair<string, SpawnPool>[] array, int arrayIndex)
		{
		}

		public bool Remove(KeyValuePair<string, SpawnPool> item)
		{
			return false;
		}

		public IEnumerator<KeyValuePair<string, SpawnPool>> GetEnumerator()
		{
			return null;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return null;
		}
	}
}
