using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthViewerHelper : MonoBehaviour
{
    [SerializeField]
    Material depthViewerMaterial = null;

    void Update()
    {
        if (AzureKinectUnityAPI.Instance.RGBTexture != null)
        {
            depthViewerMaterial.SetTexture("_MainTex", AzureKinectUnityAPI.Instance.RGBTexture);
        }

        if (AzureKinectUnityAPI.Instance.DepthTexture != null)
        {
            depthViewerMaterial.SetTexture("_DepthTex", AzureKinectUnityAPI.Instance.DepthTexture);
        }
    }
}
