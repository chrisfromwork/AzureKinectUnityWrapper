using System;
using System.IO;
using UnityEngine;

public class PointCloudCapture : MonoBehaviour
{
    [SerializeField]
    public KeyCode captureKeyCode = KeyCode.Space;

    [SerializeField]
    public AzureKinectHelper azureKinectHelper;

    void Update()
    {
        if (azureKinectHelper != null &&
            Input.GetKeyDown(captureKeyCode))
        {
            var rgbTexture = azureKinectHelper.GetRGBTexture();
            if (azureKinectHelper.TryGetImageBuffers(out var colorImageBuffer, out var depthImageBuffer, out var pointCloudImageBuffer))
            {
                string capturePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PointCloudCaptures");
                if (!Directory.Exists(capturePath))
                {
                    Directory.CreateDirectory(capturePath);
                }

                string filePath = Path.Combine(capturePath, $"PointCloudCapture.{DateTime.Now.ToString("yyyy.MM.dd_hh.mm.ss")}.bin");
                byte[] data = SerializeBuffers(rgbTexture.width, rgbTexture.height, colorImageBuffer, depthImageBuffer, pointCloudImageBuffer);
                File.WriteAllBytes(filePath, data);
            }
            else
            {
                Debug.LogError("Unable to save image buffers");
            }
        }
    }

    public static byte[] SerializeBuffers(int width, int height, byte[] colorImageBuffer, byte[] depthImageBuffer, byte[] pointCloudImageBuffer)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(width);
            writer.Write(height);
            writer.Write(colorImageBuffer);
            writer.Write(depthImageBuffer);
            writer.Write(pointCloudImageBuffer);
            writer.Flush();
            return stream.ToArray();
        }
    }

    public static bool TryDeserializeBuffers(byte[] data, out Texture2D rgbTexture, out Texture2D depthTexture, out Texture2D pointCloudTexture)
    {
        rgbTexture = null;
        depthTexture = null;
        pointCloudTexture = null;

        try
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();

                // BGRA32
                int rgbSize = width * height * 4 * sizeof(byte);

                // R16
                int depthSize = width * height * sizeof(short);

                // RGBAFLOAT
                int pointCloudSize = width * height * 4 * sizeof(float);

                var rgbData = reader.ReadBytes(rgbSize);
                var depthData = reader.ReadBytes(depthSize);
                var pointCloudData = reader.ReadBytes(pointCloudSize);

                rgbTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
                rgbTexture.LoadRawTextureData(rgbData);
                rgbTexture.Apply();

                depthTexture = new Texture2D(width, height, TextureFormat.R16, false);
                depthTexture.LoadRawTextureData(depthData);
                depthTexture.Apply();

                pointCloudTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                pointCloudTexture.LoadRawTextureData(pointCloudData);
                pointCloudTexture.Apply();
            }

            return true;
        } catch(Exception e){}

        return false;
    }
}
