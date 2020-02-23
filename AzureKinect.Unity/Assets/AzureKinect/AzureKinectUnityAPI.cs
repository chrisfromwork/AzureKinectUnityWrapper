using Microsoft.MixedReality.PhotoCapture;
using Microsoft.MixedReality.SpectatorView;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public enum k4a_image_format_t : int
{
    K4A_IMAGE_FORMAT_COLOR_MJPG = 0,
    K4A_IMAGE_FORMAT_COLOR_NV12,
    K4A_IMAGE_FORMAT_COLOR_YUY2,
    K4A_IMAGE_FORMAT_COLOR_BGRA32,
    K4A_IMAGE_FORMAT_DEPTH16,
    K4A_IMAGE_FORMAT_IR16,
    K4A_IMAGE_FORMAT_CUSTOM8,
    K4A_IMAGE_FORMAT_CUSTOM16,
    K4A_IMAGE_FORMAT_CUSTOM,
}

[Serializable]
public enum k4a_color_resolution_t : int
{
    K4A_COLOR_RESOLUTION_OFF = 0, /**< Color camera will be turned off with this setting */
    K4A_COLOR_RESOLUTION_720P,    /**< 1280 * 720  16:9 */
    K4A_COLOR_RESOLUTION_1080P,   /**< 1920 * 1080 16:9 */
    K4A_COLOR_RESOLUTION_1440P,   /**< 2560 * 1440 16:9 */
    K4A_COLOR_RESOLUTION_1536P,   /**< 2048 * 1536 4:3  */
    K4A_COLOR_RESOLUTION_2160P,   /**< 3840 * 2160 16:9 */
    K4A_COLOR_RESOLUTION_3072P,   /**< 4096 * 3072 4:3  */
}

[Serializable]
public enum k4a_depth_mode_t : int
{
    K4A_DEPTH_MODE_OFF = 0,        /**< Depth sensor will be turned off with this setting. */
    K4A_DEPTH_MODE_NFOV_2X2BINNED, /**< Depth captured at 320x288. Passive IR is also captured at 320x288. */
    K4A_DEPTH_MODE_NFOV_UNBINNED,  /**< Depth captured at 640x576. Passive IR is also captured at 640x576. */
    K4A_DEPTH_MODE_WFOV_2X2BINNED, /**< Depth captured at 512x512. Passive IR is also captured at 512x512. */
    K4A_DEPTH_MODE_WFOV_UNBINNED,  /**< Depth captured at 1024x1024. Passive IR is also captured at 1024x1024. */
    K4A_DEPTH_MODE_PASSIVE_IR,     /**< Passive IR only, captured at 1024x1024. */
}

[Serializable]
public enum k4a_fps_t : int
{
    K4A_FRAMES_PER_SECOND_5 = 0, /**< 5 FPS */
    K4A_FRAMES_PER_SECOND_15,    /**< 15 FPS */
    K4A_FRAMES_PER_SECOND_30,    /**< 30 FPS */
}

public class AzureKinectUnityAPI
{
    private const string AzureKinectPluginDll = "AzureKinect.Unity";

    [DllImport(AzureKinectPluginDll, EntryPoint = "GetDeviceCount")]
    internal static extern uint GetDeviceCountNative();

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryGetDeviceSerialNumber")]
    internal static extern bool TryGetDeviceSerialNumberNative(uint index, char[] serialNum, uint serialNumSize);

