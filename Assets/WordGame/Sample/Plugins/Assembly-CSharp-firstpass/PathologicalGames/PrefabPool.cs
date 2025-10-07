using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PathologicalGames
{
	[Serializable]
	public class PrefabPool
	{
		[CompilerGenerated]
		private sealed class _003CCullDespawned_003Ed__37 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public PrefabPool _003C_003E4__this;

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
			public _003CCullDespawned_003Ed__37(int _003C_003E1__state)
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
		private sealed class _003CPreloadOverTime_003Ed__44 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public PrefabPool _003C_003E4__this;

			private int _003Cremainder_003E5__2;

			private int _003CnumPerFrame_003E5__3;

			private int _003CnumThisFrame_003E5__4;

			private int _003Ci_003E5__5;

			private int _003Cn_003E5__6;

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
			public _003CPreloadOverTime_003Ed__44(int _003C_003E1__state)
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

		public Transform prefab;

		internal GameObject prefabGO;

		public int preloadAmount;

		public bool preloadTime;

		public int preloadFrames;

		public float preloadDelay;

		public bool limitInstances;

		public int limitAmount;

		public bool limitFIFO;

		public bool cullDespawned;

		public int cullAbove;

		public int cullDelay;

		public int cullMaxPerPass;

		public bool _logMessages;

		private bool forceLoggingSilent;

		public SpawnPool spawnPool;

		private bool cullingActive;

		internal List<Transform> _spawned;

		internal List<Transform> _despawned;

		private bool _preloaded;

		public bool logMessages => false;

		public List<Transform> spawned => null;

		public List<Transform> despawned => null;

		public int totalCount => 0;

		internal bool preloaded
		{
			get
			{
				return false;
			}
			private set
			{
			}
		}

		public PrefabPool(Transform prefab)
		{
		}

		public PrefabPool()
		{
		}

		internal void inspectorInstanceConstructor()
		{
		}

		internal void SelfDestruct()
		{
		}

		internal bool DespawnInstance(Transform xform)
		{
			return false;
		}

		internal bool DespawnInstance(Transform xform, bool sendEventMessage)
		{
			return false;
		}

		[IteratorStateMachine(typeof(_003CCullDespawned_003Ed__37))]
		internal IEnumerator CullDespawned()
		{
			return null;
		}

		internal Transform SpawnInstance(Vector3 pos, Quaternion rot)
		{
			return null;
		}

		public Transform SpawnNew()
		{
			return null;
		}

		public Transform SpawnNew(Vector3 pos, Quaternion rot)
		{
			return null;
		}

		private void SetRecursively(Transform xform, int layer)
		{
		}

		internal void AddUnpooled(Transform inst, bool despawn)
		{
		}

		internal void PreloadInstances()
		{
		}

		[IteratorStateMachine(typeof(_003CPreloadOverTime_003Ed__44))]
		private IEnumerator PreloadOverTime()
		{
			return null;
		}

		public bool Contains(Transform transform)
		{
			return false;
		}

		private void nameInstance(Transform instance)
		{
		}
	}
}
