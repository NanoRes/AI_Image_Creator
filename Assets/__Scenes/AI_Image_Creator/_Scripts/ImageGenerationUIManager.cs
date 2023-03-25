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
    private TMP_InputField imagePrompt = null;
    [SerializeField]
    private Button requestImageButton = null;
    [SerializeField]
    private TMP_Text characterCountText = null;
    [SerializeField]
    private TMP_Text versionText = null;

    [Header("Animations")]
    [SerializeField] private Animator headerAnimator = null;
    [SerializeField] private Animator footerAnimator = null;
    [SerializeField] private Animator spaceshipAnimator = null;

    public delegate void BeginSpaceshipAnimation();
    public static BeginSpaceshipAnimation beginSpaceshipAnimation = null;

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
    }

    public void PromptTextValueChanged()
    {
        if (string.IsNullOrEmpty(imagePrompt.text) == true)
        {
            requestImageButton.interactable = false;
        }
        else
        {
            requestImageButton.interactable = true;
        }

        applicationData.imageCreationPrompt = imagePrompt.text;

        characterCountText.text = applicationData.imageCreationPrompt.Length + " / " + 
            applicationData.characterLimit.ToString();
    }

    public void ClearText()
    {
        imagePrompt.text = string.Empty;
        applicationData.imageCreationPrompt = string.Empty;
        requestImageButton.interactable = false;
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
            StartTheVisualShowAndSubmitPrompt();
            return;
        }

        ConfirmPaymentMethod(applicationData.currentPaymentMethodSelected);
    }

    public void ConfirmPaymentMethod(ApplicationData.PaymentMethod newMethodSelected)
    {
        applicationData.currentPaymentMethodSelected = newMethodSelected;

        Run();
    }

    private void Awake()
    {
        versionText.text = applicationData.versionTextPrefix + Application.version;
        
        ClearText();
    }

    private void StartTheVisualShowAndSubmitPrompt()
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

        yield return new WaitForSeconds(2);

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

                print("Total Solana: " + userData.totalSolanaTokens);
                print("Solana Pricing: " + applicationData.pricingInLamports);

                txBuilder = RequestPurchaseWithSOL(blockHash.Result.Value.Blockhash, fromAccount);
                
                break;

            case ApplicationData.PaymentMethod.DGLN:

                print("Total Dogelana: " + userData.totalDogelanaTokens);
                print("Dogelana Pricing: " + applicationData.pricingInDGLN);

                var tokenAccounts = await Web3.Rpc.GetTokenAccountsByOwnerAsync(
                    fromAccount.PublicKey,
                    applicationData.mintDGLNAddress,
                    tokenProgramId: applicationData.tokenProgramID);

                foreach (TokenAccount account in tokenAccounts.Result.Value)
                {
                    if(account == null)
                    {
                        continue;
                    }

                    if (account.Account.Data.Parsed.Info.Mint != 
                        applicationData.mintDGLNAddress)
                    {
                        continue;
                    }

                    txBuilder = RequestPurchaseWithDGLN(blockHash.Result.Value.Blockhash, 
                        fromAccount, account.PublicKey);

                    break;
                }

                break;
        }

        if (txBuilder == null)
        {
            Debug.LogWarning("No SPL Token Account was found that matches the targetMintAddress: " 
                + applicationData.mintDGLNAddress);
            return;
        }

        byte[] msgBytes = txBuilder.CompileMessage();
        Debug.Log("msgBytes: " + msgBytes);
        byte[] signature = fromAccount.Sign(msgBytes);
        Debug.Log("signature: " + signature);
        byte[] tx = txBuilder.AddSignature(signature).Serialize();
        Debug.Log("tx: " + tx);

        RequestResult<ResponseValue<SimulationLogs>> txSim = 
            await Web3.Rpc.SimulateTransactionAsync(tx);

        if (txSim.WasSuccessful == false)
        {
            Debug.LogWarning(txSim.ServerErrorCode + ": " + txSim.ErrorData);
            Debug.LogWarning(txSim.WasSuccessful + ": " + txSim.WasHttpRequestSuccessful + ": " 
                + txSim.WasRequestSuccessfullyHandled);
            Debug.LogWarning("Reason: " + txSim.Reason);
            Debug.LogWarning("RawRpcRequest: " + txSim.RawRpcRequest);
            Debug.LogWarning("RawRpcResponse: " + txSim.RawRpcResponse);
            Debug.LogWarning("HttpStatusCode: " + txSim.HttpStatusCode);
            return;
        }

        RequestResult<string> firstSig = await Web3.Rpc.SendTransactionAsync(tx);

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
            return;
        }

        Debug.Log($"Tx Signature: {firstSig.Result}");

        StartTheVisualShowAndSubmitPrompt();
    }

    private TransactionBuilder RequestPurchaseWithSOL(string blockhash, Account fromAccount)
    {
        string toWalletAddress = applicationData.payToSolanaAddress;
        long playmentInSOL = applicationData.pricingInLamports;

        //if (RemoteConfig.info != null)
        //{
        //    playmentInSOL = RemoteConfig.info.PricingInLamports;
        //    toWalletAddress = RemoteConfig.info.PayToSOLWallet;
        //}

        TransactionBuilder txBuilder = new TransactionBuilder()
            .SetRecentBlockHash(blockhash)
            .SetFeePayer(fromAccount.PublicKey)
            .AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey,
                new PublicKey(toWalletAddress), (ulong)playmentInSOL))
            .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, 
                applicationData.transactionMemoStatement + Application.version + " SOL"));

        return txBuilder;
    }

    private TransactionBuilder RequestPurchaseWithDGLN(string blockhash, Account fromAccount, 
        string fromDGLNAccount)
    {
        string toWalletAddress = applicationData.payToDogelanaAddress;
        long playmentInDGLN = applicationData.pricingInDGLN;

        //if (RemoteConfig.info != null)
        //{
        //    playmentInDGLN = RemoteConfig.info.PricingInDGLN;
        //    toWalletAddress = RemoteConfig.info.PayToDGLNWallet;
        //}

        TransactionBuilder txBuilder = new TransactionBuilder()
            .SetRecentBlockHash(blockhash)
            .SetFeePayer(fromAccount.PublicKey)
            .AddInstruction(TokenProgram.TransferChecked(new PublicKey(fromDGLNAccount),
                new PublicKey(toWalletAddress), (ulong)playmentInDGLN, 9, fromAccount.PublicKey,
                new PublicKey(applicationData.mintDGLNAddress)))
            .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, 
                applicationData.transactionMemoStatement + Application.version + " DGLN"));

        return txBuilder;
    }
}