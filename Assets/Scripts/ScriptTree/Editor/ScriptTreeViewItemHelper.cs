﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Helper = ScriptTree.ScriptTreeViewItemHelper;

namespace ScriptTree
{
    public class ScriptItemView
    {
        public int id;
        public string display;
        public bool canRemove = true;
        public bool canRename = true;
        public bool isParameter = false;
        public string paramName = "";
        public string hint = string.Empty;
        public ScriptItemView parent;
        public TreeViewItem displayItem;
        public List<ScriptItemView> children = new List<ScriptItemView>();
        public Action<ScriptItemView, ScriptTreeView> onClick;
        public Action<ScriptItemView, ScriptTreeView> onInspector;
        public Action<ScriptItemView, ScriptTreeView> onRemove;
        public object data;

        public void AddChild(ScriptItemView item)
        {
            children.Add(item);
            item.parent = this;
        }
    }

    public class CallFuncItemView : ScriptItemView
    {
        public CallFuncStatNode m_node;

        public CallFuncItemView(CallFuncStatNode stat)
        {
            m_node = stat;
            onClick = OnClick;

            display = $"@{stat.exp.funcName}()";
            var func = ScriptTreeFunctionManager.GetFunction(stat.exp.funcName);
            for (int i = 0; i < func.parameterInfoes.Count; i++)
            {
                var index = i;
                var param = func.parameterInfoes[i];
                var subNode = Helper.NewScriptItemView<ParameterItemView>();
                subNode.Init(param);
                subNode.Load(stat.exp.parameters[i]);
                subNode.onUseLiteral = (literal) =>
                {
                    stat.exp.parameters[index] = literal;
                };
                subNode.onUseCall = (call) =>
                {
                    stat.exp.parameters[index] = call;
                };

                AddChild(subNode);
            }
        }

        private void OnClick(ScriptItemView view, ScriptTreeView tree)
        {

        }
    }

    public class ParameterItemView : ScriptItemView
    {
        public BaseExpNode m_node;
        public Action<LiteralExpNode> onUseLiteral;
        public Action<CallFuncExpNode> onUseCall;
        public ParameterInfo info;

        public bool IsLiteral => m_node is LiteralExpNode;

        public ParameterItemView()
        {
            canRename = false;
            canRemove = false;

            onClick = (view, tree) =>
            {
                Helper.OpenSelectionForm((ret) =>
                {
                    var call = new CallFuncExpNode()
                    {
                        funcName = ret.name,
                        id = m_node.id,
                        parameters = new List<BaseExpNode>()
                    };
                    Load(call);
                    onUseCall?.Invoke(call);
                    tree.Reload();
                }, info.type);
            };
        }

        public void Init(ParameterInfo info)
        {
            this.info = info;
            paramName = info.name;
        }

        public void Load(BaseExpNode node)
        {
            children.Clear();

            m_node = node;
            if (node is LiteralExpNode exp)
            {
                display = $"{paramName}: literal: {exp.GetValueString()}";
            }
            else if (node is CallFuncExpNode callFunc)
            {
                display = $"{paramName}: {callFunc.funcName}()";


            }
            else
            {
                display = $"{paramName}: null";
            }
        }
    }

    public class SerializedData<T>
    {
        private Action<T> setter;
        private Func<T> getter;

        public void BindTarget(object obj, string name)
        {
            var type = obj.GetType();
            var field = type.GetField(name);
            if (field == null)
            {
                throw new Exception($"{type.FullName} 不存在field {name}！");
            }
            if (!field.FieldType.IsInstanceOfType(typeof(T)))
            {
                throw new Exception($"{type.FullName} 的field {name} 类型与{typeof(T).Name}不匹配！");
            }

            setter = (T t) =>
            {
                field.SetValue(obj, t);
            };
            getter = () =>
            {
                return (T)field.GetValue(obj);
            };
        }

        public T GetData()
        {
            return getter();
        }

        public void SetData(T t)
        {
            setter(t);
        }
    }

    public static class ScriptTreeViewItemHelper
    {
        private static int counter = 0;

        public static void ResetCounter() => counter = 0;

        public static ScriptItemView NewScriptItemView(string title = null)
        {
            ScriptItemView view = new ScriptItemView
            {
                id = ++counter,
                display = title ?? $"#{counter}"
            };

            return view;
        }

        public static T NewScriptItemView<T>(string title = null) where T : ScriptItemView, new()
        {
            var view = new T
            {
                id = ++counter,
                display = title ?? $"#{counter}"
            };

            return view;
        }

        public static ScriptItemView BuildIfStruct(IfStatNode node, ScriptItemView view = null)
        {
            ScriptItemView root;
            if (view != null)
            {
                view.display = "#if";
                root = view;
            }
            else
            {
                root = NewScriptItemView("#if");
            }
            var def = NewScriptItemView("default");
            var caseInserter = BuildInserter(BuildCaseStructOverView, "$newCase");
            caseInserter.hint = "新建条件分支";

            def.AddChild(BuildActionInserter());

            root.data = node;
            def.data = node;
            caseInserter.data = node;
            root.AddChild(caseInserter);
            root.AddChild(def);

            def.canRemove = false;
            def.canRename = false;
            caseInserter.canRemove = false;
            caseInserter.canRename = false;
            return root;
        }

