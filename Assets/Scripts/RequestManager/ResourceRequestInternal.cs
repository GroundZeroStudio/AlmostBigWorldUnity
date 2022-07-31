using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源请求，资源模块内部使用
public class ResourceRequestInternal
{
    public string abPath;

    public string assetName;

    public Type type;

    public AssetBundle assetBundle;

    // 加载ab请求
    public AssetBundleCreateRequest abRequest;

    // 加载资源请求
    public AssetBundleRequest loadAssetRequest;

    // 依赖请求
    public List<ResourceRequestInternal> depsRequests = new List<ResourceRequestInternal>();

    // 外部请求实例
    public ResourceRequset outResourceRequest;
}
