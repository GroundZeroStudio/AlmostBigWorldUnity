using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABResourceManager
{
    // ab引用计数
    public int refCount;
    
    // AssetBundle实例
    private AssetBundle m_assetBundle;
    
    // 已加载的asset
    private Dictionary<string, UnityEngine.Object> m_assetsDict = new Dictionary<string, UnityEngine.Object>();
    private UnityEngine.Object[] m_assets;

    public ABResourceManager(AssetBundle assetBundle, UnityEngine.Object[] assets)
    {
        m_assetBundle = assetBundle;
        m_assets = assets;
        for (int i = 0; i < m_assets.Length; ++i)
        {
            var asset = m_assets[i];
            // Mesh格式的文件优先存储在dict中
            if (m_assetsDict.ContainsKey(asset.name) && asset is Mesh)
                m_assetsDict[asset.name] = asset;
            else
                m_assetsDict.Add(asset.name, asset);
        }
    }
    
    // 获取已加载的asset资源
    // 返回是否获取成功，以打印错误日志
    public bool GetLoadedAsset(ResourceRequestInternal resourceRequest)
    {
        ++refCount;

        // 依赖AB无外部请求，仅增加引用计数
        if (resourceRequest.outResourceRequest == null)
            return true;

        resourceRequest.outResourceRequest.isDone = true;
        if (m_assetsDict.ContainsKey(resourceRequest.assetName))
        {
            resourceRequest.outResourceRequest.asset = m_assetsDict[resourceRequest.assetName];
            return true;
        }
        else
            return false;
    }

    public void UnloadAssetBundle()
    {
        m_assetBundle.Unload(true);
    }
}
