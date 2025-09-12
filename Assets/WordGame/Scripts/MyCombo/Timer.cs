using UnityEngine;
using System.Threading.Tasks;

public class Timer{
    public delegate void TaskCallback();

    public static async void Schedule(MonoBehaviour behaviour, float delay, TaskCallback task)
    {
        await DoTask(task, delay);
    }

    private static async System.Threading.Tasks.Task DoTask(TaskCallback task, float delay)
    {
        await System.Threading.Tasks.Task.Delay((int)(delay * 1000));
        task();
    }
}