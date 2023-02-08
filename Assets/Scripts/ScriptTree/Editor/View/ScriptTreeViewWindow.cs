using ScriptTree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class ScriptTreeViewWindow : EditorWindow
{
    private TreeViewState state;
    private ScriptTreeView treeView;

    private BlockStatNode node;
    private string json = "[]";

    private float inspectorWidth = 350;

    private Func<string> jsonGetter = null;
    private Action<string> jsonSetter = null;

    [MenuItem("Test/ScriptTreeView/Real")]
    public static void OpenWindow()
    {
        var window = GetWindow<ScriptTreeViewWindow>();
        window.minSize = new Vector2(256, 256);
        window.jsonGetter = () => window.json;
        window.jsonSetter = str => window.json = str;
        window.ReadFromJson(window.jsonGetter());
    }

    public static void OpenWindow(Func<string> jsonGetter, Action<string> jsonSetter)
    {
        var window = GetWindow<ScriptTreeViewWindow>();
        window.minSize = new Vector2(256, 256);
        window.jsonGetter = jsonGetter;
        window.jsonSetter = jsonSetter;
        window.ReadFromJson(window.jsonGetter());
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
        treeView.OnGUI(SplitRect(bottomRect, inspectorWidth, 0));

        EditorGUIUtility.AddCursorRect(new Rect(position.width - inspectorWidth - 4, 8, 32, position.height - 32), MouseCursor.ResizeHorizontal);
        if (Event.current.type == EventType.MouseDrag)
        {
            inspectorWidth -= Event.current.delta.x;
            inspectorWidth = Mathf.Clamp(inspectorWidth, 50, position.width - 50);
            Event.current.Use();
        }
        GUILayout.BeginArea(SplitRect(bottomRect, inspectorWidth, 1), "Inspector", GUI.skin.window);
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

        if (GUILayout.Button("保存并验证结构", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
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
            jsonSetter(ScriptTreeSerializer.ToJson(node));
            Debug.Log(jsonGetter());
        }
        if (GUILayout.Button("读取结构(json)", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            ReadFromJson(jsonGetter());
        }
        if (GUILayout.Button("读取结构(json、form)", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            InputContentFormWindow.OpenWindow("请输入json", (content) =>
            {
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                JSONObject obj = new JSONObject(content);
                if (!obj.IsArray)
                {
                    Debug.LogError("无效json！");
                    return;
                }

                var node = ScriptTreeSerializer.BuildBlock(obj);
                treeView.dataSourceRoot = ScriptTreeItemViewHelper.BuildBlock(node);
                treeView.SetDirty();
            });
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("尝试执行", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
        {
            var node = ScriptTreeItemViewHelper.BuildBlockNodeData(treeView.dataSourceRoot);
            ScriptTreeInterpreter.ExecuteStat(node);
        }


        EditorGUILayout.EndHorizontal();
    }

    public void ReadFromJson(string json)
    {
        treeView.dataSourceRoot = ScriptTreeItemViewHelper.BuildBlock(ScriptTreeSerializer.ToBlock(json));
        treeView.SetDirty();
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
