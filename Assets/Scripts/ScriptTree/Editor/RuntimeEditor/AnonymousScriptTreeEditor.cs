using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnonymousScriptTree))]
public class AnonymousScriptTreeEditor : Editor
{
    private void OnEnable()
    {
        AnonymousScriptTree tree = (AnonymousScriptTree)target;
        if (tree.asset == null)
        {
            tree.asset = ScriptableObject.CreateInstance<ScriptTreeAsset>();
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        AnonymousScriptTree tree = (AnonymousScriptTree)target;
        if (GUILayout.Button("打开编辑器"))
        {
            NewScriptTreeViewWindow.OpenWindow($"{(tree.gameObject.scene.IsValid() ? tree.gameObject.scene.name : "NoScene")} - {tree.gameObject.name} - <AnonymousScriptTree>",
                ()=>tree.json,
                (str)=>
                {
                    tree.json = str;
                    EditorUtility.SetDirty(tree);
                },
                tree.info
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
