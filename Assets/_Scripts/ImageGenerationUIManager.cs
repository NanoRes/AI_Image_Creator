using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Solana.Unity.Examples;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using Org.BouncyCastle.Crypto.Agreement.Srp;

public class ImageGenerationUIManager : MonoBehaviour
{
    public enum PaymentMethod { SOL, DGLN }
    [SerializeField]
    private PaymentMethod currentPaymentMethodSelected;

    [SerializeField]
    private AIImageCreator imageCreator = null;
    [SerializeField]
    private TMP_InputField imagePrompt = null;

    public bool isFreeForTesting = false;

    [SerializeField]
    private Button optionsButton = null;
    [SerializeField]
    private Button requestImageButton = null;
    [SerializeField]
    private TMP_Text characterCountText = null;
    [SerializeField]
    private TMP_Text versionText = null;

    [Header("Animations")]
    [SerializeField] private Animator headerAnimator;
    [SerializeField] private Animator footerAnimator;
    [SerializeField] private Animator spaceshipAnimator;

    private const int characterLimit = 1000;
    private const string versionTextPrefix = "Version - ";
    private const string transactionMemoStatement = "NanoRes Studios: AI Image Creator Purchase - v";
    private const string defaultToAddress = "2kKVz2CfERu4XEcdCwtkAPxYL9p2h3iwxa93STUfT7Ty";
    private const string defaultToDGLNAddress = "9xuySURUpG8YJ5ZJfRCjfjBuNbJfaGhj2Ti3vj5cqKPA";
    private const string mintDGLNAddress = "E6UU5M1z4CvSAAF99d9wRoXsasWMEXsvHrz3JQRXtm2X";
    private const int pricingInLamports = 25000000;
    private const long pricingInDGLN = 25000000000000;

    private bool playeLightSpeedAnimation;

    public delegate void BeginSpaceshipAnimation();
    public static BeginSpaceshipAnimation beginSpaceshipAnimation;

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

        characterCountText.text = imagePrompt.text.Length + " / " + characterLimit.ToString();
    }

    public void ClearText()
    {
        imagePrompt.text = string.Empty;
        requestImageButton.interactable = false;
    }

    public void SubmitRequest()
    {
        if (string.IsNullOrEmpty(imagePrompt.text) == true)
        {
            return;
        }

        if ((IsWalletConnected() == false) && (isFreeForTesting == false))
        {
            optionsButton.onClick.Invoke();
            return;
        }

        // Replace this with the call on the "choose which pay option" UI.
        SelectPaymentMethod(currentPaymentMethodSelected);
    }

    // Call this function when the User is confirming their pay option via the confirmation UI.
    public void SelectPaymentMethod(PaymentMethod newMethodSelected)
    {
        currentPaymentMethodSelected = newMethodSelected;
        Run();

        StartCoroutine(TransitionAnimation());
        //   imageCreator.gameObject.SetActive(false);
        //  StartCoroutine(ImageRequest());
    }

    private void Awake()
    {
        requestImageButton.interactable = false;
        versionText.text = versionTextPrefix + Application.version;
        ClearText();
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


    private IEnumerator TransitionAnimation()
    {
        footerAnimator.SetBool("InTransition", true);
        headerAnimator.SetBool("InTransition", true);
        playeLightSpeedAnimation = false;

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
        playeLightSpeedAnimation = true;
        beginSpaceshipAnimation?.Invoke();

        yield return new WaitForSeconds(2);

        imageCreator.gameObject.SetActive(false);
        StartCoroutine(ImageRequest());

        /*  while (playeLightSpeedAnimation == false)
          {
              foreach (AnimationClip clip in spaceshipAnimator.runtimeAnimatorController.animationClips)
              {
                  if (clip.name == "SpaceShip_LightSpeed") 
                  {
                      playeLightSpeedAnimation = true;
                      beginSpaceshipAnimation?.Invoke();
                  }
              }

              yield return null;
          }*/
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

    public async void Run()
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

        switch (currentPaymentMethodSelected)
        {
            case PaymentMethod.SOL:
                txBuilder = RequestPurchaseWithSOL(blockHash.Result.Value.Blockhash, fromAccount);
                break;

            case PaymentMethod.DGLN:

                var tokenAccounts = await Web3.Rpc.GetTokenAccountsByOwnerAsync(fromAccount.PublicKey,
                    tokenProgramId: "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");

                string fromDGLNAccount = string.Empty;

                foreach (TokenAccount account in tokenAccounts.Result.Value)
                {
                    if (account.Account.Data.Parsed.Info.Mint == mintDGLNAddress)
                    {
                        fromDGLNAccount = account.PublicKey;
                    }

                    if (string.IsNullOrEmpty(fromDGLNAccount) == true)
                    {
                        continue;
                    }

                    txBuilder = RequestPurchaseWithDGLN(blockHash.Result.Value.Blockhash, fromAccount, fromDGLNAccount);
                    break;
                }

                break;
        }

        if (txBuilder == null)
        {
            Debug.LogWarning("No SPL Token Account was found that matches the targetMintAddress: " + mintDGLNAddress);
            return;
        }

        byte[] msgBytes = txBuilder.CompileMessage();
        byte[] signature = fromAccount.Sign(msgBytes);
        byte[] tx = txBuilder.AddSignature(signature).Serialize();

        RequestResult<ResponseValue<SimulationLogs>> txSim = await Web3.Rpc.SimulateTransactionAsync(tx);

        if (txSim.WasSuccessful == false)
        {
            Debug.LogWarning(txSim.ServerErrorCode + ": " + txSim.ErrorData);
            Debug.LogWarning(txSim.WasSuccessful + ": " + txSim.WasHttpRequestSuccessful + ": " + txSim.WasRequestSuccessfullyHandled);
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
            Debug.LogWarning(firstSig.WasSuccessful + ": " + firstSig.WasHttpRequestSuccessful + ": " + firstSig.WasRequestSuccessfullyHandled);
            Debug.LogWarning("Reason: " + firstSig.Reason);
            Debug.LogWarning("RawRpcRequest: " + firstSig.RawRpcRequest);
            Debug.LogWarning("RawRpcResponse: " + firstSig.RawRpcResponse);
            Debug.LogWarning("HttpStatusCode: " + firstSig.HttpStatusCode);
            return;
        }

        Debug.Log($"Tx Signature: {firstSig.Result}");

        imageCreator.gameObject.SetActive(false);
        StartCoroutine(ImageRequest());
    }

    private TransactionBuilder RequestPurchaseWithSOL(string blockhash, Account fromAccount)
    {
        string toWalletAddress = defaultToAddress;
        long playmentInSOL = pricingInLamports;

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
            .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, transactionMemoStatement + Application.version + " SOL"));

        return txBuilder;
    }

    private TransactionBuilder RequestPurchaseWithDGLN(string blockhash, Account fromAccount, string fromDGLNAccount)
    {
        string toWalletAddress = defaultToDGLNAddress;
        long playmentInDGLN = pricingInDGLN;

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
            new PublicKey(mintDGLNAddress)))
            .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, transactionMemoStatement + Application.version + " DGLN"));

        return txBuilder;
    }
}