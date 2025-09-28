using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordGame.Network
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher instance;
        private readonly Queue<Action> _executionQueue = new Queue<Action>();

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (instance != null) return instance;

                // Only try to find/create if we're on the main thread
                if (!Application.isPlaying)
                {
                    Debug.LogError("UnityMainThreadDispatcher not available - application not playing");
                    return null;
                }

                // This should only be called from main thread during initialization
                // If called from background thread, instance should already exist
                Debug.LogError("UnityMainThreadDispatcher.Instance accessed before initialization. Call Initialize() from main thread first.");
                return null;
            }
        }

        public static void Initialize()
        {
            if (instance != null) return;

            instance = FindObjectOfType<UnityMainThreadDispatcher>();

            if (instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        public void Enqueue(Action action)
        {
            lock (this._executionQueue)
            {
                this._executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (this._executionQueue)
            {
                while (this._executionQueue.Count > 0)
                {
                    var action = this._executionQueue.Dequeue();

                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error executing action on main thread: {ex.Message}");
                    }
                }
            }
        }
    }
}