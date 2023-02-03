using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectionFormWindow : EditorWindow
{
    private List<string> m_items;
    private Action<int> m_callback;
    private string m_label = null;
    private int selectedIndex;

    public static SelectionFormWindow OpenWindow(List<string> items, Action<int> callback, string label = null)
    {
        var window = GetWindow<SelectionFormWindow>();
        window.m_items = items;
        window.m_callback = callback;
        window.m_label = label;
        window.selectedIndex = -1;

        return window;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(m_label);
        for (int i = 0; i < m_items.Count; i++)
        {
            var e = m_items[i];
            if (GUILayout.Button(e))
            {
                selectedIndex = i;
                Close();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void OnLostFocus()
    {
        Close();
    }

    private void OnDestroy()
    {
        m_callback(selectedIndex);
    }
}
