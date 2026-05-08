using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BetterTabs.Editor
{
    /// <summary>
    /// Identifies the specific target type the tab is pinned to.
    /// </summary>
    public enum BetterTabTargetType
    {
        None,
        Folder,
        Asset,
        GameObject,
        Component
    }

    /// <summary>
    /// Serializable reference to a tab's target Object that survives domain reloads and editor restarts.
    /// </summary>
    [Serializable]
    public class BetterTabTargetReference
    {
        public BetterTabTargetType targetType;
        
        [SerializeField]
        public string globalObjectIdString;
        
        [SerializeField] 
        private Object transientTarget; // Transient fallback for unsaved objects during session

        public void SetTarget(Object target, BetterTabTargetType type)
        {
            targetType = type;
            globalObjectIdString = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
            transientTarget = target;
        }

        /// <summary>
        /// Resolves the intended target object using persistent GlobalObjectIds first,
        /// and falling back to session-transient Instance IDs if it's an unsaved scene object.
        /// </summary>
        public Object ResolveTarget()
        {
            var isIdValid = GlobalObjectId.TryParse(globalObjectIdString, out var objectID);
            if (isIdValid == false) return transientTarget;
            
            var resolvedObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectID);
            return resolvedObject != null ? resolvedObject : transientTarget;
        }
    }
}
