﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;
using System.Linq;
using System.IO;

namespace Virterix.AdMediation
{
    public enum AdEvent
    {
        None = 0,
        Selected,
        Prepared,
        Show,
        Click,
        Hide,
        PrepareFailure,
        IncentivizedComplete,
        IncentivizedIncomplete
    }

    public partial class AdNetworkAdapter : MonoBehaviour
    {
        //_______________________________________________________________________________
        #region Classes & Structs
        //-------------------------------------------------------------------------------
        /// <summary>
        /// Describes the parameters of the disabling network from handling when failed load
        /// </summary>
        public struct TimeoutParams
        {
            public AdType m_adType;
            public float m_timeout;

            public float FailedLoadingTime
            {
                set
                {
                    m_failedLoadTime = value;
                    m_isSetupFailedLoadTime = true;
                }
                get { return m_failedLoadTime; }
            }

            bool m_isSetupFailedLoadTime;
            float m_failedLoadTime;

            public bool IsTimeout
            {
                get
                {
                    bool active = false;
                    bool canUsed = m_adType != AdType.Unknown && m_timeout > 0.01f;

                    if (canUsed && m_isSetupFailedLoadTime)
                    {
                        float elapsedTime = Time.realtimeSinceStartup - m_failedLoadTime;
                        active = elapsedTime < m_timeout;
                        m_isSetupFailedLoadTime = active;
                    }
                    return active;
                }
            }
        }

        [System.Serializable]
        public struct AdParam
        {
            public AdType m_adType;
            public bool m_isCheckAvailabilityWhenPreparing;
        }

        public struct EventParam
        {
            public AdType m_adType;
            public AdInstanceData m_adInstance;
            public AdEvent m_adEvent;
        }

        public enum AdState
        {
            Uncertain = 0,
            Loading,
            Received,
            NotAvailable
        }
        #endregion Classes & Structs

        public event Action<AdNetworkAdapter, AdType, AdEvent, AdInstanceData> OnEvent = delegate { };

        public string m_networkName;
        public AdParam[] m_adSupportParams;

        //_______________________________________________________________________________
        #region Properties
        //-------------------------------------------------------------------------------

        public string BannerPlacement
        {
            set; get;
        } = null;

