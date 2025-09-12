using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;

public class TimerText : MonoBehaviour
{
    public bool countUp = true;
    public bool runOnStart = false;
    public int timeValue = 0;

    public bool showHour = false;
    public bool showMinute = true;
    public bool showSecond = true;

    public Action onCountDownComplete = null;

    private bool isRunning = false;
    private CancellationTokenSource cancellationTokenSource;

    private void Start()
    {
        this.UpdateText();
        if (this.runOnStart)
        {
            this.Run();
        }
    }

    private void OnDisable()
    {
        this.Stop();
    }

    public async void Run()
    {
        if (!this.isRunning)
        {
            if (this.timeValue <= 0)
            {
                if (this.onCountDownComplete != null) this.onCountDownComplete();
                return;
            }

            this.isRunning = true;
            this.cancellationTokenSource = new CancellationTokenSource();
            await this.UpdateClockText(this.cancellationTokenSource.Token);
        }
    }

    private async Task UpdateClockText(CancellationToken cancellationToken)
    {
        try
        {
            while (this.isRunning && !cancellationToken.IsCancellationRequested)
            {
                this.UpdateText();
                await Task.Delay(1000, cancellationToken);
                if (this.countUp)
                    this.timeValue++;
                else
                {
                    if (this.timeValue == 0)
                    {
                        if (this.onCountDownComplete != null) this.onCountDownComplete();
                        this.Stop();
                    }
                    else
                        this.timeValue--;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Task was cancelled, this is expected
        }
    }

    public void SetTime(int value)
    {
        if (value < 0)
            value = 0;
        this.timeValue = value;
        this.UpdateText();
    }

    public void AddTime(int value)
    {
        this.timeValue += value;
        this.UpdateText();
    }

    private void UpdateText()
    {
        var t = TimeSpan.FromSeconds(this.timeValue);

        string text;
        if (this.showHour && this.showMinute && this.showSecond)
        {
            text = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }
        else if (this.showHour && this.showMinute)
        {
            text = $"{t.Hours:D2}:{t.Minutes:D2}";
        }
        else
        {
            text = $"{t.Minutes:D2}:{t.Seconds:D2}";
        }
        this.GetComponent<Text>().text = text;
    }

    public void Stop()
    {
        this.isRunning = false;
        if (this.cancellationTokenSource != null)
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;
        }
    }
}