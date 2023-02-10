using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace ScriptTrees
{
    public class ScriptTreeState
    {
        public const string DEFAULT_BLOCK_NAME = "unknownBlock";

        public string title = DEFAULT_BLOCK_NAME;
        public bool completed;
        public object retValue;
        public int statIndex = -1;
        public BaseStatNode curStatNode;
        public List<object> parameters;

        public Dictionary<string, object> data = new Dictionary<string, object>();
        public ScriptTreeState parent; 

        public void Clear()
        {
            title = DEFAULT_BLOCK_NAME;
            completed = false;
            retValue = null;
            data.Clear();
            curStatNode = null;
            statIndex = -1;
            parameters = null;
            parent = null;
        }

        public object GetValue(string key)
        {
            if (!data.TryGetValue(key, out object value) && parent != null)
            {
                return parent.GetValue(key);
            }
            return value;
        }

        public void SetValue(string key, object value)
        {
            data[key] = value;
        }

        public T CheckOutParameter<T>(int index)
        {
            object value = CheckOutParameter(index);
            if (!(value is T))
            {
                PrintCurrent($"没有为参数index={index}找到对应的类型，期望{typeof(T).Name}，实际上为{(value == null ? "null" : value.GetType().Name)}", true);
                return default;
            }
            return (T)value;
        }

        public object CheckOutParameter(int index)
        {
            return parameters.Count > index ? parameters[index] : null;
        }

        public void GetPrintContent(StringBuilder sb, int depth = 1)
        {
            sb.AppendLine($"{depth} - <{title}> - statIndex: {statIndex} - node:{curStatNode?.GetType().Name ?? "Unknown"}#{(curStatNode == null ? -1 : curStatNode.id)}");
        }

        public void PrintCurrent(string extraContent, bool isError = false, StringBuilder sb = null, int depth = 1)
        {
            sb = sb ?? new StringBuilder();

            if (!string.IsNullOrEmpty(extraContent))
            {
                sb.AppendLine($"##{extraContent}");
            }

            GetPrintContent(sb);

            var p = parent;
            var depth2 = depth;
            while (parent != null)
            {
                parent.GetPrintContent(sb, ++depth2);
                parent = parent.parent;
            }

            if (isError)
            {
                Debug.LogError(sb.ToString());
            }
            else
            {
                Debug.Log(sb.ToString());
            }
        }
    }

    public class BaseNode
    {
        public int id;
    }

    #region 语句
    public class BaseStatNode : BaseNode
    {
        public string comment = string.Empty;
    }

    public class BlockStatNode : BaseStatNode
    {
        public List<BaseStatNode> children;
    }

    public class IfStatNode : BaseStatNode
    {
        public List<IfCaseData> cases;
        public BlockStatNode defaultBlock;
    }

    public class IfCaseData
    {
        public BaseExpNode condition;
        public BlockStatNode block;
    }

    public class CallFuncStatNode : BaseStatNode
    {
        public CallFuncExpNode exp;
    }

    public class ReturnStatNode : BaseStatNode
    {
        public BaseExpNode exp;
    }

    #endregion

    #region 表达式

    public class BaseExpNode : BaseNode
    {
        public virtual object Execute(ScriptTreeState state)
        {
            return null;
        }
    }

    public abstract class LiteralExpNode : BaseExpNode
    {
    }

    public abstract class LiteralExpNode<T> : LiteralExpNode
    {
        public T value;
        public override object Execute(ScriptTreeState state)
        {
            return value;
        }
    }

    public class StringLiteralExpNode : LiteralExpNode<string>
    {
    }

    public class IntLiteralExpNode : LiteralExpNode<int>
    {
    }

    public class FloatLiteralExpNode : LiteralExpNode<float>
    {
    }
    public class BoolLiteralExpNode : LiteralExpNode<bool>
    {
    }

    public class GetVariableExpNode : BaseExpNode
    {
        public string key;
        public override object Execute(ScriptTreeState state)
        {
            return state.GetValue(key);
        }
    }

    public class CallFuncExpNode : BaseExpNode
    {
        public string funcName;
        public List<BaseExpNode> parameters;

        public override object Execute(ScriptTreeState state)
        {
            ScriptTree func = ScriptTreeFunctionManager.GetFunction(funcName);
            List<object> funcParameters = new List<object>();
            for (int i = 0; i < parameters.Count; i++)
            {
                var exp = parameters[i];
                var value = exp?.Execute(state) ?? func.ParameterInfoes[i].type.getDefaultValue();
                funcParameters.Add(value);
            }

            ScriptTreeState newState = new ScriptTreeState();
            newState.parent = state;
            newState.parameters = funcParameters;
            newState.title = "@" + funcName;
            return func.Execute(newState);
        }
    }

    public abstract class ValueContainer
    {
        ParameterTypeInfo info;
    }

    public class ValueContainer<T> : ValueContainer
    {
        public T value;

        public static implicit operator T(ValueContainer<T> self)
        {
            return self.value;
        }
    }

    public static class ValueContainerHelper
    {
        public static ValueContainer<T> GetValueContainer<T>(T value)
        {
            var container = new ValueContainer<T>();
            container.value = value;

            return container;
        }
    }


    #endregion


    //脚本树方法基础类
    public class ScriptTree
    {
        public ScriptTreeInfo info = new ScriptTreeInfo();
        public string Name
        {
            get { return info.name; }
            set { info.name = value; }
        }
        public string Desc
        {
            get { return info.desc; }
            set { info.desc = value; }
        }
        public bool CanCallSingle
        {
            get { return info.canCallSingle; }
            set { info.canCallSingle = value;}
        }
        public List<ParameterInfo> ParameterInfoes
        {
            get { return info.parameterInfoes; }
            set { info.parameterInfoes = value;}
        }
        public ParameterTypeInfo ReturnType
        {
            get { return info.returnType; }
            set { info.returnType = value; }
        }


        public virtual object Execute(ScriptTreeState state)
        {
            return null;
        }
    }

    public class ScriptTreeInfo
    {
        public string name;
        public string desc;
        public bool canCallSingle;
        public List<ParameterInfo> parameterInfoes = new List<ParameterInfo>();
        public ParameterTypeInfo returnType;
    }

    //由c#构成的扩展函数（硬编码）
    public class HardCodeScriptTreeFunc : ScriptTree
    {
        public Func<ScriptTreeState, ScriptTree, object> func;

        public override object Execute(ScriptTreeState state)
        {
            return func(state, this);
        }
    }

    //由ScriptTree结构构成的扩展函数（软编码）
    public class ScriptTreeFunc : ScriptTree
    {
        public BlockStatNode node;
        public override object Execute(ScriptTreeState state)
        {
            for (int i = 0; i < ParameterInfoes.Count; i++)
            {
                var info = ParameterInfoes[i];
                object value = state.CheckOutParameter(i);
                state.SetValue("@" + info.name, value);
            }

            ScriptTreeInterpreter.ExecuteStat(node, state);

            return state.retValue;
        }
    }

    public class ParameterInfo
    {
        public string name;
        public int index;
        public ParameterTypeInfo type;

        public static ParameterInfo Build(string name, int index, ParameterTypeInfo type)
        {
            return new ParameterInfo()
            {
                name = name,
                index = index,
                type = type,
            };
        }
    }

    public class ParameterTypeInfo
    {
        public string name;
        public bool canBeLiteral;
        public Func<object> getDefaultValue;
        public Type literalClass;

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ParameterTypeInfo other)
            {
                return this == other;
            }

            return false;
        }

        public static bool operator ==(ParameterTypeInfo self, ParameterTypeInfo other)
        {
            return self?.name == other?.name;
        }

        public static bool operator !=(ParameterTypeInfo self, ParameterTypeInfo other)
        {
            return self?.name != other?.name;
        }
    }

    public static partial class ParameterTypeInfoes
    {
        public static ParameterTypeInfo tvoid, tint, tfloat, tstring, tbool, tVector3, tany;
    }

    public static class ScriptTreeFunctionManager
    {
        public static bool hasInit = false;
        public static Dictionary<string, ScriptTree> m_name2func = new Dictionary<string, ScriptTree>();
        public static Dictionary<string, List<ScriptTree>> m_type2funcs = new Dictionary<string, List<ScriptTree>>();
        public static List<ScriptTree> m_allList = new List<ScriptTree>();
        public static List<ScriptTree> m_allReturnList = new List<ScriptTree>();
        public static Dictionary<string, ParameterTypeInfo> m_name2type = new Dictionary<string, ParameterTypeInfo>();

        public static ScriptTree GetFunction(string name)
        {
            m_name2func.TryGetValue(name, out var func);
            return func;
        }

        public static event Action OnInit;
        public static void InitDefaultTypeAndFunc()
        {
            if (hasInit)
                return;
            hasInit = true;
            ParameterTypeInfoes.tvoid = RegisterParameterType("void", () => null);
            ParameterTypeInfoes.tany = RegisterParameterType("any", () => null);
            ParameterTypeInfoes.tint = RegisterParameterType("int", () => 0, true);
            ParameterTypeInfoes.tfloat = RegisterParameterType("float", () => 0f, true);
            ParameterTypeInfoes.tstring = RegisterParameterType("string", () => string.Empty, true);
            ParameterTypeInfoes.tbool = RegisterParameterType("bool", () => false, true);
            ParameterTypeInfoes.tVector3 = RegisterParameterType("Vector3", () => Vector3.zero);

            BindLiteralClass<StringLiteralExpNode>(ParameterTypeInfoes.tstring);
            BindLiteralClass<IntLiteralExpNode>(ParameterTypeInfoes.tint);
            BindLiteralClass<FloatLiteralExpNode>(ParameterTypeInfoes.tfloat);
            BindLiteralClass<BoolLiteralExpNode>(ParameterTypeInfoes.tbool);

            RegisterFunction("void SetValue key:string value:any", true, (state, state2) =>
            {
                state.parent?.SetValue(state.CheckOutParameter<string>(0), state.CheckOutParameter(1));
                return null;
            });
            previousRegistee.Desc = "设置临时变量";

            RegisterFunction("any GetValue key:string", false, (state, state2) =>
            {
                return state.parent?.GetValue(state.CheckOutParameter<string>(0));
            });
            previousRegistee.Desc = "获取临时变量";

            RegisterFunction("bool Equal left:any right:any", false, (state, state2) =>
            {
                return state.CheckOutParameter(0)?.Equals(state.CheckOutParameter(1));
            });
            previousRegistee.Desc = "判断两个值是否相等";

            RegisterFunction("void Debug content:any", true, (state, state2) =>
            {
                Debug.Log(state.CheckOutParameter(0));
                return null;
            });
            previousRegistee.Desc = "打印一条调试信息";

            RegisterFunction("void Heal entityId:int healAmount:float", true, (state, state2) =>
            {
                return null;
            });
            previousRegistee.Desc = "治疗实体";

            RegisterFunction("bool And left:bool right:bool", false, (state, state2) =>
            {
                return state.CheckOutParameter<bool>(0) && state.CheckOutParameter<bool>(1);
            });
            previousRegistee.Desc = "逻辑且";

            RegisterFunction("bool Or left:bool right:bool", false, (state, state2) =>
            {
                return state.CheckOutParameter<bool>(0) || state.CheckOutParameter<bool>(1);
            });
            previousRegistee.Desc = "逻辑或";

            RegisterFunction("int IAdd left:int right:int", false, (state, state2) =>
            {
                return state.CheckOutParameter<int>(0) + state.CheckOutParameter<int>(1);
            });
            previousRegistee.Desc = "整数相加";

            RegisterFunction("float FAdd left:float right:float", false, (state, state2) =>
            {
                return state.CheckOutParameter<float>(0) + state.CheckOutParameter<float>(1);
            });
            previousRegistee.Desc = "浮点数相加";

            RegisterFunction("float Int2Float value:int", false, (state, state2) =>
            {
                return (float)state.CheckOutParameter<int>(0);
            });
            previousRegistee.Desc = "整数转浮点数";

            RegisterFunction("int Float2Int value:float", false, (state, state2) =>
            {
                return (int)state.CheckOutParameter<float>(0);
            });
            previousRegistee.Desc = "浮点数转整数";


            RegisterFunction("Vector3 NewVector3 x:float y:float z:float", false, (state, state2) =>
            {
                return new Vector3(state.CheckOutParameter<float>(0), state.CheckOutParameter<float>(1), state.CheckOutParameter<float>(2));
            });
            previousRegistee.Desc = "新建三维向量";

            RegisterFunction("void Teleport entityId:int position:Vector3", true, (state, state2) =>
            {
                int id = state.CheckOutParameter<int>(0);
                Vector3 position = state.CheckOutParameter<Vector3>(1);
                Debug.Log($"实体id={id}想要传送到位置={position}");
                return null;
            });
            previousRegistee.Desc = "传送实体";

            RegisterFunction("string StringLiteral value:string", false, (state, state2) =>
            {
                return state.CheckOutParameter<string>(0);
            });
            previousRegistee.Desc = "字符串字面量";
            RegisterFunction("int IntLiteral value:int", false, (state, state2) =>
            {
                return state.CheckOutParameter<int>(0);
            });
            previousRegistee.Desc = "整数字面量";
            RegisterFunction("float FloatLiteral value:float", false, (state, state2) =>
            {
                return state.CheckOutParameter<float>(0);
            });
            previousRegistee.Desc = "浮点数字面量";
            RegisterFunction("bool BoolLiteral value:bool", false, (state, state2) =>
            {
                return state.CheckOutParameter<bool>(0);
            });
            previousRegistee.Desc = "布尔值字面量";

            OnInit?.Invoke();
        }

        private static Dictionary<Type, ParameterTypeInfo> m_type2info = new Dictionary<Type, ParameterTypeInfo>();
        public static void BindLiteralClass<T>(ParameterTypeInfo info)
        {
            m_type2info[typeof(T)] = info;
            info.literalClass = typeof(T);
        }

        public static ParameterTypeInfo GetLiteralTypeInfo(Type type)
        {
            m_type2info.TryGetValue(type, out ParameterTypeInfo info);
            return info;
        }

        private static ScriptTree previousRegistee;
        public static ScriptTree RegisterFunction(string name, string returnType, bool canCallSingle, List<ParameterInfo> infoes, Func<ScriptTreeState, ScriptTree, object> executeMethod)
        {
            HardCodeScriptTreeFunc func = new HardCodeScriptTreeFunc();
            func.func = executeMethod;
            func.Name = name;
            func.ReturnType = GetParameterType(returnType);
            func.ParameterInfoes = infoes;
            func.CanCallSingle = canCallSingle;
            m_name2func.Add(name, func);
            m_allList.Add(func);
            if (returnType != ParameterTypeInfoes.tvoid.name)
            {
                m_allReturnList.Add(func);
            }
            InsertFuncToType2Funcs(func);
            previousRegistee = func;
            return func;
        }

        public static ScriptTree RegisterFunction(string descText, bool canCallSingle, Func<ScriptTreeState, ScriptTree, object> executeMethod)
        {
            string[] splits = descText.Split(' ');
            string returnType = splits[0];
            string name = splits[1];
            List<ParameterInfo> infoes = new List<ParameterInfo>();
            for (int i = 2; i < splits.Length; i++)
            {
                var paramInfo = splits[i].Split(':');
                var paramName = paramInfo[0];
                var paramType = paramInfo.Length > 1 ? paramInfo[1] : "any";
                infoes.Add(ParameterInfo.Build(paramName, i - 2, GetParameterType(paramType)));
            }

            return RegisterFunction(name, returnType, canCallSingle, infoes, executeMethod);
        }

        public static ParameterTypeInfo RegisterParameterType(string name, Func<object> defValue, bool canBeLiteral = false)
        {
            ParameterTypeInfo info = new ParameterTypeInfo();
            info.name = name;
            info.canBeLiteral = canBeLiteral;
            info.getDefaultValue = defValue;
            m_name2type[name] = info;
            return info;
        }

        public static ParameterTypeInfo GetParameterType(string name)
        {
            return m_name2type[name];
        }

        private static List<ScriptTree> GetOrCreateListOfType2Funcs(string typeName)
        {
            if (!m_type2funcs.TryGetValue(typeName, out var list))
            {
                list = new List<ScriptTree>();
                m_type2funcs[typeName] = list;
            }

            return list;
        }

        private static void InsertFuncToType2Funcs(ScriptTree func)
        {
            if (func.ReturnType == null)
            {
                GetOrCreateListOfType2Funcs("void").Add(func);
                return;
            }
            GetOrCreateListOfType2Funcs(func.ReturnType.name).Add(func);
        }

        public static List<ScriptTree> GetReturnTypeOf(string name)
        {
            return GetOrCreateListOfType2Funcs(name);
        }
    }

    public static class ScriptTreeInterpreter
    {
        public static ScriptTreeState ExecuteTree(ScriptTree tree)
        {
            var state = new ScriptTreeState();
            tree.Execute(state);
            return state;
        }
        public static void ExecuteTree(ScriptTree tree, ScriptTreeState state)
        {
            tree.Execute(state);
        }

        public static ScriptTreeState ExecuteStat(BlockStatNode block)
        {
            ScriptTreeState state = new ScriptTreeState();
            ExecuteStat(block, state);
            return state;
        }

        public static void ExecuteStat(BlockStatNode block, ScriptTreeState state, bool isInline = true)
        {
            var rootContainer = block.children;
            foreach (var root in rootContainer)
            {
                state.statIndex++;
                state.curStatNode = root;

                if (root is ReturnStatNode returnStatNode)
                {
                    state.completed = true;
                    state.retValue = returnStatNode.exp.Execute(state);
                    return;
                }
                else if (root is IfStatNode ifStatNode)
                {
                    bool executed = false;
                    for (int i = 0; i < ifStatNode.cases.Count; i++)
                    {
                        var @case = ifStatNode.cases[i];
                        var condition = @case.condition;
                        var trueblock = @case.block;
                        var value = condition.Execute(state);
                        if (value is bool b && b)
                        {
                            executed = true;
                            ExecuteStat(trueblock, state);
                            if (state.completed)
                            {
                                return;
                            }
                        }
                    }

                    if (!executed && ifStatNode.defaultBlock != null)
                    {
                        ExecuteStat(ifStatNode.defaultBlock, state);
                        if (state.completed)
                        {
                            return;
                        }
                    }
                }
                else if (root is CallFuncStatNode callFuncStatNode)
                {
                    var exp = callFuncStatNode.exp;
                    exp.Execute(state);
                }
            }

            if (!isInline)
            {
                state.completed = true;
            }
        }
    }
}