        private string AdInstanceParametersPath
        {
            get
            {
                string path = "";
                if (AdInstanceParametersFolder.Length > 0)
                {
                    path = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}/", AdMediationSystem._AD_SETTINGS_PATH,
                        AdMediationSystem.Instance.m_projectName, AdMediationSystem._AD_INSTANCE_PARAMETERS_ROOT_FOLDER, AdInstanceParametersFolder);
                }
                return path;
            }
        }

        protected virtual string AdInstanceParametersFolder
        {
            get
            {
                return "";
            }
        }

        #endregion Properties

        private bool[] m_arrLastAdPreparedState;
        private bool[] m_arrEnableState;
        private AdState[] m_arrAdState;

        private List<EventParam> m_events = new List<EventParam>();
        private TimeoutParams[] m_timeoutParameters;
        protected List<IAdInstanceParameters> m_adInstanceParameters = new List<IAdInstanceParameters>();
        private List<AdInstanceData> m_adInstances = new List<AdInstanceData>();

        //_______________________________________________________________________________
        #region MonoBehavior Methods
        //-------------------------------------------------------------------------------

        protected void Awake()
        {
            int count = Enum.GetNames(typeof(AdType)).Length;
            m_arrLastAdPreparedState = new bool[count];
            m_arrAdState = new AdState[count];
            m_arrEnableState = new bool[count];

            if (this.enabled)
            {
                for (int i = 0; i < count; i++)
                {
                    m_arrEnableState[i] = true;
                }
            }
        }

        protected void Update()
        {
            UpdateEvents();
        }

        protected void OnDisable()
        {
            UpdateEvents();
            StopAllCoroutines();
        }
        #endregion MonoBehavior Methods

        //_______________________________________________________________________________
        #region Public Methods
        //-------------------------------------------------------------------------------

        public virtual void Initialize(Dictionary<string, string> parameters = null, JSONArray adInstances = null)
        {
            if (parameters != null)
            {
                InitializeParameters(parameters, adInstances);
            }

#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("AdNetworkAdapter.Initialize() Initialize network adapter: " + m_networkName + " adInstances:" + m_adInstances.Count);
#endif
        }

        /// <summary>
        /// Not working!
        /// </summary>
        public virtual void DisableWhenInitialize()
        {

#if AD_MEDIATION_DEBUG_MODE
            Debug.Log("AdNetworkAdapter.DisableWhenInitialize() " + m_networkName);
#endif

            this.enabled = false;
        }

        public virtual bool IsReady(AdInstanceData adInstance) { return false; }

        public virtual void Prepare(AdInstanceData adInstance, string placement = AdMediationSystem._PLACEMENT_DEFAULT_NAME) { }

        public virtual bool Show(AdInstanceData adInstance, string placement = AdMediationSystem._PLACEMENT_DEFAULT_NAME) { return false; }

        public virtual void Hide(AdInstanceData adInstance) { }

        public bool IsReady(AdType adType, string adInstanceName = AdInstanceData._AD_INSTANCE_DEFAULT_NAME)
        {
            AdInstanceData adInstance = GetAdInstance(adType, adInstanceName);
            bool result = IsReady(adInstance);
            return result;
        }

        public void Prepare(AdType adType, string adInstanceName = AdInstanceData._AD_INSTANCE_DEFAULT_NAME) 
        {
            AdInstanceData adInstance = GetAdInstance(adType, adInstanceName);
            Prepare(adInstance);
        }

        public bool Show(AdType adType, string adInstanceName = AdInstanceData._AD_INSTANCE_DEFAULT_NAME) 
        {
            AdInstanceData adInstance = GetAdInstance(adType, adInstanceName);
            bool result = Show(adInstance);
            return result; 
        }

        public void Hide(AdType adType, string adInstanceName = AdInstanceData._AD_INSTANCE_DEFAULT_NAME) 
        {
            AdInstanceData adInstance = GetAdInstance(adType, adInstanceName);
            Hide(adInstance);
        }

        public virtual void HideBannerTypeAdWithoutNotify(AdInstanceData adInstance)
        {
        }

        public virtual void ResetAd(AdInstanceData adInstance)
        {
        }

        public virtual bool IsSupported(AdType adType)
        {
            AdParam adSupportParam = GetAdParam(adType);
            bool isSupported = adSupportParam.m_adType != AdType.Unknown;
            return isSupported;
        }

        public bool IsCheckAvailabilityWhenPreparing(AdType adType)
        {
            AdParam adSupportParam = GetAdParam(adType);
            return adSupportParam.m_isCheckAvailabilityWhenPreparing;
        }

        public void AddEvent(AdType adType, AdEvent adEvent, AdInstanceData adInstance = null)
        {
            EventParam eventParam = new EventParam();
            eventParam.m_adType = adType;
            eventParam.m_adInstance = adInstance;
            eventParam.m_adEvent = adEvent;
            m_events.Add(eventParam);
        }

        public void NotifyEvent(AdType adType, AdEvent adEvent, AdInstanceData adInstance = null)
        {
            string adInstanceName = adInstance != null ? adInstance.Name : AdInstanceData._AD_INSTANCE_DEFAULT_NAME;

            if (adEvent == AdEvent.PrepareFailure || adEvent == AdEvent.Prepared)
            {
                if (adInstance != null)
                {
                    adInstance.m_lastAdPrepared = adEvent == AdEvent.Prepared;
                }
                else
                {
                    m_arrLastAdPreparedState[(int)adType] = adEvent == AdEvent.Prepared;
                }
            }
            OnEvent(this, adType, adEvent, adInstance);
        }

        public bool GetLastAdPreparedStatus(AdType adType, AdInstanceData adInstance = null)
        {
            if (adInstance == null)
            {
                return m_arrLastAdPreparedState[(int)adType];
            }
            else
            {
                return adInstance.m_lastAdPrepared;
            }
        }

        TimeoutParams GetTimeoutParams(AdType adType)
        {
            TimeoutParams foundParams = new TimeoutParams();
            if (m_timeoutParameters != null)
            {
                foreach (TimeoutParams timeoutParams in m_timeoutParameters)
                {
                    if (timeoutParams.m_adType == adType)
                    {
                        foundParams = timeoutParams;
                        break;
                    }
                }
            }
            return foundParams;
        }

        public bool IsTimeout(AdType adType, AdInstanceData adInstance)
        {
            bool isTimeout = false;
            if (adInstance != null)
            {
                isTimeout = adInstance.m_timeout.Value.IsTimeout;
            }
            else
            {
                if (m_timeoutParameters != null)
                {
                    TimeoutParams failedInfo = GetTimeoutParams(adType);
                    isTimeout = failedInfo.IsTimeout;
                }
            }
            return isTimeout;
        }

        public void SaveFailedLoadingTime(AdType adType, AdInstanceData adInstance)
        {
            if (adInstance != null)
            {
                TimeoutParams timeoutParameters = adInstance.m_timeout.Value;
                timeoutParameters.FailedLoadingTime = Time.realtimeSinceStartup;
                adInstance.m_timeout = timeoutParameters;
            }
            else
            {
                if (m_timeoutParameters != null)
                {
                    for (int i = 0; i < m_timeoutParameters.Length; i++)
                    {
                        if (m_timeoutParameters[i].m_adType == adType)
                        {
                            m_timeoutParameters[i].FailedLoadingTime = Time.realtimeSinceStartup;
                            break;
                        }
                    }
                }
            }
        }

        public IAdInstanceParameters GetAdInstanceParams(AdType adType, string adInstanceName)
        {
            IAdInstanceParameters foundParams = null;
            foreach (IAdInstanceParameters itemParameters in m_adInstanceParameters)
            {
                if (itemParameters.AdvertiseType == adType && itemParameters.Name == adInstanceName)
                {
                    foundParams = itemParameters;
                }
            }
            return foundParams;
        }

        public void AddAdInstance(AdInstanceData adInstance)
        {
            m_adInstances.Add(adInstance);
        }

        public AdInstanceData GetAdInstance(AdType adType, string adInstanceName)
        {
            AdInstanceData foundData = null;

            if (m_adInstances.Count > 0)
            {
                foreach (AdInstanceData data in m_adInstances)
                {
                    if (data.m_adType != adType)
                    {
                        continue;
                    }
                    if (data.Name == adInstanceName)
                    {
                        foundData = data;
                        break;
                    }
                }
            }
            return foundData;
        }

        public AdInstanceData GetAdInstance(string adInstanceName)
        {
            AdInstanceData foundData = null;

            if (m_adInstances.Count > 0)
            {
                foreach (AdInstanceData data in m_adInstances)
                {
                    if (data.Name == adInstanceName)
                    {
                        foundData = data;
                        break;
                    }
                }
            }
            return foundData;
        }

        public AdInstanceData GetAdInstanceByAdId(string adId)
        {
            AdInstanceData foundData = null;

            if (m_adInstances.Count > 0)
            {
                foreach (AdInstanceData data in m_adInstances)
                {
                    if (data.m_adID == adId)
                    {
                        foundData = data;
                        break;
                    }
                }
            }
            return foundData;
        }

        public AdType GetAdTypeByAdInstanceId(string adId)
        {
            AdType instanceAdType = AdType.Unknown;

            foreach (AdInstanceData adInstance in m_adInstances)
            {
                if (adInstance.m_adID == adId)
                {
                    instanceAdType = adInstance.m_adType;
                    break;
                }
            }
            return instanceAdType;
        }

        /// <summary>
        /// GDPR Compliance
        /// </summary>
        /// <param name="isPersonalizedAds"></param>
        public virtual void SetPersonalizedAds(bool isPersonalizedAds)
        {
        }

        #endregion Public Methods

        //_______________________________________________________________________________
        #region Internal Methods
        //-------------------------------------------------------------------------------

        private void UpdateEvents()
        {
            if (m_events.Count > 0)
            {
                for (int i = 0; i < m_events.Count; i++)
                {
                    EventParam eventParam = m_events[i];
                    NotifyEvent(eventParam.m_adType, eventParam.m_adEvent, eventParam.m_adInstance);
                }
                m_events.Clear();
            }
        }

        /// <summary>
        /// Initialises ad instance from config file (Default instance initialise manually). When overriding it the base method call required or setup parameters
        /// </summary>
        protected virtual void InitializeAdInstanceData(AdInstanceData adInstance, JSONValue jsonAdInstance)
        {
            adInstance.Name = jsonAdInstance.Obj.ContainsKey("name") ? jsonAdInstance.Obj.GetString("name") : AdInstanceData._AD_INSTANCE_DEFAULT_NAME;
            string parametersName = jsonAdInstance.Obj.ContainsKey("param") ? jsonAdInstance.Obj.GetString("param") : AdInstanceParameters._AD_INSTANCE_PARAMETERS_DEFAULT_NAME;
            adInstance.m_adType = AdTypeConvert.StringToAdType(jsonAdInstance.Obj.GetString("adType"));
            adInstance.m_adID = jsonAdInstance.Obj.GetString("id");
            adInstance.m_adInstanceParams = GetAdInstanceParams(adInstance.m_adType, parametersName);
            if (jsonAdInstance.Obj.ContainsKey("timeout"))
            {
                TimeoutParams timeoutParameters = new TimeoutParams();
                timeoutParameters.m_timeout = (float)jsonAdInstance.Obj.GetNumber("timeout");
                timeoutParameters.m_adType = adInstance.m_adType;
                adInstance.m_timeout = timeoutParameters;
            }

            string pepareWhenChangeNetworkKey = "prepareWhenChangeNetwork";
            if (jsonAdInstance.Obj.ContainsKey(pepareWhenChangeNetworkKey))
            {
                adInstance.m_isPepareWhenChangeNetwork = jsonAdInstance.Obj.GetBoolean(pepareWhenChangeNetworkKey);
            }

            string waitResponseTimeKey = "waitResponseTime";
            if (jsonAdInstance.Obj.ContainsKey(waitResponseTimeKey))
            {
                adInstance.m_waitingResponseTime = (float)jsonAdInstance.Obj.GetNumber(waitResponseTimeKey);
            }

            //InitializeAdInstanceTimeout(adInstance);
            m_adInstances.Add(adInstance);
        }

        /// <summary>
        /// old iplementation timeout
        /// </summary>
        /// <param name="adInstance"></param>
        /*
        private void InitializeAdInstanceTimeout(AdInstanceData adInstance)
        {
            if (adInstance.m_timeout == null)
            {
                TimeoutParams timeoutParameters = new TimeoutParams();
                TimeoutParams commonTimeout = GetTimeoutParams(adInstance.m_adType);
                timeoutParameters.m_timeout = commonTimeout.m_timeout;
                timeoutParameters.m_adType = adInstance.m_adType;
                adInstance.m_timeout = timeoutParameters;
            }
        }
        */

        protected virtual void InitializeParameters(Dictionary<string, string> parameters, JSONArray jsonAdInstances)
        {
            InitializeAdInstanceParameters();

            // Legacy timeout
            for (int i = 0; i < m_adSupportParams.Length; i++)
            {
                /*
                if (m_adSupportParams[i].m_useTimeout)
                {
                    if (m_timeoutParameters == null)
                    {
                        m_timeoutParameters = new TimeoutParams[m_adSupportParams.Length];
                    }

                    TimeoutParams timeoutParams = new TimeoutParams();
                    timeoutParams.m_adType = m_adSupportParams[i].m_adType;

                    if (parameters != null)
                    {
                        string timeoutKey = "timeout-" + AdTypeConvert.AdTypeToString(timeoutParams.m_adType);
                        string timeoutParam = "";

                        if (parameters.ContainsKey(timeoutKey))
                        {
                            timeoutParam = parameters[timeoutKey];
                        }

                        if (timeoutParam.Length > 0)
                        {
                            timeoutParams.m_timeout = (float)System.Convert.ToDouble(timeoutParam);
                        }
                    }
                    m_timeoutParameters[i] = timeoutParams;
                }*/
            }

            if (jsonAdInstances != null)
            {
                foreach (JSONValue jsonAdInstance in jsonAdInstances)
                {
                    AdInstanceData adInstance = CreateAdInstanceData(jsonAdInstance);
                    InitializeAdInstanceData(adInstance, jsonAdInstance);
                }
            }
        }

        /// <summary>
        /// Should implementation in inheritors (Fabric method)
        /// </summary>
        protected virtual AdInstanceData CreateAdInstanceData(JSONValue jsonAdInstance)
        {
            return new AdInstanceData();
        }

        private AdParam GetAdParam(AdType adType)
        {
            AdParam adSupportParam = new AdParam();
            foreach (AdParam param in m_adSupportParams)
            {
                if (param.m_adType == adType)
                {
                    adSupportParam = param;
                    break;
                }
            }
            return adSupportParam;
        }

        /// <summary>
        /// Initialises parameters from srciptable objects
        /// </summary>
        private void InitializeAdInstanceParameters()
        {
            if (AdInstanceParametersPath.Length > 0)
            {
                string path = AdInstanceParametersPath;
                UnityEngine.Object[] parameters = Resources.LoadAll(path);
                foreach (UnityEngine.Object itemParameters in parameters)
                {
                    IAdInstanceParameters adInstanceParameters = itemParameters as IAdInstanceParameters;
                    if (adInstanceParameters != null)
                    {
                        m_adInstanceParameters.Add(adInstanceParameters);
                    }
                }
            }
        }
        #endregion Internal Methods

    }
} // namespace Virterix.AdMediation
