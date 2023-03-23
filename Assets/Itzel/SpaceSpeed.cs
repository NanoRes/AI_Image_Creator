using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TunnelEffect;

public class SpaceSpeed : MonoBehaviour
{
    [SerializeField] private TunnelFX2 tunnelEffect;
    [SerializeField] private float transitionSpeed;
    [SerializeField] private GameObject lightSpeedObject;

    private readonly float defaultSpeed = 1;// 0.1f;
    private readonly float lightSpeed = 5f;

    private bool isLightSpeed;

    private void OnEnable()
    {
        tunnelEffect.layersSpeed = defaultSpeed;

        ImageGenerationUIManager.beginSpaceshipAnimation += StartLightSpeed;
        AIImageCreator.newImageRequest += StopLightSpeed;
    }

    private void OnDisable()
    {
        ImageGenerationUIManager.beginSpaceshipAnimation -= StartLightSpeed;
        AIImageCreator.newImageRequest -= StopLightSpeed;
    }


    public void StartLightSpeed()
    {
        isLightSpeed = true;
    }

    public void StopLightSpeed()
    {
        lightSpeedObject.SetActive(false);
        isLightSpeed = false;
    }

    private void Update()
    {
        if (isLightSpeed)
        {
            if (Mathf.Round(tunnelEffect.layersSpeed) > 2)
            {
              //  lightSpeedObject.SetActive(true);
                return;
            }

            tunnelEffect.hyperSpeed = 0.931f;
            tunnelEffect.layersSpeed = lightSpeed;// Mathf.Lerp(tunnelEffect.layersSpeed, lightSpeed, Time.deltaTime * transitionSpeed);
        }
        else
        {
            if (Mathf.Round(tunnelEffect.layersSpeed) == 1f)
            {
                tunnelEffect.hyperSpeed = 0;
                tunnelEffect.layersSpeed = 0.1f;
                return;
            }

            var speed = Mathf.Lerp(tunnelEffect.layersSpeed, defaultSpeed, Time.deltaTime * transitionSpeed);
            tunnelEffect.layersSpeed = speed;
        }
    }
}