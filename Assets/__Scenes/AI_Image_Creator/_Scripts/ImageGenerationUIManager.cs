using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageGenerationUIManager : MonoBehaviour
{
    [SerializeField]
    private ApplicationData applicationData = null;
    [SerializeField]
    private UserData userData = null;
    [SerializeField]
    private AIImageCreator imageCreator = null;

    [Header("UI Elements")]
    [SerializeField]
    private ButtonColorFlasher walletButtonFlasher = null;
    [SerializeField]
    private TMP_InputField imagePrompt = null;
    [SerializeField]
    private Button requestImageSOLButton = null;
    [SerializeField]
    private Button requestImageDGLNButton = null;
    [SerializeField]
    private TMP_Text characterCountText = null;
    [SerializeField]
    private TMP_Text errorText = null;
    [SerializeField]
    private Button backUpURLButton = null;
    [SerializeField]
    private TMP_Text versionText = null;

    [Header("Animations")]
    [SerializeField] private Animator headerAnimator = null;
    [SerializeField] private Animator footerAnimator = null;
    [SerializeField] private Animator spaceshipAnimator = null;

    public delegate void BeginSpaceshipAnimation();
    public static BeginSpaceshipAnimation beginSpaceshipAnimation = null;

    private const string notEnoughFunds = "Insuffient Funds\r\nRecharge Your Wallet & Try Again";

    public void SetErrorText(string newText)
    {
        errorText.gameObject.SetActive(true);
        errorText.text = newText;
    }

    public void SetBackUpURLButtonActive(bool isActive)
    {
        backUpURLButton.gameObject.SetActive(isActive);
    }

    public void ResetTransitionAnimations()
    {
        footerAnimator.SetBool("InTransition", false);
        headerAnimator.SetBool("InTransition", false);
        spaceshipAnimator.SetBool("InTransition", false);
    }

    public void ResetSpaceshipAnimation()
    {
        spaceshipAnimator.SetBool("InTransition", false);
    }

    public void SetWalletActive(bool isActive)
    {
        applicationData.isWalletOpen = isActive;

        if(isActive == true)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    public void PromptTextValueChanged()
    {
        if (string.IsNullOrEmpty(imagePrompt.text) == true)
        {
            requestImageSOLButton.interactable = false;
            requestImageDGLNButton.interactable = false;
        }
        else
        {
            requestImageSOLButton.interactable = true;
            requestImageDGLNButton.interactable = true;
        }

        applicationData.imageCreationPrompt = imagePrompt.text;

        characterCountText.text = applicationData.imageCreationPrompt.Length + " / " + 
            applicationData.characterLimit.ToString();
    }

    public void ClearText()
    {
        imagePrompt.text = string.Empty;
        applicationData.imageCreationPrompt = string.Empty;
        requestImageSOLButton.interactable = false;
        requestImageDGLNButton.interactable = false;
    }

    public void PressedSubmitAndPayWithSolana()
    {
        applicationData.currentPaymentMethodSelected = ApplicationData.PaymentMethod.SOL;
        SubmitRequest();
    }

    public void PressedSubmitAndPayWithDogelana()
    {
        applicationData.currentPaymentMethodSelected = ApplicationData.PaymentMethod.DGLN;
        SubmitRequest();
    }

    public void SubmitRequest()
    {
        if (string.IsNullOrEmpty(applicationData.imageCreationPrompt) == true)
        {
            return;
        }

        if ((IsWalletConnected() == false) && (applicationData.isFreeForTesting == false))
        {
            applicationData.isWalletOpen = true;
            return;
        }

        if (applicationData.isFreeForTesting == true)
        {
            StartTheVisualShow();
            SubmitPromptForImage();
            return;
        }

        ConfirmPaymentMethod(applicationData.currentPaymentMethodSelected);
    }

    public void ConfirmPaymentMethod(ApplicationData.PaymentMethod newMethodSelected)
    {
        applicationData.currentPaymentMethodSelected = newMethodSelected;
        errorText.gameObject.SetActive(false);
        SetBackUpURLButtonActive(false);

        Run();
    }

    private void Awake()
    {
        versionText.text = applicationData.versionTextPrefix + Application.version;
        
        ClearText();
        errorText.gameObject.SetActive(false);
        SetBackUpURLButtonActive(false);
        characterCountText.text = 0 + " / " +
            applicationData.characterLimit.ToString();
    }

    private void StartTheVisualShow()
    {
        StartCoroutine(TransitionAnimation());
    }

    private IEnumerator TransitionAnimation()
    {
        footerAnimator.SetBool("InTransition", true);
        headerAnimator.SetBool("InTransition", true);

        yield return new WaitForEndOfFrame();

        while (!spaceshipAnimator.GetBool("InTransition"))
        {
            var animStateInfo = footerAnimator.GetCurrentAnimatorStateInfo(0);
            var NTime = animStateInfo.normalizedTime;

            var animStateInfo2 = headerAnimator.GetCurrentAnimatorStateInfo(0);
            var NTime2 = animStateInfo.normalizedTime;

            if (NTime > 1.0f)
            {
                spaceshipAnimator.SetBool("InTransition", true);

            }

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        beginSpaceshipAnimation?.Invoke();
    }

    private void SubmitPromptForImage()
    {
        StartCoroutine(DelayShowingImageSS());
    }

    private IEnumerator DelayShowingImageSS()
    {
        yield return new WaitForSeconds(0.02f);

        imageCreator.gameObject.SetActive(false);
        StartCoroutine(ImageRequest());
    }

    private IEnumerator ImageRequest()
    {
        yield return new WaitForEndOfFrame();

        imageCreator.SetPrompt(imagePrompt.text);
        imageCreator.gameObject.SetActive(true);
    }

    private bool IsWalletConnected()
    {
        if (Web3.Instance == null)
        {
            return false;
        }

        if (Web3.Instance.Wallet == null)
        {
            return false;
        }

        return Web3.Instance.Wallet.Account != null;
    }

    private async void Run()
    {
        if (Web3.Instance == null)
        {
            return;
        }

        if (Web3.Instance.Wallet == null)
        {
            return;
        }

        if (Web3.Instance.Wallet.Account == null)
        {
            return;
        }

        Account fromAccount = Web3.Instance.Wallet.Account;

        RequestResult<ResponseValue<BlockHash>> blockHash = await Web3.Rpc.GetRecentBlockHashAsync();
        Debug.Log($"BlockHash >> {blockHash.Result.Value.Blockhash}");

        TransactionBuilder txBuilder = null;

        switch (applicationData.currentPaymentMethodSelected)
        {
            case ApplicationData.PaymentMethod.SOL:

                print("Total Solana: " + (userData.totalSolanaTokens));
                print("Solana Pricing: " + applicationData.pricingInLamports * 0.000000001);
                if(userData.totalSolanaTokens < (applicationData.pricingInLamports * 0.000000001))
                {
                    SetErrorText(notEnoughFunds);
                    walletButtonFlasher?.StartFlash();
                    imageCreator?.GoBackToHome();
                    return;
                }

                txBuilder = RequestPurchaseWithSOL(blockHash.Result.Value.Blockhash, fromAccount);
                
                break;

            case ApplicationData.PaymentMethod.DGLN:

                print("Total Dogelana: " + userData.totalDogelanaTokens);
                print("Dogelana Pricing: " + applicationData.pricingInDGLN);
                if (userData.totalDogelanaTokens < applicationData.pricingInDGLN)
                {
                    SetErrorText(notEnoughFunds);
                    walletButtonFlasher?.StartFlash();
                    imageCreator?.GoBackToHome();
                    return;
                }

                txBuilder = RequestPurchaseWithDGLN(blockHash.Result.Value.Blockhash);

                break;
        }

        if (txBuilder == null)
        {
            Debug.LogWarning("No SPL Token Account was found that matches the targetMintAddress: " 
                + applicationData.mintDGLNAddress);
            return;
        }

        byte[] msgBytes = txBuilder.CompileMessage();
        byte[] signature = fromAccount.Sign(msgBytes);
        byte[] tx = txBuilder.AddSignature(signature).Serialize();

        RequestResult<ResponseValue<SimulationLogs>> txSim = 
            await Web3.Rpc.SimulateTransactionAsync(tx, true);

        if (txSim.WasSuccessful == false)
        {
            Debug.LogWarning(txSim.ServerErrorCode + ": " + txSim.ErrorData);
            Debug.LogWarning(txSim.WasSuccessful + ": " + txSim.WasHttpRequestSuccessful + ": " 
                + txSim.WasRequestSuccessfullyHandled);
            Debug.LogWarning("Reason: " + txSim.Reason);
            Debug.LogWarning("RawRpcRequest: " + txSim.RawRpcRequest);
            Debug.LogWarning("RawRpcResponse: " + txSim.RawRpcResponse);
            Debug.LogWarning("HttpStatusCode: " + txSim.HttpStatusCode);

            SetErrorText(notEnoughFunds);
            imageCreator?.GoBackToHome();
            walletButtonFlasher?.StartFlash();
            return;
        }

        StartTheVisualShow();

        RequestResult<string> firstSig = await Web3.Rpc.SendAndConfirmTransactionAsync(tx);

        if (firstSig.WasSuccessful == false)
        {
            Debug.LogWarning(firstSig.ServerErrorCode + ": " + firstSig.ErrorData);
            Debug.LogWarning(firstSig.WasSuccessful + ": "
                + firstSig.WasHttpRequestSuccessful + ": "
                + firstSig.WasRequestSuccessfullyHandled);
            Debug.LogWarning("Reason: " + firstSig.Reason);
            Debug.LogWarning("RawRpcRequest: " + firstSig.RawRpcRequest);
            Debug.LogWarning("RawRpcResponse: " + firstSig.RawRpcResponse);
            Debug.LogWarning("HttpStatusCode: " + firstSig.HttpStatusCode);

            SetErrorText(notEnoughFunds);
            imageCreator?.GoBackToHome();
            walletButtonFlasher?.StartFlash();
            return;
        }

        Debug.Log($"Tx Signature: {firstSig.Result}");

        switch (applicationData.currentPaymentMethodSelected)
        {
            case ApplicationData.PaymentMethod.SOL:

                userData.totalSolanaTokens -= (applicationData.pricingInLamports * 0.000000001);

                break;

            case ApplicationData.PaymentMethod.DGLN:

                userData.totalDogelanaTokens -= applicationData.pricingInDGLN;

                break;
        }

        SubmitPromptForImage();
    }

    private TransactionBuilder RequestPurchaseWithSOL(string blockhash, Account fromAccount)
    {
        string toWalletAddress = applicationData.payToSolanaAddress;
        ulong playmentInSOL = applicationData.pricingInLamports;

        //if (RemoteConfig.info != null)
        //{
        //    playmentInSOL = RemoteConfig.info.PricingInLamports;
        //    toWalletAddress = RemoteConfig.info.PayToSOLWallet;
        //}

        TransactionBuilder txBuilder = new TransactionBuilder()
            .SetRecentBlockHash(blockhash)
            .SetFeePayer(fromAccount.PublicKey)
            .AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey,
                new PublicKey(toWalletAddress), playmentInSOL))
            .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey,
                applicationData.transactionMemoStatement + Application.version + " SOL"));


        return txBuilder;
    }

    private TransactionBuilder RequestPurchaseWithDGLN(string blockhash)
    {
        string toWalletAddress = applicationData.payToDogelanaAddress;
        ulong playmentInDGLN = applicationData.pricingInDGLN;

        //if (RemoteConfig.info != null)
        //{
        //    playmentInDGLN = RemoteConfig.info.PricingInDGLN;
        //    toWalletAddress = RemoteConfig.info.PayToDGLNWallet;
        //}

        TransactionBuilder txBuilder = new TransactionBuilder()
            .SetRecentBlockHash(blockhash)
            .SetFeePayer(Web3.Instance.Wallet.Account.PublicKey)
            .AddInstruction(TokenProgram.Transfer(new PublicKey(userData.dogelanaTokenAddress),
                new PublicKey(toWalletAddress), playmentInDGLN, 
                Web3.Instance.Wallet.Account.PublicKey))
            .AddInstruction(MemoProgram.NewMemo(Web3.Instance.Wallet.Account.PublicKey, 
                applicationData.transactionMemoStatement + Application.version + " DGLN"));

        return txBuilder;
    }
}