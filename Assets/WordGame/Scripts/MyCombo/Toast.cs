using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class Toast : MonoBehaviour
{
    public RectTransform backgroundTransform;
    public RectTransform messageTransform;

    public static Toast instance;
    [HideInInspector]
    public bool isShowing = false;

    private Queue<AToast> queue = new Queue<AToast>();

    private class AToast
    {
        public string msg;
        public float time;
        public AToast(string msg, float time)
        {
            this.msg = msg;
            this.time = time;
        }
    }

    private void Awake()
    {
        instance = this;
        this.SetEnabled(false);
    }

    public void SetMessage(string msg)
    {
        this.messageTransform.GetComponent<Text>().text = msg;
        Timer.Schedule(this, 0, () =>
        {
            this.backgroundTransform.sizeDelta = new Vector2(this.messageTransform.GetComponent<Text>().preferredWidth + 60, this.backgroundTransform.sizeDelta.y);
        });
    }

    private void Show(AToast aToast)
    {
        this.SetMessage(aToast.msg);
        this.SetEnabled(true);
        this.GetComponent<Animator>().SetBool("show", true);
        this.Invoke("Hide", aToast.time);
        this.isShowing = true;
    }

    public void ShowMessage(string msg, float time = 2f)
    {
        AToast aToast = new AToast(msg, time);
        this.queue.Enqueue(aToast);

        this.ShowOldestToast();
    }

    private void Hide()
    {
        this.GetComponent<Animator>().SetBool("show", false);
        this.Invoke("CompleteHiding", 1);
    }

    private void CompleteHiding()
    {
        this.SetEnabled(false);
        this.isShowing = false;
        this.ShowOldestToast();
    }

    private void ShowOldestToast()
    {
        if (this.queue.Count == 0) return;
        if (this.isShowing) return;

        AToast current = this.queue.Dequeue();
        this.Show(current);
    }

    private void SetEnabled(bool enabled)
    {
        foreach (Transform child in this.transform)
        {
            child.gameObject.SetActive(enabled);
        }
    }
}
