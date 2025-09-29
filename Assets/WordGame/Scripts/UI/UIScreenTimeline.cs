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

    public bool IsPlaying => isPlaying;

    public async Task PlayShowAnimation(bool fromLeft = false)
    {
        if (isPlaying) return;

        isPlaying = true;
        currentTask = new TaskCompletionSource<bool>();

        var rectTransform = GetComponent<RectTransform>();
        var direction = fromLeft ? -1f : 1f;
        var startX = rectTransform.rect.width * direction;
        var endX = 0f;

        await AnimatePosition(rectTransform, startX, endX, showDuration, showCurve);

        isPlaying = false;
        currentTask.SetResult(true);
    }

    public async Task PlayHideAnimation(bool toLeft = false)
    {
        if (isPlaying) return;

        isPlaying = true;
        currentTask = new TaskCompletionSource<bool>();

        var rectTransform = GetComponent<RectTransform>();
        var direction = toLeft ? -1f : 1f;
        var startX = 0f;
        var endX = rectTransform.rect.width * direction;

        await AnimatePosition(rectTransform, startX, endX, hideDuration, hideCurve);

        isPlaying = false;
        currentTask.SetResult(true);
    }

    public async Task WaitForCompletion()
    {
        if (currentTask != null)
        {
            await currentTask.Task;
        }
    }

    private async Task AnimatePosition(RectTransform rectTransform, float startX, float endX, float duration, AnimationCurve curve)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = curve.Evaluate(progress);

            float currentX = Mathf.Lerp(startX, endX, curveValue);
            rectTransform.anchoredPosition = new Vector2(currentX, rectTransform.anchoredPosition.y);

            await Task.Yield();
        }

        rectTransform.anchoredPosition = new Vector2(endX, rectTransform.anchoredPosition.y);
    }
}