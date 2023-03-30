using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Application_Data", 
    menuName = "NanoRes_Studios/Application Data", order = 1)]
public class ApplicationData : ScriptableObject
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

    public enum PaymentMethod { SOL, DGLN }
    
    public PaymentMethod currentPaymentMethodSelected = PaymentMethod.SOL;
    public PaymentMethod currentTransferMethodSelected = PaymentMethod.SOL;

    public bool isWalletOpen = false;
    public bool isFreeForTesting = false;

    public int characterLimit = 1000;
    public double pricingInSOL = 0.025;
    public ulong pricingInLamports = 25000000;
    public ulong pricingInDGLN = 25000000000000;

    public string[] imageSizeOptions = { "256x256", "512x512", "1024x1024" };
    public int selectedImageSizeOptionIndex = 0;

    public string imageCreationPrompt = string.Empty;
    public int numberOfRequestedImages = 1;
    public string toEditImageURL = string.Empty;

    public string currentCreatedImageURL = string.Empty;

    public Texture dogelanaTexture = null;
    public Texture solanaTexture = null;

    public string versionTextPrefix = "Version - ";
    public string transactionMemoStatement = "NanoRes Studios: AI Image Creator Purchase - v";
    public string payToSolanaAddress = "2kKVz2CfERu4XEcdCwtkAPxYL9p2h3iwxa93STUfT7Ty";
    public string payToDogelanaAddress = "9xuySURUpG8YJ5ZJfRCjfjBuNbJfaGhj2Ti3vj5cqKPA";
    public string mintDGLNAddress = "E6UU5M1z4CvSAAF99d9wRoXsasWMEXsvHrz3JQRXtm2X";
    public string tokenProgramID = "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA";
    public string apiKey = "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";
    public string createImageAPIURL = "https://api.openai.com/v1/images/generations";
    public string editImageAPIURL = "https://api.openai.com/v1/images/edits";

    [NonSerialized]
    public GameObject inSceneCoreFunctionality = null;

    public void SetInSceneCoreFunctionalityActive(bool isActive)
    {
        inSceneCoreFunctionality.SetActive(isActive);
    }
}