using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetterTabs.Editor
{
    [System.Serializable]
    public class BetterTabRecord
    {
        public string windowId;
        public BetterTabTargetReference targetReference = new();
    }

    public class BetterTabStateRegistry : ScriptableObject
    {
        private static BetterTabStateRegistry _instance;

        [SerializeField]
        private List<BetterTabRecord> records = new();

        public static BetterTabStateRegistry Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = CreateInstance<BetterTabStateRegistry>();
                _instance.hideFlags = HideFlags.HideAndDontSave;
                Load(_instance);

                return _instance;
            }
        }

        private static string GetRegistryPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/BetterTabs_Registry.json"));
        }

        private static void Load(BetterTabStateRegistry instance)
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

        public void RegisterTab(string windowInstanceId, Object target, BetterTabTargetType type)
        {
            var record = records.Find(tabRecord => tabRecord.windowId == windowInstanceId);
            if (record == null)
            {
                record = new BetterTabRecord { windowId = windowInstanceId };
                records.Add(record);
            }

            record.targetReference.SetTarget(target, type);
            Save();
        }

        public BetterTabTargetReference GetTabTargetReference(string windowInstanceId)
        {
            var record = records.Find(tabRecord => tabRecord.windowId == windowInstanceId);
            return record?.targetReference;
        }

        public void UnregisterTab(string windowInstanceId)
        {
            var removeRecordCount = records.RemoveAll(tabRecord => tabRecord.windowId == windowInstanceId);
            if (removeRecordCount <= 0) return;
            
            Save();
        }
    }
}
