using System;

// 资源请求，资源模块内部使用
public class ResourceRequestInternal
{
    public string assetName;

    public Type type;

    // 外部请求实例
    public ResourceRequset outResourceRequest;

    // 共享请求实力
    public ResourceRequestShared requestShared;
}
