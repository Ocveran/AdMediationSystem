#define _AMS_IRONSOURCE

using UnityEngine;
using System.Collections.Generic;
using Boomlagoon.JSON;
using System.Linq;
using System.Collections;

namespace Virterix.AdMediation
{
    public class IronSourceAdapter : AdNetworkAdapter
    {
        public enum IrnSrcBannerSize
        {
            Banner,
            Large,
            Rectangle,
            Smart
        }

        public enum IrnSrcBannerPosition
        {
            Top,
            Bottom
        }

        public int m_timeout = 120;

        private AdInstance m_interstitialInstance;
        private AdInstance m_incentivizedInstance;
        //private AdInstance m_bannerInstance;

        private AdInstance m_currBannerInstance;
        private bool m_bannerVisibled;
        private AdState m_bannerState;

        public static string GetSDKVersion()
        {
            string version = string.Empty;
#if UNITY_EDITOR && _AMS_IRONSOURCE
            version = IronSource.pluginVersion();
#endif
            return version;
        }

#if _AMS_IRONSOURCE
        private void OnEnable()
        {
            SubscribeEvents();
        }

        private new void OnDisable()
        {
            base.OnDisable();
            UnsubscribeEvents();
        }

        private void OnApplicationPause(bool isPaused)
        {
            IronSource.Agent.onApplicationPause(isPaused);
        }

        public static IronSourceBannerSize GetBannerSize(AdInstance adInstance)
        {
            var bannerParameters = adInstance.m_adInstanceParams as IronSourceAdInstanceBannerParameters;
            var nativeAdSize = ConvertToAdSize(bannerParameters.m_bannerSize);
            return nativeAdSize;
        }

        public static IronSourceBannerPosition GetBannerPosition(AdInstance adInstance, string placement)
        {
            IronSourceBannerPosition nativeBannerPosition = IronSourceBannerPosition.BOTTOM;
            var irnSrcAdInstanceParams = adInstance.m_adInstanceParams as IronSourceAdInstanceBannerParameters;
            var bannerPosition = irnSrcAdInstanceParams.m_bannerPositions.FirstOrDefault(p => p.m_placementName == placement);
            nativeBannerPosition = ConvertToAdPosition(bannerPosition.m_bannerPosition);
            return nativeBannerPosition;
        }

        public static IronSourceBannerSize ConvertToAdSize(IrnSrcBannerSize bannerSize)
        {
            IronSourceBannerSize nativeAdSize = IronSourceBannerSize.SMART;
            switch (bannerSize)
            {
                case IrnSrcBannerSize.Banner:
                    nativeAdSize = IronSourceBannerSize.BANNER;
                    break;
                case IrnSrcBannerSize.Large:
                    nativeAdSize = IronSourceBannerSize.LARGE;
                    break;
                case IrnSrcBannerSize.Rectangle:
                    nativeAdSize = IronSourceBannerSize.RECTANGLE;
                    break;
                case IrnSrcBannerSize.Smart:
                    nativeAdSize = IronSourceBannerSize.SMART;
                    break;
            }
            return nativeAdSize;
        }

        public static IronSourceBannerPosition ConvertToAdPosition(IrnSrcBannerPosition bannerPosition)
        {
            IronSourceBannerPosition nativeAdPosition = IronSourceBannerPosition.BOTTOM;
            switch (bannerPosition)
            {
                case IrnSrcBannerPosition.Bottom:
                    nativeAdPosition = IronSourceBannerPosition.BOTTOM;
                    break;
                case IrnSrcBannerPosition.Top:
                    nativeAdPosition = IronSourceBannerPosition.TOP;
                    break;
            }
            return nativeAdPosition;
        }

