using ScriptTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class NewScriptTreeViewWindow : EditorWindow
{
    public string label = string.Empty;
    private TreeViewState state;
    private ScriptTreeView treeView;
    private string json = "[]";

    private Func<string> jsonGetter = null;
    private Action<string> jsonSetter = null;

    private Func<string> defJsonGetter => () => json;
    private Action<string> defJsonSetter => str => json = str;

    private bool isExternEdit;


    public const int ToolBarHeight = 24;
    public const int ScriptTreeListWidth = 196;
    public const int ExtraInfoHeight = 64;
    public const int InspecorSelectionHeight = 24;
    public float InspectorWidth = 256;
    public float MinTreeViewWidth = 256;
    public float MinInspectorWidth = 128;
    public float MaxInspectorWidth = 256;

    public float MinWidth => ScriptTreeListWidth + MaxInspectorWidth + MinTreeViewWidth;
    public float MinHeight => ToolBarHeight + ExtraInfoHeight + InspecorSelectionHeight;

    public GUIStyle usingStyle;
    public GUIStyle UsingStyle
    {
        get
        {
            if (usingStyle == null)
            {
                usingStyle = new GUIStyle(GUI.skin.window);
                usingStyle.padding.top = 0;
                usingStyle.border.top = 22;
            }

            return usingStyle;
        }
    }

    [MenuItem("ScriptTree/NewWindow")]
    public static void OpenWindow()
    {
        var window = GetWindow<NewScriptTreeViewWindow>();
        window.minSize = new Vector2(window.MinWidth, window.MinHeight);
        window.jsonGetter = window.defJsonGetter;
        window.jsonSetter = window.defJsonSetter;
        window.isExternEdit = false;
        window.ReadFromJson(window.jsonGetter());
        window.label = string.Empty;
    }

    public static void OpenWindow(string label, Func<string> jsonGetter, Action<string> jsonSetter)
    {
        var window = GetWindow<NewScriptTreeViewWindow>();
        window.minSize = new Vector2(window.MinWidth, window.MinHeight);
        window.jsonGetter = jsonGetter;
        window.jsonSetter = jsonSetter;
        window.isExternEdit = true;
        window.ReadFromJson(window.jsonGetter());
        window.label = label;
    }

    public void ReadFromJson(string json)
    {
        treeView.dataSourceRoot = ScriptTreeItemViewHelper.BuildBlock(ScriptTreeSerializer.ToBlock(json));
        treeView.SetDirty();
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
        float width = position.width, height = position.height;
        Rect rToolBar, rList, rExtraInfo, rTreeView, rInspectorTypeSelections, rInspector;
        rToolBar = new Rect(0, 0, width, ToolBarHeight);
        rList = new Rect(0, rToolBar.height, ScriptTreeListWidth, height - rToolBar.yMax);
        rExtraInfo = new Rect(rList.xMax, rList.yMin, width - rList.xMax, ExtraInfoHeight);
        rTreeView = new Rect(rExtraInfo.xMin, rExtraInfo.yMax, width - rExtraInfo.xMin - InspectorWidth, height - rExtraInfo.yMax);
        rInspectorTypeSelections = new Rect(rTreeView.xMax, rTreeView.yMin, InspectorWidth, InspecorSelectionHeight);
        rInspector = new Rect(rInspectorTypeSelections.xMin, rInspectorTypeSelections.yMax, InspectorWidth, height - rInspectorTypeSelections.yMax);

        Rect rResize = new Rect(rTreeView.xMax - 4, rTreeView.yMin, 8, rTreeView.height);

        if (treeView.isDirty)
        {
            treeView.Reload();
            treeView.SetDirty(false);
        }

        DoInspectorResize(rResize);

        DrawToolBar(rToolBar);
        DrawList(rList);
        DrawExtraInfo(rExtraInfo);
        DrawTreeView(rTreeView);
        DrawInspectorSelections(rInspectorTypeSelections);
        DrawInspector(rInspector);
    }

    private float cacheWidth = 0;
    private void DoInspectorResize(Rect rResize)
    {
        EditorGUIUtility.AddCursorRect(rResize, MouseCursor.ResizeHorizontal);
        if (Event.current.type == EventType.MouseDown)
        {
            if (rResize.Contains(Event.current.mousePosition))
            {
                dragging = true;
                cacheWidth = InspectorWidth;
            }
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            dragging = false;
            cacheWidth = 0;
        }
        else if (dragging && Event.current.type == EventType.MouseDrag)
        {
            cacheWidth -= Event.current.delta.x;
            InspectorWidth = cacheWidth;
            InspectorWidth = Mathf.Clamp(InspectorWidth, MinInspectorWidth, MaxInspectorWidth);
            Event.current.Use();
        }
    }

    private void DrawToolBar(Rect rect)
    {
        GUILayout.BeginArea(rect, UsingStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("验证", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
        {
            var node = ScriptTreeItemViewHelper.BuildBlockNodeData(treeView.dataSourceRoot);
            var json = ScriptTreeSerializer.ToJson(node);
            Debug.Log(json);
            var block = ScriptTreeSerializer.ToBlock(json);
            var json2 = ScriptTreeSerializer.ToJson(block);
            Debug.Log(json2);
            Debug.Log($"verified: {json == json2}");
        }
        if (GUILayout.Button("保存", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
        {
            var node = ScriptTreeItemViewHelper.BuildBlockNodeData(treeView.dataSourceRoot);
            jsonSetter(ScriptTreeSerializer.ToJson(node));
            Debug.Log(jsonGetter());
        }
        if (GUILayout.Button("读取", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
        {
            ReadFromJson(jsonGetter());
        }
        if (GUILayout.Button("读取(form)", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
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
        if (GUILayout.Button("尝试执行", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false)))
        {
            var node = ScriptTreeItemViewHelper.BuildBlockNodeData(treeView.dataSourceRoot);
            ScriptTreeInterpreter.ExecuteStat(node);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    Vector2 ListScrollPosition = Vector2.zero;
    private void DrawList(Rect rect)
    {
        GUILayout.BeginArea(rect, UsingStyle);
        ListScrollPosition = GUILayout.BeginScrollView(ListScrollPosition);
        for (int i = 0; i < 30; i++)
        {
            string title = $"AnonymousScript#{i}";
            GUILayout.Box(title, GUI.skin.button, GUILayout.Height(24), GUILayout.ExpandWidth(true));
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawExtraInfo(Rect rect)
    {
        GUILayout.BeginArea(rect, UsingStyle);
        if (isExternEdit)
        {
            EditorGUILayout.LabelField("正在编辑：" + label, EditorStyles.wordWrappedLabel);
        }
        GUILayout.EndArea();
    }

    private void DrawTreeView(Rect rect)
    {
        GUILayout.BeginArea(rect, UsingStyle);
        var localRect = rect;
        localRect.x = 0;
        localRect.y = 0;
        treeView.OnGUI(localRect);
        GUILayout.EndArea();
    }

    private int inspectorType = 0;
    private string[] inspectorTypeNames = new string[] { "节点监视器", "脚本监视器" };
    private void DrawInspectorSelections(Rect rect)
    {
        GUILayout.BeginArea(rect, UsingStyle);
        inspectorType = GUILayout.Toolbar(inspectorType, inspectorTypeNames);
        GUILayout.EndArea();
    }

    private void DrawInspector(Rect rect)
    {
        GUILayout.BeginArea(rect, UsingStyle);
        if (treeView.selectedView != null)
        {
            treeView.selectedView.onGUI?.Invoke(treeView);
        }
        GUILayout.EndArea();
    }
}
