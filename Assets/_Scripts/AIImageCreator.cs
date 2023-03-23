using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIImageCreator : MonoBehaviour
{
    [Serializable]
    public class ReceivedText
    {
        public int created;
        public ReceivedTextData[] data;
    }

    [Serializable]
    public class ReceivedTextData
    {
        public string url;
    }

    [SerializeField]
    private RawImage rawImage = null;
    // [SerializeField]
    //private Texture loadingTexture = null;
    [SerializeField]
    private Texture errorTexture = null;

    [SerializeField]
    private string currentImageURL = string.Empty;

    private string prompt = string.Empty;
    private string editImageURL = string.Empty;
    private string[] imageSizeOptions = { "256x256", "512x512", "1024x1024" };
    private int currentImageIndex = 2;
    private const string apiKey = "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";
    private const string createImageAPIURL = "https://api.openai.com/v1/images/generations";
    private const string editImageAPIURL = "https://api.openai.com/v1/images/edits";

    [Header("Visual Effects")]
    [SerializeField] private GameObject imageGeneratorPanel;
    [SerializeField] private DissolveController dissolveController;
    [SerializeField] private ImageGenerationUIManager imageGenerationUIManager;
    [SerializeField] private Animator imagePanelAnimator;

    public delegate void NewImageRequest();
    public static NewImageRequest newImageRequest;

    public int GetCurrentImageIndex()
    {
        return currentImageIndex;
    }

    public void SetPrompt(string newPrompt)
    {
        prompt = newPrompt;
    }

    public void GoToImageURL()
    {
        if (string.IsNullOrEmpty(currentImageURL) == true)
        {
            Debug.LogWarning("AIImageCreator - GoToImageURL: The string currentImageURL is empty.");
            return;
        }

        Application.OpenURL(currentImageURL);
    }

    private void OnEnable()
    {
        //   Loading.StartLoading();

        //  rawImage.texture = loadingTexture;

        if (string.IsNullOrEmpty(editImageURL) == true)
        {
            StartCoroutine(GetRequest(createImageAPIURL));
        }
        else
        {
            StartCoroutine(GetRequest(editImageAPIURL));
        }
    }

    private IEnumerator GetRequest(string uri)
    {
        string requestData = "{\"prompt\": \"" + prompt
            + "\", \"n\": " + 1
            + ", \"size\": \"" + imageSizeOptions[currentImageIndex] + "\"}";

        //if (string.IsNullOrEmpty(editImageURL) == false)
        //{
        //  requestData = "{\"image\": \"" + editImageURL + "\", \"prompt\": \"" + prompt + "\", \"n\": " + 1 + "}";
        //}
        //else
        //{
        //  requestData = "{\"prompt\": \"" + prompt + "\", \"n\": " + 1 + "}";
        //}

        UnityWebRequest request = new UnityWebRequest(uri, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError("Connection Error: " + request.error);
                rawImage.texture = errorTexture;
                // Loading.StopLoading();
                break;

            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Data Processing Error: " + request.error);
                rawImage.texture = errorTexture;
                //Loading.StopLoading();
                break;

            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + request.error);
                rawImage.texture = errorTexture;
                //  Loading.StopLoading();
                break;

            case UnityWebRequest.Result.Success:
                Debug.Log("Received Text: " + request.downloadHandler.text);
                StartCoroutine(GetTexture(JsonUtility.FromJson<ReceivedText>(request.downloadHandler.text)));
                break;
        }
    }

    private IEnumerator GetTexture(ReceivedText receivedTextData)
    {
        currentImageURL = receivedTextData.data[0].url;
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(currentImageURL);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            rawImage.texture = errorTexture;
        }
        else
        {
            Debug.Log("Get Texture Result: " + www.result);
            imageGeneratorPanel.SetActive(true);
            imagePanelAnimator.SetBool("InTransition", true);

            dissolveController.ImageActivated();
            newImageRequest?.Invoke();
            rawImage.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
        }

        //Loading.StopLoading();
    }

    public void SubmitNewRequest()
    {
        imagePanelAnimator.SetBool("InTransition", false);
        imageGeneratorPanel.SetActive(false);
        imageGenerationUIManager.ResetTransitionAnimations();
    }

    public void ReSubmitRequest()
    {
        imagePanelAnimator.SetBool("InTransition", false);
        imageGeneratorPanel.SetActive(false);
        imageGenerationUIManager.ResetSpaceshipAnimation();
        imageGenerationUIManager.SubmitRequest();
    }
}