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
    private static int activeQuestCount = 20;

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
        request.loadAssetRequest = request.assetBundle.LoadAllAssetsAsync();
        m_activeRequest.Add(request);
    }

    // 异步加载asset
    public void LoadAllAssetAsync(ResourceRequestInternal request)
    {
        m_waitingRequest.Enqueue(request);
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