        private void SubscribeEvents()
        {
            //Add Rewarded Video Events
            IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
            IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += RewardedVideoAvailabilityChangedEvent;
            IronSourceEvents.onRewardedVideoAdStartedEvent += RewardedVideoAdStartedEvent;
            IronSourceEvents.onRewardedVideoAdEndedEvent += RewardedVideoAdEndedEvent;
            IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
            IronSourceEvents.onRewardedVideoAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
            IronSourceEvents.onRewardedVideoAdClickedEvent += RewardedVideoAdClickedEvent;

            // Add Interstitial Events
            IronSourceEvents.onInterstitialAdReadyEvent += InterstitialAdReadyEvent;
            IronSourceEvents.onInterstitialAdLoadFailedEvent += InterstitialAdLoadFailedEvent;
            IronSourceEvents.onInterstitialAdShowSucceededEvent += InterstitialAdShowSucceededEvent;
            IronSourceEvents.onInterstitialAdShowFailedEvent += InterstitialAdShowFailedEvent;
            IronSourceEvents.onInterstitialAdClickedEvent += InterstitialAdClickedEvent;
            IronSourceEvents.onInterstitialAdOpenedEvent += InterstitialAdOpenedEvent;
            IronSourceEvents.onInterstitialAdClosedEvent += InterstitialAdClosedEvent;

            // Add Banner Events
            IronSourceEvents.onBannerAdLoadedEvent += BannerAdLoadedEvent;
            IronSourceEvents.onBannerAdLoadFailedEvent += BannerAdLoadFailedEvent;
            IronSourceEvents.onBannerAdClickedEvent += BannerAdClickedEvent;
            IronSourceEvents.onBannerAdScreenPresentedEvent += BannerAdScreenPresentedEvent;
            IronSourceEvents.onBannerAdScreenDismissedEvent += BannerAdScreenDismissedEvent;
            IronSourceEvents.onBannerAdLeftApplicationEvent += BannerAdLeftApplicationEvent;

            //Add ImpressionSuccess Event
            IronSourceEvents.onImpressionSuccessEvent += ImpressionSuccessEvent;
        }

        private void UnsubscribeEvents()
        {
            //Add Rewarded Video Events
            IronSourceEvents.onRewardedVideoAdOpenedEvent -= RewardedVideoAdOpenedEvent;
            IronSourceEvents.onRewardedVideoAdClosedEvent -= RewardedVideoAdClosedEvent;
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent -= RewardedVideoAvailabilityChangedEvent;
            IronSourceEvents.onRewardedVideoAdStartedEvent -= RewardedVideoAdStartedEvent;
            IronSourceEvents.onRewardedVideoAdEndedEvent -= RewardedVideoAdEndedEvent;
            IronSourceEvents.onRewardedVideoAdRewardedEvent -= RewardedVideoAdRewardedEvent;
            IronSourceEvents.onRewardedVideoAdShowFailedEvent -= RewardedVideoAdShowFailedEvent;
            IronSourceEvents.onRewardedVideoAdClickedEvent -= RewardedVideoAdClickedEvent;

            // Add Interstitial Events
            IronSourceEvents.onInterstitialAdReadyEvent -= InterstitialAdReadyEvent;
            IronSourceEvents.onInterstitialAdLoadFailedEvent -= InterstitialAdLoadFailedEvent;
            IronSourceEvents.onInterstitialAdShowSucceededEvent -= InterstitialAdShowSucceededEvent;
            IronSourceEvents.onInterstitialAdShowFailedEvent -= InterstitialAdShowFailedEvent;
            IronSourceEvents.onInterstitialAdClickedEvent -= InterstitialAdClickedEvent;
            IronSourceEvents.onInterstitialAdOpenedEvent -= InterstitialAdOpenedEvent;
            IronSourceEvents.onInterstitialAdClosedEvent -= InterstitialAdClosedEvent;

            // Add Banner Events
            IronSourceEvents.onBannerAdLoadedEvent -= BannerAdLoadedEvent;
            IronSourceEvents.onBannerAdLoadFailedEvent -= BannerAdLoadFailedEvent;
            IronSourceEvents.onBannerAdClickedEvent -= BannerAdClickedEvent;
            IronSourceEvents.onBannerAdScreenPresentedEvent -= BannerAdScreenPresentedEvent;
            IronSourceEvents.onBannerAdScreenDismissedEvent -= BannerAdScreenDismissedEvent;
            IronSourceEvents.onBannerAdLeftApplicationEvent -= BannerAdLeftApplicationEvent;

            //Add ImpressionSuccess Event
            IronSourceEvents.onImpressionSuccessEvent -= ImpressionSuccessEvent;
        }

        protected override string AdInstanceParametersFolder
        {
            get { return IronSourceAdInstanceBannerParameters._AD_INSTANCE_PARAMETERS_FOLDER; }
        }

        protected override void InitializeParameters(Dictionary<string, string> parameters, JSONArray jsonAdInstances, bool isPersonalizedAds = true)
        {
            base.InitializeParameters(parameters, jsonAdInstances);

            string appKey = "";
            if (parameters != null)
            {
                if (!parameters.TryGetValue("appId", out appKey))
                {
                    appKey = "";
                }
            }

            m_interstitialInstance = AdFactory.CreateAdInstacne(this, AdType.Interstitial, AdInstance.AD_INSTANCE_DEFAULT_NAME, "", m_timeout);
            AddAdInstance(m_interstitialInstance);
            m_incentivizedInstance = AdFactory.CreateAdInstacne(this, AdType.Incentivized, AdInstance.AD_INSTANCE_DEFAULT_NAME, "", m_timeout);
            AddAdInstance(m_incentivizedInstance);
            //m_bannerInstance = AdFactory.CreateAdInstacne(this, AdType.Banner, AdInstance.AD_INSTANCE_DEFAULT_NAME, "", m_timeout);
            //AddAdInstance(m_bannerInstance);

            SetPersonalizedAds(isPersonalizedAds);
            IronSource.Agent.init(appKey, IronSourceAdUnits.INTERSTITIAL, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.BANNER);

            IronSource.Agent.validateIntegration();
        }

