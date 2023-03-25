using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System;

public class UnityRemoteConfig : MonoBehaviour
{
    [System.Serializable]
    public class UnityRemoteConfigInfo
    {
        public string PayToSOLWallet = string.Empty;
        public long PricingInLamports = 0;
        public string PayToDGLNWallet = string.Empty;
        public long PricingInDGLN = 0;
    }

    [SerializeField]
    private ApplicationData applicationData = null;

    public static UnityRemoteConfigInfo info = new UnityRemoteConfigInfo();

    public struct userAttributes { }

    public struct appAttributes { }

    async Task InitializeRemoteConfigAsync()
    {
        await UnityServices.InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn == false)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Start()
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        StartItUp();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    async Task StartItUp()
    {
        if (Utilities.CheckForInternetConnection())
        {
            await InitializeRemoteConfigAsync();
        }

        RemoteConfigService.Instance.FetchCompleted += ApplyRemoteSettings;
        RemoteConfigService.Instance.FetchConfigs(new userAttributes(), new appAttributes());
    }

    void ApplyRemoteSettings(ConfigResponse configResponse)
    {
        try
        {
            JsonUtility.FromJsonOverwrite(RemoteConfigService.Instance.appConfig.config.ToString(), info);
            applicationData.pricingInDGLN = info.PricingInDGLN;
            applicationData.pricingInLamports = info.PricingInLamports;
            applicationData.payToDogelanaAddress = info.PayToDGLNWallet;
            applicationData.payToSolanaAddress = info.PayToSOLWallet;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }
}