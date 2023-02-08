using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class InputContentFormWindow : EditorWindow
{
    private string m_title;
    private string m_content;
    private Action<string> m_action;

    public static void OpenWindow(string title, Action<string> callback, string content = null)
    {
        var window = CreateInstance<InputContentFormWindow>();
        window.Show();
        window.m_action = callback;
        window.m_title = title;
        window.m_content = content ?? string.Empty;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(m_title);
        m_content = EditorGUILayout.TextArea(m_content, GUILayout.Height(position.height - 48), GUILayout.ExpandWidth(true));
        if (GUILayout.Button("提交", GUILayout.Height(32)))
        {
            if (m_action != null)
            {
                m_action(m_content);
            }
            Close();
        }
        EditorGUILayout.EndVertical();
    }

    private void OnLostFocus()
    {
        Close();
    }

}
