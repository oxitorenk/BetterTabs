using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorTabs.Editor
{
    [System.Serializable]
    public class TabRecord
    {
        public string windowId; 
        public TabInfo info = new();
    }
    
    public class TabStateRegistry : ScriptableObject
    {
        private static TabStateRegistry _instance;
        
        [SerializeField]
        private List<TabRecord> records = new();

        public static TabStateRegistry Instance
        {
            get
            {
                if (_instance != null) return _instance;
                
                _instance = CreateInstance<TabStateRegistry>();
                _instance.hideFlags = HideFlags.HideAndDontSave;
                Load(_instance);
                
                return _instance;
            }
        }
        
        private static string GetRegistryPath() 
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/EditorTabs_Registry.json"));
        }

        private static void Load(TabStateRegistry instance)
        {
            var path = GetRegistryPath();
            if (!File.Exists(path)) return;
            
            var json = File.ReadAllText(path);
            EditorJsonUtility.FromJsonOverwrite(json, instance);
        }

        private void Save()
        {
            var path = GetRegistryPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            
            var json = EditorJsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
        }

        public void RegisterTab(string windowInstanceId, Object target, TabTargetType type)
        {
            var record = records.Find(tabRecord => tabRecord.windowId == windowInstanceId);
            if (record == null)
            {
                record = new TabRecord { windowId = windowInstanceId };
                records.Add(record);
            }

            record.info.SetTarget(target, type);
            Save();
        }

        public TabInfo GetTabInfo(string windowInstanceId)
        {
            var record = records.Find(tabRecord => tabRecord.windowId == windowInstanceId);
            return record?.info;
        }

        public void UnregisterTab(string windowInstanceId)
        {
            var removeRecordCount = records.RemoveAll(tabRecord => tabRecord.windowId == windowInstanceId);
            if (removeRecordCount <= 0) return;
            
            Save();
        }
    }
}
