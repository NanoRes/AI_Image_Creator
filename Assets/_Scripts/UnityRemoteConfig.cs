#if USINGCONFIG

using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;

#endif

using UnityEngine;

public class UnityRemoteConfig : MonoBehaviour
{
    [SerializeField]
    private ApplicationData applicationData = null;

#if USINGCONFIG

    [System.Serializable]
    public class UnityRemoteConfigInfo
    {
        public string PayToSOLWallet = string.Empty;
        public long PricingInLamports = 0;
        public string PayToDGLNWallet = string.Empty;
        public long PricingInDGLN = 0;
    }

    public static UnityRemoteConfigInfo info = new UnityRemoteConfigInfo();

    public struct userAttributes { }

    public struct appAttributes { }

    private async Task InitializeRemoteConfigAsync()
    {
        await UnityServices.InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn == false)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async Task StartItUp()
    {
        if (Utilities.CheckForInternetConnection())
        {
            await InitializeRemoteConfigAsync();
        }

        RemoteConfigService.Instance.FetchCompleted += ApplyRemoteSettings;
        RemoteConfigService.Instance.FetchConfigs(new userAttributes(), new appAttributes());
    }

    private void ApplyRemoteSettings(ConfigResponse configResponse)
    {
        try
        {
            JsonUtility.FromJsonOverwrite(RemoteConfigService.Instance.appConfig.config.ToString(), info);
            applicationData.pricingInDGLN = (ulong)info.PricingInDGLN;
            applicationData.pricingInLamports = (ulong)info.PricingInLamports;
            applicationData.pricingInSOL = applicationData.pricingInLamports * 0.000000001d;
            applicationData.payToDogelanaAddress = info.PayToDGLNWallet;
            applicationData.payToSolanaAddress = info.PayToSOLWallet;
            
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }

#endif

}