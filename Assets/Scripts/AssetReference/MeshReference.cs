using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格引用
/// </summary>
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

    public Bounds bounds;
    public bool isVisible;

    private LoadState m_loadState;
    private ResourceRequset meshLoadRequest;
    private ResourceRequset[] materialRequests;

    private void Start()
    {
        SceneVisible.Instance.Register(this);
    }

    public void LoadAssetAsync()
    {
        if (m_loadState != LoadState.NotLoad)
            return;
        m_loadState = LoadState.Loading;
        if (meshFilter != null)
            meshLoadRequest = ResourceManager.Instance.LoadAssetAsync(mesh.abPath.ToLower(), mesh.assetName, typeof(Mesh));

        if (meshRenderer != null)
        {
            materialRequests = new ResourceRequset[materials.Length];
            for (int i = 0; i < materials.Length; ++i)
            {
                ResourceRequset req = ResourceManager.Instance.LoadAssetAsync(materials[i].abPath.ToLower(), materials[i].assetName, typeof(Material));
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

        // 网格已加载
        if (meshLoadRequest == null)
            ResourceManager.Instance.Unload(mesh.abPath.ToLower());
        // 加载中
        else
        {
            ResourceManager.Instance.Unload(mesh.abPath.ToLower(), meshLoadRequest);
            meshLoadRequest = null;
        }

        // 材质已完全加载
        if (materialRequests == null)
        {
            for (int i = 0; i < materials.Length; ++i)
                ResourceManager.Instance.Unload(materials[i].abPath.ToLower());
        }
        // 材质全部或部分加载中
        else
        {
            for (int i = 0; i < materialRequests.Length; ++i)
            {
                // 已加载
                if (materialRequests[i].isDone)
                    ResourceManager.Instance.Unload(mesh.abPath.ToLower());
                // 加载中
                else
                    ResourceManager.Instance.Unload(mesh.abPath.ToLower(), materialRequests[i]);
            }
            materialRequests = null;
        }
    }

    public void OnUpdate()
    {
        //if (m_loadState != LoadState.Loading)
        //    return;
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

        if (m_loadState == LoadState.Loading && meshLoadRequest == null && (materialRequests == null || materialRequests.Length == 0))
            m_loadState = LoadState.Loaded;
    }
}
