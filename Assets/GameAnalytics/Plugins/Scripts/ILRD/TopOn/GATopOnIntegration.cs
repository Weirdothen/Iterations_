using System;
using UnityEngine;

public class GATopOnIntegration
{
#if gameanalytics_topon_enabled && !(UNITY_EDITOR)
    private static bool _subscribed = false;
#endif

    public static void ListenForImpressions(Action<string> callback)
    {
#if gameanalytics_topon_enabled && !(UNITY_EDITOR)
        if (_subscribed)
        {
            Debug.Log("Ignoring duplicate gameanalytics subscription");
            return;
        }

        // TopOn plugin 2.x (the "TPN" generation) dropped the setListener() wrappers on
        // the ad classes; impressions are delivered as events on the platform client.
        AnyThinkAds.Api.ATInterstitialAd.Instance.client.onAdShowEvent += (sender, args) => callback(args.callbackInfo.getOriginJSONString());
        AnyThinkAds.Api.ATBannerAd.Instance.client.onAdImpressEvent += (sender, args) => callback(args.callbackInfo.getOriginJSONString());
        AnyThinkAds.Api.ATRewardedVideo.Instance.client.onAdVideoStartEvent += (sender, args) => callback(args.callbackInfo.getOriginJSONString());
        AnyThinkAds.Api.ATNativeAd.Instance.client.onAdImpressEvent += (sender, args) => callback(args.callbackInfo.getOriginJSONString());
        _subscribed = true;
#endif
    }
}
