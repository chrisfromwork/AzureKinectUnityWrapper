using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class AzureKinectHelper : MonoBehaviour
{
    [SerializeField]
    RawImage rgbImage = null;

    [SerializeField]
    RawImage irImage = null;

    [SerializeField]
    RawImage depthImage = null;

    [SerializeField]
    Text serialNumberText = null;

    void OnEnable()
    {
        AzureKinectUnityAPI.Instance.Start();
        if (AzureKinectUnityAPI.Instance.SerialNumber != null)
        {
            serialNumberText.text = AzureKinectUnityAPI.Instance.SerialNumber;
        }
        else
        {
            serialNumberText.text = "Device Not Found";
        }
    }

    void OnDisable()
    {
        AzureKinectUnityAPI.Instance.Stop();
    }

    void Update()
    {
        AzureKinectUnityAPI.Instance.Update();
        if (rgbImage.texture == null &&
            AzureKinectUnityAPI.Instance.RGBTexture != null)
        {
            rgbImage.texture = AzureKinectUnityAPI.Instance.RGBTexture;
        }

        if (irImage.texture == null &&
            AzureKinectUnityAPI.Instance.IRTexture != null)
        {
            irImage.texture = AzureKinectUnityAPI.Instance.IRTexture;
        }

        if (depthImage.texture == null &&
            AzureKinectUnityAPI.Instance.DepthTexture != null)
        {
            depthImage.texture = AzureKinectUnityAPI.Instance.DepthTexture;
        }
    }
}
