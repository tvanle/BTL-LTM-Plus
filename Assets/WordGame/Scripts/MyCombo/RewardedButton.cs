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

this.InvokeRepeating("IUpdate", 0, 1);
this.button.onClick.AddListener(this.OnClick);
    }

    private void IUpdate()
    {
        this.SetActive(this.IsAvailableToShow());
    }

    private void SetActive(bool isActive)
    {
        this.button.interactable = isActive;
        this.lightObj.SetActive(isActive);
    }

    public void OnClick()
    {
        AdmobController.instance.ShowRewardBasedVideo();
    }

    public bool IsAvailableToShow()
    {
        return this.IsActionAvailable() && this.IsAdAvailable();
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