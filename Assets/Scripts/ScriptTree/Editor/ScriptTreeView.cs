using ScriptTree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class ScriptTreeView : TreeView
{


    private ScriptItemView dataSourceRoot;

    public ScriptTreeView(TreeViewState state) : base(state)
    {
        showBorder = true;
        showAlternatingRowBackgrounds = true;
        dataSourceRoot = new ScriptItemView();

        dataSourceRoot.AddChild(ScriptTreeViewItemHelper.BuildIfStruct(null));
        dataSourceRoot.AddChild(ScriptTreeViewItemHelper.BuildActionInserter());
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

        Stack<ScriptItemView> stack = new Stack<ScriptItemView>();
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

    private void AddChildRescursive(int depth, List<ScriptItemView> children, List<TreeViewItem> list)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var testItem = children[i];
            TreeViewItem item = new TreeViewItem()
            {
                id = testItem.id,
                displayName = testItem.display,
                depth = depth
            };
            //testItem.displayItem = item;

            list.Add(item);
            testItem.displayItem = item;
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
            item.onClick?.Invoke(item, this);
        }
    }

    protected override void SingleClickedItem(int id)
    {
        base.SingleClickedItem(id);
    }

    public ScriptItemView selectedView;
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

    private ScriptItemView GetScriptItem(int id, ScriptItemView root)
    {
        if (root == null)
        {
            return null;
        }

        foreach (var item in root.children)
        {
            if (item.id == id)
                return item;

            var ret = GetScriptItem(id, item); ;
            if (ret != null)
                return ret;
        }

        return null;
    }

    private ScriptItemView GetScriptItem(int id)
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
            dataSourceRoot.AddChild(NewTestItem());
            Reload();
        });
    }

    private ScriptItemView NewTestItem()
    {
        return new ScriptItemView()
        {
            id = ++counter,
            display = counter.ToString(),
        };
    }

    private void AddCertainMenuItem(GenericMenu menu, int id)
    {
        var item = GetScriptItem(id);

        if (item.isParameter)
        {
            menu.AddItem(new GUIContent("使用Literal"), false, () => { ScriptTreeViewItemHelper.SetParameterAsLiteral(item, "null");Repaint(); });
            menu.AddSeparator("");
        }
        if (item?.canRemove ?? false)
        {
            menu.AddItem(new GUIContent("移除"), false, () => {
                item.parent?.children.Remove(item);
                Reload();
            });
        }else
        {
            menu.AddDisabledItem(new GUIContent("移除"));
        }
        menu.AddItem(new GUIContent("复制"), false, () => { });
        menu.AddItem(new GUIContent("插入"), false, () =>
        {
            if (item != null)
            {
                item.AddChild(NewTestItem());
                Reload();
            }
        });
        menu.AddItem(new GUIContent("粘贴"), false, () => { });
    }

    protected override bool CanRename(TreeViewItem item)
    {
        var sitem = GetScriptItem(item.id);
        return sitem == null ? false : sitem.canRename;
    }

    protected override void RenameEnded(RenameEndedArgs args)
    {
        if (args.acceptedRename)
        {
            var item2 = GetScriptItem(args.itemID);
            item2.display = args.newName;
            Reload();
        }
    }
}
