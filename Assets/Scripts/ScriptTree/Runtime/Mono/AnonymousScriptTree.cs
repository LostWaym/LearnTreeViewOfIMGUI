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
    public ScriptTreeInfo info = new ScriptTreeInfo();
    public ScriptTreeAsset asset;

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

        func = new ScriptTreeFunc
        {
            Name = title ?? "AnonymousScriptTree",
            ReturnType = ParameterTypeInfoes.tvoid,
            ParameterInfoes = new List<ParameterInfo>(),
            CanCallSingle = true,
            Desc = "",
            node = GetNode(),
            info = info
        };

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
