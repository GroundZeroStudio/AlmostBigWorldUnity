using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 资源管理器
// todo：
// 增加引用计数机制
// 增加卸载机制
public class ResourceManager : SingletonClass<ResourceManager>
{
    // 资源请求管理器
    private ABAssetRequestManager m_assetRequestManager = new ABAssetRequestManager();

    // ab请求管理器
    private ABRequestManager m_abRequestManager = new ABRequestManager();

    // 已加载的ab
    private Dictionary<string, ABResourceManager> m_abResManagerDict = new Dictionary<string, ABResourceManager>();
    private List<ABResourceManager> m_abResManagers = new List<ABResourceManager>();

    // 已加载ab依赖
    private Dictionary<string, string[]> m_depABDict = new Dictionary<string, string[]>();

    // 加载中的ab
    private HashSet<string> m_loadingAB = new HashSet<string>();

    // 加载中的ab请求
    private List<ResourceRequestInternal> m_loadingABRequests = new List<ResourceRequestInternal>();

    // 等待其它ab请求完成列表
    private List<ResourceRequestInternal> m_waitOtherLoadABRequests = new List<ResourceRequestInternal>();

    // 等待其它ab请求完成列表
    private List<ResourceRequestInternal> m_waitDependencyLoadABRequests = new List<ResourceRequestInternal>();

    // ab卸载队列
    private ABUnloadQueue m_abUnloadQueue = new ABUnloadQueue();

    // ab依赖相关
    private AssetBundle mainAssetBundle;
    private AssetBundleManifest mainifest;

    public ResourceManager()
    {
        string mainAB = Path.Combine(Application.streamingAssetsPath, "AssetBundles/AssetBundles");
        mainAssetBundle = AssetBundle.LoadFromFile(mainAB);
        mainifest = mainAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
    }


    public void Update()
    {
        m_assetRequestManager.Update();
        m_abRequestManager.Update();
        m_abUnloadQueue.Update();

        // 已加载ab资源管理器更新
        for (int i = 0; i < m_abResManagers.Count; ++i)
            m_abResManagers[i].Update();

        // 检测加载ab请求完成
        for (int i  = m_loadingABRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestInternal resourceRequest = m_loadingABRequests[i];
            if (resourceRequest.assetBundle != null)
            {
                m_loadingABRequests.RemoveAt(i);
                ABResourceManager abResourceManager = new ABResourceManager(m_assetRequestManager, resourceRequest.assetBundle);
                m_loadingAB.Remove(resourceRequest.abPath);
                m_abResManagerDict.Add(resourceRequest.abPath, abResourceManager);
                m_abResManagers.Add(abResourceManager);
                ProcessLoadAsset(resourceRequest);
            }
        }

        // 检测等待其它ab请求加载完成的请求
        for (int i = m_waitOtherLoadABRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestInternal request = m_waitOtherLoadABRequests[i];
            if (m_abResManagerDict.ContainsKey(request.abPath))
            {
                m_abResManagerDict[request.abPath].LoadAssetAsync(request);
                m_waitOtherLoadABRequests.RemoveAt(i);
            }
        }

        // 检测等待依赖项ab加载完成
        for (int i = m_waitDependencyLoadABRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestInternal resourceRequest = m_waitDependencyLoadABRequests[i];
            for (int j = resourceRequest.depsRequests.Count - 1; j >= 0; --j)
            {
                if (resourceRequest.depsRequests[j].outResourceRequest.isDone)
                    resourceRequest.depsRequests.RemoveAt(j);
            }
            if (resourceRequest.depsRequests.Count == 0)
            {
                m_waitDependencyLoadABRequests.RemoveAt(i);
                m_abRequestManager.LoadAssetBundleAsync(resourceRequest);
                m_loadingABRequests.Add(resourceRequest);
            }
        }
    }

    // 异步加载资源
    public ResourceRequset LoadAssetAsync(string abPath, string assetName, Type type)
    {
        if (string.IsNullOrEmpty(abPath))
            return null;
        ++totalAssetCount;
        ResourceRequestInternal resourceRequest = new ResourceRequestInternal();
        resourceRequest.abPath = abPath;
        resourceRequest.assetName = assetName;
        resourceRequest.type = type;
        resourceRequest.outResourceRequest = new ResourceRequset();
        ProcessLoadAssetBunlde(resourceRequest);
        return resourceRequest.outResourceRequest;
    }

