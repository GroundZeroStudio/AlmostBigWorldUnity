using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABResourceManager
{
    // ab引用计数
    public int refCount;

    // 资源加载管理器
    private ABAssetRequestManager m_assetRequestManager;

    // AssetBundle实例
    private AssetBundle m_assetBundle;

    // 加载状态
    private LoadState m_loadState;
    
    // 加载资源请求
    public ResourceRequestInternal loadAssetRequest;
    
    // 等待的请求加载
    private List<ResourceRequestInternal> m_waitLoadingRequests = new List<ResourceRequestInternal>();

    // 已加载的asset
    private Dictionary<string, UnityEngine.Object> m_assetsDict = new Dictionary<string, UnityEngine.Object>();
    private UnityEngine.Object[] m_assets;

    public ABResourceManager(ABAssetRequestManager assetRequestManager, AssetBundle assetBundle)
    {
        m_assetBundle = assetBundle;
        m_assetRequestManager = assetRequestManager;
    }

    public void Update()
    {
        if (m_loadState == LoadState.Loading)
        {
            if (loadAssetRequest.loadAssetRequest != null && loadAssetRequest.loadAssetRequest.isDone)
            {
                m_assets = loadAssetRequest.loadAssetRequest.allAssets;
                for (int j = 0; j < m_assets.Length; ++j)
                {
                    var asset = m_assets[j];
                    // Mesh格式的文件优先存储在dict中
                    if (m_assetsDict.ContainsKey(asset.name) && asset is Mesh)
                        m_assetsDict[asset.name] = asset;
                    else
                        m_assetsDict.Add(asset.name, asset);
                }
                m_loadState = LoadState.Loaded;
                loadAssetRequest = null;
            }
        }
        if (m_loadState == LoadState.Loaded && m_waitLoadingRequests.Count > 0)
        {
            for (int i = 0; i < m_waitLoadingRequests.Count; ++i)
            {
                ResourceRequestInternal resourceRequest = m_waitLoadingRequests[i];
                if (m_assetsDict.ContainsKey(resourceRequest.assetName))
                {
                    resourceRequest.outResourceRequest.isDone = true;
                    resourceRequest.outResourceRequest.asset = m_assetsDict[resourceRequest.assetName];
                }
                else
                    Debug.LogFormat("asset not fount {0},{1}", resourceRequest.abPath, resourceRequest.assetName);
            }
            m_waitLoadingRequests.Clear();
        }
    }

    public void LoadAssetAsync(ResourceRequestInternal resourceRequest)
    {
        ++refCount;
        // assetName为空，不需要加载，如依赖AB
        if (string.IsNullOrEmpty(resourceRequest.assetName))
        {
            resourceRequest.outResourceRequest.isDone = true;
            return;
        }

        switch (m_loadState)
        {
            case LoadState.NotLoad:
                resourceRequest.assetBundle = m_assetBundle;
                m_assetRequestManager.LoadAllAssetAsync(resourceRequest);
                loadAssetRequest = resourceRequest;
                m_waitLoadingRequests.Add(resourceRequest);
                m_loadState = LoadState.Loading;
                break;
            case LoadState.Loading:
                m_waitLoadingRequests.Add(resourceRequest);
                break;
            case LoadState.Loaded:
                if (m_assetsDict.ContainsKey(resourceRequest.assetName))
                    resourceRequest.outResourceRequest.asset = m_assetsDict[resourceRequest.assetName];
                else
                    Debug.LogFormat("asset not fount {0},{1}", resourceRequest.abPath, resourceRequest.assetName);
                resourceRequest.outResourceRequest.isDone = true;
                break;
        }
    }

    public void UnloadAssetBundle()
    {
        m_assetBundle.Unload(true);
    }
}
