using UnityEngine;
using System.Threading.Tasks;

public class UIScreenTimeline : MonoBehaviour
{
    [SerializeField] private float showDuration = 0.5f;
    [SerializeField] private float hideDuration = 0.5f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private TaskCompletionSource<bool> currentTask;
    private bool isPlaying = false;

    public bool IsPlaying => this.isPlaying;

    public async Task PlayShowAnimation(bool fromLeft = false)
    {
        if (this.isPlaying) return;

        this.isPlaying = true;
        this.currentTask  = new TaskCompletionSource<bool>();

        var rectTransform = this.GetComponent<RectTransform>();
        var direction     = fromLeft ? -1f : 1f;
        var startX        = rectTransform.rect.width * direction;
        var endX          = 0f;

        await this.AnimatePosition(rectTransform, startX, endX, this.showDuration, this.showCurve);

        this.isPlaying = false;
        this.currentTask.SetResult(true);
    }

    public async Task PlayHideAnimation(bool toLeft = false)
    {
        if (this.isPlaying) return;

        this.isPlaying = true;
        this.currentTask  = new TaskCompletionSource<bool>();

        var rectTransform = this.GetComponent<RectTransform>();
        var direction     = toLeft ? -1f : 1f;
        var startX        = 0f;
        var endX          = rectTransform.rect.width * direction;

        await this.AnimatePosition(rectTransform, startX, endX, this.hideDuration, this.hideCurve);

        this.isPlaying = false;
        this.currentTask.SetResult(true);
    }

    public async Task WaitForCompletion()
    {
        if (this.currentTask != null)
        {
            await this.currentTask.Task;
        }
    }

    private async Task AnimatePosition(RectTransform rectTransform, float startX, float endX, float duration, AnimationCurve curve)
    {
        var elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            var progress = elapsedTime / duration;
            var curveValue = curve.Evaluate(progress);

            var currentX = Mathf.Lerp(startX, endX, curveValue);
            rectTransform.anchoredPosition = new Vector2(currentX, rectTransform.anchoredPosition.y);

            await Task.Yield();
        }

        rectTransform.anchoredPosition = new Vector2(endX, rectTransform.anchoredPosition.y);
    }
}