    public void UnloadWithStatistics(string abPath)
    {
        if (!string.IsNullOrEmpty(abPath))
            --totalAssetCount;
        Unload(abPath);
    }

    public void Unload(string abPath)
    {
        if (string.IsNullOrEmpty(abPath))
            return;
        if (m_abResManagerDict.ContainsKey(abPath))
        {
            ABResourceManager abResManager = m_abResManagerDict[abPath];
            --abResManager.refCount;
            if (abResManager.refCount == 0)
            {
                m_abUnloadQueue.Add(abPath, abResManager);
                m_abResManagerDict.Remove(abPath);
                if (m_depABDict.ContainsKey(abPath))
                {
                    string[] depAB = m_depABDict[abPath];
                    for (int i = 0; i < depAB.Length; ++i)
                        Unload(depAB[i]);
                }
            }
            else if (abResManager.refCount < 0)
                Debug.Log("ref count < 0, " + abPath);
        }
        else
            Debug.Log("unload can't find " + abPath);
    }
    
    private void ProcessLoadAssetBunlde(ResourceRequestInternal resourceRequest)
    {
        // 当前已包含资源，直接加载asset
        if (m_abResManagerDict.ContainsKey(resourceRequest.abPath))
        {
            ++m_abResManagerDict[resourceRequest.abPath].refCount;
            ProcessLoadAsset(resourceRequest);
        }
        // AB已在加载中，加入等待其它请求中的ab完成
        else if (m_loadingAB.Contains(resourceRequest.abPath))
        {
            m_waitOtherLoadABRequests.Add(resourceRequest);
        }
        // AB未加载，先加载AB，再加载资源
        else
        {
            m_loadingAB.Add(resourceRequest.abPath);

            // 检测依赖项
            string[] depABPaths = mainifest.GetAllDependencies(resourceRequest.abPath);
            if (!m_depABDict.ContainsKey(resourceRequest.abPath))
                m_depABDict.Add(resourceRequest.abPath, depABPaths);
            List<ResourceRequestInternal> depRequests = null;
            for (int i = 0; i < depABPaths.Length; ++i)
            {
                if (!m_abResManagerDict.ContainsKey(depABPaths[i]))
                {
                    if (depRequests == null)
                        depRequests = new List<ResourceRequestInternal>();
                    ResourceRequestInternal depRequest = new ResourceRequestInternal();
                    depRequest.outResourceRequest = new ResourceRequset();
                    depRequest.abPath = depABPaths[i];
                    depRequests.Add(depRequest);
                }
            }
            // 若需要首先加载依赖
            if (depRequests != null)
            {
                resourceRequest.depsRequests = depRequests;
                m_waitDependencyLoadABRequests.Add(resourceRequest);
                for (int i = 0; i < depRequests.Count; ++i)
                    ProcessLoadAssetBunlde(depRequests[i]);
            }
            // 直接加载
            else
            {
                m_abRequestManager.LoadAssetBundleAsync(resourceRequest);
                m_loadingABRequests.Add(resourceRequest);
            }
        }
    }

    private void ProcessLoadAsset(ResourceRequestInternal resourceRequest)
    {
        m_abResManagerDict[resourceRequest.abPath].LoadAssetAsync(resourceRequest);
    }

    #region 调试信息
    public int totalAssetCount;

    public int GetLoadingRequestCount()
    {
        return m_loadingABRequests.Count;
    }
    public int GetABWaitDepsCount()
    {
        return m_waitDependencyLoadABRequests.Count;
    }
    public int GetABWaitOtherABRequestCount()
    {
        return m_waitOtherLoadABRequests.Count;
    }
    public int GetABActiveRequestCount()
    {
        return m_abRequestManager.GetActiveRequestCount();
    }
    public int GetABWaitingRequestCount()
    {
        return m_abRequestManager.GetWaitingRequestCount();
    }
    public int GetAssetActiveLoadRequestCount()
    {
        return m_assetRequestManager.GetActiveRequestCount();
    }
    public int GetAssetWaitingLoadRequestCount()
    {
        return m_assetRequestManager.GetWaitingRequestCount();
    }
    public Dictionary<string, ABResourceManager> GetABResManagerDict()
    {
        return m_abResManagerDict;
    }
    public int GetWaitUnloadABCount()
    {
        return m_abUnloadQueue.GetWaitUnloadABCount();
    }
    #endregion
}
