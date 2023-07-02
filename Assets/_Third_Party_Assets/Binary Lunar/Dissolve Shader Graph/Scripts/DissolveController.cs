using UnityEngine;
using UnityEngine.UI;

public class DissolveController : MonoBehaviour
{
    public float dissolveAmount;
    public float dissolveSpeed;
    public bool isDissolving;
    [ColorUsageAttribute(true,true)]
    public Color outColor;
    [ColorUsageAttribute(true, true)]
    public Color inColor;

    private Material mat;

    [SerializeField] private GameObject pictureFrame;

    // Start is called before the first frame update
    private void OnEnable()
    {
        if (mat == null)
        {
            mat = GetComponent<RawImage>().material;
        }

        isDissolving = true;
        dissolveAmount = 0;

       // AIImageCreator.newImageRequest += RemoveFrame;
    }

    private void OnDisable()
    {
        isDissolving = true;
        dissolveAmount = 0;
        pictureFrame.SetActive(false);
      //  AIImageCreator.newImageRequest -= RemoveFrame;
    }

    // Update is called once per frame
    void Update()
    {
       /* if (Input.GetKeyDown(KeyCode.A))
            isDissolving = true;

        if (Input.GetKeyDown(KeyCode.S))
            isDissolving = false;

        if (GetComponent<RawImage>().texture != null && imageGenerated == false) 
        {
            imageGenerated = true;
            isDissolving = false;
        }*/

        if (isDissolving)
        {
            DissolveOut(dissolveSpeed, outColor);
        }

        if (!isDissolving)
        {
            DissolveIn(dissolveSpeed, inColor);
        }

        mat.SetFloat("_DissolveAmount", dissolveAmount);
    }


    public void DissolveOut(float speed, Color color)
    {
        mat.SetColor("_DissolveColor", color);
        if (dissolveAmount > -0.1)
        {
            dissolveAmount -= Time.deltaTime * speed;
        }
    }

    public void DissolveIn(float speed, Color color)
    {
        mat.SetColor("_DissolveColor", color);
        if (dissolveAmount < 1)
        {
            dissolveAmount += Time.deltaTime * dissolveSpeed;
        }
        if(Mathf.Round(dissolveAmount) >= 1)
        {
            pictureFrame.SetActive(true);
        }
    }

    public void ImageActivated() 
    {
        isDissolving = false;
        dissolveAmount = 0;
    }

    private void RemoveFrame() 
    {
        pictureFrame.SetActive(false);
    }
}
