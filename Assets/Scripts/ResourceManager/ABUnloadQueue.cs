using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABUnloadQueue
{
    private const int UnloadABCountPerFrame = 1;

    private Dictionary<string, ABResourceManager> m_unloadDict = new Dictionary<string, ABResourceManager>();

    public void Add(string abPath, ABResourceManager abResManager)
    {
        if (m_unloadDict.ContainsKey(abPath))
        {
            Debug.LogError("unexpected error in unload " + abPath);
            return;
        }
        m_unloadDict.Add(abPath, abResManager);
    }

    public ABResourceManager GetIfInQueue(string abPath)
    {
        if (m_unloadDict.ContainsKey(abPath))
            return m_unloadDict[abPath];
        else
            return null;
    }

    public void Update()
    {
        if (m_unloadDict.Count > 0)
        {
            for (int i = 0; i < UnloadABCountPerFrame; ++i)
                UnloadOneAB();
        }
    }

    private void UnloadOneAB()
    {
        string removeKey = null;
        foreach (var pair in m_unloadDict)
        {
            removeKey = pair.Key;
            pair.Value.UnloadAssetBundle();
            break;
        }
        m_unloadDict.Remove(removeKey);
    }

    #region 调试信息
    public int GetWaitUnloadABCount()
    {
        return m_unloadDict.Count;
    }
    #endregion
}
