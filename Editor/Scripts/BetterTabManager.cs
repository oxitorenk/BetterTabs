using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace BetterTabs.Editor
{
    [InitializeOnLoad]
    public static class BetterTabManager
    {
        private static Type _dockAreaType;
        private static PropertyInfo _screenPosProp;
        private static MethodInfo _addTabMethod;
        private static bool _isHookActive;
        
        private const float MouseYThreshold = 35f;

        static BetterTabManager()
        {
            InitializeReflection();
            HookGlobalEvent();
        }

        private static void InitializeReflection()
        {
            _dockAreaType = typeof(EditorWindow).Assembly.GetType("UnityEditor.DockArea");
            if (_dockAreaType != null)
            {
                // GUIView uses "screenPosition" which translates to absolute Editor rendering coordinates
                _screenPosProp = _dockAreaType.GetProperty("screenPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               ?? _dockAreaType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                // AddTab(EditorWindow window, bool sendPaneEvents) or AddTab(EditorWindow window)
                _addTabMethod = _dockAreaType.GetMethod("AddTab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(EditorWindow), typeof(bool) }, null)
                              ?? _dockAreaType.GetMethod("AddTab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(EditorWindow) }, null);

                if (_screenPosProp == null) Debug.LogError("[BetterTabs] Failed to find DockArea position property.");
                if (_addTabMethod == null) Debug.LogError("[BetterTabs] Failed to find DockArea AddTab method.");
            }
            else
            {
                Debug.LogError("[BetterTabs] Failed to find DockArea type.");
            }
        }

        private static void HookGlobalEvent()
        {
            EditorApplication.update -= RefreshDockEventHandlers;
            EditorApplication.update += RefreshDockEventHandlers;
        }

        private static void RefreshDockEventHandlers()
        {
            if (_dockAreaType == null) return;

            var isDragging = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0;
            
            if (isDragging && !_isHookActive)
            {
                _isHookActive = true;
                ToggleDragHooks(true);
            }
            else if (!isDragging && _isHookActive)
            {
                _isHookActive = false;
                ToggleDragHooks(false);
            }
        }

        private static void ToggleDragHooks(bool shouldAddHooks)
        {
            var allDocks = Resources.FindObjectsOfTypeAll(_dockAreaType);
            foreach (var dock in allDocks)
            {
                var dockScriptableObject = dock as ScriptableObject;
                if (dockScriptableObject == null) continue;

                var visualTreeProp = dockScriptableObject.GetType().GetProperty("visualTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) 
                                     ?? dockScriptableObject.GetType().GetProperty("rootVisualElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (visualTreeProp == null) continue;
                if (visualTreeProp.GetValue(dockScriptableObject) is not VisualElement root) continue;
                
                // Clean up previous registrations
                root.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
                root.UnregisterCallback<DragPerformEvent>(OnDragPerform);

                if (!shouldAddHooks) continue;
                
                root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                root.RegisterCallback<DragPerformEvent>(OnDragPerform);
            }
        }

        private static void OnDragUpdated(DragUpdatedEvent dragUpdateEvent)
        {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) return;
            
            var targetObject = DragAndDrop.objectReferences[0];
            var targetType = IdentifyType(targetObject);
            if (targetType == BetterTabTargetType.None) return;

            if (dragUpdateEvent.currentTarget is not VisualElement) return;
            if (dragUpdateEvent.localMousePosition.y >= MouseYThreshold) return;
           
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            dragUpdateEvent.StopPropagation(); // Stop native rejection
        }

        private static void OnDragPerform(DragPerformEvent dragPerformEvent)
        {
            if (dragPerformEvent.localMousePosition.y > MouseYThreshold) return; // Ignore drops strictly inside the window content
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) return;
            
            var targetObject = DragAndDrop.objectReferences[0];
            var targetType = IdentifyType(targetObject);
            if (targetType == BetterTabTargetType.None) return;

            var targetElement = dragPerformEvent.currentTarget as VisualElement;
            var dockArea = GetDockAreaFromVisualElement(targetElement);
            if (dockArea == null) return;
            
            DragAndDrop.AcceptDrag();
            PerformDrop(dockArea, targetObject, targetType);
            dragPerformEvent.StopPropagation();
        }

        private static object GetDockAreaFromVisualElement(VisualElement element)
        {
            if (element == null || _dockAreaType == null) return null;
            
            var allDocks = Resources.FindObjectsOfTypeAll(_dockAreaType);
            foreach (var dock in allDocks)
            {
                var dockScriptableObject = dock as ScriptableObject;
                if (dockScriptableObject == null) continue;

                var visualTreeProp = dockScriptableObject.GetType().GetProperty("visualTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) 
                                     ?? dockScriptableObject.GetType().GetProperty("rootVisualElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (visualTreeProp == null) continue;
                if (visualTreeProp.GetValue(dockScriptableObject) is not VisualElement root) continue;
                if (root != element && !root.Contains(element)) continue;
                
                return dock;
            }
            
            return null;
        }

        private static void PerformDrop(object dockArea, UnityEngine.Object targetObject, BetterTabTargetType targetType)
        {
            var windowId = Guid.NewGuid().ToString();

            // Register in the persistent singleton BEFORE creating the instance so OnEnable can find it
            BetterTabStateRegistry.Instance.RegisterTab(windowId, targetObject, targetType);

            var window = ScriptableObject.CreateInstance<BetterTabsWindow>();
            window.Initialize(windowId);
            if (_addTabMethod != null && dockArea != null)
            {
                if (_addTabMethod.GetParameters().Length == 2)
                {
                    _addTabMethod.Invoke(dockArea, new object[] { window, true });
                }
                else
                {
                    _addTabMethod.Invoke(dockArea, new object[] { window });
                }
            }
            else
            {
                window.Show(); // Fallback 
            }
            
            // Give it immediate focus 
            window.Focus();
        }

        private static BetterTabTargetType IdentifyType(UnityEngine.Object target)
        {
            switch (target)
            {
                case Component:
                    return BetterTabTargetType.Component;
                
                case GameObject when AssetDatabase.Contains(target):
                    return BetterTabTargetType.Asset;
                
                case GameObject:
                    return BetterTabTargetType.GameObject;
                
                case DefaultAsset:
                {
                    var path = AssetDatabase.GetAssetPath(target);
                    if (AssetDatabase.IsValidFolder(path)) return BetterTabTargetType.Folder;
                    
                    break;
                }
            }

            return AssetDatabase.Contains(target) ? BetterTabTargetType.Asset : BetterTabTargetType.None;
        }
    }
}
