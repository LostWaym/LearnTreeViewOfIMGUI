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

        if (treeView.isDirty)
        {
            treeView.Reload();
            treeView.SetDirty(false);
        }

        treeView.OnGUI(SplitRect(rect, 350, 0));
        GUILayout.BeginArea(SplitRect(rect, 350, 1), "Inspector", GUI.skin.window);
        if (treeView.selectedView != null)
        {
            //if (!string.IsNullOrEmpty(treeView.selectedView.hint))
            //{
            //    EditorGUILayout.LabelField(treeView.selectedView.hint);
            //}
            treeView.selectedView.onGUI?.Invoke(treeView);
        }
        GUILayout.EndArea();
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
