using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GenerateTest
{
    [MenuItem("Test/Generate Single")]
    public static void Generate()
    {
        if (Selection.activeGameObject == null)
            return;
        MeshFilter mf = Selection.activeGameObject.GetComponent<MeshFilter>();
        Debug.Log("is sub asset:" + AssetDatabase.IsSubAsset(mf.sharedMesh));
        string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
        Debug.Log("path:" + path);

        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < objs.Length; ++i)
        {
            Debug.Log("obj :" + objs[i].name);
            AssetDatabase.CreateAsset(mf.sharedMesh, "Assets/" + objs[i].name + ".mesh");

        }
        /*
        path = path + "/" + mf.sharedMesh.name;
        Debug.Log("sub path:" + path);
        Mesh testMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (testMesh == null)
            Debug.Log("load result null");
        else
            Debug.Log("load mesh tri " + testMesh.triangles.Length);
        */
        //AssetDatabase.CreateAsset(mf.sharedMesh, "Assets/" + mf.sharedMesh.name + ".mesh");
    }
}
