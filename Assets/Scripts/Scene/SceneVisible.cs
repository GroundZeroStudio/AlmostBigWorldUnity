using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SceneVisible : MonoBehaviour
{
    public static SceneVisible Instance;

    public new Camera camera;

    private List<MeshReference> meshReferences = new List<MeshReference>();

    private Bounds cameraBounds;

    void Awake()
    {
        Instance = this;
        cameraBounds.center = camera.transform.position;
        cameraBounds.extents = new Vector3(100, 100, 100);
    }

    public void Register(MeshReference meshReference)
    {
        meshReferences.Add(meshReference);
    }

    public void Update()
    {
        ProcessVisible();
    }

    public void ProcessVisible()
    {
        cameraBounds.center = camera.transform.position;
        for (int i = 0; i < meshReferences.Count; ++i)
        {
            MeshReference meshReference = meshReferences[i];
            bool isVisible = cameraBounds.Intersects(meshReference.bounds);//(distance < 100);
            if (isVisible)
            {

                if (meshReference.isVisible != isVisible)
                    meshReferences[i].LoadAssetAsync();
            }
            meshReference.isVisible = isVisible;
        }
    }
}