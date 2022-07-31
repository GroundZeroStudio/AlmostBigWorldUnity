using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABResourceManager
{
    // 资源加载管理器
    private ABAssetRequestManager m_assetRequestManager;

    // AssetBundle实例
    private AssetBundle m_assetBundle;

    // 加载中的Asset
    private HashSet<string> m_loadingAsset = new HashSet<string>();
    
    // 加载中的资源请求
    private List<ResourceRequestInternal> m_loadingAssetRequests = new List<ResourceRequestInternal>();

    // 等待其它请求加载
    private List<ResourceRequestInternal> m_waitOtherLoadingRequests = new List<ResourceRequestInternal>();

    // 已加载的asset
    private Dictionary<string, UnityEngine.Object> m_loadedAsset = new Dictionary<string, UnityEngine.Object>();

    public ABResourceManager(ABAssetRequestManager assetRequestManager, AssetBundle assetBundle)
    {
        m_assetBundle = assetBundle;
        m_assetRequestManager = assetRequestManager;
    }

    public void Update()
    {
        for (int i = m_loadingAssetRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestInternal resourceRequest = m_loadingAssetRequests[i];
            if (resourceRequest.outResourceRequest.isDone)
            {
                UnityEngine.Object[] assets = resourceRequest.loadAssetRequest.allAssets;
                for (int j = 0; j < assets.Length; ++j)
                {
                    m_loadingAsset.Remove(assets[j].name);
                    if (!m_loadedAsset.ContainsKey(assets[j].name))
                        m_loadedAsset.Add(assets[j].name, assets[j]);
                }
                m_loadingAssetRequests.RemoveAt(i);
            }
        }
        for (int i = m_waitOtherLoadingRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestInternal resourceRequest = m_waitOtherLoadingRequests[i];
            if (m_loadedAsset.ContainsKey(resourceRequest.assetName))
            {
                resourceRequest.outResourceRequest.isDone = true;
                resourceRequest.outResourceRequest.asset = m_loadedAsset[resourceRequest.assetName];
                m_waitOtherLoadingRequests.RemoveAt(i);
            }
        }
    }

    public void LoadAssetAsync(ResourceRequestInternal resourceRequest)
    {
        // assetName为空，不需要加载，如依赖AB
        if (string.IsNullOrEmpty(resourceRequest.assetName))
        {
            resourceRequest.outResourceRequest.isDone = true;
        }
        // 已加载
        else if (m_loadedAsset.ContainsKey(resourceRequest.assetName))
        {
            resourceRequest.outResourceRequest.asset = m_loadedAsset[resourceRequest.assetName];
            resourceRequest.outResourceRequest.isDone = true;
        }
        // 加载中，等待其它请求加载完成
        else if (m_loadingAsset.Contains(resourceRequest.assetName))
        {
            m_waitOtherLoadingRequests.Add(resourceRequest);
        }
        // 未加载
        else
        {
            resourceRequest.assetBundle = m_assetBundle;
            bool isLoadAllAsset = m_assetRequestManager.LoadAssetAsync(resourceRequest);
            if (isLoadAllAsset)
            {
                string[] assetNames = m_assetBundle.GetAllAssetNames();
                for (int i = 0; i < assetNames.Length; ++i)
                    m_loadingAsset.Add(assetNames[i]);
            }
            else
                m_loadingAsset.Add(resourceRequest.assetName);
            m_loadingAssetRequests.Add(resourceRequest);
        }
    }
}
