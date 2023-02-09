using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NewScriptTreeViewWindow : EditorWindow
{
    public const int ToolBarHeight = 24;
    public const int ScriptTreeListWidth = 96;
    public const int ExtraInfoHeight = 64;
    public const int InspecorSelectionHeight = 24;
    public float InspectorWidth = 256;

    [MenuItem("ScriptTree/NewWindow")]
    public static void OpenWindow()
    {
        GetWindow<NewScriptTreeViewWindow>();
    }

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

        foreach (var item in new List<Rect>() { rToolBar, rList, rExtraInfo, rTreeView, rInspectorTypeSelections, rInspector })
        {
            GUI.Box(item, "a", "helpbox");
        }
    }
}
