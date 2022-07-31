using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StreamingSceneGenerate
{
    [MenuItem("StreamingScene/Generate")]
    public static void Generate()
    {
        Generate("Assets/Scenes/CityScene.unity");
    }

    public static void Generate(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path);
        string sceneDir = Path.GetDirectoryName(path);
        string generateScenePath = sceneDir + "/" + scene.name + "_streaming.unity";
        GameObject sceneRoot = GameObject.Find("SceneRoot");
        UnpackPrefabs(sceneRoot.transform);
        EditorSceneManager.SaveScene(scene, generateScenePath, true);
        EditorSceneManager.CloseScene(scene, true);

        scene = EditorSceneManager.OpenScene(generateScenePath);
        sceneRoot = GameObject.Find("SceneRoot");
        GenerateMeshReference(sceneRoot.transform);
        EditorSceneManager.SaveScene(scene, generateScenePath, true);
        AssetDatabase.Refresh();
    }

    static void UnpackPrefabs(Transform transform)
    {
        if (PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject))
            PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        
        if (!transform.gameObject.activeSelf)
        {
            GameObject.DestroyImmediate(transform.gameObject);
            return;
        }

        for (int i = 0; i < transform.childCount; ++i)
            UnpackPrefabs(transform.GetChild(i));
    }

    static void GenerateMeshReference(Transform root)
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        FindMeshFilters(meshFilters, root);
        HashSet<string> createdAssets = new HashSet<string>();
        SceneRootAssetReference sceneRootReference = root.gameObject.AddComponent<SceneRootAssetReference>();
        sceneRootReference.meshReferences = new List<MeshReference>();
        for (int i = 0; i < meshFilters.Count; ++i)
        {
            MeshFilter meshFilter = meshFilters[i];
            EditorUtility.DisplayProgressBar("Generate Mesh Reference", meshFilter.sharedMesh.name, (float)i / (float)meshFilters.Count);
            MeshRenderer meshRenderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
            MeshReference meshReference = SetMeshRef(meshFilter, meshRenderer);
            sceneRootReference.meshReferences.Add(meshReference);
            meshFilter.sharedMesh = null;
            try
            {
                if (meshRenderer != null && meshRenderer.sharedMaterials != null)
                    meshRenderer.sharedMaterials = new Material[0];
            }catch(System.Exception e)
            {
                if (meshRenderer != null && meshRenderer.sharedMaterials != null)
                    meshRenderer.sharedMaterials = null;
            }
        }
        EditorUtility.ClearProgressBar();
    }

    static MeshReference SetMeshRef(MeshFilter meshFilter, MeshRenderer meshRenderer)
    {
        MeshReference meshReference = meshFilter.gameObject.AddComponent<MeshReference>();
        meshReference.mesh = new MeshReference.MeshRefInfo();
        meshReference.mesh.abPath = GetABPath(meshFilter.sharedMesh);
        meshReference.mesh.assetName = meshFilter.sharedMesh.name;
        meshReference.meshFilter = meshFilter;
        meshReference.meshRenderer = meshRenderer;

        if (meshRenderer != null && meshRenderer.sharedMaterials != null)
        {
            meshReference.materials = new MeshReference.MatRefInfo[meshRenderer.sharedMaterials.Length];
            for (int i = 0; i < meshReference.materials.Length; ++i)
            {
                Material mat = meshRenderer.sharedMaterials[i];
                var matRefInfo = new MeshReference.MatRefInfo();
                matRefInfo.abPath = GetABPath(mat);
                matRefInfo.assetName = meshRenderer.sharedMaterials[i].name;
                string[] texNames = mat.GetTexturePropertyNames();
                List<MeshReference.MatTexInfo> texInfoList = new List<MeshReference.MatTexInfo>();
                for (int j = 0; j < texNames.Length; ++j)
                {
                    Texture texture = mat.GetTexture(texNames[j]);
                    if (texture != null)
                    {
                        var texInfo = new MeshReference.MatTexInfo();
                        texInfo.texName = texNames[j];
                        texInfo.abPath = GetABPath(texture);
                        texInfo.assetName = texture.name;
                        texInfoList.Add(texInfo);
                    }
                }
                matRefInfo.textures = texInfoList.ToArray();
                meshReference.materials[i] = matRefInfo;
            }
        }
        return meshReference;
    }

    static string GetABPath(Object obj)
    {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        if (!assetPath.StartsWith("Assets"))
            return "";
        return assetPath.Substring(7).ToLower() + ".ab";
    }

    static void FindMeshFilters(List<MeshFilter> meshFilters, Transform transform)
    {
        for (int i = 0; i < transform.childCount; ++i)
            FindMeshFilters(meshFilters, transform.GetChild(i));
        MeshFilter mf = transform.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            meshFilters.Add(mf);
    }
}
