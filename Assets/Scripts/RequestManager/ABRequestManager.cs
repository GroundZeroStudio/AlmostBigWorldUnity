using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// AB异步请求管理器
public class ABRequestManager
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
            if (m_activeRequest[i].abRequest.isDone)
            {
                ResourceRequestInternal request = m_activeRequest[i];
                request.assetBundle = request.abRequest.assetBundle;
                request.abRequest = null;
                m_activeRequest.RemoveAt(i);
            }
        }
    }

    // 发起一个加载请求
    private void ProcessOneLoadRequest()
    {
        ResourceRequestInternal request = m_waitingRequest.Dequeue();
        string abPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles", request.abPath);
        request.abRequest = AssetBundle.LoadFromFileAsync(abPath);
        m_activeRequest.Add(request);
    }

    // 异步加载bundle
    public void LoadAssetBundleAsync(ResourceRequestInternal request)
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
