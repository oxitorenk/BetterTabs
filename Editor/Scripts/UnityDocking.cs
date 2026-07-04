using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetterTabs.Editor
{
    internal static class UnityDocking
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type _dockAreaType;
        private static PropertyInfo _rootProperty;
        private static MethodInfo _addTabMethod;

        internal static bool IsSupported { get; private set; }

        internal static void Initialize(Assembly editorAssembly)
        {
            _dockAreaType = editorAssembly.GetType("UnityEditor.DockArea");
            if (_dockAreaType != null)
            {
                _rootProperty = _dockAreaType.GetProperty("visualTree", InstanceFlags);
                _addTabMethod = _dockAreaType.GetMethod("AddTab", InstanceFlags, null, 
                    new[] { typeof(EditorWindow), typeof(bool) }, null);
            }

            IsSupported = _dockAreaType != null && _rootProperty != null && _addTabMethod != null;
            if (!IsSupported)
            {
                Debug.LogError("[BetterTabs] Unity DockArea API is unavailable. Tab drops are disabled.");
            }
        }

        internal static IEnumerable<VisualElement> GetRoots()
        {
            foreach (var dock in Resources.FindObjectsOfTypeAll(_dockAreaType))
            {
                if (dock is not ScriptableObject dockObject) continue;
                if (_rootProperty.GetValue(dockObject) is not VisualElement root) continue;

                yield return root;
            }
        }

        internal static object FindDockArea(VisualElement element)
        {
            if (element == null) return null;

            foreach (var dock in Resources.FindObjectsOfTypeAll(_dockAreaType))
            {
                if (dock is not ScriptableObject dockObject) continue;
                if (_rootProperty.GetValue(dockObject) is not VisualElement root) continue;
                if (root != element && !root.Contains(element)) continue;

                return dock;
            }

            return null;
        }

        internal static void AddTab(object dockArea, EditorWindow window)
        {
            _addTabMethod.Invoke(dockArea, new object[] { window, true });
        }
    }
}