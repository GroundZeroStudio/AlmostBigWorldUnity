using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源异步请求管理
public class ABAssetRequestManager
{
    // 等待中加载请求
    private Queue<ResourceRequestInternal> m_waitingRequest = new Queue<ResourceRequestInternal>();

    // 活动中加载的请求
    private List<ResourceRequestInternal> m_activeRequest = new List<ResourceRequestInternal>();

    // 同时活动的最大请求数
    private static int activeQuestCount = 10;

    public void Update()
    {
        // 填充加载请求
        while (m_waitingRequest.Count > 0 && m_activeRequest.Count < activeQuestCount)
            ProcessOneLoadRequest();

        // 处理已完成加载bundle请求
        for (int i = m_activeRequest.Count - 1; i >= 0; --i)
        {
            if (m_activeRequest[i].loadAssetRequest.isDone)
            {
                ResourceRequestInternal request = m_activeRequest[i];
                UnityEngine.Object[] allAssets = request.loadAssetRequest.allAssets;
                for (int j = 0; j < allAssets.Length; ++j)
                {
                    if (allAssets[j].name == request.assetName)
                        request.outResourceRequest.asset = allAssets[j];
                }
                request.outResourceRequest.isDone = true;
                m_activeRequest.RemoveAt(i);
            }
        }
    }

    // 发起一个加载请求
    private void ProcessOneLoadRequest()
    {
        ResourceRequestInternal request = m_waitingRequest.Dequeue();
        // fbx中的Mesh必须使用LoadAssetAsync，否则无法加载
        if (request.type == typeof(Mesh))
            request.loadAssetRequest = request.assetBundle.LoadAllAssetsAsync();
        else
            request.loadAssetRequest = request.assetBundle.LoadAssetAsync(request.assetName, request.type);
        m_activeRequest.Add(request);
    }

    // 异步加载asset
    // 返回是否需要加载ab中所有资源，若为Mesh类型则需要
    public bool LoadAssetAsync(ResourceRequestInternal request)
    {
        m_waitingRequest.Enqueue(request);
        return request.type == typeof(Mesh);
    }

    #region 调试信息
    public int GetActiveRequestCount()
    {
        return m_activeRequest.Count;
    }
    public int GetWaitingRequestCount()
    {
        return m_waitingRequest.Count;
    }
    #endregion
}
