using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WalletHolder : MonoBehaviour
{
    [SerializeField]
    private Button toggleWallet_btn = null;
    [SerializeField]
    private GameObject wallet = null;

    [SerializeField]
    private TMP_Text versionText = null;

    private const string versionTextPrefix = "Version - ";

    private void Awake()
    {
        versionText.text = versionTextPrefix + Application.version;
    }

    void Start()
    {
        wallet.SetActive(false);

        toggleWallet_btn.onClick.AddListener(() => 
        {
            wallet.SetActive(!wallet.activeSelf);
        });
    }
}