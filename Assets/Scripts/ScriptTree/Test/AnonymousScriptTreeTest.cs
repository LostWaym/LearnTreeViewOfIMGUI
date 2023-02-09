﻿using ScriptTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnonymousScriptTreeTest : MonoBehaviour
{
    public Button m_button;
    public AnonymousScriptTree m_tree;
    public InputField m_field;
    public int passValue;

    // Start is called before the first frame update
    void Start()
    {
        m_button.onClick.AddListener(() =>
        {
            var node = m_tree.GetNode();
            ScriptTreeState state = new ScriptTreeState();
            state.title = "测试匿名树";
            state.SetValue("@outer", m_field.text);
            state.SetValue("@vec3", new Vector3(1.23f, -2.25f, 6694.2f));
            state.SetValue("@condition", passValue);
            ScriptTreeInterpreter.ExecuteStat(node, state);
            Debug.Log($"返回内容{state.retValue}");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
