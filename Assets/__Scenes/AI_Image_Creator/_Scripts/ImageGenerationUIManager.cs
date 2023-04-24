using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

#if USINGCONFIG

    [SerializeField]
    private UnityAnalyticsManager unityAnalyticsManager = null;
    [SerializeField]
    private UnityRemoteConfig unityRemoteConfig = null;

#endif

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
    [SerializeField]
    private Text solPricingButtonText = null;
    [SerializeField]
    private Text solBalanceText = null;
    [SerializeField]
    private Text dglnPricingButtonText = null;
    [SerializeField]
    private Text dglnBalanceText = null;
    [SerializeField]
    private Text promptHeader = null;

    [Header("Animations")]
    [SerializeField] private Animator headerAnimator = null;
    [SerializeField] private Animator footerAnimator = null;
    [SerializeField] private Animator spaceshipAnimator = null;

    public delegate void BeginSpaceshipAnimation();
    public static BeginSpaceshipAnimation beginSpaceshipAnimation = null;

    private const string notEnoughFunds = "Insuffient Funds\r\nRecharge Your Wallet & Try Again";
    private const string promptHeaderStandard = "Enter Image Description";

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

        promptHeader.text = promptHeaderStandard;
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

        UpdateBalanceAndPricingText();
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

        RunTransaction();
    }

    private void Awake()
    {
        versionText.text = applicationData.versionTextPrefix + Application.version;
        
        ClearText();
        errorText.gameObject.SetActive(false);
        SetBackUpURLButtonActive(false);
        characterCountText.text = 0 + " / " +
            applicationData.characterLimit.ToString();
        solBalanceText.text = "";
        dglnBalanceText.text = "";
        promptHeader.text = promptHeaderStandard;
    }

#if USINGCONFIG

    private async Task StartAsync()
    {
        await unityAnalyticsManager.StartItUp();

        await unityRemoteConfig.StartItUp();

        solPricingButtonText.text = applicationData.pricingInSOL + " SOL";
        dglnPricingButtonText.text = (applicationData.pricingInDGLN * 0.000000001d).ToString("0,000") + " DGLN";
    }

