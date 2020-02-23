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
    uint deviceIndex = 0;

    [SerializeField]
    private k4a_image_format_t colorFormat = k4a_image_format_t.K4A_IMAGE_FORMAT_COLOR_BGRA32;

    [SerializeField]
    private k4a_color_resolution_t colorResolution = k4a_color_resolution_t.K4A_COLOR_RESOLUTION_1080P;

    [SerializeField]
    private k4a_depth_mode_t depthMode = k4a_depth_mode_t.K4A_DEPTH_MODE_NFOV_UNBINNED;

    [SerializeField]
    private k4a_fps_t fps = k4a_fps_t.K4A_FRAMES_PER_SECOND_30;

    [SerializeField]
    RawImage rgbImage = null;

    [SerializeField]
    RawImage depthImage = null;

    [SerializeField]
    RawImage pointCloudImage = null;

    [SerializeField]
    float markerSize = 0.05f;

    [SerializeField]
    GameObject markerVisualPrefab;

    [SerializeField]
    bool testMarker = false;

    [SerializeField]
    Vector3 testMarkerPosition;

    private SpectatorViewOpenCVInterface opencvAPI;
    private byte[] colorImage;
    private Dictionary<int, GameObject> markerPrefab = new Dictionary<int, GameObject>();
    private bool locatingMarker = false;

    public void StartLocatingMarker()
    {
        AzureKinectUnityAPI.Instance(deviceIndex).PointTransform = Matrix4x4.identity;
        locatingMarker = true;
    }

    public void StopLocatingMarker()
    {
        locatingMarker = false;
    }

    protected void Awake()
    {
        AzureKinectUnityAPI.Instance(deviceIndex).SetConfiguration(colorFormat, colorResolution, depthMode, fps);
        AzureKinectUnityAPI.Instance(deviceIndex).Start();
        opencvAPI = new SpectatorViewOpenCVInterface();
        opencvAPI.Initialize(markerSize);
    }

    protected void OnDestroy()
    {
        AzureKinectUnityAPI.Instance(deviceIndex).Stop();
    }

    protected void Update()
    {
        AzureKinectUnityAPI.Instance(deviceIndex).Update();
        if (rgbImage != null &&
            rgbImage.texture == null &&
            AzureKinectUnityAPI.Instance(deviceIndex).RGBTexture != null)
        {
            rgbImage.texture = AzureKinectUnityAPI.Instance(deviceIndex).RGBTexture;
        }

        if (depthImage != null &&
            depthImage.texture == null &&
            AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture != null)
        {
            depthImage.texture = AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture;
        }

        if (pointCloudImage != null &&
            pointCloudImage.texture == null &&
            AzureKinectUnityAPI.Instance(deviceIndex).PointCloudTemplateTexture != null)
        {
            pointCloudImage.texture = AzureKinectUnityAPI.Instance(deviceIndex).PointCloudTemplateTexture;
        }

        if (locatingMarker)
        {
            if (testMarker)
            {
                AzureKinectUnityAPI.Instance(deviceIndex).PointTransform = Matrix4x4.TRS(testMarkerPosition, Quaternion.identity, Vector3.one);
                locatingMarker = false;
            }
            else if (AzureKinectUnityAPI.Instance(deviceIndex).ColorIntrinsics != null &&
                AzureKinectUnityAPI.Instance(deviceIndex).TryGetColorImageBuffer(ref colorImage))
            {
                CameraExtrinsics extrinsics = new CameraExtrinsics();
                extrinsics.ViewFromWorld = Matrix4x4.identity;
                Dictionary<int, Marker> dictionary = opencvAPI.ProcessImage(
                    colorImage,
                    AzureKinectUnityAPI.Instance(deviceIndex).ColorIntrinsics.ImageWidth,
                    AzureKinectUnityAPI.Instance(deviceIndex).ColorIntrinsics.ImageHeight,
                    PixelFormat.BGRA8,
                    AzureKinectUnityAPI.Instance(deviceIndex).ColorIntrinsics,
                    new CameraExtrinsics());

                foreach (var markerPair in dictionary)
                {
                    if (!markerPrefab.TryGetValue(markerPair.Key, out var go) &&
                        markerVisualPrefab != null)
                    {
                        go = Instantiate(markerVisualPrefab);
                        markerPrefab.Add(markerPair.Key, go);
                        go.transform.localScale *= markerSize;
                    }

                    // We won't support multiple markers.
                    var markerTransform = Matrix4x4.TRS(markerPair.Value.Position, markerPair.Value.Rotation, Vector3.one);
                    AzureKinectUnityAPI.Instance(deviceIndex).PointTransform = markerTransform.inverse;
                    locatingMarker = false;
                    break;
                }
            }
        }
    }
}
