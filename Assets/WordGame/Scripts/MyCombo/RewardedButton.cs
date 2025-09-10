using UnityEngine;
using UnityEngine.UI;

public class RewardedButton : MonoBehaviour
{
    public Button button;
    public GameObject lightObj;

    private const string ACTION_NAME = "rewarded_video";

    private void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
#else
        SetActive(false);
#endif

        InvokeRepeating("IUpdate", 0, 1);
        button.onClick.AddListener(OnClick);
    }

    private void IUpdate()
    {
        SetActive(IsAvailableToShow());
    }

    private void SetActive(bool isActive)
    {
        button.interactable = isActive;
        lightObj.SetActive(isActive);
    }

    public void OnClick()
    {
        AdmobController.instance.ShowRewardBasedVideo();
    }

    public bool IsAvailableToShow()
    {
        return IsActionAvailable() && IsAdAvailable();
    }

    public bool IsActionAvailable()
    {
        return CUtils.IsActionAvailable(ACTION_NAME, GameConfig.instance.rewardedVideoPeriod);
    }

    private bool IsAdAvailable()
    {
        return true;
    }

    private void OnDestroy()
    {
    }
}