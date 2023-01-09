using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源请求，请求同一资源时内部共享
public class ResourceRequestShared
{
    public string abPath;

    public AssetBundle assetBundle;

    // 加载ab请求
    public AssetBundleCreateRequest abRequest;

    // 加载资源请求
    public AssetBundleRequest loadAssetRequest;

    // 依赖请求
    public List<ResourceRequestShared> depsRequests = new List<ResourceRequestShared>();

    // 已加载完成的依赖ab
    public List<string> loadedDepAB = new List<string>();

    // 引用计数
    public int refCount;

    // 是否已完成
    public bool isDone;
    
    // 关联的内部请求列表
    public List<ResourceRequestInternal> requestInternalList = new List<ResourceRequestInternal>();
}
