using ScriptTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class ScriptTreeOnLoad
{
    [InitializeOnLoadMethod]
    public static void Method()
    {
        ScriptTreeFunctionManager.InitDefaultTypeAndFunc();
    }
}

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

    public bool IsAncestorOf(NodeItemView view)
    {
        if (view == this || view == null)
            return false;

        var parent = view.parent;
        while(parent != null)
        {
            if (parent == this)
            {
                return true;
            }
            parent = parent.parent;
        }

        return false;
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
    public ParameterTypeInfo paramInfo2;
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
        view.type = "if";
        view.name = "if";
        view.displayName = "#如果";
        view.canRename = false;
        view.canRemove = true;

        if (node != null)
        {
            foreach (var ifcase in node.cases)
            {
                var caseView = BuildIfCase(ifcase);
                view.AddChild(caseView);
            }
        }

        {
            var inserterView = NewItemView;
            inserterView.name = "...newCase";
            inserterView.displayName = "...新建条件分支";
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
            blockView.displayName = "否则执行";

            view.AddChild(blockView);
        }

        return view;
    }

    public static NodeItemView BuildReturnStat(ReturnStatNode node)
    {
        var view = BuildParameter(node?.exp, "return", ParameterTypeInfoes.tany, "#返回");
        view.type = "return";

        return view;
    }

    public static NodeItemView BuildIfCase(IfCaseData ifcase)
    {
        var view = NewItemView;
        view.type = "case";
        view.name = "case";
        view.displayName = "分支";
        view.canRename = false;
        view.canRemove = true;

        var conditionView = BuildParameter(ifcase?.condition, "condition", ParameterTypeInfoes.tbool, "条件");

        var blockView = BuildBlock(ifcase?.block);
        blockView.name = "stats";
        blockView.displayName = "执行内容";

        view.AddChild(conditionView);
        view.AddChild(blockView);

        return view;
    }

    //双击切换表达式类型
    public static NodeItemView BuildFunc(ScriptTrees.ScriptTree func, CallFuncExpNode exp)
    {
        var view = NewItemView;
        view.canRemove = true;
        view.canRename = false;
        view.type = "func";
        view.name = func.Name; 
        view.displayName = $"#调用 {func.Name}()";
        FillParameter(view, func.ParameterInfoes, exp?.parameters);

        view.onClick = tree =>
        {
            OpenFuncSelectionForm(ret =>
            {
                if (ret == null)
                    return;

                view.name = ret.Name;
                view.displayName = $"#调用 {ret.Name}()";
                FillParameter(view, ret.ParameterInfoes, null);
                tree.SetDirty();
            });
        };

        return view;
    }

    public static void FillParameter(NodeItemView view, List<ParameterInfo> infoes, List<BaseExpNode> exps)
    {
        view.children.Clear();

        if (exps != null)
        {
            for (int i = 0; i < exps.Count; i++)
            {
                var param = exps[i];
                var info = infoes[i];
                var paramNode = BuildParameter(param, info.name, info.type);
                view.AddChild(paramNode);
            }
        }
        else
        {
            for (int i = 0; i < infoes.Count; i++)
            {
                var info = infoes[i];
                var paramNode = BuildParameter(null, info.name, info.type);
                view.AddChild(paramNode);
            }
        }
    }

    //双击切换表达式或字面量类型
    public static NodeItemView BuildParameter(BaseExpNode node, string name, ParameterTypeInfo typeInfo, string localName = null)
    {
        localName = localName ?? name;
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
                FillParameter(view, func.ParameterInfoes, callFunc.parameters);
                data.literalValue = false;
                data.funcName = callFunc.funcName;
                view.displayName = $"{localName}: {func.Name}()";
            }
            else if (node is LiteralExpNode literalExpNode)
            {
                view.displayName = $"{localName}: literal: {literalExpNode.Execute(null)}";
                data.isLiteral = true;
                data.literalValue = literalExpNode.Execute(null);
                if (data.paramInfo == ParameterTypeInfoes.tany)
                {
                    data.paramInfo2 = ScriptTreeFunctionManager.GetLiteralTypeInfo(node.GetType());
                }
            }
        }
        else
        {
            data.isLiteral = true;
            if (data.paramInfo.canBeLiteral)
            {
                data.literalValue = data.paramInfo.getDefaultValue();
                view.displayName = $"{localName}: literal: {data.literalValue}";
            }
            else
            {
                data.literalValue = null;
                view.displayName = $"{localName}: null";
            }
        }

        view.onClick = tree =>
        {
            OpenSelectionForm((index, selection) =>
            {
                if (index == 0)
                {
                    if (typeInfo.canBeLiteral)
                    {
                        data.paramInfo2 = null;
                        data.isLiteral = true;
                        data.literalValue = data.paramInfo.getDefaultValue();
                        view.displayName = $"{localName}: literal: {data.literalValue}";
                        view.children.Clear();
                        tree.SetDirty();
                    }
                    else if (typeInfo == ParameterTypeInfoes.tany)
                    {
                        OpenSelectionForm((i, s) =>
                        {
                            if (i == 0)
                            {
                                data.paramInfo2 = ParameterTypeInfoes.tstring;
                            }
                            else if(i == 1)
                            {
                                data.paramInfo2 = ParameterTypeInfoes.tint;
                            }
                            else if(i == 2)
                            {
                                data.paramInfo2 = ParameterTypeInfoes.tfloat;
                            }
                            else if(i == 3)
                            {
                                data.paramInfo2 = ParameterTypeInfoes.tbool;
                            }
                            if (data.paramInfo2 != null)
                            {
                                data.isLiteral = true;
                                data.literalValue = data.paramInfo2.getDefaultValue();
                                view.displayName = $"{localName}: literal: {data.literalValue}";
                                view.children.Clear();
                                tree.SetDirty();
                            }
                        }, "StringLiteral", "IntLiteral", "FloatLiteral", "BoolLiteral");
                    }
                }
                else if (index == 1)
                {
                    OpenParameterSelectionForm(ret =>
                    {
                        if (ret == null)
                            return;

                        FillParameter(view, ret.ParameterInfoes, null);
                        data.isLiteral = false;
                        data.paramInfo2 = null;
                        data.literalValue = null;
                        data.funcName = ret.Name;
                        view.displayName = $"{localName}: {ret.Name}()";
                        tree.SetDirty();
                    }, typeInfo);
                }
            }, "Literal", "Expression");
        };

        view.onGUI = tree =>
        {
            if (data.isLiteral)
            {
                var paramInfo = data.paramInfo2 == null ? data.paramInfo : data.paramInfo2;
                if (paramInfo == ParameterTypeInfoes.tint)
                {
                    int value = (int)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.IntField("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{localName}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
                else if (paramInfo == ParameterTypeInfoes.tfloat)
                {
                    float value = (float)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.FloatField("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{localName}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
                else if (paramInfo == ParameterTypeInfoes.tstring)
                {
                    string value = (string)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.TextField("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{localName}: literal: {data.literalValue}";
                        tree.SetDirty();
                    }
                }
                else if (paramInfo == ParameterTypeInfoes.tbool)
                {
                    bool value = (bool)data.literalValue;
                    EditorGUI.BeginChangeCheck();
                    value = EditorGUILayout.Toggle("value", value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.literalValue = value;
                        view.displayName = $"{localName}: literal: {data.literalValue}";
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
                    var callFuncStatView = BuildFunc(func, callFuncStat.exp);
                    view.AddChild(callFuncStatView);
                }else if (item is ReturnStatNode returnStat)
                {
                    var retView = BuildReturnStat(returnStat);
                    view.AddChild(retView);
                }
            }
        }

        {
            var inserterView = NewItemView;
            inserterView.name = "...new";
            inserterView.displayName = "...新动作";
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
                        OpenFuncSelectionForm(func =>
                        {
                            if (func == null)
                            {
                                return;
                            }

                            var insertIndex = view.IndexOfChild(inserterView);
                            var inserteeView = BuildFunc(func, null);
                            view.InsertChild(insertIndex, inserteeView);
                            tree.SetDirty();
                        });
                    }
                    else if(index == 2)
                    {
                        var insertIndex = view.IndexOfChild(inserterView);
                        var inserteeView = BuildReturnStat(null);
                        view.InsertChild(insertIndex, inserteeView);
                        tree.SetDirty();
                    }
                }, "If-Stat", "Expression", "Return-Stat");
            };

            view.AddChild(inserterView);
        }


        return view;
    }


    public static void OpenFuncSelectionForm(Action<ScriptTrees.ScriptTree> callback, ParameterTypeInfo info = null)
    {
        List<ScriptTrees.ScriptTree> list;
        if (info == null || info == ParameterTypeInfoes.tany)
        {
            list = ScriptTreeFunctionManager.m_allList;
        }
        else
        {
            list = ScriptTreeFunctionManager.GetReturnTypeOf(info.name);
        }

        var newList = list.Where((func) =>
        {
            return func.CanCallSingle;
        }).ToList();
        List<string> names = newList.Select(x => x.Name).ToList();
        SelectionFormWindow.OpenWindow(names, (index) =>
        {
            callback?.Invoke(index == -1 ? null : newList[index]);
        }, "选择");
    }

    public static void OpenParameterSelectionForm(Action<ScriptTrees.ScriptTree> callback, ParameterTypeInfo info = null)
    {
        List<ScriptTrees.ScriptTree> list;
        if (info == null || info == ParameterTypeInfoes.tany)
        {
            list = ScriptTreeFunctionManager.m_allReturnList;
        }
        else
        {
            list = new List<ScriptTrees.ScriptTree>(ScriptTreeFunctionManager.GetReturnTypeOf(info.name));
            ScriptTreeFunctionManager.GetReturnTypeOf(ParameterTypeInfoes.tany.name).ForEach(b => list.Add(b));
        }

        List<string> names = list.Select(x => x.Name).ToList();
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

    public static BlockStatNode BuildBlockNodeData(NodeItemView root)
    {
        BlockStatNode node = new BlockStatNode();
        node.children = new List<BaseStatNode>();
        foreach (var item in root.children)
        {
            if (item.type == "if")
            {
                IfStatNode ifStatNode = new IfStatNode();
                ifStatNode.cases = new List<IfCaseData>();
                foreach (var item2 in item.children)
                {
                    if (item2.name == "case")
                    {
                        IfCaseData ifCaseData = new IfCaseData();
                        ifCaseData.block = BuildBlockNodeData(item2.GetChild("stats"));
                        ifCaseData.condition = BuildParameterNodeData(item2.GetChild("condition"));
                        
                        ifStatNode.cases.Add(ifCaseData);
                    }
                }

                ifStatNode.defaultBlock = BuildBlockNodeData(item.GetChild("default"));

                node.children.Add(ifStatNode);
            }
            else if (item.type == "func")
            {
                CallFuncStatNode callFuncStatNode = new CallFuncStatNode();
                CallFuncExpNode callFuncExpNode = new CallFuncExpNode();
                callFuncStatNode.exp = callFuncExpNode;

                callFuncExpNode.funcName = item.name;
                callFuncExpNode.parameters = new List<BaseExpNode>();
                foreach (var parameter in item.children)
                {
                    callFuncExpNode.parameters.Add(BuildParameterNodeData(parameter));
                }

                node.children.Add(callFuncStatNode);
            }
            else if (item.type == "return")
            {
                ReturnStatNode stat = new ReturnStatNode();
                stat.exp = BuildParameterNodeData(item);

                node.children.Add(stat);
            }
        }

        return node;
    }

    public static BaseExpNode BuildParameterNodeData(NodeItemView root)
    {
        if (root.type != "parameter" && root.type != "return")
        {
            throw new Exception($"传入的类型不是parameter或return！");
        }

        ParameterData data = root.data as ParameterData;
        BaseExpNode exp = null;
        if (data.isLiteral)
        {
            var paramInfo = data.paramInfo2 == null ? data.paramInfo : data.paramInfo2;
            object literalValue = data.literalValue == null ? paramInfo.getDefaultValue() : data.literalValue;
            if (paramInfo == ParameterTypeInfoes.tint)
            {
                IntLiteralExpNode node = new IntLiteralExpNode();
                node.value = (int)literalValue;
                exp = node;
            }
            else if(paramInfo == ParameterTypeInfoes.tfloat)
            {

                FloatLiteralExpNode node = new FloatLiteralExpNode();
                node.value = (float)literalValue;
                exp = node;
            }
            else if(paramInfo == ParameterTypeInfoes.tstring)
            {

                StringLiteralExpNode node = new StringLiteralExpNode();
                node.value = (string)literalValue;
                exp = node;
            }
            else if(paramInfo == ParameterTypeInfoes.tbool)
            {

                BoolLiteralExpNode node = new BoolLiteralExpNode();
                node.value = (bool)literalValue;
                exp = node;
            }
        }
        else
        {
            CallFuncExpNode call = new CallFuncExpNode();
            call.funcName = data.funcName;
            call.parameters = new List<BaseExpNode>();
            foreach (var parameter in root.children)
            {
                call.parameters.Add(BuildParameterNodeData(parameter));
            }
            exp = call;
        }
        return exp;
    }
}