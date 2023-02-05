using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ScriptTree
{
    public class ScriptTreeState
    {
        public bool completed;
        public object retValue;
        public int statIndex = -1;
        public BaseStatNode curStatNode;
        public List<object> parameters;

        public Dictionary<string, object> data = new Dictionary<string, object>();

        public void Clear()
        {
            completed = false;
            retValue = null;
            data.Clear();
            curStatNode = null;
            statIndex = -1;
            parameters = null;
        }

        public object GetValue(string key)
        {
            data.TryGetValue(key, out object value);
            return value;
        }

        public void SetValue(string key, object value)
        {
            data[key] = value;
        }

        public T CheckOutParameter<T>(int index)
        {
            return (T)parameters[index];
        }

        public object CheckOutParameter(int index)
        {
            return parameters[index];
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
        public abstract string GetValueString();
    }

    public class StringLiteralExpNode : LiteralExpNode
    {
        public string value;
        public override object Execute(ScriptTreeState state)
        {
            return value;
        }

        public override string GetValueString()
        {
            return $"\"{value}\"";
        }
    }

    public class IntLiteralExpNode : LiteralExpNode
    {
        public int value;
        public override object Execute(ScriptTreeState state)
        {
            return value;
        }

        public override string GetValueString()
        {
            return value.ToString();
        }
    }

    public class CallFuncExpNode : BaseExpNode
    {
        public string funcName;
        public List<BaseExpNode> parameters;

        public override object Execute(ScriptTreeState state)
        {
            ScriptTreeFuncBase func = ScriptTreeFunctionManager.GetFunction(funcName);
            List<object> funcParameters = new List<object>();
            parameters.ForEach(exp => funcParameters.Add(exp.Execute(state)));

            ScriptTreeState newState = new ScriptTreeState();
            newState.parameters = funcParameters;
            return func.Execute(newState);
        }
    }


    #endregion


    //扩展方法基础类
    public class ScriptTreeFuncBase
    {
        public string name;
        public List<ParameterInfo> parameterInfoes = new List<ParameterInfo>();
        public ParameterTypeInfo returnType;
        public object Execute()
        {
            return Execute(new ScriptTreeState());
        }

        public virtual object Execute(ScriptTreeState state)
        {
            return null;
        }

        //public T CheckOutParameter<T>(int index)
        //{
        //    return (T)parameters[index];
        //}

        //public object CheckOutParameter(int index)
        //{
        //    return parameters[index];
        //}
    }

    public class HardCodeScriptTreeFunc : ScriptTreeFuncBase
    {
        public Func<ScriptTreeState, ScriptTreeFuncBase, object> func;

        public override object Execute(ScriptTreeState state)
        {
            return func(state, this);
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
    }

    //由ScriptTree结构构成的扩展函数（软编码）
    public class ScriptTreeFunc : ScriptTreeFuncBase
    {
        public string scriptName;
        public BlockStatNode node;
        public override object Execute(ScriptTreeState state)
        {
            for (int i = 0; i < parameterInfoes.Count; i++)
            {
                var info = parameterInfoes[i];
                object value = state.CheckOutParameter(i);
                state.SetValue("@"+info.name, value);
            }

            ScriptTreeInterpreter.ExecuteStat(node, state);

            return state.retValue;
        }
    }

    public static partial class ParameterTypeInfoes
    {
        public static ParameterTypeInfo tvoid, tint, tfloat, tstring, tbool, tVector3, tany;
    }

    public static class ScriptTreeFunctionManager
    {
        public static bool hasInit = false;
        public static Dictionary<string, ScriptTreeFuncBase> m_name2func = new Dictionary<string, ScriptTreeFuncBase>();
        public static Dictionary<string, List<ScriptTreeFuncBase>> m_type2funcs = new Dictionary<string, List<ScriptTreeFuncBase>>();
        public static List<ScriptTreeFuncBase> m_allList = new List<ScriptTreeFuncBase>();
        public static Dictionary<string, ParameterTypeInfo> m_name2type = new Dictionary<string, ParameterTypeInfo>();

        public static ScriptTreeFuncBase GetFunction(string name)
        {
            m_name2func.TryGetValue(name, out var func);
            return func;
        }

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

            RegisterFunction("void SetValue key:string value:any", (state, state2) =>
            {
                state.SetValue(state.CheckOutParameter<string>(0), state.CheckOutParameter(1));
                return null;
            });

            RegisterFunction("any GetValue key:string", (state, state2) =>
            {
                return state.GetValue(state.CheckOutParameter<string>(0));
            });

            RegisterFunction("bool Equal left:any right:any", (state, state2) =>
            {
                return state.CheckOutParameter(0)?.Equals(state.CheckOutParameter(1));
            });

            RegisterFunction("void Debug content:any", (state, state2) =>
            {
                Debug.Log(state.CheckOutParameter(0));
                return null;
            });

            RegisterFunction("void Heal entityId:int healAmount:float", (state, state2) =>
            {
                return null;
            });

            RegisterFunction("int IAdd left:int right:int", (state, state2) =>
            {
                return null;
            });

            RegisterFunction("Vector3 NewVector3 x:float y:float z:float", (state, state2) =>
            {
                return new Vector3(state.CheckOutParameter<float>(0), state.CheckOutParameter<float>(1), state.CheckOutParameter<float>(2));
            });

            RegisterFunction("void Teleport entityId:int position:Vector3", (state, state2) =>
            {
                int id = state.CheckOutParameter<int>(0);
                Vector3 position = state.CheckOutParameter<Vector3>(1);
                Debug.Log($"实体id={id}想要传送到位置={position}");
                return null;
            });

            RegisterFunction("string StringLiteral value:string", (state, state2) =>
            {
                return state.CheckOutParameter<string>(0);
            });
            RegisterFunction("string IntLiteral value:int", (state, state2) =>
            {
                return state.CheckOutParameter<int>(0);
            });
            RegisterFunction("string FloatLiteral value:float", (state, state2) =>
            {
                return state.CheckOutParameter<float>(0);
            });
            RegisterFunction("string BoolLiteral value:bool", (state, state2) =>
            {
                return state.CheckOutParameter<bool>(0);
            });
        }

        public static ScriptTreeFuncBase RegisterFunction(string name, string returnType, List<ParameterInfo> infoes, Func<ScriptTreeState, ScriptTreeFuncBase, object> executeMethod)
        {
            HardCodeScriptTreeFunc func = new HardCodeScriptTreeFunc();
            func.func = executeMethod;
            func.name = name;
            func.returnType = GetParameterType(returnType);
            func.parameterInfoes = infoes;
            m_name2func.Add(name, func);
            m_allList.Add(func);
            InsertFuncToType2Funcs(func);
            return func;
        }

        public static ScriptTreeFuncBase RegisterFunction(string descText, Func<ScriptTreeState, ScriptTreeFuncBase, object> executeMethod)
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

            return RegisterFunction(name, returnType, infoes, executeMethod);
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

        private static List<ScriptTreeFuncBase> GetOrCreateListOfType2Funcs(string typeName)
        {
            if (!m_type2funcs.TryGetValue(typeName, out var list))
            {
                list = new List<ScriptTreeFuncBase>();
                m_type2funcs[typeName] = list;
            }

            return list;
        }

        private static void InsertFuncToType2Funcs(ScriptTreeFuncBase func)
        {
            if (func.returnType == null)
            {
                GetOrCreateListOfType2Funcs("void").Add(func);
                return;
            }
            GetOrCreateListOfType2Funcs(func.returnType.name).Add(func);
        }

        public static List<ScriptTreeFuncBase> GetReturnTypeOf(string name)
        {
            return GetOrCreateListOfType2Funcs(name);
        }
    }

    public static class ScriptTreeInterpreter
    {
        public static ScriptTreeState ExecuteStat(BlockStatNode block)
        {
            ScriptTreeState state = new ScriptTreeState();
            ExecuteStat(block, state);
            return state;
        }

        public static void ExecuteStat(BlockStatNode block, ScriptTreeState state)
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
                    for (int i = 0; i < ifStatNode.cases.Count; i++)
                    {
                        var @case = ifStatNode.cases[i];
                        var condition = @case.condition;
                        var trueblock = @case.block;
                        if ((bool)condition.Execute(state))
                        {
                            ExecuteStat(trueblock, state);
                            if (state.completed)
                            {
                                return;
                            }
                        }
                        else if (ifStatNode.defaultBlock != null)
                        {
                            ExecuteStat(ifStatNode.defaultBlock, state);
                            if (state.completed)
                            {
                                return;
                            }
                        }
                    }
                }
                else if (root is CallFuncStatNode callFuncStatNode)
                {
                    var exp = callFuncStatNode.exp;
                    exp.Execute(state);
                }
            }

            state.completed = true;
        }
    }
}