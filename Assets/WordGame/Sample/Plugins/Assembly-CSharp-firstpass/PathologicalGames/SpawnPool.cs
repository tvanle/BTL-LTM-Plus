using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PathologicalGames
{
	[AddComponentMenu("Path-o-logical/PoolManager/SpawnPool")]
	public sealed class SpawnPool : MonoBehaviour, IList<Transform>, ICollection<Transform>, IEnumerable<Transform>, IEnumerable
	{
		public delegate GameObject InstantiateDelegate(GameObject prefab, Vector3 pos, Quaternion rot);

		public delegate void DestroyDelegate(GameObject instance);

		[CompilerGenerated]
		private sealed class _003CDoDespawnAfterSeconds_003Ed__56 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public Transform instance;

			public float seconds;

			public bool useParent;

			public SpawnPool _003C_003E4__this;

			public Transform parent;

			private GameObject _003Cgo_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CDoDespawnAfterSeconds_003Ed__56(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CGetEnumerator_003Ed__73 : IEnumerator<Transform>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private Transform _003C_003E2__current;

			public SpawnPool _003C_003E4__this;

			private int _003Ci_003E5__2;

			Transform IEnumerator<Transform>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CGetEnumerator_003Ed__73(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CListForAudioStop_003Ed__63 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public AudioSource src;

			public SpawnPool _003C_003E4__this;

			private GameObject _003CsrcGameObject_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CListForAudioStop_003Ed__63(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CListenForEmitDespawn_003Ed__64 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public ParticleSystem emitter;

			public SpawnPool _003C_003E4__this;

			private float _003Csafetimer_003E5__2;

			private GameObject _003CemitterGO_003E5__3;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CListenForEmitDespawn_003Ed__64(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[CompilerGenerated]
		private sealed class _003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__74 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public SpawnPool _003C_003E4__this;

			private int _003Ci_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__74(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		public string poolName;

		public bool matchPoolScale;

		public bool matchPoolLayer;

		public bool dontReparent;

		public bool _dontDestroyOnLoad;

		public bool logMessages;

		public List<PrefabPool> _perPrefabPoolOptions;

		public Dictionary<object, bool> prefabsFoldOutStates;

		public float maxParticleDespawnTime;

		public PrefabsDict prefabs;

		public Dictionary<object, bool> _editorListItemStates;

		private List<PrefabPool> _prefabPools;

		internal List<Transform> _spawned;

		public InstantiateDelegate instantiateDelegates;

		public DestroyDelegate destroyDelegates;

		public bool dontDestroyOnLoad
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public Transform group { get; private set; }

		public Dictionary<string, PrefabPool> prefabPools => null;

		public Transform this[int index]
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public int Count => 0;

		public bool IsReadOnly => false;

		private void Awake()
		{
		}

		internal GameObject InstantiatePrefab(GameObject prefab, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		internal void DestroyInstance(GameObject instance)
		{
		}

		private void OnDestroy()
		{
		}

		public void CreatePrefabPool(PrefabPool prefabPool)
		{
		}

		public void Add(Transform instance, string prefabName, bool despawn, bool parent)
		{
		}

		public void Add(Transform item)
		{
		}

		public void Remove(Transform item)
		{
		}

		public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot, Transform parent)
		{
			return null;
		}

		public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		public Transform Spawn(Transform prefab)
		{
			return null;
		}

		public Transform Spawn(Transform prefab, Transform parent)
		{
			return null;
		}

		public Transform Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
		{
			return null;
		}

		public Transform Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		public Transform Spawn(GameObject prefab)
		{
			return null;
		}

		public Transform Spawn(GameObject prefab, Transform parent)
		{
			return null;
		}

		public Transform Spawn(string prefabName)
		{
			return null;
		}

		public Transform Spawn(string prefabName, Transform parent)
		{
			return null;
		}

		public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot, Transform parent)
		{
			return null;
		}

		public AudioSource Spawn(AudioSource prefab, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		public AudioSource Spawn(AudioSource prefab)
		{
			return null;
		}

		public AudioSource Spawn(AudioSource prefab, Transform parent)
		{
			return null;
		}

		public AudioSource Spawn(AudioSource prefab, Vector3 pos, Quaternion rot, Transform parent)
		{
			return null;
		}

		public ParticleSystem Spawn(ParticleSystem prefab, Vector3 pos, Quaternion rot)
		{
			return null;
		}

		public ParticleSystem Spawn(ParticleSystem prefab, Vector3 pos, Quaternion rot, Transform parent)
		{
			return null;
		}

		public void Despawn(Transform instance)
		{
		}

		public void Despawn(Transform instance, Transform parent)
		{
		}

		public void Despawn(Transform instance, float seconds)
		{
		}

		public void Despawn(Transform instance, float seconds, Transform parent)
		{
		}

		[IteratorStateMachine(typeof(_003CDoDespawnAfterSeconds_003Ed__56))]
		private IEnumerator DoDespawnAfterSeconds(Transform instance, float seconds, bool useParent, Transform parent)
		{
			return null;
		}

		public void DespawnAll()
		{
		}

		public bool IsSpawned(Transform instance)
		{
			return false;
		}

		public PrefabPool GetPrefabPool(Transform prefab)
		{
			return null;
		}

		public PrefabPool GetPrefabPool(GameObject prefab)
		{
			return null;
		}

		public Transform GetPrefab(Transform instance)
		{
			return null;
		}

		public GameObject GetPrefab(GameObject instance)
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CListForAudioStop_003Ed__63))]
		private IEnumerator ListForAudioStop(AudioSource src)
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CListenForEmitDespawn_003Ed__64))]
		private IEnumerator ListenForEmitDespawn(ParticleSystem emitter)
		{
			return null;
		}

		public override string ToString()
		{
			return null;
		}

		public bool Contains(Transform item)
		{
			return false;
		}

		public void CopyTo(Transform[] array, int arrayIndex)
		{
		}

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__73))]
		public IEnumerator<Transform> GetEnumerator()
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CSystem_002DCollections_002DIEnumerable_002DGetEnumerator_003Ed__74))]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return null;
		}

		public int IndexOf(Transform item)
		{
			return 0;
		}

		public void Insert(int index, Transform item)
		{
		}

		public void RemoveAt(int index)
		{
		}

		public void Clear()
		{
		}

		bool ICollection<Transform>.Remove(Transform item)
		{
			return false;
		}
	}
}
