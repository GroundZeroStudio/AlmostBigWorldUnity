using System;
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

    // 加载中的ab，共享请求字典
    private Dictionary<string, ResourceRequestShared> m_requestSharedDict = new Dictionary<string, ResourceRequestShared>();

    // 内部请求记录
    private List<ResourceRequestInternal> m_requestInternalList = new List<ResourceRequestInternal>();

    // 已加载ab依赖
    private Dictionary<string, string[]> m_depABDict = new Dictionary<string, string[]>();
    
    // 加载中的ab请求
    private List<ResourceRequestShared> m_loadingABRequests = new List<ResourceRequestShared>();

    // 加载中的asset请求
    private List<ResourceRequestShared> m_loadingAssetRequests = new List<ResourceRequestShared>();

    // 等待ab依赖请求完成列表
    private List<ResourceRequestShared> m_waitDependencyLoadABRequests = new List<ResourceRequestShared>();

    // ab卸载队列
    private ABUnloadQueue m_abUnloadQueue = new ABUnloadQueue();

    // 等待卸载请求列表
    private List<ResourceRequestShared> m_waitUnloadABRequests = new List<ResourceRequestShared>();

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

        // 检测加载ab请求完成
        for (int i = m_loadingABRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestShared request = m_loadingABRequests[i];
            if (request.abRequest != null && request.abRequest.isDone)
            {
                request.assetBundle = request.abRequest.assetBundle;
                request.abRequest = null;

                m_loadingABRequests.RemoveAt(i);

                // ab加载完成后加载所有资源
                m_loadingAssetRequests.Add(request);
                m_assetRequestManager.LoadAllAssetAsync(request);
            }
        }

        // 检测资源加载请求完成
        for (int i = m_loadingAssetRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestShared request = m_loadingAssetRequests[i];
            if (request.loadAssetRequest != null && request.loadAssetRequest.isDone)
            {
                request.isDone = true;
                m_loadingAssetRequests.RemoveAt(i);

                // 完全加载完成，添加到资源管理表中
                ABResourceManager abResourceManager = new ABResourceManager(request.assetBundle, request.loadAssetRequest.allAssets);
                m_requestSharedDict.Remove(request.abPath);
                m_abResManagerDict.Add(request.abPath, abResourceManager);
                m_abResManagers.Add(abResourceManager);

                // 加载过程中被卸载了,进入卸载流程
                if (request.requestInternalList.Count == 0)
                    DoUnload(request.abPath);
                // 获取所有关联内部请求的资源
                else
                {
                    for (int j = 0; j < request.requestInternalList.Count; ++j)
                    {
                        abResourceManager.GetLoadedAsset(request.requestInternalList[j]);
                        m_requestInternalList.Remove(request.requestInternalList[j]);
                    }
                }
            }
        }

        // 检测等待依赖项ab加载完成
        for (int i = m_waitDependencyLoadABRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestShared resourceRequest = m_waitDependencyLoadABRequests[i];
            for (int j = resourceRequest.depsRequests.Count - 1; j >= 0; --j)
            {
                ResourceRequestShared depRequest = resourceRequest.depsRequests[j];
                if (depRequest.isDone)
                {
                    --depRequest.refCount;
                    resourceRequest.loadedDepAB.Add(depRequest.abPath);
                    resourceRequest.depsRequests.RemoveAt(j);
                }
            }
            if (resourceRequest.depsRequests.Count == 0)
            {
                m_waitDependencyLoadABRequests.RemoveAt(i);
                m_abRequestManager.LoadAssetBundleAsync(resourceRequest);
                m_loadingABRequests.Add(resourceRequest);
            }
        }

        // 等待卸载的ab请求列表，在加载完成后卸载
        for (int i = m_waitUnloadABRequests.Count - 1; i >= 0; --i)
        {
            ResourceRequestShared request = m_waitUnloadABRequests[i];
            if (m_abResManagerDict.ContainsKey(request.abPath))
            {
                DoUnload(request.abPath);
                m_waitUnloadABRequests.RemoveAt(i);
            }
        }
    }

    // 异步加载资源
    public ResourceRequset LoadAssetAsync(string abPath, string assetName, Type type)
    {
        if (string.IsNullOrEmpty(abPath))
            return null;
        ++totalAssetCount;
        ResourceRequestInternal request = new ResourceRequestInternal();
        request.assetName = assetName;
        request.type = type;
        request.outResourceRequest = new ResourceRequset();
        ProcessLoadAssetBunlde(request, abPath);
        return request.outResourceRequest;
    }

    // 加载中资源卸载
    public void Unload(string abPath, ResourceRequset request)
    {
        ResourceRequestInternal unloadInternalRequest = null;
        for (int i = 0; i < m_requestInternalList.Count; ++i)
        {
            if (m_requestInternalList[i].outResourceRequest == request)
            {
                unloadInternalRequest = m_requestInternalList[i];
                // 卸载从列表删除
                if (unloadInternalRequest.requestShared.refCount <= 1)
                    m_requestInternalList.RemoveAt(i);
                break;
            }
        }
        if (unloadInternalRequest == null)
        {
            Debug.LogError("error can't find internal request " + abPath);
            return;
        }

        Unload(unloadInternalRequest.requestShared);
    }

    // 引用计数为0时实际卸载，返回true，否则返回false
    private void Unload(ResourceRequestShared unloadSharedRequest)
    {
        --unloadSharedRequest.refCount;

        if (unloadSharedRequest.refCount > 0)
            return;

        // 只会同时存在一个列表中，删除效率优化
        bool removed = Util.RemoveInList(m_loadingABRequests, unloadSharedRequest);
        if (!removed)
            removed = Util.RemoveInList(m_waitDependencyLoadABRequests, unloadSharedRequest);
        if (!removed)
            Util.RemoveInList(m_loadingAssetRequests, unloadSharedRequest);

        m_waitUnloadABRequests.Add(unloadSharedRequest);

        // 卸载依赖项
        for (int i = 0; i < unloadSharedRequest.depsRequests.Count; ++i)
            Unload(unloadSharedRequest.depsRequests[i]);

        for (int i = 0; i < unloadSharedRequest.loadedDepAB.Count; ++i)
            Unload(unloadSharedRequest.loadedDepAB[i]);
    }

    // 已加载资源卸载
    public void Unload(string abPath)
    {
        // 统计
        if (!string.IsNullOrEmpty(abPath))
            --totalAssetCount;

        if (m_abResManagerDict.ContainsKey(abPath))
            DoUnload(abPath);
        // 错误情况
        else
            Debug.Log("unload can't find " + abPath);
    }

    private void DoUnload(string abPath)
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
    
    private void ProcessLoadAssetBunlde(ResourceRequestInternal request, string abPath)
    {
        // ab已加载，直接获取asset
        if (m_abResManagerDict.ContainsKey(abPath))
        {
            ABResourceManager abResourceManager = m_abResManagerDict[abPath];
            ++abResourceManager.refCount;
            if (!abResourceManager.GetLoadedAsset(request))
                Debug.LogErrorFormat("asset not fount {0},{1}", abPath, request.assetName);
        }
        // AB已在加载中，增加共享请求引用计数
        else if (m_requestSharedDict.ContainsKey(abPath))
        {
            m_requestInternalList.Add(request);

            request.requestShared = m_requestSharedDict[abPath];
            request.requestShared.requestInternalList.Add(request);
            ++request.requestShared.refCount;
        }
        // AB未加载，先加载AB，再加载资源
        else
        {
            m_requestInternalList.Add(request);

            ResourceRequestShared requestShared = new ResourceRequestShared();
            requestShared.abPath = abPath;
            requestShared.refCount = 1;
            requestShared.requestInternalList.Add(request);
            request.requestShared = requestShared;
            m_requestSharedDict.Add(abPath, requestShared);

            string[] depABPaths = mainifest.GetAllDependencies(abPath);
            // 记录依赖
            if (!m_depABDict.ContainsKey(abPath))
                m_depABDict.Add(abPath, depABPaths);

            List<string> needLoadDepAB = new List<string>();
            for (int i = 0; i < depABPaths.Length; ++i)
                needLoadDepAB.Add(depABPaths[i]);
            // 若需要首先加载依赖
            if (depABPaths != null && depABPaths.Length > 0)
            {
                m_waitDependencyLoadABRequests.Add(requestShared);
                for (int i = 0; i < depABPaths.Length; ++i)
                {
                    ResourceRequestInternal depRequest = new ResourceRequestInternal();
                    ProcessLoadAssetBunlde(depRequest, depABPaths[i]);
                    if (depRequest.requestShared != null)
                        requestShared.depsRequests.Add(depRequest.requestShared);
                }
            }
            // 直接加载
            else
            {
                m_abRequestManager.LoadAssetBundleAsync(requestShared);
                m_loadingABRequests.Add(requestShared);
            }
        }
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
        return this.m_requestSharedDict.Count;
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
