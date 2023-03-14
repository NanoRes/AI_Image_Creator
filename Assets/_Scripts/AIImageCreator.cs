using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIImageCreator : MonoBehaviour
{
    [SerializeField]
    private RawImage rawImage = null;
    [SerializeField]
    private Texture loadingTexture = null;
    [SerializeField]
    private Texture errorTexture = null;

    [SerializeField]
    private string currentImageURL = string.Empty;

    private string prompt = string.Empty;
    private string editImageURL = string.Empty;

    private const string apiKey = "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";
    private const string createImageAPIURL = "https://api.openai.com/v1/images/generations";
    private const string editImageAPIURL = "https://api.openai.com/v1/images/edits";

    public void SetPrompt(string newPrompt)
    {
        prompt = newPrompt;
    }

    public void GoToImageURL()
    {
        if(string.IsNullOrEmpty(currentImageURL) == true)
        {
            Debug.LogWarning("The string currentImageURL is empty within DALL_E's GoToImageURL().");
            return;
        }

        Application.OpenURL(currentImageURL);
    }

    private void OnEnable()
    {
        rawImage.texture = loadingTexture;

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
        string requestData = "{\"prompt\": \"" + prompt + "\", \"n\": " + 1 + "}";

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
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Error: " + request.error);
                rawImage.texture = errorTexture;
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + request.error);
                rawImage.texture = errorTexture;
                break;
            case UnityWebRequest.Result.Success:
                Debug.Log("Received Text: " + request.downloadHandler.text);
                Debug.Log("Received Data: " + request.downloadHandler.data);
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
            rawImage.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
        }
    }
}

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
