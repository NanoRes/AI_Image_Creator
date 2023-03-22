using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System;

public class RemoteConfig : MonoBehaviour
{
    [System.Serializable]
    public class RemoteConfigInfo
    {
        public string PayToSOLWallet = string.Empty;
        public long PricingInLamports = 0;
        public string PayToDGLNWallet = string.Empty;
        public long PricingInDGLN = 0;
    }

    public static RemoteConfigInfo info = new RemoteConfigInfo();

    public struct userAttributes { }

    public struct appAttributes { }

    async Task InitializeRemoteConfigAsync()
    {
        // initialize handlers for unity game services
        await UnityServices.InitializeAsync();

        // remote config requires authentication for managing environment information
        if (AuthenticationService.Instance.IsSignedIn == false)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Start()
    {
        StartItUp();
    }

    async Task StartItUp()
    {
        // initialize Unity's authentication and core services, however check for internet connection
        // in order to fail gracefully without throwing exception if connection does not exist
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
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }
}