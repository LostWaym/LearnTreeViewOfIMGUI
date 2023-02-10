using ScriptTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptTreeAsset", menuName = "ScriptTree/ScriptTreeAsset")]
public class ScriptTreeAsset : ScriptableObject
{
    [Serializable]
    public class ParameterInfo
    {
        public string name;
        public string type;
    }
    public string json;
    public string scriptName;
    public string desc;
    public string returnType;
    public bool canCallSingle;
    [HideInInspector][SerializeField]
    public List<ParameterInfo> parameters;

    private ScriptTreeFunc func;

    public ScriptTreeFunc GetFunc(bool force = false)
    {
        if (func != null && !force)
        {
            return func;
        }

        func = new ScriptTreeFunc();
        var info = func.info;
        info.desc = desc;
        info.name = scriptName;
        info.returnType = ScriptTreeFunctionManager.GetParameterType(returnType);
        info.canCallSingle = canCallSingle;
        info.parameterInfoes = new List<ScriptTrees.ParameterInfo>();
        for (int i = 0; i < parameters.Count; i++)
        {
            ParameterInfo pi = parameters[i];
            info.parameterInfoes.Add(new ScriptTrees.ParameterInfo()
            {
                name = pi.name,
                index = i,
                type = ScriptTreeFunctionManager.GetParameterType(pi.type)
            });
        }

        return func;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ScriptTreeAsset))]
public class ScriptTreeAssetEditor : Editor
{
    ScriptTreeAsset asset => (ScriptTreeAsset)target;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

#endif
