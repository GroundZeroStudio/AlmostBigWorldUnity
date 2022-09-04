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

    private LoadState m_loadState;
    private ResourceRequset meshLoadRequest;
    private ResourceRequset[] materialRequests;

    public void LoadAssetAsync()
    {
        if (m_loadState != LoadState.NotLoad)
            return;
        m_loadState = LoadState.Loading;
        if (meshFilter != null)
            meshLoadRequest = ResourceManager.Instance.LoadAssetAsync(mesh.abPath, mesh.assetName, typeof(Mesh));

        if (meshRenderer != null)
        {
            materialRequests = new ResourceRequset[materials.Length];
            for (int i = 0; i < materials.Length; ++i)
            {
                ResourceRequset req = ResourceManager.Instance.LoadAssetAsync(materials[i].abPath, materials[i].assetName, typeof(Material));
                materialRequests[i] = req;
            }
        }
    }

    public void Unload()
    {
        if (m_loadState == LoadState.NotLoad)
            return;
        m_loadState = LoadState.NotLoad;
        if (meshFilter != null)
            meshFilter.sharedMesh = null;
        if (meshRenderer != null)
            meshRenderer.materials = new Material[0];
        ResourceManager.Instance.UnloadWithStatistics(mesh.abPath);
        for (int i = 0; i < materials.Length; ++i)
            ResourceManager.Instance.UnloadWithStatistics(materials[i].abPath);
    }

    private void Update()
    {
        if (m_loadState != LoadState.Loading)
            return;
        if (meshLoadRequest != null && meshLoadRequest.isDone)
        {
            meshFilter.sharedMesh = meshLoadRequest.asset as Mesh;
            meshLoadRequest = null;
        }

        if (materialRequests != null)
        {
            bool allMatDone = true;
            for (int i = 0; i < materialRequests.Length; ++i)
            {
                if (!materialRequests[i].isDone)
                {
                    allMatDone = false;
                    break;
                }
            }
            if (allMatDone)
            {
                Material[] materials = new Material[materialRequests.Length];
                for (int i = 0; i < materials.Length; ++i)
                    materials[i] = materialRequests[i].asset as Material;
                meshRenderer.sharedMaterials = materials;
                materialRequests = null;
            }
        }

        if (meshLoadRequest == null && (materialRequests == null || materialRequests.Length == 0))
            m_loadState = LoadState.Loaded;
    }
}
