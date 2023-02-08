using ScriptTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ScriptTreeSerializer
{
    private static JSONObject NewJsonObject => new JSONObject(JSONObject.Type.OBJECT);
    private static JSONObject NewJsonArray => new JSONObject(JSONObject.Type.ARRAY);

    public static string ToJson(BlockStatNode node)
    {
        JSONObject jsonObject = BuildBlockStatNodeObject(node);
        return jsonObject.ToString(true);
    }

    private static JSONObject BuildBlockStatNodeObject(BlockStatNode node)
    {
        JSONObject jsonArray = NewJsonArray;

        if (node.children != null)
        {
            foreach (var item in node.children)
            {
                if (item is IfStatNode ifStatNode)
                {
                    jsonArray.Add(BuildIfStatNodeObject(ifStatNode));
                }
                else if (item is CallFuncStatNode callFuncStatNode)
                {
                    jsonArray.Add(BuildCallFuncStatNodeObject(callFuncStatNode));
                }
            }
        }

        return jsonArray;
    }

    private static JSONObject BuildIfStatNodeObject(IfStatNode node)
    {
        JSONObject root = NewJsonObject;
        JSONObject cases = NewJsonObject;
        JSONObject defaultBlock = BuildBlockStatNodeObject(node.defaultBlock);

        root.AddField("type", "if");
        root.AddField("cases", cases);
        root.AddField("default", defaultBlock);

        foreach (var item in node.cases)
        {
            var ifCase = NewJsonObject;
            var condition = BuildParameterObject(item.condition);
            var stats = BuildBlockStatNodeObject(item.block);
            ifCase.AddField("condition", condition);
            ifCase.AddField("stats", stats);
            cases.Add(ifCase);
        }

        return root;
    }

    private static JSONObject BuildCallFuncStatNodeObject(CallFuncStatNode node)
    {
        JSONObject root = NewJsonObject;
        JSONObject parameters = NewJsonArray;

        root.AddField("type", "CallFunction");
        root.AddField("funcName", node.exp.funcName);
        root.AddField("parameters", parameters);

        foreach (var item in node.exp.parameters)
        {
            parameters.Add(BuildParameterObject(item));
        }

        return root;
    }

    private static JSONObject BuildParameterObject(BaseExpNode exp)
    {
        JSONObject root = NewJsonObject;
        string type = "unknown";

        if (exp is LiteralExpNode)
        {
            if (exp is StringLiteralExpNode stringNode)
            {
                type = "StringLiteral";
                root.AddField("value", stringNode.value);
            }
            else if (exp is IntLiteralExpNode intNode)
            {
                type = "IntLiteral";
                root.AddField("value", intNode.value);
            }
            else if (exp is FloatLiteralExpNode floatNode)
            {
                type = "FloatLiteral";
                root.AddField("value", floatNode.value);
            }
            else if (exp is BoolLiteralExpNode boolNode)
            {
                type = "BoolLiteral";
                root.AddField("value", boolNode.value);
            }
            else
            {
                throw new Exception($"Unsupported literal type of {exp.GetType().FullName}");
            }
        }
        else if (exp is CallFuncExpNode funcNode)
        {
            type = "CallFunction";
            root.AddField("funcName", funcNode.funcName);
            JSONObject parameters = NewJsonArray;
            root.AddField("parameters", parameters);
            foreach (var item in funcNode.parameters)
            {
                parameters.Add(BuildParameterObject(item));
            }
        }
        else if (exp == null)
        {
            type = "null";
        }
        root.AddField("type", type);

        return root;
    }

    private static string GetType(JSONObject jo)
    {
        return jo.GetField("type").str;
    }
    private static void GetType(JSONObject jo, out string type)
    {
        type = jo.GetField("type").str;
    }

    public static BlockStatNode ToBlock(string json)
    {
        JSONObject jsonObject = new JSONObject(json);
        BlockStatNode node = BuildBlock(jsonObject);
        return node;
    }

    public static BlockStatNode BuildBlock(JSONObject root)
    {
        BlockStatNode rootNode = new BlockStatNode();
        rootNode.children = new List<BaseStatNode>();
        if (root.list != null)
        {
            foreach (var item in root.list)
            {
                var type = GetType(item);
                if (type == "if")
                {
                    IfStatNode ifStatNode = new IfStatNode();
                    var cases = item.GetField("cases");
                    var defaultBlock = item.GetField("default");
                    ifStatNode.cases = new List<IfCaseData>();

                    foreach (var caseItem in cases.list)
                    {
                        IfCaseData ifCaseData = new IfCaseData();
                        var condition = caseItem.GetField("condition");
                        var stats = caseItem.GetField("stats");
                        ifCaseData.condition = BuildParameter(condition);
                        ifCaseData.block = BuildBlock(stats);
                        ifStatNode.cases.Add(ifCaseData);
                    }
                    ifStatNode.defaultBlock = BuildBlock(defaultBlock);
                    rootNode.children.Add(ifStatNode);
                }
                else if (type == "CallFunction")
                {
                    CallFuncStatNode statNode = new CallFuncStatNode();
                    statNode.exp = BuildCallFuncExpNode(item);
                    rootNode.children.Add(statNode);
                }
                else
                {
                    throw new Exception($"Unsupported statement type of {type}");
                }
            }
        }

        return rootNode;
    }

    public static BaseExpNode BuildParameter(JSONObject root)
    {
        BaseExpNode ret = null;

        GetType(root, out var type);
        if (type == "StringLiteral")
        {
            StringLiteralExpNode node = new StringLiteralExpNode();
            root.GetField(ref node.value, "value");
            ret = node;
        }
        else if (type == "IntLiteral")
        {
            IntLiteralExpNode node = new IntLiteralExpNode();
            root.GetField(ref node.value, "value");
            ret = node;
        }
        else if (type == "FloatLiteral")
        {
            FloatLiteralExpNode node = new FloatLiteralExpNode();
            root.GetField(ref node.value, "value");
            ret = node;
        }
        else if (type == "BoolLiteral")
        {
            BoolLiteralExpNode node = new BoolLiteralExpNode();
            root.GetField(ref node.value, "value");
            ret = node;
        }
        else if (type == "CallFunction")
        {
            ret = BuildCallFuncExpNode(root);
        }
        else if (type == "null")
        {
            return null;
        }
        else
        {
            throw new Exception($"Unsupported parameter type of {type}");
        }

        return ret;
    }

    public static CallFuncExpNode BuildCallFuncExpNode(JSONObject root)
    {
        CallFuncExpNode node = new CallFuncExpNode();
        root.GetField(ref node.funcName, "funcName");
        node.parameters = new List<BaseExpNode>();
        foreach (var item in root.GetField("parameters").list)
        {
            var param = BuildParameter(item);
            node.parameters.Add(param);
        }
        return node;
    }
}