using Microsoft.MixedReality.PhotoCapture;
using Microsoft.MixedReality.SpectatorView;
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

    [SerializeField]
    float markerSize = 0.05f;

    [SerializeField]
    GameObject markerVisualPrefab;

    [SerializeField]
    Vector3 markerRotation;

    private SpectatorViewOpenCVInterface opencvAPI;
    private byte[] colorImage;
    private Dictionary<int, GameObject> markerPrefab = new Dictionary<int, GameObject>();

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

        opencvAPI = new SpectatorViewOpenCVInterface();
        opencvAPI.Initialize(markerSize);
    }

    void OnDestroy()
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

        if (AzureKinectUnityAPI.Instance.ColorIntrinsics != null &&
            AzureKinectUnityAPI.Instance.TryGetColorImageBuffer(ref colorImage))
        {
            CameraExtrinsics extrinsics = new CameraExtrinsics();
            extrinsics.ViewFromWorld = Matrix4x4.identity;
            var dictionary = opencvAPI.ProcessImage(
                colorImage,
                AzureKinectUnityAPI.Instance.ColorIntrinsics.ImageWidth,
                AzureKinectUnityAPI.Instance.ColorIntrinsics.ImageHeight,
                PixelFormat.BGRA8,
                AzureKinectUnityAPI.Instance.ColorIntrinsics,
                new CameraExtrinsics());

            foreach(var markerPair in dictionary)
            {
                if (!markerPrefab.TryGetValue(markerPair.Key, out var go))
                {
                    go = Instantiate(markerVisualPrefab);
                    markerPrefab.Add(markerPair.Key, go);
                    go.transform.localScale *= markerSize;
                }

                // I'm not sure if applying the depth transform inverse here is correct, luckily the depth transform is an identity transform for the chosen azure kinect calibration
                Matrix4x4 markerTransform = Matrix4x4.TRS(markerPair.Value.Position, markerPair.Value.Rotation, Vector3.one);
                Matrix4x4 markerLocationRelativeToDepth =
                    AzureKinectUnityAPI.Instance.DepthExtrinsics.ViewFromWorld.inverse *
                    markerTransform;
                Vector3 position = markerLocationRelativeToDepth.GetColumn(3);
                Quaternion rotation = Quaternion.LookRotation(markerLocationRelativeToDepth.GetColumn(2), markerLocationRelativeToDepth.GetColumn(1));
                go.transform.localPosition = position;
                go.transform.localRotation = rotation;
                //AzureKinectUnityAPI.Instance.PointTransform = markerTransform.inverse;
            }
        }
    }
}
