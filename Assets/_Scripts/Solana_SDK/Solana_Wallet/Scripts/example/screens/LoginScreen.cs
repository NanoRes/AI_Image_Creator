using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class LoginScreen : SimpleScreen
    {
        [SerializeField]
        private TMP_InputField passwordInputField = null;
        [SerializeField]
        private TextMeshProUGUI passwordText = null;
        [SerializeField]
        private Button loginBtn = null;
        [SerializeField]
        private Button loginBtnGoogle = null;
        [SerializeField]
        private Button loginBtnTwitter = null;
        [SerializeField]
        private Button loginBtnPhantom = null;
        [SerializeField]
        private Button loginBtnXNFT = null;
        [SerializeField]
        private TextMeshProUGUI messageTxt = null;

        private GameObject wallet = null;

        private void Awake()
        {
            wallet = GameObject.Find("Wallet");
        }

        private void OnEnable()
        {
            passwordInputField.text = string.Empty;
            messageTxt.gameObject.SetActive(false);
            messageTxt.text = string.Empty;
        }

        private void Start()
        {
            passwordText.text = "";

            passwordInputField.onSubmit.AddListener(delegate { LoginChecker(); });

            loginBtn.onClick.AddListener(LoginChecker);
            loginBtnPhantom.onClick.AddListener(LoginCheckerPhantom);

            loginBtnXNFT.onClick.AddListener(LoginCheckerXnft);

            if (Application.platform != RuntimePlatform.Android &&
                Application.platform != RuntimePlatform.IPhonePlayer
                && Application.platform != RuntimePlatform.WindowsPlayer
                && Application.platform != RuntimePlatform.WindowsEditor
                && Application.platform != RuntimePlatform.LinuxPlayer
                && Application.platform != RuntimePlatform.LinuxEditor
                && Application.platform != RuntimePlatform.OSXPlayer
                && Application.platform != RuntimePlatform.OSXEditor)
            {
                loginBtnGoogle.gameObject.SetActive(false);
                loginBtnTwitter.gameObject.SetActive(false);
            }

            loginBtnXNFT.gameObject.SetActive(false);

            if (messageTxt != null)
            {
                messageTxt.gameObject.SetActive(false);
            }

#if UNITY_EDITOR

            string savedPassword = PlayerPrefs.GetString("EDITOR_PASSWORD", "");

            if (string.IsNullOrEmpty(savedPassword) == false)
            {
                passwordInputField.SetTextWithoutNotify(savedPassword);
                LoginChecker();
                OnClose();
                return;
            }
#endif
        }

        private async void LoginChecker()
        {
            var password = passwordInputField.text;
            var account = await Web3.Instance.LoginInGameWallet(password);
            CheckAccount(account);
        }

        private async void LoginCheckerPhantom()
        {
            var account = await Web3.Instance.LoginPhantom();
            CheckAccount(account);
        }

        private async void LoginCheckerWeb3Auth(Provider provider)
        {
            var account = await Web3.Instance.LoginInWeb3Auth(provider);
            CheckAccount(account);
        }

        public void TryLoginBackPack()
        {
            LoginCheckerXnft();
        }

        private async void LoginCheckerXnft()
        {
            if (Web3.Instance == null) return;
            var account = await Web3.Instance.LoginXNFT();
            messageTxt.text = "";
            CheckAccount(account);
        }


        private void CheckAccount(Account account)
        {
            if (account != null)
            {
                manager.ShowScreen(this, "wallet_screen");
                messageTxt.gameObject.SetActive(false);
                gameObject.SetActive(false);

#if UNITY_EDITOR
                PlayerPrefs.SetString("EDITOR_PASSWORD", passwordInputField.text);
                PlayerPrefs.Save();
#endif
            }
            else
            {
                passwordInputField.text = string.Empty;
                messageTxt.gameObject.SetActive(true);
                messageTxt.text = "Incorrect Password";
            }
        }

        public void OnClose()
        {
            wallet.SetActive(false);
        }
    }
}