    [DllImport(AzureKinectPluginDll, EntryPoint = "Initialize")]
    internal static extern bool InitializeNative();

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryGetShaderResourceViews")]
    internal static extern bool TryGetShaderResourceViewsNative(
        uint index,
        out IntPtr rgbSrv,
        out uint rgbWidth,
        out uint rgbHeight,
        out uint rgbBpp,
        out IntPtr depthSrv,
        out uint depthWidth,
        out uint depthHeight,
        out uint depthBpp,
        out IntPtr pointCloudTemplateSrv,
        out uint pointCloudTemplateWidth,
        out uint pointCloudTemplateHeight,
        out uint pointCloudTemplateBpp);

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryStartStreams")]
    internal static extern bool TryStartStreamsNative(
        uint index,
        int colorFormat,
        int colorResolution,
        int depthMode,
        int fps);

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryUpdate")]
    internal static extern bool TryUpdateNative();

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryGetCalibration")]
    internal static extern bool TryGetCalibrationNative(
        int index,
        out int colorWidth,
        out int colorHeight,
        float[] colorRotation,
        float[] colorTranslation,
        out int colorIntrinsicsCount,
        int colorIntrinsicsLength,
        float[] colorIntrinsics,
        out int depthWidth,
        out int depthHeight,
        float[] depthRotation,
        float[] depthTranslation,
        out int depthIntrinsicsCount,
        int depthIntrinsicsLength,
        float[] depthIntrinsics);

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryGetCachedColorImage")]
    internal static extern bool TryGetCachedColorImageNative(
        int index,
        byte[] data,
        int size,
        out int imageWidth,
        out int imageHeight,
        out int bytesPerPixel);

    [DllImport(AzureKinectPluginDll, EntryPoint = "StopStreaming")]
    internal static extern void StopStreamingNative(uint index);


    public static AzureKinectUnityAPI Instance(uint deviceIndex)
    {
        if (!apiDictionary.TryGetValue(deviceIndex, out AzureKinectUnityAPI api))
        {
            api = new AzureKinectUnityAPI(deviceIndex);
            apiDictionary.Add(deviceIndex, api);
        }

        return api;
    }
    private static Dictionary<uint, AzureKinectUnityAPI> apiDictionary = new Dictionary<uint, AzureKinectUnityAPI>();

    // Note these textures are flipped vertically
    public Texture2D RGBTexture { get; private set; }
    public Texture2D DepthTexture { get; private set; }
    public Texture2D PointCloudTemplateTexture { get; private set; }
    public string SerialNumber { get; private set; }
    public CameraIntrinsics ColorIntrinsics => colorIntrinsics;
    public CameraExtrinsics ColorExtrinsics => colorExtrinsics;
    public CameraExtrinsics DepthExtrinsics => depthExtrinsics;

    public bool DebugLogging
    {
        get
        {
            return debugLogging;
        }

        set
        {
            debugLogging = value;
        }
    }
    private bool debugLogging = true;

    public Matrix4x4 PointTransform
    {
        get
        {
            return pointTransform;
        }
        set
        {
            pointTransform = value;
        }
    }
    private Matrix4x4 pointTransform = Matrix4x4.identity;

    private bool initialized = false;
    private bool streaming = false;
    private uint deviceIndex = 0;
    private float lastUpdate = 0.0f;
    private CameraIntrinsics colorIntrinsics;
    private CameraExtrinsics colorExtrinsics;
    private CameraIntrinsics depthIntrinsics;
    private CameraExtrinsics depthExtrinsics;
    private k4a_image_format_t colorFormat = k4a_image_format_t.K4A_IMAGE_FORMAT_COLOR_BGRA32;
    private k4a_color_resolution_t colorResolution = k4a_color_resolution_t.K4A_COLOR_RESOLUTION_1080P;
    private k4a_depth_mode_t depthMode = k4a_depth_mode_t.K4A_DEPTH_MODE_NFOV_UNBINNED;
    private k4a_fps_t fps = k4a_fps_t.K4A_FRAMES_PER_SECOND_30;

    private AzureKinectUnityAPI(
        uint deviceIndex)
    {
        this.deviceIndex = deviceIndex;
    }

    public void SetConfiguration(
        k4a_image_format_t colorFormat,
        k4a_color_resolution_t colorResolution,
        k4a_depth_mode_t depthMode,
        k4a_fps_t fps)
    {
        this.colorFormat = colorFormat;
        this.colorResolution = colorResolution;
        this.depthMode = depthMode;
        this.fps = fps;
    }