#endif

    private void Start()
    {

#if USINGCONFIG

        _ = StartAsync();

#else

        solPricingButtonText.text = applicationData.pricingInSOL + " SOL";
        dglnPricingButtonText.text = (applicationData.pricingInDGLN * 0.000000001d).ToString("0,000") + " DGLN";

#endif

    }

    public void UpdateBalanceAndPricingText()
    {
        solPricingButtonText.text = applicationData.pricingInSOL + " SOL";
        solBalanceText.text = "Your SOL Balance\n" + userData.totalSolanaTokens.ToString("0.0000");

        dglnPricingButtonText.text = (applicationData.pricingInDGLN * 0.000000001d).ToString("0,000") + " DGLN";

        float finalDGLNFormat = userData.totalDogelanaTokens * 0.000000001f;

        if (finalDGLNFormat < 1000)
        {
            dglnBalanceText.text = "Your DGLN Balance\n" + finalDGLNFormat.ToString("0");
        }
        else
        {
            dglnBalanceText.text = "Your DGLN Balance\n" + finalDGLNFormat.ToString("0,000");
        }
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

        UpdateBalanceAndPricingText();
        imageCreator.gameObject.SetActive(false);
        StartCoroutine(ImageRequest());
    }

    private IEnumerator ImageRequest()
    {
        yield return new WaitForEndOfFrame();

        imageCreator.SetPrompt(imagePrompt.text);
        imageCreator.gameObject.SetActive(true);
        promptHeader.text = "Retrieving Your New AI Created Image..";
    }

    public void SetPromptHeaderTextForImageReceived()
    {
        promptHeader.text = "Congratulations! Here's Your New AI Created Image!";
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

    private void RunTransaction()
    {
        if (Web3.Account == null)
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

        confirmationAttemptCounter = 0;

        switch (applicationData.currentPaymentMethodSelected)
        {
            case ApplicationData.PaymentMethod.SOL:

                if (userData.totalLamportUnits < applicationData.pricingInLamports)
                {
                    SetErrorText(notEnoughFunds);
                    walletButtonFlasher?.StartFlash();
                    imageCreator.SubmitNewRequest();
                    return;
                }

                if ((userData.totalLamportUnits - applicationData.pricingInLamports) < 5000)
                {
                    SetErrorText("Not Enough SOL To Pay Gas Fees.");
                    walletButtonFlasher?.StartFlash();
                    imageCreator.SubmitNewRequest();
                    return;
                }

                TransferSol();

                break;

            case ApplicationData.PaymentMethod.DGLN:

                if (userData.totalDogelanaTokens < applicationData.pricingInDGLN)
                {
                    SetErrorText(notEnoughFunds);
                    walletButtonFlasher?.StartFlash();
                    imageCreator.SubmitNewRequest();
                    return;
                }

                if (userData.totalLamportUnits < 5000)
                {
                    SetErrorText("Not Enough SOL To Pay Gas Fees.");
                    walletButtonFlasher?.StartFlash();
                    imageCreator.SubmitNewRequest();
                    return;
                }

                TransferDogelana();

                break;
        }        
    }

    private async void TransferSol()
    {
        RequestResult<ResponseValue<BlockHash>> blockHash = await Web3.Rpc.GetRecentBlockHashAsync();
        Debug.Log($"BlockHash >> {blockHash.Result.Value.Blockhash}");

        var transaction = new Transaction
        {
            RecentBlockHash = blockHash.Result.Value.Blockhash,
            FeePayer = Web3.Account.PublicKey,
            Instructions = new List<TransactionInstruction>
            {
                SystemProgram.Transfer(
                    Web3.Account.PublicKey,
                    new PublicKey(applicationData.payToSolanaAddress),
                    applicationData.pricingInLamports),
                MemoProgram.NewMemo(Web3.Account.PublicKey,
                    applicationData.transactionMemoStatement + Application.version + " | SOL")
            },
            Signatures = new List<SignaturePubKeyPair>()
        };

        var firstSig = await Web3.Instance.Wallet.SignAndSendTransaction(transaction);
        
        if (string.IsNullOrEmpty(firstSig.Result) == false)
        {
            userData.currentTransactionSignature = firstSig.Result;
            Debug.Log($"First Tx Signature: {firstSig.Result}");

            Web3.WsRpc.SubscribeSignature(firstSig.Result, HandlePaySOLResponse, Commitment.Finalized);
            StartTheVisualShow();
            promptHeader.text = "Waiting For Transaction Confirmation..";

            StartTransactionTimer();
        }
        else
        {
            errorText.text = "Transaction Error: " + firstSig.Reason;
            Debug.Log("Transaction Error: " + firstSig.Reason);
            imageCreator.SubmitNewRequest();
        }
    }

    private void HandlePaySOLResponse(SubscriptionState subState, ResponseValue<ErrorResult> responseValue)
    {
        if (subState.LastError == null)
        {
            UpdateBalanceAndPricingText();
            promptHeader.text = "Retrieving Your New AI Created Image..";
            SubmitPromptForImage();
            StopTransactionTimer();
        }
        else
        {
            Debug.Log(subState.LastError);
            errorText.text = subState.LastError;
        }

        Web3.WsRpc.Unsubscribe(subState);
    }

    private async void TransferDogelana()
    {
        RequestResult<ResponseValue<BlockHash>> blockHash = await Web3.Rpc.GetRecentBlockHashAsync();
        Debug.Log($"BlockHash >> {blockHash.Result.Value.Blockhash}");

        var transaction = new Transaction
        {
            RecentBlockHash = blockHash.Result.Value.Blockhash,
            FeePayer = Web3.Account.PublicKey,
            Instructions = new List<TransactionInstruction>
            {
                TokenProgram.Transfer(
                    new PublicKey(userData.dogelanaTokenAddress),
                    new PublicKey(applicationData.payToDogelanaAddress),
                    applicationData.pricingInDGLN,
                    Web3.Account.PublicKey),
                MemoProgram.NewMemo(Web3.Account.PublicKey,
                    applicationData.transactionMemoStatement + Application.version + " | DGLN")
            },
            Signatures = new List<SignaturePubKeyPair>()
        };

        var firstSig = await Web3.Instance.Wallet.SignAndSendTransaction(transaction);
        
        if (string.IsNullOrEmpty(firstSig.Result) == false)
        {
            userData.currentTransactionSignature = firstSig.Result;
            Debug.Log($"First Tx Signature: {firstSig.Result}");

            Web3.WsRpc.SubscribeSignature(firstSig.Result, HandlePayDGLNResponse, Commitment.Finalized);
            StartTheVisualShow();
            promptHeader.text = "Waiting For Transaction Confirmation..";

            StartTransactionTimer();
        }
        else
        {
            errorText.text = firstSig.Reason;
            Debug.Log("Transaction Error: " + firstSig.Reason);
            imageCreator.SubmitNewRequest();
        }
    }

    private void HandlePayDGLNResponse(SubscriptionState subState, ResponseValue<ErrorResult> responseValue)
    {
        if (subState.LastError == null)
        {
            UpdateBalanceAndPricingText();
            promptHeader.text = "Retrieving Your New AI Created Image..";
            SubmitPromptForImage();
            StopTransactionTimer();
        }
        else
        {
            Debug.Log(subState.LastError);
            errorText.text = subState.LastError;
        }

        Web3.WsRpc.Unsubscribe(subState);
        
    }

    private float currentWaitingTransactionTimerSeconds = -1.0f;
    private float waitingTransactionTimerSeconds = 15.0f;

    private void StartTransactionTimer()
    {
        currentWaitingTransactionTimerSeconds = 0.0f;
    }

    private void StopTransactionTimer()
    {
        currentWaitingTransactionTimerSeconds = -1f;
    }

    private void LateUpdate()
    {
        if (currentWaitingTransactionTimerSeconds < 0)
        {
            return;
        }

        currentWaitingTransactionTimerSeconds += Time.deltaTime;

        if (currentWaitingTransactionTimerSeconds < waitingTransactionTimerSeconds)
        {
            return;
        }

        currentWaitingTransactionTimerSeconds = -2f;

        _ = ConfirmTransaction();
    }

    private int confirmationAttemptCounter = 0;
    private const int confirmationAttempts = 10;

    public async Task ConfirmTransaction()
    {
        List<string> array = new List<string>();
        array.Add(userData.currentTransactionSignature);
        var state = await Web3.Rpc.GetSignatureStatusesAsync(array);

        confirmationAttemptCounter++;

        if (state.WasSuccessful == true)
        {
            Debug.Log(state.Result.Value[0].ConfirmationStatus);
            Debug.Log(state.Result.Value[0].Confirmations);
            Debug.Log(state.Result.Value[0].Signature);

            if (state.Result.Value[0].ConfirmationStatus == "finalized")
            {
                errorText.text = "Transaction Confirmation Successful.";

                switch (applicationData.currentPaymentMethodSelected)
                {
                    case ApplicationData.PaymentMethod.SOL:

                        userData.totalLamportUnits -= applicationData.pricingInLamports;
                        userData.totalSolanaTokens -= applicationData.pricingInSOL;
                        UpdateBalanceAndPricingText();
                        promptHeader.text = "Retrieving Your New AI Created Image..";
                        SubmitPromptForImage();

                        break;
                    case ApplicationData.PaymentMethod.DGLN:

                        userData.totalDogelanaTokens -= applicationData.pricingInDGLN;
                        UpdateBalanceAndPricingText();
                        promptHeader.text = "Retrieving Your New AI Created Image..";
                        SubmitPromptForImage();

                        break;
                }

                return;
            }

            if (confirmationAttempts - confirmationAttemptCounter != 1)
            {
                errorText.text = "Transaction Confirmation #" + confirmationAttemptCounter
                    + " Failed; " + (confirmationAttempts - confirmationAttemptCounter) + " attempts remaining.";
            }
            else
            {
                errorText.text = "Transaction Confirmation #" + confirmationAttemptCounter
                    + " Failed; " + (confirmationAttempts - confirmationAttemptCounter) + " attempt remaining.";
            }

            StartTransactionTimer();
        }
        else
        {
            errorText.text = state.Reason;
        }

        if(confirmationAttemptCounter >= confirmationAttempts)
        {
            StopTransactionTimer();
            imageCreator.SubmitNewRequest();

            errorText.text = "Transaction Confirmation Failed After 10 Attempts.";
        }
    }
}