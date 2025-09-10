using UnityEngine;
using System;

public class AdmobController : MonoBehaviour
{
    [Header("Interstitial")]
    public string androidInterstitial;
    public string iosInterstitial;

    [Header("Rewarded Video")]
    public string androidRewardedVideo;
    public string iosRewardedVideo;

    public static AdmobController instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        RequestInterstitial();

        InitRewardedVideo();
        RequestRewardBasedVideo();
    }

    private void InitRewardedVideo()
    {
    }

    public void RequestBanner()
    {
        // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
        string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = "";
#elif UNITY_IPHONE
        string adUnitId = "";
#else
        string adUnitId = "unexpected_platform";
#endif


    }

    public void RequestInterstitial()
    {
        // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
        string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = androidInterstitial.Trim();
#elif UNITY_IPHONE
        string adUnitId = iosInterstitial.Trim();
#else
        string adUnitId = "unexpected_platform";
#endif


    }

    public void RequestRewardBasedVideo()
    {
#if UNITY_EDITOR
        string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = androidRewardedVideo.Trim();
#elif UNITY_IPHONE
        string adUnitId = iosRewardedVideo.Trim();
#else
        string adUnitId = "unexpected_platform";
#endif


    }

    public void HideBanner()
    {
    }

    public bool ShowInterstitial()
    {
        return false;
    }

    public void ShowRewardBasedVideo()
    {
    }

    #region Banner callback handlers

    public void HandleAdLoaded(object sender, EventArgs args)
    {
        HideBanner();
        print("HandleAdLoaded event received.");
    }

    public void HandleAdOpened(object sender, EventArgs args)
    {
        print("HandleAdOpened event received");
    }

    public void HandleAdClosed(object sender, EventArgs args)
    {
        print("HandleAdClosed event received");
    }

    public void HandleAdLeftApplication(object sender, EventArgs args)
    {
        print("HandleAdLeftApplication event received");
    }

    #endregion

    #region Interstitial callback handlers

    #endregion

    #region RewardBasedVideo callback handlers

    public void HandleRewardBasedVideoLoaded(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleRewardBasedVideoLoaded event received");
        Timer.Schedule(this, 2, RequestRewardBasedVideo);
    }

    public void HandleRewardBasedVideoOpened(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleRewardBasedVideoOpened event received");
    }

    public void HandleRewardBasedVideoStarted(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleRewardBasedVideoStarted event received");
    }

    public void HandleRewardBasedVideoClosed(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleRewardBasedVideoClosed event received");
    }


    public void HandleRewardBasedVideoLeftApplication(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleRewardBasedVideoLeftApplication event received");
    }

    #endregion
}