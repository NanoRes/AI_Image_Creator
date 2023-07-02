using UnityEngine;

public class Loading : MonoBehaviour
{
    private static GameObject loadingObject = null;

    private void Awake()
    {
        loadingObject = transform.GetChild(0).gameObject;
    }

    public static void StartLoading()
    {
        if (loadingObject == null)
        {
            return;
        }

        loadingObject.transform.GetChild(0)?.gameObject.SetActive(true);
    }
    
    public static void StopLoading()
    {
        if (loadingObject == null)
        {
            return;
        }

        loadingObject.gameObject.SetActive(false);
    }
}