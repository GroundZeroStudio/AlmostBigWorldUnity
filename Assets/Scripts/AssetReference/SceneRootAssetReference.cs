using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneRootAssetReference : MonoBehaviour
{
    public List<MeshReference> meshReferences;

    private void Start()
    {
        for (int i = 0; i < meshReferences.Count; ++i)
        {
            meshReferences[i].LoadAssetAsync();
        }
    }

    private void Update()
    {
        ResourceManager.Instance.Update();
    }
}
