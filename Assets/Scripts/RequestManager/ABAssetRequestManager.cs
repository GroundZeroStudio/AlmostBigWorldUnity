using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源异步请求管理
public class ABAssetRequestManager
{
    // 等待中加载请求
    private Queue<ResourceRequestShared> m_waitingRequest = new Queue<ResourceRequestShared>();

    // 活动中加载的请求
    private List<ResourceRequestShared> m_activeRequest = new List<ResourceRequestShared>();

    // 同时活动的最大请求数
    private static int activeQuestCount = 5;

    public void Update()
    {
        // 填充加载请求
        while (m_waitingRequest.Count > 0 && m_activeRequest.Count < activeQuestCount)
            ProcessOneLoadRequest();

        // 处理已完成加载asset请求
        for (int i = m_activeRequest.Count - 1; i >= 0; --i)
        {
            if (m_activeRequest[i].loadAssetRequest.isDone)
                m_activeRequest.RemoveAt(i);
        }
    }

    // 发起一个加载请求
    private void ProcessOneLoadRequest()
    {
        ResourceRequestShared request = m_waitingRequest.Dequeue();
        request.loadAssetRequest = request.assetBundle.LoadAllAssetsAsync();
        m_activeRequest.Add(request);
    }

    // 异步加载asset
    public void LoadAllAssetAsync(ResourceRequestShared request)
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
