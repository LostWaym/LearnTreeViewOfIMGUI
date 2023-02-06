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
        view.type = "if";
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
    public static NodeItemView BuildFunc(ScriptTreeFuncBase func, CallFuncExpNode exp)
    {
        var view = NewItemView;
        view.canRemove = true;
        view.canRename = false;
        view.type = "func";
        view.name = func.name;
        view.displayName = func.name;
        FillParameter(view, func.parameterInfoes, exp?.parameters);

        view.onClick = tree =>
        {
            OpenFuncSelectionForm(ret =>
            {
                if (ret == null)
                    return;

                view.name = ret.name;
                view.displayName = ret.name;
                FillParameter(view, ret.parameterInfoes, null);
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
                FillParameter(view, func.parameterInfoes, callFunc.parameters);
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
                    OpenParameterSelectionForm(ret =>
                    {
                        if (ret == null)
                            return;

                        FillParameter(view, ret.parameterInfoes, null);
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
                    var callFuncStatView = BuildFunc(func, callFuncStat.exp);
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
                }, "If-Stat", "Expression");
            };

            view.AddChild(inserterView);
        }


        return view;
    }


    public static void OpenFuncSelectionForm(Action<ScriptTreeFuncBase> callback, ParameterTypeInfo info = null)
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

        var newList = list.Where((func) =>
        {
            return func.canCallSingle;
        }).ToList();
        List<string> names = newList.Select(x => x.name).ToList();
        SelectionFormWindow.OpenWindow(names, (index) =>
        {
            callback?.Invoke(index == -1 ? null : newList[index]);
        }, "选择");
    }

    public static void OpenParameterSelectionForm(Action<ScriptTreeFuncBase> callback, ParameterTypeInfo info = null)
    {
        List<ScriptTreeFuncBase> list;
        if (info == null || info == ParameterTypeInfoes.tany)
        {
            list = ScriptTreeFunctionManager.m_allReturnList;
        }
        else
        {
            list = new List<ScriptTreeFuncBase>(ScriptTreeFunctionManager.GetReturnTypeOf(info.name));
            ScriptTreeFunctionManager.GetReturnTypeOf(ParameterTypeInfoes.tany.name).ForEach(b => list.Add(b));
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
        }

        return node;
    }

    public static BaseExpNode BuildParameterNodeData(NodeItemView root)
    {
        if (root.type != "parameter")
        {
            throw new Exception($"传入的类型不是parameter！");
        }

        ParameterData data = root.data as ParameterData;
        BaseExpNode exp = null;
        if (data.isLiteral)
        {
            object literalValue = data.literalValue == null ? data.paramInfo.getDefaultValue() : data.literalValue;
            if (data.paramInfo == ParameterTypeInfoes.tint)
            {
                IntLiteralExpNode node = new IntLiteralExpNode();
                node.value = (int)literalValue;
                exp = node;
            }
            else if(data.paramInfo == ParameterTypeInfoes.tfloat)
            {

                FloatLiteralExpNode node = new FloatLiteralExpNode();
                node.value = (float)literalValue;
                exp = node;
            }
            else if(data.paramInfo == ParameterTypeInfoes.tstring)
            {

                StringLiteralExpNode node = new StringLiteralExpNode();
                node.value = (string)literalValue;
                exp = node;
            }
            else if(data.paramInfo == ParameterTypeInfoes.tbool)
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