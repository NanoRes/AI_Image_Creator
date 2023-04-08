using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIImageCreator : MonoBehaviour
{
    [SerializeField]
    private ApplicationData applicationData = null;

    [SerializeField]
    private RawImage rawImage = null;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject imageGeneratorPanel = null;
    [SerializeField] private DissolveController dissolveController = null;
    [SerializeField] private ImageGenerationUIManager imageGenerationUIManager = null;
    [SerializeField] private Animator imagePanelAnimator = null;

    public delegate void NewImageRequest();
    public static NewImageRequest newImageRequest;

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

    public void ShareButtonPressed()
    {
        GoToImageURL();
    }

    public void SetPrompt(string newPrompt)
    {
        applicationData.imageCreationPrompt = newPrompt;
    }

    public void GoToImageURL()
    {
        if (string.IsNullOrEmpty(applicationData.currentCreatedImageURL) == true)
        {
            Debug.LogWarning("applicationData.currentCreatedImageURL is empty.");
            return;
        }

        Application.OpenURL(applicationData.currentCreatedImageURL);
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(applicationData.toEditImageURL) == true)
        {
            StartCoroutine(GetRequest(applicationData.createImageAPIURL));
        }
        else
        {
            StartCoroutine(GetRequest(applicationData.editImageAPIURL));
        }
    }

    private IEnumerator GetRequest(string uri)
    {
        string requestData = "{\"prompt\": \"" + applicationData.imageCreationPrompt
            + "\", \"n\": " + applicationData.numberOfRequestedImages
            + ", \"size\": \"" + applicationData.imageSizeOptions[
            applicationData.selectedImageSizeOptionIndex] + "\"}";

        //if (string.IsNullOrEmpty(editImageURL) == false)
        //{
        //  requestData = "{\"image\": \"" + editImageURL + "\",
        //  \"prompt\": \"" + prompt + "\", \"n\": " + 1 + "}";
        //}

        UnityWebRequest request = new UnityWebRequest(uri, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + applicationData.apiKey);

        yield return request.SendWebRequest();

        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError("Connection Error: " + request.error);
                GoBackToHome();
                imageGenerationUIManager.SetErrorText("Connection Error: " + request.error);
                applicationData.currentCreatedImageURL = string.Empty;
                break;

            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Data Processing Error: " + request.error);
                GoBackToHome();
                imageGenerationUIManager.SetErrorText("Data Processing Error: " + request.error);
                applicationData.currentCreatedImageURL = string.Empty;
                break;

            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + request.error);
                GoBackToHome();
                imageGenerationUIManager.SetErrorText("HTTP Error: " + request.error);
                applicationData.currentCreatedImageURL = string.Empty;
                break;

            case UnityWebRequest.Result.Success:
                Debug.Log("Received Text: " + request.downloadHandler.text);

                StartCoroutine(GetTexture(JsonUtility.FromJson<
                    ApplicationData.ReceivedText>(request.downloadHandler.text)));
                break;
        }
    }

    private IEnumerator GetTexture(ApplicationData.ReceivedText receivedTextData)
    {
        applicationData.currentCreatedImageURL = receivedTextData.data[0].url;
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(
            applicationData.currentCreatedImageURL);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            GoBackToHome();
            imageGenerationUIManager.SetErrorText(string.Empty);
            imageGenerationUIManager.SetBackUpURLButtonActive(true);
        }
        else
        {
            imageGenerationUIManager.SetPromptHeaderTextForImageReceived();
            Debug.Log("Get Texture Result: " + www.result);
            RevealImage(www);
        }
    }

    private void RevealImage(UnityWebRequest www)
    {
        imageGeneratorPanel.SetActive(true);
        imagePanelAnimator.SetBool("InTransition", true);
        dissolveController.ImageActivated();
        newImageRequest?.Invoke();
        rawImage.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
    }

    public void GoBackToHome()
    {
        imagePanelAnimator.SetBool("InTransition", false);
        imageGeneratorPanel.SetActive(false);
        imageGenerationUIManager.ResetTransitionAnimations();
        newImageRequest?.Invoke();
    }
}