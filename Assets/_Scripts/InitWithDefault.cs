using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using System.Collections.Generic;

public class InitWithDefault : MonoBehaviour
{
    private void Start()
    {
        StartItUp();
    }

    async void StartItUp()
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
}