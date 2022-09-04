using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneRootAssetReference : MonoBehaviour
{
    public static SceneRootAssetReference instance;

    public List<MeshReference> meshReferences;

    private void Start()
    {
        instance = this;
        LoadAll();
    }

    public void LoadAll()
    {
        for (int i = 0; i < meshReferences.Count; ++i)
        {
            meshReferences[i].LoadAssetAsync();
        }
    }

    public void UnloadAll()
    {
        for (int i = 0; i < meshReferences.Count; ++i)
        {
            meshReferences[i].Unload();
        }
    }

    private void Update()
    {
        ResourceManager.Instance.Update();
    }
}
