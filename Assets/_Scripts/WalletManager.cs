using UnityEngine;

public class WalletManager : MonoBehaviour
{
    [SerializeField]
    private ApplicationData applicationData = null;

    private GameObject wallet = null;

    private const int walletChildIndex = 0;

    private void Awake()
    {
        wallet = transform.GetChild(walletChildIndex).gameObject;
        wallet.SetActive(false);
        applicationData.isWalletOpen = false;
    }

    private void Update()
    {
        if (applicationData.isWalletOpen == wallet.activeSelf)
        {
            return;
        }

        wallet.SetActive(applicationData.isWalletOpen);
    }
}
