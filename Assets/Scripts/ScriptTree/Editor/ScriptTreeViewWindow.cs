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
        state = new TreeViewState();
        treeView = new ScriptTreeView(state);
        treeView.Reload();
    }

    private void OnGUI()
    {
        var rect = new Rect(Vector2.zero, position.size);

        treeView.OnGUI(SplitRect(rect, 200, 0));
        GUILayout.BeginArea(SplitRect(rect, 200, 1), "Inspector", GUI.skin.window);
        //GUILayout.Label("xx");
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
