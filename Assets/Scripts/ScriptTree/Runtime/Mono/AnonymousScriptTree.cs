using ScriptTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnonymousScriptTree : MonoBehaviour
{
    [HideInInspector][SerializeField]
    public string json = "[]";
    public BlockStatNode node;

    private void CheckAndInitNode()
    {
        if (node != null)
            return;

        node = ScriptTreeSerializer.ToBlock(json);
    }
    public BlockStatNode GetNode()
    {
        CheckAndInitNode();
        return node;
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
