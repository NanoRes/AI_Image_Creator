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

public class ImageGenerationUIManager : MonoBehaviour
{
    [SerializeField]
    private AIImageCreator imageCreator = null;
    [SerializeField]
    private TMP_InputField imagePrompt = null;
    [SerializeField]
    private bool isFreeForTesting = false;
    [SerializeField] 
    private Button optionsButton = null;
    [SerializeField]
    private Button requestImageButton = null;
    [SerializeField]
    private TMP_Text characterCountText = null;
    [SerializeField]
    private TMP_Text versionText = null;

    private const int characterLimit = 1000;
    private const string versionTextPrefix = "Version - ";
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
        if(string.IsNullOrEmpty(imagePrompt.text) == true)
        {
            return;
        }

        if ((IsWalletConnected() == false) && (isFreeForTesting == false))
        {
            optionsButton.onClick.Invoke();
            return;
        }

        Run();

        imageCreator.gameObject.SetActive(false);
        StartCoroutine(ImageRequest());
    }

    private void Awake()
    {
        requestImageButton.interactable = false;
        versionText.text = versionTextPrefix + Application.version;
        ClearText();
    }

    private IEnumerator ImageRequest()
    {
        yield return new WaitForEndOfFrame();

        imageCreator.SetPrompt(imagePrompt.text);
        imageCreator.gameObject.SetActive(true);
    }

    private bool IsWalletConnected()
    {
        if(Web3.Instance == null)
        {
            return false;
        }

        if (Web3.Instance.Wallet == null)
        {
            return false;
        }

        return Web3.Instance.Wallet.Account != null;
    }

    private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

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
        Console.WriteLine($"BlockHash >> {blockHash.Result.Value.Blockhash}");

        TransactionBuilder txBuilder = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(fromAccount)
            .AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey,
            new PublicKey("2kKVz2CfERu4XEcdCwtkAPxYL9p2h3iwxa93STUfT7Ty"), 25000000))
            .AddInstruction(MemoProgram.NewMemo(fromAccount.PublicKey, "NanoRes.Fun - AI Image Creator"));

        byte[] msgBytes = txBuilder.CompileMessage();
        byte[] signature = fromAccount.Sign(msgBytes);

        byte[] tx = txBuilder.AddSignature(signature).Serialize();

        Debug.Log($"Tx base64: {Convert.ToBase64String(tx)}");
        RequestResult<ResponseValue<SimulationLogs>> txSim = await Web3.Rpc.SimulateTransactionAsync(tx);

        RequestResult<string> firstSig = await Web3.Rpc.SendTransactionAsync(tx);
        Debug.Log($"First Tx Signature: {firstSig.Result}");
    }
}