using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BetterTabs.Editor
{
    [InitializeOnLoad]
    public static class BetterTabManager
    {
        // Matches Unity's internal DockArea.kDockHeight, the native tab drop zone height.
        private const float DockTabDropZoneHeight = 39f;
        private const double FolderTabRestoreScanIntervalSeconds = 1d;

        private static bool _areDragHooksRegistered;
        private static double _nextFolderRestoreScan;

        static BetterTabManager()
        {
            var editorAssembly = typeof(EditorWindow).Assembly;
            UnityDocking.Initialize(editorAssembly);
            FolderTabs.Initialize(editorAssembly);
            InspectorTabs.Initialize(editorAssembly);

            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            EditorApplication.projectChanged -= FolderTabs.Refresh;
            EditorApplication.projectChanged += FolderTabs.Refresh;

            // delayCall clears its callbacks before invoking them, so no unsubscribe is needed.
            EditorApplication.delayCall += FolderTabs.Refresh;
        }

        private static void Update()
        {
            // ProjectBrowser may overwrite restored folder-tab state after editor events, so keep it repaired.
            if (EditorApplication.timeSinceStartup >= _nextFolderRestoreScan)
            {
                _nextFolderRestoreScan = EditorApplication.timeSinceStartup + FolderTabRestoreScanIntervalSeconds;
                FolderTabs.Refresh();
            }

            if (!UnityDocking.IsSupported) return;

            var isDragging = HasSupportedTargets();
            if (isDragging == _areDragHooksRegistered) return;

            ToggleDragHooks(isDragging);
        }

        private static bool HasSupportedTargets()
        {
            var targets = DragAndDrop.objectReferences;
            return targets != null && targets.Any(IsSupported);
        }

        private static bool IsSupported(Object target)
        {
            if (target == null) return false;

            return IsFolder(target) ? FolderTabs.IsSupported : InspectorTabs.IsSupported;
        }

        private static bool IsFolder(Object target)
        {
            var path = AssetDatabase.GetAssetPath(target);
            
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }

        private static void ToggleDragHooks(bool shouldAddHooks)
        {
            foreach (var root in UnityDocking.GetRoots())
            {
                root.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
                root.UnregisterCallback<DragPerformEvent>(OnDragPerform);

                if (!shouldAddHooks) continue;

                root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                root.RegisterCallback<DragPerformEvent>(OnDragPerform);
            }

            _areDragHooksRegistered = shouldAddHooks;
        }

        private static void OnDragUpdated(DragUpdatedEvent dragUpdatedEvent)
        {
            if (!IsTabDropCandidate(dragUpdatedEvent.localMousePosition.y)) return;
            if (dragUpdatedEvent.currentTarget is not VisualElement) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            // Prevents Unity's native handler from replacing our accepted-drop feedback.
            dragUpdatedEvent.StopPropagation();
        }

        private static void OnDragPerform(DragPerformEvent dragPerformEvent)
        {
            if (!IsTabDropCandidate(dragPerformEvent.localMousePosition.y)) return;

            var dockArea = UnityDocking.FindDockArea(dragPerformEvent.currentTarget as VisualElement);
            if (dockArea == null) return;

            DragAndDrop.AcceptDrag();
            CreateNativeTabs(dockArea, DragAndDrop.objectReferences);

            // Prevents Unity's native drop handler from processing the same drop again.
            dragPerformEvent.StopPropagation();
        }

        private static bool IsTabDropCandidate(float mouseY)
        {
            return mouseY < DockTabDropZoneHeight && HasSupportedTargets();
        }

        private static void CreateNativeTabs(object dockArea, Object[] targets)
        {
            EditorWindow firstWindow = null;

            foreach (var target in GetDistinctSupportedTargets(targets))
            {
                var window = IsFolder(target) ? FolderTabs.Create(dockArea, target) : InspectorTabs.Create(dockArea, target);
                firstWindow ??= window;
            }

            firstWindow?.Focus();
        }

        internal static IEnumerable<Object> GetDistinctSupportedTargets(Object[] targets)
        {
            if (targets == null) yield break;

            var seenInstanceIds = new HashSet<int>();
            foreach (var target in targets)
            {
                // Returns only supported targets whose instance ID has not appeared before.
                if (IsSupported(target) && seenInstanceIds.Add(target.GetInstanceID())) yield return target;
            }
        }
    }
}
