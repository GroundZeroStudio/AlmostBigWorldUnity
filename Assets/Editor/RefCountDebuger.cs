using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RefCountDebuger : EditorWindow
{
    private Vector2 m_scrollPos;
    private List<KeyValuePair<string, ABResourceManager>> m_resList;

    [MenuItem("Test/Ref Count Debuger")]
    public static void ShowWindow()
    {
        var rWindow = GetWindow<RefCountDebuger>(false, "ABRefCountDebugger", false);
        rWindow.autoRepaintOnSceneChange = true;
        rWindow.Show(true);
    }

    private void OnEnable()
    {
        m_resList = new List<KeyValuePair<string, ABResourceManager>>();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("Not Running");
            return;
        }
        EditorGUILayout.LabelField("RefCount Debuger: ");

        this.m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
        using (var space = new EditorGUILayout.VerticalScope())
        {
            var res = ResourceManager.Instance.GetABResManagerDict();
            m_resList.Clear();
            foreach (var pair in res)
            {
                m_resList.Add(new KeyValuePair<string, ABResourceManager>(pair.Key, pair.Value));
            }
            this.m_resList.Sort((a, b) => b.Value.refCount.CompareTo(a.Value.refCount));
            foreach (var pair in this.m_resList)
            {
                using (var space1 = new EditorGUILayout.HorizontalScope("TextField"))
                {
                    EditorGUILayout.LabelField(pair.Key);
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(pair.Value.refCount.ToString(), GUILayout.Width(50));
                }
                EditorGUILayout.Space();
            }
        }
        EditorGUILayout.EndScrollView();
    }
}