using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterTabs.Editor
{
    internal static class InspectorTabs
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type _windowType;
        private static MethodInfo _setObjectsLockedMethod;
        private static MethodInfo _refreshTitleMethod;
        private static PropertyInfo _isLockedProperty;

        internal static bool IsSupported { get; private set; }

        internal static void Initialize(Assembly editorAssembly)
        {
            _windowType = editorAssembly.GetType("UnityEditor.InspectorWindow");
            if (_windowType == null)
            {
                Debug.LogError("[BetterTabs] Unity InspectorWindow API is unavailable. Inspector tabs are disabled.");
                return;
            }

            _setObjectsLockedMethod = _windowType.GetMethod("SetObjectsLocked", InstanceFlags);
            _refreshTitleMethod = _windowType.GetMethod("RefreshTitle", InstanceFlags, null, Type.EmptyTypes, null);
            _isLockedProperty = _windowType.GetProperty("isLocked", InstanceFlags);

            IsSupported = _setObjectsLockedMethod != null
                          && _refreshTitleMethod != null
                          && _isLockedProperty?.CanWrite == true;

            if (!IsSupported)
            {
                Debug.LogError("[BetterTabs] Required Unity InspectorWindow members are unavailable. Inspector tabs are disabled.");
            }
        }

        internal static EditorWindow Create(object dockArea, Object target)
        {
            EditorWindow editorWindow = null;
            try
            {
                editorWindow = ScriptableObject.CreateInstance(_windowType) as EditorWindow;
                if (editorWindow == null)
                {
                    throw new InvalidOperationException("[BetterTabs] Could not create InspectorWindow.");
                }

                UnityDocking.AddTab(dockArea, editorWindow);
                _setObjectsLockedMethod.Invoke(editorWindow, new object[] { new List<Object> { target } });
                _isLockedProperty.SetValue(editorWindow, true);
                _refreshTitleMethod.Invoke(editorWindow, null);
                
                editorWindow.Repaint();
                return editorWindow;
            }
            catch (Exception exception)
            {
                if (editorWindow != null)
                {
                    Object.DestroyImmediate(editorWindow);
                }
                
                Debug.LogError($"[BetterTabs] Failed to create native Inspector tab for '{target.name}': {exception.GetBaseException()}");
                return null;
            }
        }
    }
}