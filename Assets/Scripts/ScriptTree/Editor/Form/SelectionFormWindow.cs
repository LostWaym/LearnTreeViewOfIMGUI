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

    private Vector2 m_scroll;

    public static SelectionFormWindow OpenWindow(List<string> items, Action<int> callback, string label = null)
    {
        var window = CreateInstance<SelectionFormWindow>();
        window.m_items = items;
        window.m_callback = callback;
        window.m_label = label;
        window.selectedIndex = -1;

        window.Show();

        return window;
    }

    private void OnGUI()
    {
        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
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
        EditorGUILayout.EndScrollView();
    }

    private void OnLostFocus()
    {
        Close();
    }

    private void OnDestroy()
    {
        m_callback?.Invoke(selectedIndex);
    }
}
