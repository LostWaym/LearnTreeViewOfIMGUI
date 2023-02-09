using ScriptTree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class ScriptTreeView : TreeView
{
    public NodeItemView dataSourceRoot;
    public bool isDirty = false;
    public void SetDirty(bool toggle = true)
    {
        isDirty = toggle;
        Repaint();
    }

    public ScriptTreeView(TreeViewState state) : base(state)
    {
        showBorder = true;
        showAlternatingRowBackgrounds = true;
        dataSourceRoot = ScriptTreeItemViewHelper.BuildBlock(null);
    }

    public ScriptTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
    {
    }

    protected override TreeViewItem BuildRoot()
    {
        return new TreeViewItem()
        {
            id = 0,
            depth = -1,
            displayName = "root",
        };
    }

    protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
    {
        List<TreeViewItem> list = new List<TreeViewItem>();
        if (dataSourceRoot != null)
        {
            root.id = dataSourceRoot.id;
            if (dataSourceRoot.children.Count > 0)
            {
                AddChildRescursive(0, dataSourceRoot.children, list);
            }
        }

        SetupParentsAndChildrenFromDepths(root, list);
        return list;
    }

    //damn it!
    protected override IList<int> GetDescendantsThatHaveChildren(int id)
    {
        List<int> ids = new List<int>();
        var item = GetScriptItem(id);
        if (item == null)
            return ids;

        Stack<NodeItemView> stack = new Stack<NodeItemView>();
        stack.Push(item);
        while (stack.Count > 0)
        {
            item = stack.Pop();
            ids.Add(item.id);
            foreach (var child in item.children)
            {
                stack.Push(child);
            }
        }
        return ids;
    }

    private void AddChildRescursive(int depth, List<NodeItemView> children, List<TreeViewItem> list)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var testItem = children[i];
            TreeViewItem item = new TreeViewItem()
            {
                id = testItem.id,
                displayName = testItem.displayName,
                depth = depth
            };
            //testItem.displayItem = item;

            list.Add(item);
            //testItem.displayItem = item;
            if (testItem.children.Count > 0)
            {
                if (IsExpanded(item.id))
                {
                    AddChildRescursive(depth + 1, testItem.children, list);
                }
                else
                {
                    item.children = CreateChildListForCollapsedParent();
                }
            }
        }
    }

    protected override void DoubleClickedItem(int id)
    {
        var item = GetScriptItem(id);
        if (item != null)
        {
            //item.AddChild(NewTestItem());
            item.onClick?.Invoke(this);
        }
    }

    protected override void SingleClickedItem(int id)
    {
        base.SingleClickedItem(id);
    }

    public NodeItemView selectedView;
    protected override void SelectionChanged(IList<int> selectedIds)
    {
        if (selectedIds.Count == 0)
        {
            selectedView = null;
            return;
        }

        int id = selectedIds[0];
        selectedView = GetScriptItem(id);
    }


    protected override void ContextClickedItem(int id)
    {
        GenericMenu menu = new GenericMenu();
        AddDefaultMenuItem(menu);
        AddCertainMenuItem(menu, id);
        menu.ShowAsContext();
    }

    private NodeItemView GetScriptItem(int id, NodeItemView root)
    {
        if (root == null)
        {
            return null;
        }
        if (root.id == id)
            return root;

        foreach (var item in root.children)
        {
            var ret = GetScriptItem(id, item); ;
            if (ret != null)
                return ret;
        }

        return null;
    }

    private NodeItemView GetScriptItem(int id)
    {
        return GetScriptItem(id, dataSourceRoot);
    }

    int counter = 0;
    private void AddDefaultMenuItem(GenericMenu menu)
    {
        menu.AddItem(new GUIContent("创建默认列表"), false, () =>
        {

        });
        menu.AddItem(new GUIContent("添加"), false, () =>
        {

            Reload();
        });
    }

    private void AddCertainMenuItem(GenericMenu menu, int id)
    {
        var item = GetScriptItem(id);

        if (item?.canRemove ?? false)
        {
            menu.AddItem(new GUIContent("移除"), false, () => {
                item.RemoveFromParent();
                SetDirty();
            });
        }else
        {
            menu.AddDisabledItem(new GUIContent("移除"));
        }
        menu.AddItem(new GUIContent("复制"), false, () => { });
        menu.AddItem(new GUIContent("插入"), false, () =>
        {
        });
        menu.AddItem(new GUIContent("粘贴"), false, () => { });
    }

    protected override bool CanStartDrag(CanStartDragArgs args)
    {
        var item = GetScriptItem(args.draggedItem.id);
        if (item == null)
            return false;
        
        return new List<string>() { "if", "func", "return" }.Contains(item.type);
    }

    protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
    {
        if (args.draggedItemIDs == null || args.draggedItemIDs.Count == 0)
            return;

        DragAndDrop.PrepareStartDrag();
        DragAndDrop.SetGenericData("move", args.draggedItemIDs[0]);
        DragAndDrop.StartDrag("move");
    }

    protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
    {
        DragAndDropVisualMode mode = DragAndDropVisualMode.None;
        TreeViewItem dragged = FindItem((int)DragAndDrop.GetGenericData("move"), rootItem);
        if (dragged == null)
            return DragAndDropVisualMode.None;

        args.parentItem = args.parentItem ?? rootItem;

        switch (args.dragAndDropPosition)
        {
            case DragAndDropPosition.UponItem://当指针挪到item上（不是上面）的时候
            case DragAndDropPosition.BetweenItems://当指针挪到item边缘的时候，如果是在rootItems下的话，那我们就理解了为什么需要rootItem了。
                if (ValidateDrag(dragged, args.parentItem))
                {
                    mode = DragAndDropVisualMode.Move; 
                    if (args.performDrop)
                    {
                        var parentItem = GetScriptItem(args.parentItem.id);
                        var draggedItem = GetScriptItem(dragged.id);
                        int indexInParent = parentItem.IndexOfChild(draggedItem);
                        if (indexInParent != -1 &&  indexInParent < args.insertAtIndex)
                        {
                            args.insertAtIndex -= 1;
                        }
                        draggedItem.RemoveFromParent();
                        parentItem.InsertChild(Mathf.Clamp(args.insertAtIndex, 0, parentItem.children.Count - 1), draggedItem);
                        this.SetSelection(new List<int>() { draggedItem.id });
                        SetDirty();
                    }
                }
                else
                {
                    mode = DragAndDropVisualMode.Rejected;
                }
                break;
            case DragAndDropPosition.OutsideItems://挪到一个没有item的地方
                if (args.performDrop)
                {
                }
                break;
            default:
                break;
        }


        return mode;
    }

    private bool ValidateDrag(TreeViewItem src, TreeViewItem dest)
    {
        var srcItem = GetScriptItem(src.id);
        var destItem = GetScriptItem(dest.id);
        if (srcItem == null || destItem == null)
            return false;

        //父对象只能是block，子对象只能是func和if和return（即stat）
        if (!(destItem.type == "block" && new List<string>() { "func", "if", "return" }.Contains(srcItem.type)))
            return false;

        //如果srcItem为destItem的父辈，就不让srcItem移动到destItem了
        if (srcItem.IsAncestorOf(destItem))
        {
            return false;
        }

        return true;
    }

    protected override bool CanRename(TreeViewItem item)
    {
        var sitem = GetScriptItem(item.id);
        //如果在返回true的时候修改displayName，即可有指定更名时候和原来名字显示不一样的功能
        return sitem == null ? false : sitem.canRename;
    }

    protected override void RenameEnded(RenameEndedArgs args)
    {
        if (args.acceptedRename)
        {
            var item2 = GetScriptItem(args.itemID);
            item2.name = args.newName;
            Reload();
        }
    }
}
