using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> queue = new Queue<Action>();
    private static UnityMainThreadDispatcher instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Enqueue(Action action)
    {
        lock (queue) queue.Enqueue(action);
    }

    void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
                queue.Dequeue()?.Invoke();
        }
    }
}
