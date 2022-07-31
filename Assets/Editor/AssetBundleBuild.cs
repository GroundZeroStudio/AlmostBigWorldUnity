using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using System.IO;
using UnityEditor.SceneManagement;

public class AssetBundleBuildEditor
{
    [MenuItem("AssetBundle/Build")]
    public static void Build()
    {
        Build("Assets/Scenes/CityScene_streaming.unity");
    }

    public static void Build(string path)
    {
        EditorSceneManager.OpenScene(path);
        List<MeshReference> meshReferences = new List<MeshReference>();
        GameObject sceneRoot = GameObject.Find("SceneRoot");
        FindMeshReferences(meshReferences, sceneRoot.transform);

        HashSet<string> buildFileSet = new HashSet<string>();
        for (int i = 0; i < meshReferences.Count; ++i)
        {
            if (!buildFileSet.Contains(meshReferences[i].mesh.abPath))
                buildFileSet.Add(meshReferences[i].mesh.abPath);
        }

        // 依赖项
        HashSet<string> dependencySet = new HashSet<string>();
        foreach (string s in buildFileSet)
        {
            string[] deps = AssetDatabase.GetDependencies(s);
            foreach (string dep in deps)
            {
                if (!buildFileSet.Contains(dep) && !dependencySet.Contains(dep))
                    dependencySet.Add(dep);
            }
        }
        foreach (string s in dependencySet)
            buildFileSet.Add(s);

        List<string> buildFiles = buildFileSet.ToList();
        buildFiles.Sort();
        //StringBuilder log = new StringBuilder();
        List<AssetBundleBuild> buildList = new List<AssetBundleBuild>();
        for (int i = 0; i < buildFiles.Count; ++i)
        {
            if (buildFiles[i].StartsWith("Assets"))
            {
                string abName = buildFiles[i].Substring(7).ToLower();
                //log.Append(abName);
                //log.Append("\n");
                AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
                assetBundleBuild.assetBundleName = abName;
                assetBundleBuild.assetBundleVariant = "ab";
                assetBundleBuild.assetNames = new string[1];
                assetBundleBuild.assetNames[0] = buildFiles[i];
                buildList.Add(assetBundleBuild);
            }
        }

        //File.WriteAllText("log.txt", log.ToString());
        if (!Directory.Exists("Assets/StreamingAssets/AssetBundles"))
            Directory.CreateDirectory("Assets/StreamingAssets/AssetBundles");
        BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/AssetBundles", 
            buildList.ToArray(), 
            BuildAssetBundleOptions.ChunkBasedCompression, 
            BuildTarget.StandaloneWindows);
    }

    static void FindMeshReferences(List<MeshReference> meshReferences, Transform transform)
    {
        for (int i = 0; i < transform.childCount; ++i)
            FindMeshReferences(meshReferences, transform.GetChild(i));
        MeshReference meshReference = transform.GetComponent<MeshReference>();
        if (meshReference != null)
            meshReferences.Add(meshReference);
    }
}
