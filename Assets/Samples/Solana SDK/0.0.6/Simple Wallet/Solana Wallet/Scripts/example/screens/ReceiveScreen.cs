using Solana.Unity.SDK.Example;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;

// ReSharper disable once CheckNamespace

public class ReceiveScreen : SimpleScreen
{
    public Button airdrop_btn;
    public Button close_btn;

    

    private void Start()
    {
        airdrop_btn.onClick.AddListener(RequestAirdrop);

        close_btn.onClick.AddListener(() =>
        {
            manager.ShowScreen(this, "wallet_screen");
        });
    }
    
    private void OnEnable()
    {
        var isDevnet = IsDevnet();
        airdrop_btn.enabled = isDevnet;
        airdrop_btn.interactable = isDevnet;
    }

    public override void ShowScreen(object data = null)
    {
        base.ShowScreen();
        gameObject.SetActive(true);

        CheckAndToggleAirdrop();

        
    }

    private void CheckAndToggleAirdrop()
    {
        airdrop_btn.gameObject.SetActive(!Web3.Instance.Wallet.ActiveRpcClient.ToString().Contains("api.mainnet"));
    }

    

    private async void RequestAirdrop()
    {
        Loading.StartLoading();
        var result = await Web3.Base.RequestAirdrop();
        if (result?.Result == null)
        {
            Debug.LogError("Airdrop failed, you may have reach the limit, try later or use a public faucet");
        }
        else
        {
            await Web3.Rpc.ConfirmTransaction(result.Result, Commitment.Confirmed);
            Debug.Log("Airdrop success, see transaction at https://explorer.solana.com/tx/" + result.Result + "?cluster=devnet");
            manager.ShowScreen(this, "wallet_screen");
        }
        Loading.StopLoading();
    }

    private static bool IsDevnet()
    {
        return  Web3.Rpc.NodeAddress.AbsoluteUri.Contains("devnet");
    }

    public void CopyPublicKeyToClipboard()
    {
        Clipboard.Copy(Web3.Instance.Wallet.Account.PublicKey.ToString());
        gameObject.GetComponent<Toast>()?.ShowToast("Public Key copied to clipboard", 3);
    }

    public override void HideScreen()
    {
        base.HideScreen();
        gameObject.SetActive(false);
    }

    public void OnClose()
    {
        var wallet = GameObject.Find("Wallet");
        wallet.SetActive(false);
    }
}