using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class PointCloudTransformSaver : MonoBehaviour
{
    [Serializable]
    private class Details
    {
        public List<PointCloudDetails> details;

        public Details(List<PointCloudDetails> details)
        {
            this.details = details;
        }
    }

    [Serializable]
    private class PointCloudDetails
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public PointCloudDetails(
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            this.name = name;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    [SerializeField]
    private string fileName = "PointCloudDetails.json";

    private PointCloudHelper[] pointClouds;

    void Start()
    {
        pointClouds = FindObjectsOfType<PointCloudHelper>();
        var dictionary = LoadPointCloudDetails();
        foreach (var pointCloud in pointClouds)
        {
            if (dictionary.TryGetValue(pointCloud.gameObject.name, out var details))
            {
                pointCloud.gameObject.transform.position = details.position;
                pointCloud.gameObject.transform.rotation = details.rotation;
                pointCloud.gameObject.transform.localScale = details.scale;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            List<PointCloudDetails> details = new List<PointCloudDetails>();
            foreach (var pointCloud in pointClouds)
            {
                var detail = new PointCloudDetails(
                    pointCloud.gameObject.name,
                    pointCloud.transform.position,
                    pointCloud.transform.rotation,
                    pointCloud.transform.localScale);
                details.Add(detail);
            }
            SavePointCloudDetails(details);
        }
    }

    private Dictionary<string, PointCloudDetails> LoadPointCloudDetails()
    {
        var dictionary = new Dictionary<string, PointCloudDetails>();
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            byte[] payload = File.ReadAllBytes(path);
            string json = Encoding.UTF8.GetString(payload);
            Details details = JsonUtility.FromJson<Details>(json);
            foreach (var detail in details.details)
            {
                dictionary.Add(detail.name, detail);
            }
            Debug.Log($"Loaded point cloud details for {details.details.Count} point clouds.");
        }
        catch (Exception e) { }

        return dictionary;
    }

    private void SavePointCloudDetails(List<PointCloudDetails> details)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        string json = JsonUtility.ToJson(new Details(details));
        byte[] payload = Encoding.UTF8.GetBytes(json);
        File.WriteAllBytes(path, payload);
        Debug.Log($"Saved point cloud details for {details.Count} point clouds.");
    }
}
