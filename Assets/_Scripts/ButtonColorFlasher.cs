using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ButtonColorFlasher : MonoBehaviour
{
    [SerializeField]
    private ImageGenerationUIManager imageGenerationUIManager = null;
    [SerializeField]
    private Color idleColor = Color.white;
    [SerializeField]
    private Color flashColor = Color.white;

    private Image walletImage = null;
    private bool shouldWeFlash = false;
    private Coroutine flasherCoroutine = null;

    public void StartFlash()
    {
        if(flasherCoroutine != null) 
        {
            StopCoroutine(flasherCoroutine);
            flasherCoroutine = null;
        }

        shouldWeFlash = true;
    }

    private void Awake()
    {
        walletImage = GetComponent<Image>();

        shouldWeFlash = false;
    }

    private void Update()
    {
        if (shouldWeFlash == false)
        {
            if(walletImage.color != idleColor)
            {
                walletImage.color = idleColor;
            }
            return;
        }

        if (walletImage.color != flashColor)
        {
            walletImage.color = flashColor;
            flasherCoroutine = StartCoroutine(StopFlashing());
        }
    }

    private IEnumerator StopFlashing()
    {
        yield return new WaitForSecondsRealtime(3f);
        shouldWeFlash = false;
        imageGenerationUIManager.SetErrorText(string.Empty);
        flasherCoroutine = null;
    }
}