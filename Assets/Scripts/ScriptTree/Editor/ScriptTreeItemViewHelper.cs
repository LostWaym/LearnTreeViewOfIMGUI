using ScriptTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

public class NodeItemView
{
    public int id;
    public string name;
    public string displayName;
    public string type;
    public NodeItemView parent;
    public List<NodeItemView> children = new List<NodeItemView>();
    public object data;

    public bool canRename;
    public bool canRemove;

    public Action<ScriptTreeView> onClick;
    public Action<ScriptTreeView> onGUI;
    public Action<ScriptTreeView> onRemoved;

    public void AddChild(NodeItemView view)
    {
        children.Add(view);
        view.parent = this;
    }

    public void InsertChild(int index, NodeItemView view)
    {
        children.Insert(index, view);
        view.parent = this;
    }

    public NodeItemView GetChild(string name)
    {
        foreach (var item in children)
        {
            if (item.name == name)
                return item;
        }

        return null;
    }

    public void RemoveFromParent()
    {
        parent?.children.Remove(this);
        parent = null;
    }

    public int IndexOfChild(NodeItemView view)
    {
        return children.IndexOf(view);
    }
}

public class ParameterData
{
    public bool isLiteral;
    public ParameterTypeInfo paramInfo;
    public string funcName;
    public object literalValue;
}


public static class ScriptTreeItemViewHelper
{
    public static int counter = 0;
    public static NodeItemView NewItemView => new NodeItemView()
    {
        id = ++counter,
        name = counter.ToString()
    };

    public static NodeItemView BuildIfStatNode(IfStatNode node)
    {
        var view = NewItemView;
        view.name = "if";
        view.displayName = view.name;
        view.canRename = false;
        view.canRemove = true;

        if (node != null)
        {
            foreach (var ifcase in node.cases)
            {
                var caseView = NewItemView;
                caseView.name = "case";
                caseView.displayName = caseView.name;
                caseView.canRename = false;
                caseView.canRemove = true;

                var conditionView = BuildParameter(ifcase.condition, "condition", ParameterTypeInfoes.tbool);

                var blockView = BuildBlock(ifcase.block);
                blockView.name = "stats";
                blockView.displayName = blockView.name;

                caseView.AddChild(conditionView);
                caseView.AddChild(blockView);

                view.AddChild(caseView);
            }
        }

        {
            var inserterView = NewItemView;
            inserterView.name = "...newCase";
            inserterView.displayName = inserterView.name;
            inserterView.type = "inserter";

            inserterView.onClick = tree =>
            {
                var insertIndex = view.IndexOfChild(inserterView);
                var inserteeView = BuildIfCase(null);
                view.InsertChild(insertIndex, inserteeView);
                tree.SetDirty();
            };

            view.AddChild(inserterView);
        }

        {
            var blockView = BuildBlock(node?.defaultBlock);
            blockView.name = "default";
            blockView.displayName = blockView.name;

            view.AddChild(blockView);
        }

        return view;
    }

    public static NodeItemView BuildIfCase(IfCaseData ifcase)
    {
        var view = NewItemView;
        view.name = "case";
        view.displayName = view.name;
        view.canRename = false;
        view.canRemove = true;

        var conditionView = BuildParameter(ifcase?.condition, "condition", ParameterTypeInfoes.tbool);

        var blockView = BuildBlock(ifcase?.block);
        blockView.name = "stats";
        blockView.displayName = blockView.name;

        view.AddChild(conditionView);
        view.AddChild(blockView);

        return view;
    }

    //双击切换表达式类型
    public static NodeItemView BuildFunc(ScriptTreeFuncBase func)
    {
        var view = NewItemView;
        view.canRemove = true;
        view.canRename = false;
        view.type = "func";
        view.name = func.name;
        view.displayName = func.name;
        FillParameter(view, func.parameterInfoes);

        view.onClick = tree =>
        {
            OpenSelectionForm(ret =>
            {
                if (ret == null)
                    return;

                view.name = ret.name;
                view.displayName = ret.name;
                FillParameter(view, ret.parameterInfoes);
                tree.SetDirty();
            });
        };

        return view;
    }

    public static void FillParameter(NodeItemView view, List<ParameterInfo> infoes)
    {
        view.children.Clear();
        foreach (var item in infoes)
        {
            var param = BuildParameter(null, item.name, item.type);
            view.AddChild(param);
        }
    }

