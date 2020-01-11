using Microsoft.MixedReality.PhotoCapture;
using Microsoft.MixedReality.SpectatorView;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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
        out IntPtr irSrv,
        out uint irWidth,
        out uint irHeight,
        out uint irBpp,
        out IntPtr depthSrv,
        out uint depthWidth,
        out uint depthHeight,
        out uint depthBpp);

    [DllImport(AzureKinectPluginDll, EntryPoint = "TryStartStreams")]
    internal static extern bool TryStartStreamsNative(uint index);

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

    public static AzureKinectUnityAPI Instance
    {
        get
        {
            return api;
        }
    }
    private static AzureKinectUnityAPI api = new AzureKinectUnityAPI();

    // Note these textures are flipped vertically
    public Texture2D RGBTexture { get; private set; }
    public Texture2D IRTexture { get; private set; }
    public Texture2D DepthTexture { get; private set; }
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

    private AzureKinectUnityAPI() { }

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

            if (TryStartStreamsNative(deviceIndex))
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
            (RGBTexture == null || IRTexture == null || DepthTexture == null))
        {
            bool succeeded = TryGetShaderResourceViewsNative(
                deviceIndex,
                out var rgbSrv,
                out var rgbWidth,
                out var rgbHeight,
                out var rgbBpp,
                out var irSrv,
                out var irWidth,
                out var irHeight,
                out var irBpp,
                out var depthSrv,
                out var depthWidth,
                out var depthHeight,
                out var depthBpp);
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
                IRTexture == null &&
                irSrv != null &&
                irWidth > 0 &&
                irHeight > 0)
            {
                DebugLog($"Creating IRTexture: {irWidth}x{irHeight}");
                IRTexture = Texture2D.CreateExternalTexture((int)irWidth, (int)irHeight, TextureFormat.R16, false, false, irSrv);
            }

            if (succeeded &&
                DepthTexture == null &&
                depthSrv != null &&
                depthWidth > 0 &&
                depthHeight > 0)
            {
                DebugLog($"Creating DepthTexture: {depthWidth}x{depthHeight}");
                DepthTexture = Texture2D.CreateExternalTexture((int)depthWidth, (int)depthHeight, TextureFormat.BGRA32, false, false, depthSrv);
            }

            // TODO - only call once
            if (succeeded)
            //if (succeeded &&
            //    (colorIntrinsics == null ||
            //    depthIntrinsics == null))
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
                    this.colorIntrinsics.ImageWidth = (uint) colorWidth;
                    this.colorIntrinsics.ImageHeight = (uint) colorHeight;
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

    // It's unclear if azure kinect uses left handed or right handed notation for rotation
    // Regardless, it seems that depth and rgb translations are zero

    private Quaternion CalculateUnityRotation(float[] azureRotation)
    {
        Matrix4x4 rotationMatrix = new Matrix4x4(
            new Vector4(azureRotation[0], azureRotation[3], azureRotation[6], 1.0f),
            new Vector4(azureRotation[1], azureRotation[4], azureRotation[7], 1.0f),
            new Vector4(azureRotation[2], azureRotation[5], azureRotation[8], 1.0f),
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
