using UnityEngine;
using UnityEditor;

namespace EditorTabs.Editor
{
    /// <summary>
    /// Identifies the specific target type the tab is pinned to.
    /// </summary>
    public enum TabTargetType
    {
        None,
        Folder,
        Asset,
        GameObject,
        Component
    }

    /// <summary>
    /// Serialized container for tab state, storing reference data that survives domain reloads.
    /// </summary>
    [System.Serializable]
    public class TabInfo
    {
        public TabTargetType targetType;
        
        [SerializeField] 
        private string globalObjectIdString;
        
        [SerializeField] 
        private Object transientTarget; // Transient fallback for unsaved objects during session

        public void SetTarget(Object target, TabTargetType type)
        {
            targetType = type;
            if (target != null)
            {
                globalObjectIdString = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
                transientTarget = target;
            }
            else
            {
                globalObjectIdString = string.Empty;
                transientTarget = null;
            }
        }

        /// <summary>
        /// Resolves the intended target object using persistent GlobalObjectIds first,
        /// and falling back to session-transient Instance IDs if it's an unsaved scene object.
        /// </summary>
        public Object ResolveTarget()
        {
            // Try resolving persistent cross-session ID first
            if (string.IsNullOrEmpty(globalObjectIdString)) return transientTarget != null ? transientTarget : null;
            if (GlobalObjectId.TryParse(globalObjectIdString, out var id) == false)
            {
                return transientTarget != null ? transientTarget : null;
            }
            
            var resolved = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            if (resolved != null)
            {
                return resolved;
            }

            // Fallback for objects that were never saved (e.g., spawned in the scene just now)
            return transientTarget != null ? transientTarget : null;
        }

        public string GetGlobalId() => globalObjectIdString;
    }
}
