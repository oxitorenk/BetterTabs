using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterTabs.Editor
{
    internal static class FolderTabs
    {
        private const string MarkerPrefix = "BetterTabs.Folder:";
        private const string GuidPrefix = "guid:";
        private const string PathPrefix = "path:";

        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly HashSet<int> KnownWindows = new();

        private static Type _windowType;
        private static MethodInfo _initMethod;
        private static MethodInfo _setTwoColumnsMethod;
        private static MethodInfo _showFolderContentsMethod;
        private static PropertyInfo _isLockedProperty;
        private static MethodInfo _folderIdFromIntMethod;
        private static Type _folderIdType;

        internal static bool IsSupported { get; private set; }

        internal static void Initialize(Assembly editorAssembly)
        {
            _windowType = editorAssembly.GetType("UnityEditor.ProjectBrowser");
            if (_windowType == null)
            {
                Debug.LogError("[BetterTabs] Unity ProjectBrowser API is unavailable. Folder tabs are disabled.");
                return;
            }

            _initMethod = _windowType.GetMethod("Init", InstanceFlags, null, 
                Type.EmptyTypes, null);
            
            _setTwoColumnsMethod = _windowType.GetMethod("SetTwoColumns", InstanceFlags, null,
                Type.EmptyTypes, null);
            
            _showFolderContentsMethod = _windowType.GetMethod("ShowFolderContents", InstanceFlags);
            _isLockedProperty = _windowType.GetProperty("isLocked", InstanceFlags);

            var showParameters = _showFolderContentsMethod?.GetParameters();
            
            // Accepts only the known signature: folder ID followed by a boolean reveal flag.
            if (showParameters is { Length: 2 } && showParameters[1].ParameterType == typeof(bool))
            {
                _folderIdType = showParameters[0].ParameterType;
                
                // Unity 6000.0 accepts int; later Unity 6000 releases use EntityId.
                if (_folderIdType != typeof(int))
                {
                    _folderIdFromIntMethod = _folderIdType.GetMethod("From", StaticFlags, 
                        null, new[] { typeof(int) }, null);
                }
            }

            IsSupported = _initMethod != null
                          && _setTwoColumnsMethod != null
                          && _showFolderContentsMethod != null
                          && _isLockedProperty?.CanWrite == true
                          && _folderIdType != null
                          && (_folderIdType == typeof(int) || _folderIdFromIntMethod != null);

            if (!IsSupported)
            {
                Debug.LogError("[BetterTabs] Required Unity ProjectBrowser members are unavailable. Folder tabs are disabled.");
            }
        }

        internal static EditorWindow Create(object dockArea, Object folder)
        {
            EditorWindow window = null;
            try
            {
                window = ScriptableObject.CreateInstance(_windowType) as EditorWindow;
                if (window == null)
                {
                    throw new InvalidOperationException("[BetterTabs] Could not create ProjectBrowser.");
                }

                UnityDocking.AddTab(dockArea, window);
                ConfigureWindow(window, folder, true);
                
                return window;
            }
            catch (Exception exception)
            {
                if (window != null)
                {
                    Object.DestroyImmediate(window);
                }
                
                Debug.LogError($"[BetterTabs] Failed to create native folder tab for '{folder.name}': {exception.GetBaseException()}");
                return null;
            }
        }

        internal static void Refresh()
        {
            if (!IsSupported) return;

            var liveWindowIds = new HashSet<int>();
            foreach (var candidate in Resources.FindObjectsOfTypeAll(_windowType))
            {
                if (candidate is not EditorWindow window) continue;
                if (!TryResolveFolder(window.name, out var folder)) continue;
                
                try
                {
                    var windowId = window.GetInstanceID();
                    liveWindowIds.Add(windowId);
                    var isNewWindow = KnownWindows.Add(windowId);
                    var titleNeedsRestore = window.titleContent == null || window.titleContent.text != folder.name;
                    var markerNeedsRefresh = window.name != CreateFolderMarker(AssetDatabase.GetAssetPath(folder));
                    if (isNewWindow || titleNeedsRestore || markerNeedsRefresh)
                    {
                        ConfigureWindow(window, folder, isNewWindow || titleNeedsRestore);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[BetterTabs] Failed to restore native folder tab '{window.name}': {exception.GetBaseException()}");
                }
            }

            // Removes IDs for folder windows that were closed since the previous scan.
            KnownWindows.RemoveWhere(windowId => !liveWindowIds.Contains(windowId));
        }

        private static void ConfigureWindow(EditorWindow window, Object folder, bool shouldShowFolder)
        {
            _initMethod.Invoke(window, null);
            _setTwoColumnsMethod.Invoke(window, null);

            if (shouldShowFolder)
            {
                _showFolderContentsMethod.Invoke(window, new[] { CreateFolderId(folder), true });
            }

            _isLockedProperty.SetValue(window, true);

            var path = AssetDatabase.GetAssetPath(folder);
            window.name = CreateFolderMarker(path);
            window.titleContent = new GUIContent(folder.name, EditorGUIUtility.ObjectContent(folder, folder.GetType()).image);
            window.Repaint();
        }

        internal static object CreateFolderId(Object folder)
        {
            var instanceId = folder.GetInstanceID();
            return _folderIdType == typeof(int) 
                ? instanceId 
                : _folderIdFromIntMethod.Invoke(null, new object[] { instanceId });
        }

        internal static string CreateFolderMarker(string path)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            return MarkerPrefix + (string.IsNullOrEmpty(guid) ? PathPrefix + path : GuidPrefix + guid);
        }

        internal static bool TryResolveFolder(string marker, out Object folder)
        {
            folder = null;
            if (string.IsNullOrEmpty(marker) || !marker.StartsWith(MarkerPrefix, StringComparison.Ordinal)) return false;

            var payload = marker[MarkerPrefix.Length..];
            string path;

            if (payload.StartsWith(GuidPrefix, StringComparison.Ordinal))
            {
                path = AssetDatabase.GUIDToAssetPath(payload.Substring(GuidPrefix.Length));
            }
            else if (payload.StartsWith(PathPrefix, StringComparison.Ordinal))
            {
                path = payload[PathPrefix.Length..];
            }
            else
            {
                return false;
            }

            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return false;
            
            folder = AssetDatabase.LoadMainAssetAtPath(path);
            return folder != null;
        }
    }
}