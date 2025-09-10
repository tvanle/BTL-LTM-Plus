using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class TimerText : MonoBehaviour
{
    public bool countUp = true;
    public bool runOnStart = false;
    public int timeValue = 0;

    public bool showHour = false;
    public bool showMinute = true;
    public bool showSecond = true;

    public Action onCountDownComplete;

    private bool isRunning = false;

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
        this.isRunning = false;
    }

    public void Run()
    {
        if (!this.isRunning)
        {
            if (this.timeValue <= 0)
            {
                if (this.onCountDownComplete != null) this.onCountDownComplete();
                return;
            }

            this.isRunning = true;
            this.StartCoroutine(this.UpdateClockText());
        }
    }

    private IEnumerator UpdateClockText()
    {
        while (this.isRunning)
        {
            this.UpdateText();
            yield return new WaitForSeconds(1);
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
        TimeSpan t = TimeSpan.FromSeconds(this.timeValue);

        string text;
        if (this.showHour && this.showMinute && this.showSecond)
        {
            text = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
        }
        else if (this.showHour && this.showMinute)
        {
            text = string.Format("{0:D2}:{1:D2}", t.Hours, t.Minutes);
        }
        else
        {
            text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }
        this.GetComponent<Text>().text = text;
    }

    public void Stop()
    {
        this.isRunning = false;
    }
}