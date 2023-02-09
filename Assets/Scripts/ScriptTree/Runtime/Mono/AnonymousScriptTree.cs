using ScriptTrees;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnonymousScriptTree : MonoBehaviour
{
    [HideInInspector][SerializeField]
    public string json = "[]";
    public string title;
    public BlockStatNode node;
    public ScriptTreeFunc func;

    private void CheckAndInitNode()
    {
        if (node != null)
            return;

        node = ScriptTreeSerializer.ToBlock(json);
    }
    private BlockStatNode GetNode()
    {
        CheckAndInitNode();
        return node;
    }

    public ScriptTree GetScriptTree()
    {
        if (func != null)
            return func;

        func = new ScriptTreeFunc();
        func.name = title ?? "AnonymousScriptTree";
        func.returnType = ParameterTypeInfoes.tvoid;
        func.parameterInfoes = new List<ParameterInfo>();
        func.canCallSingle = true;
        func.desc = "";
        func.node = GetNode();

        return func;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
