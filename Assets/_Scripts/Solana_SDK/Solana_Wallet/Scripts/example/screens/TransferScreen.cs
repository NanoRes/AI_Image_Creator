using System;
using Solana.Unity.Extensions.TokenMint;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solana.Unity.SDK.Example
{
    public class TransferScreen : SimpleScreen
    {
        [SerializeField] private ApplicationData applicationData = null;
        [SerializeField] private UserData userData = null;

        public TextMeshProUGUI ownedAmountTxt;
        public TextMeshProUGUI nftTitleTxt;
        public TextMeshProUGUI errorTxt;
        public TMP_InputField toPublicTxt;
        public TMP_InputField amountTxt;
        public Button transferBtn;
        public RawImage nftImage;
        public Button closeBtn;
        
        private const long SolLamports = 1000000000;

        private void Start()
        {
            transferBtn.onClick.AddListener(TryTransfer);

            closeBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "wallet_screen");
            });
        }

        private void TryTransfer()
        {
            if (applicationData.currentTransferMethodSelected == ApplicationData.PaymentMethod.SOL)
            {
                if (CheckInput())
                {
                    TransferSol();
                }
            }
            else if (applicationData.currentTransferMethodSelected == ApplicationData.PaymentMethod.DGLN)
            {
                if (CheckInput())
                {
                    TransferDogelana();
                }
            }
        }

        private async void TransferSol()
        {
            RequestResult<string> result = await Web3.Instance.Wallet.Transfer(
                new PublicKey(toPublicTxt.text),
                Convert.ToUInt64(float.Parse(amountTxt.text)*SolLamports));
            HandleResponse(result);
        }

        private async void TransferDogelana()
        {
            RequestResult<string> result = await Web3.Instance.Wallet.Transfer(
                new PublicKey(toPublicTxt.text),
                new PublicKey(applicationData.mintDGLNAddress),
                Convert.ToUInt64(float.Parse(amountTxt.text) * 10000000));
            HandleResponse(result);
        }

        bool CheckInput()
        {
            if (string.IsNullOrEmpty(amountTxt.text))
            {
                errorTxt.text = "Please input transfer amount";
                return false;
            }

            if (string.IsNullOrEmpty(toPublicTxt.text))
            {
                errorTxt.text = "Please enter receiver public key";
                return false;
            }

            if (applicationData.currentTransferMethodSelected == ApplicationData.PaymentMethod.SOL)
            {
                if (float.Parse(amountTxt.text) > userData.totalSolanaTokens)
                {
                    errorTxt.text = "Not enough SOL for this transaction.";
                    return false;
                }
            }
            else if (applicationData.currentTransferMethodSelected == ApplicationData.PaymentMethod.DGLN)
            {
                if (ulong.Parse(amountTxt.text) > userData.totalDogelanaTokens)
                {
                    errorTxt.text = "Not enough Dogelana for this transaction.";
                    return false;
                }
            }

            errorTxt.text = "";
            return true;
        }

        private void HandleResponse(RequestResult<string> result)
        {
            errorTxt.text = result.Result == null ? result.Reason : "";
            if (result.Result != null)
            {
                manager.ShowScreen(this, "wallet_screen");
            }
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();

            ResetInputFields();
            gameObject.SetActive(true);

            if(applicationData.currentTransferMethodSelected == ApplicationData.PaymentMethod.SOL)
            {
                nftImage.texture = applicationData.solanaTexture;
                nftTitleTxt.text = userData.totalSolanaTokens + "";
            }
            else if(applicationData.currentTransferMethodSelected == ApplicationData.PaymentMethod.DGLN)
            {
                nftImage.texture = applicationData.dogelanaTexture;
                nftTitleTxt.text = (userData.totalDogelanaTokens * 0.000000001f) + "";
            }
        }

        public void OnClose()
        {
            applicationData.isWalletOpen = false;
        }

        private void ResetInputFields()
        {
            errorTxt.text = "";
            amountTxt.text = "";
            toPublicTxt.text = "";
            amountTxt.interactable = true;
        }

        public override void HideScreen()
        {
            base.HideScreen();

            gameObject.SetActive(false);
        }
    }
}