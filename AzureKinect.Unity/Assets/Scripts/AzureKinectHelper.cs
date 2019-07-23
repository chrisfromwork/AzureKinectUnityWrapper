using System.Collections;
using System.Collections.Generic;
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

    void OnEnable()
    {
        AzureKinectUnityAPI.Instance.Start();
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