    //双击切换表达式或字面量类型
    public static NodeItemView BuildParameter(BaseExpNode node, string name, ParameterTypeInfo typeInfo)
    {
        ParameterData data = new ParameterData();
        var view = NewItemView;
        view.canRemove = false;
        view.canRename = false;
        view.type = "parameter";
        view.name = name;
        view.displayName = name;
        view.data = data;

        data.paramInfo = typeInfo;

        if (node != null)
        {
            if (node is CallFuncExpNode callFunc)
            {
                var func = ScriptTreeFunctionManager.GetFunction(callFunc.funcName);
                FillParameter(view, func.parameterInfoes);
                data.literalValue = false;
                data.funcName = callFunc.funcName;
                view.displayName = $"{name}: {func.name}()";
            }
            else if (node is LiteralExpNode literalExpNode)
            {
                view.displayName = $"{name}: literal: {literalExpNode.Execute(null)}";
                data.isLiteral = true;
                data.literalValue = literalExpNode.Execute(null);
            }
        }
        else
        {
            data.isLiteral = true;
            if (data.paramInfo.canBeLiteral)
            {
                data.literalValue = data.paramInfo.getDefaultValue();
                view.displayName = $"{name}: literal: {data.literalValue}";
            }
            else
            {
                data.literalValue = null;
                view.displayName = $"{name}: null";
            }
        }

        view.onClick = tree =>
        {
            OpenSelectionForm((index, selection) =>
            {
                if (index == 0 && typeInfo.canBeLiteral)
                {
                    data.isLiteral = true;
                    data.literalValue = data.paramInfo.getDefaultValue();
                    view.displayName = $"{name}: literal: {data.literalValue}";
                    view.children.Clear();
                    tree.SetDirty();
                }
                else if (index == 1)
                {
                    OpenSelectionForm(ret =>
                    {
                        if (ret == null)
                            return;

                        FillParameter(view, ret.parameterInfoes);
                        data.isLiteral = false;
                        data.literalValue = null;
                        data.funcName = ret.name;
                        view.displayName = $"{name}: {ret.name}()";
                        tree.SetDirty();
                    }, typeInfo);
                }
            }, "Literal", "Expression");
        };

        view.onGUI = tree =>
        {
            if (data.isLiteral)
            {
                if (data.paramInfo == ParameterTypeInfoes.tint)
                {
                    int value = (int)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.IntField("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{name}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
                else if (data.paramInfo == ParameterTypeInfoes.tfloat)
                {
                    float value = (float)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.FloatField("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{name}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
                else if (data.paramInfo == ParameterTypeInfoes.tstring)
                {
                    string value = (string)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.TextField("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{name}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
                else if (data.paramInfo == ParameterTypeInfoes.tbool)
                {
                    bool value = (bool)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.Toggle("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{name}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
            }
        };

        return view;
    }

    //可放置语句或者表达式
    public static NodeItemView BuildBlock(BlockStatNode node)
    {
        var view = NewItemView;
        view.canRemove = false;
        view.canRename = false;
        view.type = "block";

        if (node != null)
        {
            foreach (var item in node.children)
            {
                if (item is IfStatNode ifStat)
                {
                    var ifStatView = BuildIfStatNode(ifStat);
                    view.AddChild(ifStatView);
                }else if (item is CallFuncStatNode callFuncStat)
                {
                    var func = ScriptTreeFunctionManager.GetFunction(callFuncStat.exp.funcName);
                    var callFuncStatView = BuildFunc(func);
                    view.AddChild(callFuncStatView);
                }else if (item is ReturnStatNode returnStat)
                {
                    var retView = NewItemView;
                    retView.type = "return";
                    view.AddChild(retView);
                }
            }
        }

        {
            var inserterView = NewItemView;
            inserterView.name = "...new";
            inserterView.displayName = inserterView.name;
            inserterView.type = "inserter";

            inserterView.onClick = tree =>
            {
                OpenSelectionForm((index, selection) =>
                {
                    if (index == 0)
                    {
                        var insertIndex = view.IndexOfChild(inserterView);
                        var inserteeView = BuildIfStatNode(null);
                        view.InsertChild(insertIndex, inserteeView);
                        tree.SetDirty();
                    }
                    else if (index == 1)
                    {
                        OpenSelectionForm(func =>
                        {
                            if (func == null)
                            {
                                return;
                            }

                            var insertIndex = view.IndexOfChild(inserterView);
                            var inserteeView = BuildFunc(func);
                            view.InsertChild(insertIndex, inserteeView);
                            tree.SetDirty();
                        });
                    }
                }, "If-Stat", "Expression");
            };

            view.AddChild(inserterView);
        }


        return view;
    }


    public static void OpenSelectionForm(Action<ScriptTreeFuncBase> callback, ParameterTypeInfo info = null)
    {
        List<ScriptTreeFuncBase> list;
        if (info == null || info == ParameterTypeInfoes.tany)
        {
            list = ScriptTreeFunctionManager.m_allList;
        }
        else
        {
            list = ScriptTreeFunctionManager.GetReturnTypeOf(info.name);
        }

        List<string> names = list.Select(x => x.name).ToList();
        SelectionFormWindow.OpenWindow(names, (index) =>
        {
            callback?.Invoke(index == -1 ? null : list[index]);
        }, "选择");
    }

    public static void OpenSelectionForm(Action<int, string> callback, params string[] selections)
    {
        var list = selections.ToList();
        SelectionFormWindow.OpenWindow(list, (index) =>
        {
            callback?.Invoke(index, index == -1 ? null : list[index]);
        });
    }
}