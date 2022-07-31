using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshReference : MonoBehaviour
{
    [System.Serializable]
    public class MeshRefInfo
    {
        public string abPath;
        public string assetName;
    }

    [System.Serializable]
    public class MatRefInfo
    {
        public string abPath;
        public string assetName;
        public MatTexInfo[] textures;
    }

    [System.Serializable]
    public class MatTexInfo
    {
        public string texName;
        public string abPath;
        public string assetName;
    }

    public MeshRefInfo mesh;
    public MatRefInfo[] materials;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    private bool _isLoading;
    private ResourceRequset meshLoadRequest;
    private List<ResourceRequset> materialRequests;

    public void LoadAssetAsync()
    {
        _isLoading = true;
        meshLoadRequest = ResourceManager.Instance.LoadAssetAsync(mesh.abPath, mesh.assetName, typeof(Mesh));
        materialRequests = new List<ResourceRequset>();
        for (int i = 0; i < materials.Length; ++i)
        {
            ResourceRequset req = ResourceManager.Instance.LoadAssetAsync(materials[i].abPath, materials[i].assetName, typeof(Material));
            materialRequests.Add(req);
        }
    }

    private void Update()
    {
        if (!_isLoading)
            return;
        if (meshLoadRequest != null && meshFilter != null && meshLoadRequest.isDone)
        {
            meshFilter.sharedMesh = meshLoadRequest.asset as Mesh;
            meshLoadRequest = null;
        }

        if (materialRequests != null && meshRenderer != null)
        {
            bool allMatDone = true;
            for (int i = 0; i < materialRequests.Count; ++i)
            {
                if (!materialRequests[i].isDone)
                {
                    allMatDone = false;
                    break;
                }
            }
            if (allMatDone)
            {
                Material[] materials = new Material[materialRequests.Count];
                for (int i = 0; i < materials.Length; ++i)
                    materials[i] = materialRequests[i].asset as Material;
                meshRenderer.sharedMaterials = materials;
                materialRequests = null;
            }
        }

        if (meshLoadRequest == null && materialRequests == null)
            _isLoading = false;
    }
}
