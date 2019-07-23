using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    public Texture2D RGBTexture { get; private set; }
    public Texture2D IRTexture { get; private set; }
    public Texture2D DepthTexture { get; private set; }

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

    private bool initialized = false;
    private bool streaming = false;
    private uint deviceIndex = 0;

    private AzureKinectUnityAPI() { }

    public void Start()
    {
        Initialize();
        if (initialized)
        {
            uint deviceCount = GetDeviceCountNative();
            DebugLog($"Devices Found: {deviceCount}");

            if (TryStartStreamsNative(deviceIndex))
            {
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
        if (streaming)
        {
            if (!TryUpdateNative())
            {
                DebugLog("Failed to update AzureKinect.Unity plugin.");
            }
            else if (RGBTexture == null ||
                IRTexture == null ||
                DepthTexture == null)
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

                if (succeeded && RGBTexture == null)
                {
                    RGBTexture = Texture2D.CreateExternalTexture((int) rgbWidth, (int) rgbHeight, TextureFormat.RGBA32, false, false, rgbSrv);
                }

                if (succeeded && IRTexture == null)
                {
                    IRTexture = Texture2D.CreateExternalTexture((int) irWidth, (int) irHeight, TextureFormat.R16, false, false, irSrv);
                }

                if (succeeded && DepthTexture == null)
                {
                    RGBTexture = Texture2D.CreateExternalTexture((int) depthWidth, (int) depthHeight, TextureFormat.R16, false, false, depthSrv);
                }
            }
        }
    }

    public void Stop()
    {
        if (streaming)
        {
            StopStreamingNative(deviceIndex);
            streaming = false;
        }
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