    public void Start()
    {
        if (streaming)
        {
            return; 
        }

        Initialize();
        if (initialized)
        {
            uint deviceCount = GetDeviceCountNative();
            DebugLog($"Devices Found: {deviceCount}");

            if (TryStartStreamsNative(
                deviceIndex,
                (int)colorFormat,
                (int)colorResolution,
                (int)depthMode,
                (int)fps))
            {
                char[] serialNumber = new char[256];
                if(TryGetDeviceSerialNumberNative(deviceIndex, serialNumber, (uint) serialNumber.Length))
                {
                    SerialNumber = new string(serialNumber);
                }
                else
                {
                    DebugLog($"Failed to obtain device serial number: {deviceIndex}");
                }

                streaming = true;
            }
            else
            {
                DebugLog($"Failed to start device streaming: {deviceIndex}");
            }
        }
    }

    public void Update()
    {
        if (lastUpdate == Time.time)
        {
            return;
        }

        lastUpdate = Time.time;

        if (!streaming)
        {
            Start();
        }

        if (streaming &&
            TryUpdateNative() &&
            (RGBTexture == null || DepthTexture == null || PointCloudTemplateTexture == null))
        {
            bool succeeded = TryGetShaderResourceViewsNative(
                deviceIndex,
                out var rgbSrv,
                out var rgbWidth,
                out var rgbHeight,
                out var rgbBpp,
                out var depthSrv,
                out var depthWidth,
                out var depthHeight,
                out var depthBpp,
                out var pointCloudSrv,
                out var pointCloudWidth,
                out var pointCloudHeight,
                out var pointCloudBpp);
            DebugLog($"Succeeded obtaining shader resource views: {succeeded}");

            if (succeeded &
                RGBTexture == null &&
                rgbSrv != null &&
                rgbWidth > 0 &&
                rgbHeight > 0)
            {
                DebugLog($"Creating RGBTexture: {rgbWidth}x{rgbHeight}");
                RGBTexture = Texture2D.CreateExternalTexture((int)rgbWidth, (int)rgbHeight, TextureFormat.BGRA32, false, false, rgbSrv);
            }

            if (succeeded &&
                DepthTexture == null &&
                depthSrv != null &&
                depthWidth > 0 &&
                depthHeight > 0)
            {
                DebugLog($"Creating DepthTexture: {depthWidth}x{depthHeight}");
                DepthTexture = Texture2D.CreateExternalTexture((int)depthWidth, (int)depthHeight, TextureFormat.R16, false, false, depthSrv);
            }

            if (succeeded &&
                PointCloudTemplateTexture == null &&
                pointCloudSrv != null &&
                pointCloudWidth > 0 &&
                pointCloudHeight > 0)
            {
                DebugLog($"Creating PointCloudTemplateTexture: {pointCloudWidth}x{pointCloudHeight}");
                PointCloudTemplateTexture = Texture2D.CreateExternalTexture((int)pointCloudWidth, (int)pointCloudHeight, TextureFormat.BGRA32, false, false, pointCloudSrv);
            }

            if (succeeded &&
                (colorIntrinsics == null ||
                depthIntrinsics == null))
            {
                float[] colorRotation = new float[9];
                float[] colorTranslation = new float[3];
                float[] colorIntrinsics = new float[15];
                float[] depthRotation = new float[9];
                float[] depthTranslation = new float[3];
                float[] depthIntrinsics = new float[15];

                bool obtainCalibration = TryGetCalibrationNative(
                    (int)deviceIndex,
                    out var colorWidth,
                    out var colorHeight,
                    colorRotation,
                    colorTranslation,
                    out var colorIntrinsicsCount,
                    colorIntrinsics.Length,
                    colorIntrinsics,
                    out var depthCalibrationWidth,
                    out var depthCalibrationHeight,
                    depthRotation,
                    depthTranslation,
                    out var depthCalibrationIntrinsicsCount,
                    depthIntrinsics.Length,
                    depthIntrinsics);

                if (obtainCalibration)
                {
                    this.colorIntrinsics = new CameraIntrinsics();
                    this.colorIntrinsics.ImageWidth = (uint)colorWidth;
                    this.colorIntrinsics.ImageHeight = (uint)colorHeight;
                    this.colorIntrinsics.PrincipalPoint.x = colorIntrinsics[0];
                    this.colorIntrinsics.PrincipalPoint.y = colorIntrinsics[1];
                    this.colorIntrinsics.FocalLength.x = colorIntrinsics[2];
                    this.colorIntrinsics.FocalLength.y = colorIntrinsics[3];
                    this.colorIntrinsics.RadialDistortion = new Vector3(colorIntrinsics[4], colorIntrinsics[5], colorIntrinsics[6]);
                    this.colorIntrinsics.TangentialDistortion = new Vector3(colorIntrinsics[12], colorIntrinsics[13]);

                    this.colorExtrinsics = new CameraExtrinsics();
                    this.colorExtrinsics.ViewFromWorld = Matrix4x4.TRS(CalculateUnityTranslation(colorTranslation), CalculateUnityRotation(colorRotation), Vector3.one);

                    this.depthIntrinsics = new CameraIntrinsics();
                    this.depthIntrinsics.ImageWidth = (uint)depthWidth;
                    this.depthIntrinsics.ImageHeight = (uint)depthHeight;
                    this.depthIntrinsics.PrincipalPoint.x = depthIntrinsics[0];
                    this.depthIntrinsics.PrincipalPoint.y = depthIntrinsics[1];
                    this.depthIntrinsics.FocalLength.x = depthIntrinsics[2];
                    this.depthIntrinsics.FocalLength.y = depthIntrinsics[3];
                    this.depthIntrinsics.RadialDistortion = new Vector3(depthIntrinsics[4], depthIntrinsics[5], depthIntrinsics[6]);
                    this.depthIntrinsics.TangentialDistortion = new Vector3(depthIntrinsics[12], depthIntrinsics[13]);

                    Matrix4x4 depthRotationMatrix = new Matrix4x4(
                        new Vector4(depthRotation[0], depthRotation[3], depthRotation[6], 1.0f),
                        new Vector4(depthRotation[1], depthRotation[4], depthRotation[7], 1.0f),
                        new Vector4(depthRotation[2], depthRotation[5], depthRotation[8], 1.0f),
                        Vector4.one);

                    this.depthExtrinsics = new CameraExtrinsics();
                    this.depthExtrinsics.ViewFromWorld = Matrix4x4.TRS(CalculateUnityTranslation(depthTranslation), CalculateUnityRotation(depthRotation), Vector3.one);
                }
            }
        }
    }

