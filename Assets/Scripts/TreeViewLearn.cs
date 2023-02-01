using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// 歪魔邪道写法，不要学啊！

public class TreeViewLearn : EditorWindow
{
    private TreeView treeView;
    private TreeViewState state;

    private void OnEnable()
    {
        state = new TreeViewState();
        treeView = new MyTreeView(state);
        treeView.Reload();
    }

    private void OnGUI()
    {
        var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
        treeView.OnGUI(rect);
    }

    [MenuItem("Test/TreeViewLearn/Open Window")]
    public static void OpenWindow()
    {
        GetWindow<TreeViewLearn>();
    }
}

public class MyTreeView : TreeView
{
    public MyTreeView(TreeViewState state) : base(state)
    {
    }

    public MyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
    {
    }

    private TreeViewItem fuckingCache;
    protected override TreeViewItem BuildRoot()
    {
        if (fuckingCache != null)
            return fuckingCache;

        var root = new TreeViewItem(counter++, -1, "omg1");

        var children = new List<TreeViewItem>()
        {
            new TreeViewItem(counter++, 0, "omg2"),
            new TreeViewItem(counter++, 1, "omg3"),
            new TreeViewItem(counter++, 1, "omg4"),
            new TreeViewItem(counter++, 0, "omg5"),
            new TreeViewItem(counter++, 1, "omg6"),
        };
        root.children = children;

        SetupParentsAndChildrenFromDepths(root, children);
        fuckingCache = root;
        return root;
    }

    protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
    {
        SetupDepthsFromParentsAndChildren(root);
        return base.BuildRows(root);
    }

    int counter = 1;

    protected override bool CanRename(TreeViewItem item)
    {
        return true;
    }

    protected override void RenameEnded(RenameEndedArgs args)
    {
        args.acceptedRename = !string.IsNullOrEmpty(args.newName) &&args.newName.Length <= 8;
    }

    protected override bool CanMultiSelect(TreeViewItem item)
    {
        return false;
    }

    protected override bool CanStartDrag(CanStartDragArgs args)
    {
        return true;
    }

    protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
    {
        DragAndDrop.PrepareStartDrag();
        DragAndDrop.SetGenericData("omg", args.draggedItemIDs[0]);
        DragAndDrop.StartDrag("omgTitle");
    }

    protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
    {
        int id = (int)DragAndDrop.GetGenericData("omg");
        var item = FindItem(id, rootItem);

        void InsertFunction()
        {
            var list = GetAncestors(args.parentItem.id);
            if (!list.Contains(id))
            {
                if (item.parent == args.parentItem)
                {
                    args.insertAtIndex -= item.parent.children.IndexOf(item) < args.insertAtIndex ? 1 : 0;
                }
                item.parent.children.Remove(item);
                item.parent = args.parentItem;

                if (!args.parentItem.hasChildren)
                {
                    args.parentItem.AddChild(item);
                }else
                {
                    args.parentItem.children.Insert(args.insertAtIndex == -1 ? 0 : args.insertAtIndex, item);
                }
                //SetupDepthsFromParentsAndChildren(rootItem);
                //SetExpanded(args.parentItem.id, false);
                //SetExpanded(args.parentItem.id, true);
                Reload();
                SetSelection(new List<int>() { id });
            }
            else
            {
                Debug.Log("不可以将父项设置为子项！");
            }
        }

        switch (args.dragAndDropPosition)
        {
            case DragAndDropPosition.UponItem://当指针挪到item上（不是上面）的时候
                Debug.Log("中心");
                if (args.performDrop)
                {
                    Debug.Log($"中心parentItem: {args.parentItem.displayName}\tindex: {args.insertAtIndex}");
                    InsertFunction();
                }
                break;
            case DragAndDropPosition.BetweenItems://当指针挪到item边缘的时候，如果是在rootItems下的话，那我们就理解了为什么需要rootItem了。
                Debug.Log("列表内");
                if (args.performDrop)
                {
                    Debug.Log($"列表内parentItem: {args.parentItem.displayName}\tindex: {args.insertAtIndex}");
                    InsertFunction();
                }
                break;
            case DragAndDropPosition.OutsideItems://挪到一个没有item的地方
                Debug.Log("最外层");
                if (args.performDrop)
                {
                    Debug.Log($"最外层parentItem: {args.parentItem?.displayName}\tindex: {args.insertAtIndex}");
                    item.parent.children.Remove(item);
                    rootItem.AddChild(item);
                    //SetupDepthsFromParentsAndChildren(rootItem);
                    //SetExpanded(rootItem.id, false);
                    //SetExpanded(rootItem.id, true);
                    SetExpanded(rootItem.id, false);
                    SetExpanded(rootItem.id, true);
                    SetSelection(new List<int>() { id });
                }
                break;
            default:
                break;
        }


        return DragAndDropVisualMode.Move;
    }

    protected override void KeyEvent()
    {
        Debug.Log(Event.current.character);
    }

    protected override void SingleClickedItem(int id)
    {
        Debug.Log(Event.current.button);
    }

    protected override void DoubleClickedItem(int id)
    {
        var item = FindItem(id, rootItem);
        if (item != null)
        {
            item.AddChild(new TreeViewItem()
            {
                id = counter++,
                depth = item.depth + 1,
                displayName = "xx" + counter.ToString()
            });
        }

        Reload();
        //if (IsExpanded(id))
        //{
        //    SetExpanded(id, false);
        //    SetExpanded(id, true);
        //}

    }

    protected override void ContextClickedItem(int id)
    {
        Debug.Log(id + "xx");
        var item = FindItem(id, rootItem);

        if (item == null)
            return;

        item.parent.children.Remove(item);
        item.parent = null;
        //BuildRows(rootItem);
        //RefreshCustomRowHeights();
        //Repaint();
        Reload();
    }

    protected override float GetCustomRowHeight(int row, TreeViewItem item)
    {
        return rowHeight;
    }

    protected override void SelectionChanged(IList<int> selectedIds)
    {
        if (selectedIds.Count == 0)
            return;

        var id = selectedIds[0];
        
        
        Debug.Log($"you select id = {id} !");
    }
}