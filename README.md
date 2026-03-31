# Editor Tabs for Unity

**Editor Tabs** is a productivity extension for the Unity Editor that provides a browser-like, tabbed interface to manage your workspace. It allows you to break free from the single-target Inspector by pinning folders, assets, GameObjects, and specific components into **native Unity tab area bars**.

## Installation

You can install this plugin into your Unity project using one of two methods:

### Option A: Unity Package Manager (Recommended)
If you just want to use the tool and don't need to edit the source code, install it directly as a UPM package:
1. Open the Unity Editor and navigate to **Window > Package Manager**.
2. Click the **+** (plus) icon in the top left and select **"Add package from git URL..."**
3. Paste the repository URL and click Add. Unity will download and mount the tool securely in your `Packages/` directory.

### Option B: Git Submodule (For Active Development)
If you plan to modify the plugin's codebase and want to commit changes back to the repository, embed it directly into your Assets:
1. Open your terminal at the root of your Unity project.
2. Run the following command to clone the tool as a submodule:
   ```bash
   git submodule add [REPOSITORY_URL] Assets/Plugins/EditorTabs
   ```
3. Unity will compile the source files. You can now edit the code and commit changes directly to the `EditorTabs` submodule repository.

## How to Use
Simply use **Drag and Drop** to create new tabs. Click and hold an item, drag it over to any native Unity tab bar (e.g., next to the "Scene", "Game", or "Inspector" text headers), and drop it. A new Editor Tab will instantly dock itself there.

### 1. Advanced Folder Navigation
*   **Workflow**: Drag any folder from the **Project Window** into a tab bar.
*   **Behavior**: Creates a dedicated "Focused Project View" for that folder, displaying its nested hierarchy.
*   **Tip**: Keep your `Prefabs/Player/` or `Art/Materials/` folders constantly pinned for instant access. Single-clicking an asset inside the tab will "ping" it in the main Project window without losing your current selection.

### 2. Persistent Asset Tabs
*   **Workflow**: Drag any asset file (Prefabs, Materials, ScriptableObjects, AudioClips) into the tab bar.
*   **Behavior**: Opens a locked Inspector tab representing that specific asset. 
*   **Tip**: Keep global configurations (e.g., `GameConfig.asset`) open on a secondary monitor while tweaking gameplay in the main window.

### 3. Hierarchy GameObject Tabs
*   **Workflow**: Drag any **GameObject** from the **Hierarchy Window** into the tab bar.
*   **Behavior**: Creates a pinned Inspector tab for that specific scene instance. It perfectly replicates the default Inspector, including all attached components and custom property drawers.
*   **Tip**: Pin major scene anchors like `LevelManager` or the `Player` root object to adjust their values without constantly searching through the Hierarchy.

### 4. Isolated Component Tabs
*   **Workflow**: Drag a **Component Header** from an existing Inspector window into the tab bar.
*   **Behavior**: Creates a tab dedicated **exclusively** to that single Component.
*   **Tip**: Pin a `Rigidbody` or an `Animator` of the main character to have extremely compact, high-focus views without the clutter of the rest of the GameObject's components.