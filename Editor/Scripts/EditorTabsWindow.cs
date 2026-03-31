using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace EditorTabs.Editor
{
    public class FolderTreeItem
    {
        public string Name;
        public string AssetPath;
        public Texture Icon;
    }

    /// <summary>
    /// The actual custom tab window representing pinned assets, objects, or folders.
    /// </summary>
    public class EditorTabsWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField]
        private string windowID;
        
        private TabInfo _tabInfo;
        private Object _target;
        private Vector2 _inspectorScrollPosition;
        private int _currentTreeID;
        private TreeView _treeView;
        
        private readonly List<UnityEditor.Editor> _cachedEditors = new();
        private readonly Dictionary<Object, bool> _expandedStates = new();

        public void Initialize(string windowId)
        {
            windowID = windowId;
            _tabInfo = TabStateRegistry.Instance.GetTabInfo(windowID);
            
            if (_tabInfo != null)
            {
                _target = _tabInfo.ResolveTarget();
            }
            
            RenderUI();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(windowID)) return;

            _tabInfo = TabStateRegistry.Instance.GetTabInfo(windowID);
            if (_tabInfo != null)
            {
                _target = _tabInfo.ResolveTarget();
            }

            RenderUI();
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(windowID))
            {
                TabStateRegistry.Instance.UnregisterTab(windowID);
            }

            ClearEditors();
        }

        private void ClearEditors()
        {
            if (_cachedEditors == null) return;
            
            foreach (var editorReference in _cachedEditors)
            {
                if (editorReference != null)
                {
                    DestroyImmediate(editorReference);
                }
            }
            
            _cachedEditors.Clear();
        }

        private void RenderUI()
        {
            rootVisualElement.Clear();

            // Toolbar Layer
            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);

            if (_target == null || _tabInfo == null)
            {
                RenderMissingTarget(toolbar);
                return;
            }

            // Sync Window Title Native Appearance
            Texture icon;
            if (_target is Component comp)
            {
                icon = EditorGUIUtility.ObjectContent(comp.gameObject, typeof(GameObject)).image; // Fallback or retrieve actual component icon
            }
            else
            {
                icon = EditorGUIUtility.ObjectContent(_target, _target.GetType()).image;
            }

            titleContent = new GUIContent(_target.name, icon);

            // Populate Toolbar 
            var showButton = new ToolbarButton(() => EditorGUIUtility.PingObject(_target)) { text = "Show" };
            var refreshButton = new ToolbarButton(RenderUI) { text = "Refresh" };
            
            toolbar.Add(showButton);
            toolbar.Add(refreshButton);
            toolbar.Add(new ToolbarSpacer() { flex = true });

            // Render Polymorphic Content
            if (_tabInfo.targetType == TabTargetType.Folder)
            {
                RenderFolderView(rootVisualElement);
            }
            else
            {
                RenderInspectorView(rootVisualElement);
            }
        }

        private void RenderMissingTarget(Toolbar toolbar)
        {
            titleContent = new GUIContent("Missing Target");
            var label = new Label("Target is missing or has been deleted.")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    flexGrow = 1,
                    color = new Color(0.9f, 0.3f, 0.3f),
                    fontSize = 14,
                    marginTop = 20
                }
            };

            toolbar.Add(new ToolbarButton(Close) { text = "Close Tab" });
            rootVisualElement.Add(label);
        }

        private void RenderInspectorView(VisualElement container)
        {
            if (_target == null) return;

            ClearEditors();

            var imGuiContainer = new IMGUIContainer(() =>
            {
                // Safety check if the target was destroyed entirely
                if (_target == null) return;

                _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

                if (_target is GameObject targetGameObject)
                {
                    // Initialize editors if the cache is empty
                    if (_cachedEditors.Count == 0)
                    {
                        _cachedEditors.Add(UnityEditor.Editor.CreateEditor(targetGameObject)); // Header editor
                        
                        var components = targetGameObject.GetComponents<Component>();
                        foreach (var currentComponent in components)
                        {
                            if (currentComponent != null)
                            {
                                _cachedEditors.Add(UnityEditor.Editor.CreateEditor(currentComponent));
                            }
                        }
                    }

                    // Draw GameObject Header (Tags, Layers, Transform enable toggle, etc.)
                    if (_cachedEditors[0] != null)
                    {
                        _cachedEditors[0].DrawHeader();
                        _cachedEditors[0].OnInspectorGUI();
                    }

                    // Draw Stacked Component Inspectors with Native Title Bars
                    for (var i = 1; i < _cachedEditors.Count; i++)
                    {
                        var editorReference = _cachedEditors[i];
                        if (editorReference == null || editorReference.target == null) continue;

                        var componentTarget = editorReference.target;
                        
                        // Default all components to expand if unset
                        _expandedStates.TryAdd(componentTarget, true);
                        EditorGUILayout.Space(2);

                        // Draw native component collapsible bar
                        _expandedStates[componentTarget] = EditorGUILayout.InspectorTitlebar(_expandedStates[componentTarget], componentTarget);
                        if (_expandedStates[componentTarget])
                        {
                            editorReference.OnInspectorGUI();
                        }
                    }
                }
                else
                {
                    // For standard assets (Materials, Scripts, etc.), we just draw the single editor.
                    if (_cachedEditors.Count == 0)
                    {
                        _cachedEditors.Add(UnityEditor.Editor.CreateEditor(_target));
                    }

                    if (_cachedEditors[0] != null)
                    {
                        _cachedEditors[0].DrawHeader();
                        _cachedEditors[0].OnInspectorGUI();
                    }
                }

                EditorGUILayout.EndScrollView();
            });

            imGuiContainer.style.flexGrow = 1;
            container.Add(imGuiContainer);
        }

        private void RenderFolderView(VisualElement container)
        {
            var folderAssetPath = AssetDatabase.GetAssetPath(_target);

            _currentTreeID = 0;
            var roots = BuildTreeChildren(folderAssetPath);

            if (roots.Count == 0)
            {
                 var emptyLabel = new Label("Folder is completely empty.") 
                 { 
                     style = { marginTop = 20, unityTextAlign = TextAnchor.MiddleCenter, color = new Color(0.6f, 0.6f, 0.6f) } 
                 };
                 container.Add(emptyLabel);
                 return;
            }

            _treeView = new TreeView
            {
                style =
                {
                    flexGrow = 1
                },
                viewDataKey = "EditorTabs-Tree-" + windowID, // Persists expansion states across domain reloads
                makeItem = () =>
                {
                    var row = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center
                        }
                    };

                    var icon = new Image
                    {
                        name = "icon",
                        style =
                        {
                            width = 16,
                            height = 16,
                            marginRight = 4
                        }
                    };

                    var label = new Label
                    {
                        name = "label",
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleLeft
                        }
                    };

                    row.Add(icon);
                    row.Add(label);
                    return row;
                }
            };

            _treeView.bindItem = (element, index) =>
            {
                var data = _treeView.GetItemDataForIndex<FolderTreeItem>(index);
                
                element.Q<Image>("icon").image = data.Icon;
                element.Q<Label>("label").text = data.Name;
            };

            _treeView.SetRootItems(roots);
            _treeView.selectionChanged += (selection) =>
            {
                foreach (var obj in selection)
                {
                    if (obj is not FolderTreeItem item) continue;
                    
                    var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                    }
                }
            };

            _treeView.itemsChosen += (selection) =>
            {
                foreach (var currentObject in selection)
                {
                    if (currentObject is not FolderTreeItem item) continue;
                    
                    var asset = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                    if (asset != null)
                    {
                        AssetDatabase.OpenAsset(asset);
                    }
                }
            };

            container.Add(_treeView);
        }

        private List<TreeViewItemData<FolderTreeItem>> BuildTreeChildren(string folderAssetPath)
        {
            var children = new List<TreeViewItemData<FolderTreeItem>>();
            var subFolders = AssetDatabase.GetSubFolders(folderAssetPath);
            foreach (var dirAssetPath in subFolders)
            {
                var folderData = new FolderTreeItem()
                {
                    Name = System.IO.Path.GetFileName(dirAssetPath),
                    AssetPath = dirAssetPath,
                    Icon = EditorGUIUtility.IconContent("Folder Icon").image,
                };
                
                var childNodes = BuildTreeChildren(dirAssetPath);
                children.Add(new TreeViewItemData<FolderTreeItem>(_currentTreeID++, folderData, childNodes));
            }

            var absoluteFolderPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../" + folderAssetPath));
            if (System.IO.Directory.Exists(absoluteFolderPath) == false) return children;
            
            var files = System.IO.Directory.GetFiles(absoluteFolderPath);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta") || file.EndsWith(".DS_Store")) continue;

                var assetPath = folderAssetPath + "/" + System.IO.Path.GetFileName(file);
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset == null || asset is DefaultAsset) continue; // Skip unimported or manual folder files

                var data = new FolderTreeItem
                {
                    Name = asset.name, // Unity's standard asset name stripped of internal extensions
                    AssetPath = assetPath,
                    Icon = EditorGUIUtility.ObjectContent(asset, asset.GetType()).image,
                };

                children.Add(new TreeViewItemData<FolderTreeItem>(_currentTreeID++, data));
            }

            return children;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Refresh Tab Content"), false, RenderUI);
        }
    }
}
