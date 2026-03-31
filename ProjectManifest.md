# Project Manifest: Editor Tabs Plugin

## 1. Core Identity
**Project Name**: Editor Tabs  
**Objective**: A high-performance, UI Toolkit-based productivity extension for hijacking the native Unity tab area to provide locked, persistent views of specific assets, folders, and components.

## 2. Technical Stack
- **Engine**: Unity 6 (6000.x.x)
- **Primary Framework**: Unity UI Toolkit (VisualElements) + IMGUI Fallback Stack
- **Language**: C# 12 / .NET 8 (Editor-only assembly)
- **Assembly Definition**: `EditorTabs.Editor.asmdef`
- **Package ID**: `com.oxitorenk.editortabs` (UPM compatible)
- **Data Persistence**: JSON serialization inside the `Library/` folder (bypassing Version Control and retaining Editor-only cross-session tracking via `GlobalObjectId`). Absolute pathing ensures location-agnostic compatibility (Assets vs. Packages).

## 3. Architectural Standards
### Core Components
- **TabManager (Static/Global)**: 
    - `[InitializeOnLoad]` listener for global `DragAndDrop` events.
    - Zero-allocation drag checking logic (avoids GC spikes).
    - Uses reflection for internal Unity APIs: `UnityEditor.DockArea` and `UnityEditor.HostView`.
    - Logic for detecting drops on native horizontal docking strips.
- **EditorTabsWindow (UI)**:
    - Adaptive window that docks natively using `DockArea.AddTab`.
    - **Visual Hierarchy**:
        - `Root (VisualElement)`: Main container.
        - `Toolbar (UnityEditor.UIElements.Toolbar)`: Top strip with Refresh and Ping buttons.
        - `Viewport (VisualElement)`: Display area for the target content.
- **TabStateRegistry (Data)**:
    - A `ScriptableObject` that tracks all open tab GUIDs and targets with absolute pathing.
    - Ensures tabs survive domain reloads and project restarts.

### Interaction Logic
- **Native Interception**: The tool MUST target the standard horizontal docking strip where Scene, Game, and Inspector tabs reside.
- **Context Locking**: Every tab is locked by default to its initial target.
- **UX Requirement**: Zero-click accessibility for pinned context.

## 4. UI Rendering Protocols
- **Asset Rendering**: 
    - Use `UnityEditor.UIElements.InspectorElement` for high-performance, reactive UI for standard assets (Materials, ScriptableObjects).
- **GameObject Rendering (Pitfall Resolved)**:
    - `InspectorElement` natively fails to build internal GameObject component stacks. 
    - **Protocol**: Route GameObjects through an `IMGUIContainer` replicating the native `ActiveEditorTracker` stack using `Editor.CreateEditor()` and `EditorGUILayout.InspectorTitlebar()`.
- **Folder Rendering (Pitfall Resolved)**:
    - A flat `ListView` fails to provide necessary parent-child nesting context. Raw `System.IO` parsing parses hidden files.
    - **Protocol**: Use natively nested `UnityEngine.UIElements.TreeView` coupled with `AssetDatabase.GetSubFolders` to build a visual, collapsible hierarchy tree safely. Interactions are restricted strictly to `PingObject`.
- **Component Rendering**:
    - Use `InspectorElement` focused specifically on the target Component instance.

## 5. Development Constraints & Coding Standards
- **Aesthetics**: Professional, text-focused design. **Strictly NO emojis** in code comments, documentation, or UI.
- **Never List**: 
    - No `GameObject.Find` or `GameObject.FindWithTag`.
    - No `OnGUI` (except where strictly required by legacy EditorWindow APIs).
    - No non-serialized transient state for tab targets (must survive domain reloads).
- **Automation**: Minimize manual setup. The tool should be "invisible" until a drag event occurs.
- **Coding Standards (Enforced)**:
    - **Naming**: Private backing fields and instance variables must strictly use the `_camelCase` convention (do not use `m_` prefixes).
    - **Language Features**: Leverage modern C# syntax. Utilize `var` for implicit local declarations where types are obvious, and use target-typed `new()` expressions to clean up instantiations.
    - **Control Flow**: Strictly enforce the "Return Early" (Guard Clause) pattern to eliminate unnecessary nesting. Loop logic should aggressively use `continue` rather than nested `if` blocks.
    - **Pattern Matching**: Use modern C# pattern matching constructs (e.g., `if (obj is not VisualElement root)`) instead of explicit null casting cascades.

## 6. Feature Scope
1. **Folder Tabs**: Pinned project navigation.
2. **Asset Tabs**: Locked inspector views for project files.
3. **GameObject Tabs**: Persistent hierarchy instance views.
4. **Component Tabs**: Isolated data inspection for specific component types.

## 7. Development Journey & Resolved Pitfalls
During the creation and stabilization of this tool, several critical Unity Editor API pitfalls were navigated:
- **Global Drag & Drop Resolution**: Migrated away from deprecated global IMGUI `EditorApplication.update` loops in favor of robust, event-driven UI Toolkit `DragUpdatedEvent` and `DragPerformEvent` injected directly into the native `DockArea` interfaces via Reflection. 
- **Performance Optimization**: Created a zero-allocation polling mechanism to attach reflection hooks only during active Drag actions to prevent GC spikes.
- **TreeView Deprecation Fixes**: Addressed Unity 6 API shifts by migrating from obsoleted `BaseVerticalCollectionView.onSelectionChange` events directly to the modern `selectionChanged` handlers.
- **Empty TreeView Crashes**: Implemented graceful empty-folder guards to prevent UIElements from throwing internal `ArgumentException` failures when rendering empty `roots` lists.
- **GameObject Inspector Rendering Bug**: Discovered that relying purely on `UnityEditor.UIElements.InspectorElement` for GameObjects only draws the header/metadata, entirely dropping attached Components. This was resolved by constructing a heavily optimized `IMGUIContainer` replicating the internal `ActiveEditorTracker` stack and manually rendering elements.
- **Persistence Pathing & Obsolete Targets**: Transitioned tab persistence away from volatile integer `GetInstanceID()` calls to robust `GlobalObjectId` tracking. Shifted JSON serialization paths from relative strings to strictly absolute `Application.dataPath` evaluations to prevent domain-reload failures in headless/CI pipelines.
- **UPM Compatibility (Git URL Support)**: Implemented `package.json` and refactored all internal paths (Folder recursion, Registry) to be location-agnostic. This allows the plugin to function perfectly whether installed in `Assets/Plugins/` or via a remote Git URL into the `Packages/` directory.
