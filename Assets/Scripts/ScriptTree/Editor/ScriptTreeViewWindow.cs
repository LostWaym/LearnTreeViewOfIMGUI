using ScriptTree;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class ScriptTreeViewWindow : EditorWindow
{
    private TreeViewState state;
    private ScriptTreeView treeView;

    private BlockStatNode node;
    private string json = "[]";

    [MenuItem("Test/ScriptTreeView/Real")]
    public static void OpenWindow()
    {
        GetWindow<ScriptTreeViewWindow>();
    }

    private void OnEnable()
    {
        ScriptTreeFunctionManager.InitDefaultTypeAndFunc();
        state = new TreeViewState();
        treeView = new ScriptTreeView(state);
        treeView.Reload();
    }

    private void OnGUI()
    {
        var rect = new Rect(Vector2.zero, position.size);
        var toolbarRect = rect;
        toolbarRect.height = 32;
        var bottomRect = rect;
        bottomRect.y = 32;
        bottomRect.height -= 32;

        if (treeView.isDirty)
        {
            treeView.Reload();
            treeView.SetDirty(false);
        }

        EditorGUILayout.BeginVertical();
        GUILayout.BeginArea(toolbarRect);
        DrawToolBar();
        GUILayout.EndArea();
        treeView.OnGUI(SplitRect(bottomRect, 350, 0));
        GUILayout.BeginArea(SplitRect(bottomRect, 350, 1), "Inspector", GUI.skin.window);
        if (treeView.selectedView != null)
        {
            //if (!string.IsNullOrEmpty(treeView.selectedView.hint))
            //{
            //    EditorGUILayout.LabelField(treeView.selectedView.hint);
            //}
            treeView.selectedView.onGUI?.Invoke(treeView);
        }
        GUILayout.EndArea();
        EditorGUILayout.EndVertical();
    }

    private void DrawToolBar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("保存结构", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            node = ScriptTreeItemViewHelper.BuildBlockNodeData(treeView.dataSourceRoot);

            var json = ScriptTreeSerializer.ToJson(node);
            Debug.Log(json);
            var block = ScriptTreeSerializer.ToBlock(json);
            var json2 = ScriptTreeSerializer.ToJson(block);
            Debug.Log(json2);
            Debug.Log($"verified: {json == json2}");
            this.json = json;
        }

        if (GUILayout.Button("读取结构", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            treeView.dataSourceRoot = ScriptTreeItemViewHelper.BuildBlock(node);
            treeView.SetDirty();
        }



        if (GUILayout.Button("保存并输出结构(json)", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            var node = ScriptTreeItemViewHelper.BuildBlockNodeData(treeView.dataSourceRoot);
            json = ScriptTreeSerializer.ToJson(node);
            Debug.Log(json);
        }
        if (GUILayout.Button("读取结构(json)", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            treeView.dataSourceRoot = ScriptTreeItemViewHelper.BuildBlock(ScriptTreeSerializer.ToBlock(json));
            treeView.SetDirty();
        }


        EditorGUILayout.EndHorizontal();
    }

    private Rect SplitRect(Rect rect, float splitWidth, int index)
    {
        if (index == 0)
        {
            rect.width -= splitWidth;
            return rect;
        }else if (index == 1)
        {
            Rect newRect = new Rect(rect.x + rect.width - splitWidth, rect.y, splitWidth, rect.height);
            return newRect;
        }

        return rect;
    }
}
