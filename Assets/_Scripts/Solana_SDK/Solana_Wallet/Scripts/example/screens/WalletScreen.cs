using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;
using Cysharp.Threading.Tasks;
using Solana.Unity.Extensions;
using Solana.Unity.Rpc.Types;
using System;
using Solana.Unity.Rpc.Models;
using System.Collections;
using Org.BouncyCastle.Ocsp;

namespace Solana.Unity.SDK.Example
{
    public class WalletScreen : SimpleScreen
    {
        [SerializeField]
        private ApplicationData applicationData = null;
        [SerializeField]
        private UserData userData = null;

        [Header("UI Elements")]
        [SerializeField]
        private TextMeshProUGUI lamports = null;
        [SerializeField]
        private TextMeshProUGUI dogelanaTokenTotal = null;
        [SerializeField]
        private Button refreshBtn = null;
        [SerializeField]
        private Button sendSolBtn = null;
        [SerializeField]
        private Button sendDGLNBtn = null;
        [SerializeField]
        private Button swapBtn = null;
        [SerializeField]
        private Button logoutBtn = null;
        [SerializeField]
        private Button saveMnemonicsBtn = null;
        [SerializeField]
        private Button savePublicKeyBtn = null;
        [SerializeField]
        private Button savePrivateKeyBtn = null;
        [SerializeField]
        private Button qrCodeBtn = null;
        [SerializeField]
        private TextMeshProUGUI publicKeyText = null;
        [SerializeField]
        private RawImage qrCodeImage = null;

        private bool hasInitialRefreshOccurred = false;
        private CancellationTokenSource _stopTask = null;

        private static TokenMintResolver _tokenResolver = null;

        private void Awake()
        {
            hasInitialRefreshOccurred = false;
        }

