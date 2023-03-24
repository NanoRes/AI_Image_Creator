using System.Collections.Generic;
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

namespace Solana.Unity.SDK.Example
{
    public class WalletScreen : SimpleScreen
    {
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
        private Texture theDGLNLogo = null;

        public TextMeshProUGUI publicKey_txt = null;
        public RawImage qrCode_img = null;

        private ulong totalDGLNTokens = 0;
        private CancellationTokenSource _stopTask = null;
        private static TokenMintResolver _tokenResolver = null;

        public void Start()
        {
            refreshBtn.onClick.AddListener(RefreshWallet);

            sendSolBtn.onClick.AddListener(() =>
            {
                TransitionToTransfer();
            });

            sendDGLNBtn.onClick.AddListener(() =>
            {
                TransitionToTransfer();
            });

            swapBtn.onClick.AddListener(() =>
            {
                manager.ShowScreen(this, "swap_screen");
            });

            logoutBtn.onClick.AddListener(() =>
            {
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
                    Debug.Log("Account changed!, updated lamport: " + accountInfo.Value.Lamports);
                    RefreshWallet();
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

            Texture2D tex = QRGenerator.GenerateQRTexture(Web3.Instance.Wallet.Account.PublicKey, 256, 256);
            qrCode_img.texture = tex;
        }

        private void RefreshWallet()
        {
            UpdateWalletBalanceDisplay().AsUniTask().Forget();
            GetOwnedTokenAccounts().AsAsyncUnitUniTask().Forget();
        }

        private void OnEnable()
        {
            gameObject.GetComponent<Toast>()?.ShowToast("", 1);

            Loading.StopLoading();

            var hasPrivateKey = !string.IsNullOrEmpty(Web3.Instance.Wallet?.Account.PrivateKey);
            savePrivateKeyBtn.gameObject.SetActive(hasPrivateKey);
            var hasMnemonics = !string.IsNullOrEmpty(Web3.Instance.Wallet?.Mnemonic?.ToString());
            saveMnemonicsBtn.gameObject.SetActive(hasMnemonics);
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

            var sol = await Web3.Base.GetBalance(Commitment.Confirmed);
            MainThreadDispatcher.Instance().Enqueue(() =>
            {
                lamports.text = $"{sol}";
            });

            GenerateQr();
            publicKey_txt.text = Web3.Instance.Wallet.Account.PublicKey;
        }

        private async UniTask GetOwnedTokenAccounts()
        {
            var tokens = await Web3.Base.GetTokenAccounts(Commitment.Confirmed);
            if (tokens is {Length: > 0})
            {
                var tokenAccounts = tokens.OrderByDescending(
                    tk => tk.Account.Data.Parsed.Info.TokenAmount.AmountUlong);
                foreach (var item in tokenAccounts)
                {
                    if (item.Account.Data.Parsed.Info.Mint !=
                        "E6UU5M1z4CvSAAF99d9wRoXsasWMEXsvHrz3JQRXtm2X")
                    {
                        break;
                    }

                    totalDGLNTokens = item.Account.Data.Parsed.Info.TokenAmount.AmountUlong;
                    float finalDGLNFormat = totalDGLNTokens * 0.000000001f;
                    dogelanaTokenTotal.text = finalDGLNFormat.ToString();
                    break;
                }
            }
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
            UpdateWalletBalanceDisplay().AsUniTask().Forget();
            GetOwnedTokenAccounts().Forget();
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

        private void OnDestroy()
        {
            if (_stopTask is null) return;
            _stopTask.Cancel();
        }
    }
}