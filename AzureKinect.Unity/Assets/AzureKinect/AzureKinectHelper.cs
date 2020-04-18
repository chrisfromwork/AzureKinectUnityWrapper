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

    protected void Awake()
    {
        AzureKinectUnityAPI.Instance(deviceIndex).SetConfiguration(colorFormat, colorResolution, depthMode, fps);
        AzureKinectUnityAPI.Instance(deviceIndex).Start();
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
    }

    public bool TryGetImageBuffers(out byte[] transformedColorImageBuffer, out byte[] depthImageBuffer, out byte[] pointCloudImageBuffer)
    {
        return AzureKinectUnityAPI.Instance(deviceIndex).TryGetImageBuffers(out transformedColorImageBuffer, out depthImageBuffer, out pointCloudImageBuffer);
    }

    public Texture2D GetRGBTexture()
    {
        return AzureKinectUnityAPI.Instance(deviceIndex).RGBTexture;
    }
}