        public void Start()
        {
            refreshBtn.onClick.AddListener(RefreshWallet);

            sendSolBtn.onClick.AddListener(() =>
            {
                applicationData.currentTransferMethodSelected = ApplicationData.PaymentMethod.SOL;
                TransitionToTransfer();
            });

            sendDGLNBtn.onClick.AddListener(() =>
            {
                applicationData.currentTransferMethodSelected = ApplicationData.PaymentMethod.DGLN;
                TransitionToTransfer();
            });

            swapBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "swap_screen");
            });

            logoutBtn.onClick.AddListener(() =>
            {
                hasInitialRefreshOccurred = false;
                userData.ResetData();
                Web3.Instance.Logout();
                manager.ShowScreen(this, "login_screen");
            });

            qrCodeBtn.onClick.AddListener(SavePublicKeyOnClick);
            savePublicKeyBtn.onClick.AddListener(SavePublicKeyOnClick);
            savePrivateKeyBtn.onClick.AddListener(SavePrivateKeyOnClick);
            saveMnemonicsBtn.onClick.AddListener(SaveMnemonicsOnClick);

            _stopTask = new CancellationTokenSource();

            Web3.WsRpc.SubscribeAccountInfo(
                Web3.Instance.Wallet.Account.PublicKey,
                (_, accountInfo) =>
                {
                    Debug.Log("Account changed!, updated lamport: " 
                        + accountInfo.Value.Lamports);
                    lamports.text = $"{ accountInfo.Value.Lamports * 0.000000001f }";
                    userData.totalSolanaTokens = accountInfo.Value.Lamports * 0.000000001f;
                },
                Commitment.Confirmed
            );

            Web3.WsRpc.SubscribeTokenAccount(
                "8UxoXccJjiSMjPuP77BCM5mVx1tqBWVyo8RcHa1jasnS",
                (_, tokenInfo) =>
                {
                    Debug.Log("Account changed!, updated token account: "
                        + tokenInfo.Value.Data.Parsed.Info.TokenAmount.ToString());
                    userData.totalDogelanaTokens = tokenInfo.Value.Data.Parsed.Info.TokenAmount.AmountUlong;
                    float finalDGLNFormat = userData.totalDogelanaTokens ;
                    dogelanaTokenTotal.text = finalDGLNFormat.ToString();
                },
                Commitment.Confirmed
            );
        }

        private void GenerateQr()
        {
            if(Web3.Instance.Wallet == null)
            {
                return;
            }

            if (Web3.Instance.Wallet.Account == null)
            {
                return;
            }

            Texture2D tex = QRGenerator.GenerateQRTexture(
                Web3.Instance.Wallet.Account.PublicKey, 256, 256);
            qrCodeImage.texture = tex;
        }

        private void RefreshWallet()
        {
            publicKeyText.text = Web3.Instance.Wallet.Account.PublicKey;
            GenerateQr();

            UpdateWalletBalanceDisplay().AsUniTask().Forget();
            ShowDGLNBalance().AsUniTask().Forget();
            //GetOwnedTokenAccounts().AsUniTask().Forget();
        }

        private void OnEnable()
        {
            gameObject.GetComponent<Toast>()?.ShowToast("", 1);

            Loading.StopLoading();   
        }

        private void SavePublicKeyOnClick()
        {
            if (!gameObject.activeSelf) return;

            if (string.IsNullOrEmpty(Web3.Instance.Wallet.Account.PublicKey?.ToString())) return;

            Clipboard.Copy(Web3.Instance.Wallet.Account.PublicKey.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Public Key Copied!", 3);
        }

        private void SavePrivateKeyOnClick()
        {
            if (!gameObject.activeSelf) return;

            if (string.IsNullOrEmpty(Web3.Instance.Wallet.Account.PrivateKey?.ToString())) return;

            Clipboard.Copy(Web3.Instance.Wallet.Account.PrivateKey.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Private Key Copied!", 3);
        }
        
        private void SaveMnemonicsOnClick()
        {
            if (!gameObject.activeSelf) return;

            if (string.IsNullOrEmpty(Web3.Instance.Wallet.Mnemonic?.ToString())) return;

            Clipboard.Copy(Web3.Instance.Wallet.Mnemonic.ToString());
            gameObject.GetComponent<Toast>()?.ShowToast("Mnemonics Copied!", 3);
        }

        private void TransitionToTransfer(object data = null)
        {
            manager.ShowScreen(this, "transfer_screen", data);
        }

        private async Task UpdateWalletBalanceDisplay()
        {
            if (Web3.Instance.Wallet.Account is null) return;

            double sol = await Web3.Base.GetBalance(Commitment.Confirmed);

            MainThreadDispatcher.Instance().Enqueue(() =>
            {
                lamports.text = $"{sol}";
                userData.totalSolanaTokens = sol;
            });
        }

        private IEnumerator ShowDGLNTokens(TokenAccount[] tokenAccounts)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (tokenAccounts.Length <= 0)
            {
                yield break;
            }

            foreach (var tokenAccount in tokenAccounts)
            {
                if (tokenAccount.Account.Data.Parsed.Info.Mint != applicationData.mintDGLNAddress)
                {
                    continue;
                }

                userData.totalDogelanaTokens = tokenAccount.Account.Data.Parsed.Info.
                    TokenAmount.AmountUlong;
                float finalDGLNFormat = userData.totalDogelanaTokens * 0.000000001f;
                dogelanaTokenTotal.text = finalDGLNFormat.ToString();

                userData.dogelanaTokenAddress = tokenAccount.PublicKey;
                Debug.Log("Dogelana Token Account Public Key: " + tokenAccount.PublicKey);

                yield break;
            }
        }

        private IEnumerator ShowDGLNBalance(TokenBalance tokenBalance)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            userData.totalDogelanaTokens = tokenBalance.AmountUlong;
            float finalDGLNFormat = userData.totalDogelanaTokens * 0.000000001f;
            dogelanaTokenTotal.text = finalDGLNFormat.ToString();
        }

        private async Task ShowDGLNBalance()
        {
            TokenBalance tokenBalance = await Web3.Base.GetTokenBalanceByOwnerAsync(
                new Wallet.PublicKey(applicationData.mintDGLNAddress));

            MainThreadDispatcher.Instance().Enqueue(ShowDGLNBalance(tokenBalance));
        }

        private async Task GetOwnedTokenAccounts()
        {
            TokenAccount[] tokenBalance = await Web3.Base.GetTokenAccounts(
                new Wallet.PublicKey(applicationData.mintDGLNAddress),
                null);

            MainThreadDispatcher.Instance().Enqueue(ShowDGLNTokens(tokenBalance));
        }
        
        public static async UniTask<TokenMintResolver> GetTokenMintResolver()
        {
            if(_tokenResolver != null) return _tokenResolver;

            var tokenResolver = await TokenMintResolver.LoadAsync();

            if(tokenResolver != null) _tokenResolver = tokenResolver;

            return _tokenResolver;
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();

            gameObject.SetActive(true);
            lamports.text = string.Empty;
            dogelanaTokenTotal.text = string.Empty;
            publicKeyText.text = string.Empty;

            lamports.text = $"{ userData.totalSolanaTokens }";
            dogelanaTokenTotal.text = (userData.totalDogelanaTokens * 0.000000001f).ToString();

            publicKeyText.text = Web3.Instance.Wallet.Account.PublicKey;

            if(hasInitialRefreshOccurred == true)
            {
                return;
            }
            hasInitialRefreshOccurred = true;

            GenerateQr();

            MainThreadDispatcher.Instance().Enqueue(DelayRefreshingData());
        }

        private IEnumerator DelayRefreshingData()
        {
            yield return new WaitForSeconds(1f);

            publicKeyText.text = Web3.Instance.Wallet.Account.PublicKey;
            GenerateQr();

            UpdateWalletBalanceDisplay().AsUniTask().Forget();
            //ShowDGLNBalance().AsUniTask().Forget();
            GetOwnedTokenAccounts().AsUniTask().Forget();

            var hasPrivateKey = !string.IsNullOrEmpty(Web3.Instance.Wallet?.Account.PrivateKey);
            savePrivateKeyBtn.gameObject.SetActive(hasPrivateKey);

            var hasMnemonics = !string.IsNullOrEmpty(Web3.Instance.Wallet?.Mnemonic?.ToString());
            saveMnemonicsBtn.gameObject.SetActive(hasMnemonics);
        }

        public override void HideScreen()
        {
            base.HideScreen();

            gameObject.SetActive(false);
        }
        
        public void OnClose()
        {
            applicationData.isWalletOpen = false;
        }

        private void OnDestroy()
        {
            if (_stopTask is null) return;

            _stopTask.Cancel();
        }
    }
}