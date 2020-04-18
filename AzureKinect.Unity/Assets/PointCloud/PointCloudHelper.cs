using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PointCloudHelper : MonoBehaviour
{
    [SerializeField]
    bool loadFromFile = false;

    [SerializeField]
    private uint deviceIndex = 0;

    [SerializeField]
    private string fileName;

    [SerializeField]
    [Range(0.00001f, 0.003f)]
    private float cubeScale = 0.005f;

    [SerializeField]
    [Range(0.01f, 10)]
    private float minDepth = 0.001f;

    [SerializeField]
    [Range(0.01f, 10)]
    private float maxDepth = 2;

    private Material material = null;
    private bool deviceInitialized = false;
    private bool fileLoaded = false;

    private void Update()
    {
        if (loadFromFile)
        {
            UpdateFileCloud();
        }
        else
        {
            UpdateDeviceCloud();
        }
    }

    private void UpdateFileCloud()
    {
        if (!fileLoaded &&
            !string.IsNullOrEmpty(fileName))
        {
            if (File.Exists(fileName))
            {
                var data = File.ReadAllBytes(fileName);
                if (PointCloudCapture.TryDeserializeBuffers(
                    data,
                    out var rgbTexture,
                    out var depthTexture,
                    out var pointCloudTexture))
                {

                    CreatePointCloud(
                        rgbTexture.width,
                        rgbTexture.height);

                    if (material == null)
                    {
                        material = GetComponent<MeshRenderer>().material;
                    }

                    if (material != null)
                    {
                        material.SetTexture("_MainTex", rgbTexture);
                        material.SetTexture("_DepthTex", depthTexture);
                        material.SetTexture("_PointCloudTemplateTex", pointCloudTexture);

                        fileLoaded = true;
                    }
                }
                else
                {
                    Debug.LogError("Failed to deserialize point cloud file.");
                }
            }
        }

        if (material != null)
        {
            material.SetFloat("_CubeScale", cubeScale);
            material.SetFloat("_MinDepth", minDepth);
            material.SetFloat("_MaxDepth", maxDepth);
            material.SetMatrix("_PointTransform", Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale));
        }
    }

    private void UpdateDeviceCloud()
    {
        if (!deviceInitialized)
        {
            if (AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture == null)
            {
                return;
            }

            CreatePointCloud(
                AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture.width,
                AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture.height);
            deviceInitialized = true;
        }

        if (material == null)
        {
            material = GetComponent<MeshRenderer>().material;
        }

        if (material != null)
        {
            material.SetFloat("_CubeScale", cubeScale);
            material.SetFloat("_MinDepth", minDepth);
            material.SetFloat("_MaxDepth", maxDepth);
            material.SetMatrix("_PointTransform", Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale));

            if (AzureKinectUnityAPI.Instance(deviceIndex).RGBTexture != null)
            {
                material.SetTexture("_MainTex", AzureKinectUnityAPI.Instance(deviceIndex).RGBTexture);
            }

            if (AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture != null)
            {
                material.SetTexture("_DepthTex", AzureKinectUnityAPI.Instance(deviceIndex).DepthTexture);
            }

            if (AzureKinectUnityAPI.Instance(deviceIndex).PointCloudTemplateTexture != null)
            {
                material.SetTexture("_PointCloudTemplateTex", AzureKinectUnityAPI.Instance(deviceIndex).PointCloudTemplateTexture);
            }
        }
    }

    private void CreatePointCloud(int origWidth, int origHeight)
    {
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("PointCloud"));
        var meshFilter = gameObject.AddComponent<MeshFilter>();

        var mesh = new Mesh();
        float scale = 1.0f;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        float width = origWidth / 4.0f;
        float height = origHeight / 4.0f;

        for (int i = 0; i < (int)height; i++)
        {
            for (int j = 0; j < (int)width; j++)
            {
                int vertexNumOffset = vertices.Count;

                Vector3 vertex1 = new Vector3(j * scale, 0, i * scale);
                Vector3 vertex2 = new Vector3((j + 1) * scale, 0, i * scale);
                Vector3 vertex3 = new Vector3((j + 1) * scale, 0, (i + 1) * scale);
                Vector3 vertex4 = new Vector3(j * scale, 0, (i + 1) * scale);

                vertices.Add(vertex1);
                vertices.Add(vertex2);
                vertices.Add(vertex3);
                vertices.Add(vertex4);

                uvs.Add(new Vector2(j / width, i / height));
                uvs.Add(new Vector2((j + 1) / width, i / height));
                uvs.Add(new Vector2((j + 1) / width, (i + 1) / height));
                uvs.Add(new Vector2(j / width, (i + 1) / height));

                triangles.Add(vertexNumOffset);
                triangles.Add(vertexNumOffset + 1);
                triangles.Add(vertexNumOffset + 2);
                triangles.Add(vertexNumOffset);
                triangles.Add(vertexNumOffset + 2);
                triangles.Add(vertexNumOffset + 3);
                triangles.Add(vertexNumOffset);
                triangles.Add(vertexNumOffset + 2);
                triangles.Add(vertexNumOffset + 1);
                triangles.Add(vertexNumOffset);
                triangles.Add(vertexNumOffset + 3);
                triangles.Add(vertexNumOffset + 2);

                normals.Add(Vector3.down);
                normals.Add(Vector3.down);
                normals.Add(Vector3.down);
                normals.Add(Vector3.down);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        meshFilter.mesh = mesh;
    }
}
