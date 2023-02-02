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

        public void Clear()
        {
            completed = false;
            retValue = null;
        }

        public object GetValue(string key)
        {
            return null;
        }

        public void SetValue(string key, object value)
        {

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

    public class AssignStatNode : BaseStatNode
    {
        public BaseExpNode keyExp;
        public BaseExpNode valueExp;
    }

    public class IfStatNode : BaseStatNode
    {
        public List<IfCaseData> cases;
        public List<BaseExpNode> defaultBlock;
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

    public class LiteralExpNode : BaseExpNode
    {
        public string value;
        public override object Execute(ScriptTreeState state)
        {
            return value;
        }
    }

    public class IntLiteralExpNode : BaseExpNode
    {
        public int value;
        public override object Execute(ScriptTreeState state)
        {
            return value;
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
            func.parameters = funcParameters;
            return func.Execute();
        }
    }

    public class GetVariableExpNode : BaseExpNode
    {
        public BaseExpNode keyExp;

        public override object Execute(ScriptTreeState state)
        {
            return state.GetValue(keyExp.Execute(state) as string);
        }
    }


    #endregion



    public class ScriptTreeFuncBase
    {
        public List<object> parameters;
        public object Execute()
        {
            return Execute(new ScriptTreeState());
        }

        public virtual object Execute(ScriptTreeState state)
        {
            return null;
        }
    }

    public class ScriptTreeFunc : ScriptTreeFuncBase
    {
        public string scriptName;
        public BlockStatNode node;
        public List<string> parametersName = new List<string>();
        public override object Execute(ScriptTreeState state)
        {
            Assert.AreEqual(parameters.Count, parameters.Count, $"{scriptName}方法参数数量对不上！");

            for (int i = 0; i < parametersName.Count; i++)
            {
                string key = parametersName[i];
                object value = parameters[i];
                state.SetValue(key, value);
            }

            ScriptTreeInterpreter.ExecuteStat(node, state);

            return state.retValue;
        }
    }

    public static class ScriptTreeFunctionManager
    {
        public static ScriptTreeFuncBase GetFunction(string name)
        {
            return null;
        }
    }

    namespace Funcs
    {
        public class DebugLog : ScriptTreeFuncBase
        {
            public override object Execute(ScriptTreeState state)
            {
                Debug.Log(parameters[0]);
                return null;
            }
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

                if (root is AssignStatNode assignStatNode)
                {
                    state.SetValue(assignStatNode.keyExp.Execute(state) as string, assignStatNode.valueExp.Execute(state));
                }
                else if (root is ReturnStatNode returnStatNode)
                {
                    state.completed = true;
                    state.retValue = returnStatNode.exp.Execute(state);
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
                            ExecuteStat(trueblock, state);
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