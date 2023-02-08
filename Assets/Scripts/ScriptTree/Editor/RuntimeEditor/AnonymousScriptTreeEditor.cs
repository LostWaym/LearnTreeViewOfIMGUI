using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnonymousScriptTree))]
public class AnonymousScriptTreeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        AnonymousScriptTree tree = (AnonymousScriptTree)target;
        if (GUILayout.Button("打开编辑器"))
        {
            ScriptTreeViewWindow.OpenWindow(
                ()=>tree.json,
                (str)=>tree.json = str
            );
        }
        if (GUILayout.Button("输出json"))
        {
            InputContentFormWindow.OpenWindow("匿名树json", (result) =>
            {
                tree.json = result;
            }, tree.json);
        }
    }
}