        //将view打造成CaseStruct
        public static void BuildCaseStructOverView(ScriptItemView view)
        {
            view.children.Clear();
            view.display = "#case";
            var condition = BuildParameter("condition");
            var stats = NewScriptItemView("stats");

            stats.AddChild(BuildActionInserter());

            view.AddChild(condition);
            view.AddChild(stats);

            condition.data = view.data;
            stats.data = view.data;

            condition.canRemove = false;
            stats.canRemove = false;
            stats.canRename = false;
        }

        public static ScriptItemView BuildParameter(string paramName)
        {
            var root = NewScriptItemView(paramName);
            root.paramName = paramName;
            root.canRemove = false;
            root.canRename = false;
            root.isParameter = true;
            root.onInspector = null;
            root.display = $"{paramName}: null";
            root.onClick = (item, view) =>
            {
                OpenSelectionForm((func) =>
                {
                    if (func == null)
                        return;

                    root.onInspector = null;
                    root.display = $"{paramName}: {func.name}()";
                    InsertFunctionParamsOverNode(root, func);
                    view.Reload();
                }, ParameterTypeInfoes.tany);
            };

            SetParameterAsLiteral(root, "null");

            return root;
        }

        public static void SetParameterAsLiteral(ScriptItemView view, string value)
        {
            view.children.Clear();
            view.data = value;
            view.display = $"{view.paramName}: literal: {value}";
            view.onInspector = (item, treeView) =>
            {
                value = view.data as string;
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.TextField("literal", value);
                if (EditorGUI.EndChangeCheck())
                {
                    view.data = value;
                    view.display = $"{view.paramName}: literal: {value}";
                    view.displayItem.displayName = view.display;
                    treeView.Repaint();
                }
            };
        }


        public static ScriptItemView BuildInserter(Action<ScriptItemView> init = null, string title = null)
        {
            var addItem = NewScriptItemView();
            addItem.canRemove = false;
            addItem.canRename = false;
            addItem.display = title ?? "$...";
            addItem.hint = "新空表达式";
            addItem.onClick += (ScriptItemView item, ScriptTreeView view) =>
            {
                var index = item.parent.children.IndexOf(item);
                var newitem = NewScriptItemView();
                newitem.parent = item.parent;
                item.parent.children.Insert(index, newitem);
                init?.Invoke(newitem);
                view.Reload();
            };

            return addItem;
        }

        public static ScriptItemView BuildInserter(Func<ScriptItemView> getter, string title = null)
        {
            var addItem = NewScriptItemView();
            addItem.canRemove = false;
            addItem.canRename = false;
            addItem.display = title ?? "$...";
            addItem.hint = "新空表达式";
            addItem.onClick += (ScriptItemView item, ScriptTreeView view) =>
            {
                var index = item.parent.children.IndexOf(item);
                var newitem = getter();
                newitem.parent = item.parent;
                item.parent.children.Insert(index, newitem);
                view.Reload();
            };

            return addItem;
        }

        public static ScriptItemView BuildActionInserter(string title = null, Action<ScriptTreeFuncBase> insertee = null)
        {
            var addItem = NewScriptItemView();
            addItem.canRemove = false;
            addItem.canRename = false;
            addItem.display = title ?? "$...";
            addItem.hint = "新表达式";
            addItem.onClick += (ScriptItemView item, ScriptTreeView view) =>
            {
                OpenSelectionForm((f) =>
                {
                    if (f == null)
                        return;

                    var index = item.parent.children.IndexOf(item);
                    var newitem = BuildFunctionNode(f);
                    newitem.parent = item.parent;
                    item.parent.children.Insert(index, newitem);
                    insertee?.Invoke(f);
                    view.Reload();
                }, ParameterTypeInfoes.tany);
            };

            return addItem;
        }


        private static ScriptItemView BuildFunctionNode(ScriptTreeFuncBase func)
        {
            var item = NewScriptItemView(func.name);
            item.canRename = false;
            item.display = "@" + func.name;

            InsertFunctionParamsOverNode(item, func);

            return item;
        }

        private static void InsertFunctionParamsOverNode(ScriptItemView view, ScriptTreeFuncBase func)
        {
            if (view.isParameter)
            {
                view.onInspector = (item, treeView) =>
                {
                    if (GUILayout.Button("使用Literal"))
                    {
                        SetParameterAsLiteral(view, "null");
                        treeView.Reload();
                    } 
                };
            }
            view.children.Clear();
            foreach (var info in func.parameterInfoes)
            {
                view.AddChild(BuildParameter(info.name));
            }
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
    }
}