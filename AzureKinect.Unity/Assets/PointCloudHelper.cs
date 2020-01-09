using UnityEngine;

public class PointCloudHelper : MonoBehaviour
{
    private Material material = null;

    void Update()
    {
        if (material == null)
        {
            material = GetComponent<MeshRenderer>().material;
        }

        if (AzureKinectUnityAPI.Instance.RGBTexture != null)
        {
            material.SetTexture("_MainTex", AzureKinectUnityAPI.Instance.RGBTexture);
        }

        if (AzureKinectUnityAPI.Instance.DepthTexture != null)
        {
            material.SetTexture("_DepthTex", AzureKinectUnityAPI.Instance.DepthTexture);
        }

        if (AzureKinectUnityAPI.Instance.PointTransform != null)
        {
            material.SetMatrix("_PointTransform", AzureKinectUnityAPI.Instance.PointTransform);
        }
    }
}