        public override void Prepare(AdInstance adInstance = null, string placement = AdMediationSystem.PLACEMENT_DEFAULT_NAME)
        {
            switch (adInstance.m_adType)
            {
                case AdType.Banner:
                    if (m_bannerState != AdState.Loading)
                    {
                        float requestDelay = 0.0f;
                        if (m_bannerState == AdState.Received)
                        {
                            IronSource.Agent.destroyBanner();
                            requestDelay = 0.5f;
                        }
                        m_bannerState = AdState.Loading;
                        StartCoroutine(RequestBanner(adInstance, placement, requestDelay));
                    }
                    break;
                case AdType.Interstitial:
                    IronSource.Agent.loadInterstitial();
                    break;
                case AdType.Incentivized:
                    break;
            }
        }

        private IEnumerator RequestBanner(AdInstance adInstance, string placement, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            IronSourceBannerPosition bannerPos = GetBannerPosition(adInstance, placement);
            IronSourceBannerSize bannerSize = GetBannerSize(adInstance);         
            m_currBannerInstance = adInstance;
            IronSource.Agent.loadBanner(bannerSize, bannerPos, placement);
            yield break;
        }

        public override bool Show(AdInstance adInstance = null, string placement = AdMediationSystem.PLACEMENT_DEFAULT_NAME)
        {
            if (adInstance.m_adType == AdType.Banner)
            {
                m_bannerVisibled = true;
            }

            if (IsReady(adInstance))
            {
                switch (adInstance.m_adType)
                {
                    case AdType.Banner:
                        IronSource.Agent.displayBanner();
                        break;
                    case AdType.Interstitial:
                        IronSource.Agent.showInterstitial(placement);
                        break;
                    case AdType.Incentivized:
                        IronSource.Agent.showRewardedVideo(placement);
                        break;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Hide(AdInstance adInstance = null)
        {
            if (adInstance.m_adType == AdType.Banner)
            {
                m_bannerVisibled = false;
                IronSource.Agent.hideBanner();
                NotifyEvent(AdEvent.Hiding, adInstance);
            }
        }

        public override void HideBannerTypeAdWithoutNotify(AdInstance adInstance = null)
        {   
            switch (adInstance.m_adType)
            {
                case AdType.Banner:
                    m_bannerVisibled = false;
                    if (m_bannerState == AdState.Received)
                    {
                        IronSource.Agent.hideBanner();
                    }
                    break;
            }
        }

        public override bool IsReady(AdInstance adInstance = null)
        {
#if UNITY_EDITOR
            return false;
#endif
            bool isReady = false;
            switch (adInstance.m_adType)
            {
                case AdType.Banner:
                    isReady = adInstance == m_currBannerInstance && m_bannerState == AdState.Received;
                    break;
                case AdType.Interstitial:
                    isReady = IronSource.Agent.isInterstitialReady();
                    break;
                case AdType.Incentivized:
                    isReady = IronSource.Agent.isRewardedVideoAvailable();
                    break;
            }
            return isReady;
        }

        protected override void SetPersonalizedAds(bool isPersonalizedAds)
        {
            IronSource.Agent.setConsent(isPersonalizedAds);
            IronSource.Agent.setMetaData("do_not_sell", isPersonalizedAds ? "false" : "true");
        }

        public override void NotifyEvent(AdEvent adEvent, AdInstance adInstance)
        {
            if (adInstance.m_adType == AdType.Banner && adEvent == AdEvent.PreparationFailed)
            {
                m_bannerState = AdState.Unavailable;
            }
            base.NotifyEvent(adEvent, adInstance);
        }

        //------------------------------------------------------------------------
        #region Interstitial callback handlers

        void InterstitialAdReadyEvent()
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.InterstitialAdReadyEvent()");
#endif
            m_interstitialInstance.State = AdState.Received;
            AddEvent(m_interstitialInstance.m_adType, AdEvent.Prepared, m_interstitialInstance);
        }

        void InterstitialAdLoadFailedEvent(IronSourceError error)
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.InterstitialAdLoadFailedEvent() code:" + error.getErrorCode() + " desc:" + error.getDescription());
#endif
            m_interstitialInstance.State = AdState.Unavailable;
            AddEvent(m_interstitialInstance.m_adType, AdEvent.PreparationFailed, m_interstitialInstance);
        }

        void InterstitialAdShowSucceededEvent()
        {
            AddEvent(m_interstitialInstance.m_adType, AdEvent.Show, m_interstitialInstance);
        }

        void InterstitialAdShowFailedEvent(IronSourceError error)
        {
            m_interstitialInstance.State = AdState.Unavailable;
        }

        void InterstitialAdClickedEvent()
        {
            AddEvent(m_interstitialInstance.m_adType, AdEvent.Click, m_interstitialInstance);
        }

        void InterstitialAdOpenedEvent()
        {
        }

        void InterstitialAdClosedEvent()
        {
            AddEvent(m_interstitialInstance.m_adType, AdEvent.Hiding, m_interstitialInstance);
        }

        #endregion // Interstitial callback handlers

        //------------------------------------------------------------------------
        #region Rewarded Video callback handlers

        void RewardedVideoAvailabilityChangedEvent(bool canShowAd)
        {
            m_incentivizedInstance.State = AdState.Received;
            AddEvent(m_incentivizedInstance.m_adType, AdEvent.Prepared, m_incentivizedInstance);
        }

        void RewardedVideoAdOpenedEvent()
        {
            AddEvent(m_incentivizedInstance.m_adType, AdEvent.Show, m_incentivizedInstance);
        }

        void RewardedVideoAdRewardedEvent(IronSourcePlacement ssp)
        {
            m_lastReward.label = ssp.getRewardName();
            m_lastReward.amount = ssp.getRewardAmount();
            AddEvent(m_incentivizedInstance.m_adType, AdEvent.IncentivizedCompleted, m_incentivizedInstance);
        }

        void RewardedVideoAdClosedEvent()
        {
            AddEvent(m_incentivizedInstance.m_adType, AdEvent.Hiding, m_incentivizedInstance);
        }

        void RewardedVideoAdStartedEvent()
        {
        }

        void RewardedVideoAdEndedEvent()
        {
        }

        void RewardedVideoAdShowFailedEvent(IronSourceError error)
        {
            m_incentivizedInstance.State = AdState.Unavailable;
        }

        void RewardedVideoAdClickedEvent(IronSourcePlacement ssp)
        {
            AddEvent(m_incentivizedInstance.m_adType, AdEvent.Click, m_incentivizedInstance);
        }

        #endregion // Rewarded Video callback handlers

        //------------------------------------------------------------------------
        #region Banner callback handlers
        private void BannerAdLoadedEvent()
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.BannerAdLoadedEvent()");
#endif

            m_bannerState = AdState.Received;
            if (m_bannerVisibled)
            {
                IronSource.Agent.displayBanner();
            }
            else
            {
                IronSource.Agent.hideBanner();
            }
            AddEvent(AdType.Banner, AdEvent.Prepared, m_currBannerInstance);
        }

