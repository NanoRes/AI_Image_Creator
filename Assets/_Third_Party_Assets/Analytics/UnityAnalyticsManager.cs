using UnityEngine;

#if USINGCONFIG

using Unity.Services.Core;
using Unity.Services.Analytics;

#endif

using System.Collections.Generic;
using System.Threading.Tasks;

public class UnityAnalyticsManager : MonoBehaviour
{

#if USINGCONFIG

    public async Task StartItUp()
    {
        try
        {
            await UnityServices.InitializeAsync();
            List<string> consentIdentifiers = await AnalyticsService.Instance.CheckForRequiredConsents();
        }
        catch (ConsentCheckException e)
        {
            Debug.LogWarning(e.ErrorCode + ": " + e.Message);
        }
    }

#endif

}