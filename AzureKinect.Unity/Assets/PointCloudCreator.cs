using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;

public class PointCloudCreator : MonoBehaviour
{
    [MenuItem("Azure Kinect/Create Point Cloud Game Object", priority = 100)]
    public static void CreateDepthPlane()
    {
        GameObject pointCloud = new GameObject();
        pointCloud.name = "Point Cloud";
        var renderer = pointCloud.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("PointCloud"));
        var meshFilter = pointCloud.AddComponent<MeshFilter>();
        pointCloud.AddComponent<PointCloudHelper>();

        var mesh = new Mesh();
        float scale = 1.0f;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        float width = 512 / 4.0f;
        float height = 512 / 4.0f;

        for (int i = 0; i < (int) height; i+=1)
        {
            for (int j = 0; j < (int) width; j+=1)
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

                uvs.Add(new Vector2(j / width, i / height));
                uvs.Add(new Vector2((j + 1) / width, i / height));
                uvs.Add(new Vector2((j + 1) / width, (i + 1) / height));
                uvs.Add(new Vector2(j / width, (i + 1) / height));

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
