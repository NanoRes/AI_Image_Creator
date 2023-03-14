using System.Collections;
using TMPro;
using UnityEngine;

public class ImageGenerationUIManager : MonoBehaviour
{
    public AIImageCreator dalleManager = null;
    public TMP_InputField imagePrompt = null;

    public void ClearText()
    {
        imagePrompt.text = string.Empty;
    }

    public void SubmitRequest()
    {
        if(string.IsNullOrEmpty(imagePrompt.text) == true)
        {
            return;
        }

        dalleManager.gameObject.SetActive(false);

        StartCoroutine(ImageRequest());
    }

    private IEnumerator ImageRequest()
    {
        yield return new WaitForEndOfFrame();

        dalleManager.SetPrompt(imagePrompt.text);
        dalleManager.gameObject.SetActive(true);
    }
}