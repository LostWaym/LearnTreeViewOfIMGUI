using ScriptTree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class ScriptTreeViewWindow : EditorWindow
{
    public string label = string.Empty;
    private TreeViewState state;
    private ScriptTreeView treeView;

    private BlockStatNode node;
    private string json = "[]";

    private float inspectorWidth = 350;

    private Func<string> jsonGetter = null;
    private Action<string> jsonSetter = null;

    private Func<string> defJsonGetter => ()=> json;
    private Action<string> defJsonSetter => str => json = str;

    private bool isExternEdit;


    [MenuItem("Test/ScriptTreeView/Real")]
    public static void OpenWindow()
    {
        var window = GetWindow<ScriptTreeViewWindow>();
        window.minSize = new Vector2(256, 256);
        window.jsonGetter = window.defJsonGetter;
        window.jsonSetter = window.defJsonSetter;
        window.isExternEdit = false;
        window.ReadFromJson(window.jsonGetter());
        window.label = string.Empty;
    }

    public static void OpenWindow(string label, Func<string> jsonGetter, Action<string> jsonSetter)
    {
        var window = GetWindow<ScriptTreeViewWindow>();
        window.minSize = new Vector2(256, 256);
        window.jsonGetter = jsonGetter;
        window.jsonSetter = jsonSetter;
        window.isExternEdit = true;
        window.ReadFromJson(window.jsonGetter());
        window.label = label;
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += PlayModeChanged;

        isExternEdit = false;
        label = string.Empty;
        jsonSetter = defJsonSetter;
        jsonGetter = defJsonGetter;

        ScriptTreeFunctionManager.InitDefaultTypeAndFunc();
        state = new TreeViewState();
        treeView = new ScriptTreeView(state);
        treeView.Reload();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= PlayModeChanged;
    }

    private void PlayModeChanged(PlayModeStateChange args)
    {
        if (!isExternEdit)
            return;

        if (args == PlayModeStateChange.ExitingEditMode || args == PlayModeStateChange.ExitingPlayMode)
        {
            jsonSetter = defJsonSetter;
            jsonGetter = defJsonGetter;
            isExternEdit = false;
            treeView.dataSourceRoot = null;
            treeView.Reload();
        }
    }

    private bool dragging = false;
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

        var resizeRect = new Rect(position.width - inspectorWidth - 4, 8, 32, position.height - 32);
        EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);
        if (Event.current.type == EventType.MouseDown)
        {
            if (resizeRect.Contains(Event.current.mousePosition))
            {
                dragging = true;
            }
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            dragging = false;
        }
        else if (dragging && Event.current.type == EventType.MouseDrag)
        {
            inspectorWidth -= Event.current.delta.x;
            inspectorWidth = Mathf.Clamp(inspectorWidth, 50, position.width - 50);
            Event.current.Use();
        }
        GUILayout.BeginArea(SplitRect(bottomRect, inspectorWidth, 1), "Inspector", GUI.skin.window);
        if (isExternEdit) {
            EditorGUILayout.LabelField("正在编辑：" + label, EditorStyles.wordWrappedLabel);
        } 
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
            }, jsonGetter());
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