        private void BannerAdLoadFailedEvent(IronSourceError error)
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.BannerAdLoadedEvent() code: " + error.getCode() + ", description: " + error.getDescription());
#endif
            m_bannerState = AdState.Unavailable;
            AddEvent(AdType.Banner, AdEvent.PreparationFailed, m_currBannerInstance);
        }

        void BannerAdClickedEvent()
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.BannerAdClickedEvent()");
#endif
            AddEvent(AdType.Banner, AdEvent.Click, m_currBannerInstance);
        }

        void BannerAdScreenPresentedEvent()
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.BannerAdScreenPresentedEvent()");
#endif
            AddEvent(AdType.Banner, AdEvent.Show, m_currBannerInstance);
        }

        void BannerAdScreenDismissedEvent()
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.BannerAdScreenDismissedEvent()");
#endif
        }

        void BannerAdLeftApplicationEvent()
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.BannerAdLeftApplicationEvent()");
#endif
        }

        #endregion // Banner callback handlers

        //------------------------------------------------------------------------
        #region ImpressionSuccess callback handler

        void ImpressionSuccessEvent(IronSourceImpressionData impressionData)
        {
#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("[AMS] IronSourceAdapter.ImpressionSuccessEvent()");
            Debug.Log("unity - script: I got ImpressionSuccessEvent ToString(): " + impressionData.ToString());
            Debug.Log("unity - script: I got ImpressionSuccessEvent allData: " + impressionData.allData);
#endif
        }

        #endregion

#endif // _AMS_IRONSOURCE

    }
} // namespace Virterix.AdMediation