    private Quaternion CalculateUnityRotation(float[] azureRotation)
    {
        Matrix4x4 rotationMatrix = new Matrix4x4(
            new Vector4(azureRotation[0], azureRotation[1], azureRotation[2], 1.0f),
            new Vector4(azureRotation[3], azureRotation[4], azureRotation[5], 1.0f),
            new Vector4(azureRotation[6], azureRotation[7], azureRotation[8], 1.0f),
            Vector4.one);
        Quaternion rotationQuat = rotationMatrix.rotation;
        return rotationQuat;
    }

    private Vector3 CalculateUnityTranslation(float[] azureTranslation)
    {
        // Convert mm to m
        return new Vector3(azureTranslation[0] / 1000.0f, azureTranslation[1] / 1000.0f, azureTranslation[2] / 1000.0f);
    }

    public void Stop()
    {
        if (streaming)
        {
            StopStreamingNative(deviceIndex);
            streaming = false;
        }
    }

    public bool TryGetColorImageBuffer(ref byte[] colorImage)
    {
        if (colorIntrinsics == null ||
            RGBTexture == null)
        {
            return false;
        }

        int colorImageSize = 4 * (int) colorIntrinsics.ImageWidth * (int) colorIntrinsics.ImageHeight;
        if (colorImage == null ||
            colorImage.Length < colorImageSize)
        {
            colorImage = new byte[colorImageSize];
        }

        return TryGetCachedColorImageNative(
            (int)deviceIndex,
            colorImage,
            colorImage.Length,
            out var imageWidth,
            out var imageHeight,
            out var bytesPerPixel);
    }

    private void Initialize()
    {
        if (!initialized)
        {
            initialized = InitializeNative();
            if (!initialized)
            {
                DebugLog("Failed to initialize AzureKinect.Unity plugin.");
            }
        }
    }

    private void DebugLog(string message)
    {
        if (debugLogging)
        {
            UnityEngine.Debug.Log($"AzureKinectUnityAPI: {message}");
        }
    }
}
