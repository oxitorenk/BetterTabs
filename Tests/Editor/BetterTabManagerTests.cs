#if HAS_TEST_FRAMEWORK
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterTabs.Editor.Tests
{
    public class BetterTabManagerTests
    {
        private const string TestFolderPath = "Assets/BetterTabsTests";
        private const string MovedTestFolderPath = "Assets/BetterTabsTestsMoved";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolders();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestFolders();
        }

        [Test]
        public void DockAreaApiMatchesSupportedContract()
        {
            var dockAreaType = GetEditorType("UnityEditor.DockArea");

            var rootProperty = dockAreaType.GetProperty("visualTree", InstanceFlags);
            var addTabMethod = dockAreaType.GetMethod("AddTab", InstanceFlags, 
                null, new[] { typeof(EditorWindow), typeof(bool) }, null);

            Assert.NotNull(rootProperty, "DockArea root VisualElement property was not found.");
            Assert.NotNull(addTabMethod, "DockArea.AddTab was not found.");
        }

        [Test]
        public void ProjectBrowserApiMatchesSupportedContract()
        {
            var projectBrowserType = GetEditorType("UnityEditor.ProjectBrowser");

            Assert.NotNull(projectBrowserType.GetMethod("Init", InstanceFlags, null,
                Type.EmptyTypes, null));
            
            Assert.NotNull(projectBrowserType.GetMethod("SetTwoColumns", InstanceFlags, 
                null, Type.EmptyTypes, null));
            
            Assert.NotNull(projectBrowserType.GetProperty("isLocked", InstanceFlags));

            var showFolderContents = projectBrowserType.GetMethod("ShowFolderContents", 
                InstanceFlags);
            
            Assert.NotNull(showFolderContents);

            var parameters = showFolderContents.GetParameters();
            Assert.AreEqual(2, parameters.Length);
            Assert.AreEqual(typeof(bool), parameters[1].ParameterType);
            
            Assert.IsTrue(parameters[0].ParameterType == typeof(int)
                          || parameters[0].ParameterType.GetMethod("From", StaticFlags, null, 
                              new[] { typeof(int) }, null) != null,
                $"Unsupported folder ID type: {parameters[0].ParameterType.FullName}");
        }

        [Test]
        public void InspectorWindowApiMatchesSupportedContract()
        {
            var inspectorWindowType = GetEditorType("UnityEditor.InspectorWindow");

            Assert.NotNull(inspectorWindowType.GetMethod("SetObjectsLocked", InstanceFlags));
            Assert.NotNull(inspectorWindowType.GetMethod("RefreshTitle", InstanceFlags, 
                null, Type.EmptyTypes, null));
            
            Assert.NotNull(inspectorWindowType.GetProperty("isLocked", InstanceFlags));
        }

        [Test]
        public void DistinctSupportedTargetsPreserveSourceOrderAndExactObjects()
        {
            var gameObject = new GameObject("BetterTabs Test GameObject");
            var component = gameObject.AddComponent<BoxCollider>();
            var asset = ScriptableObject.CreateInstance<TestAsset>();
            CreateTestFolder(TestFolderPath);
            var folder = AssetDatabase.LoadMainAssetAtPath(TestFolderPath);

            try
            {
                var input = new[] { component, folder, null, gameObject, component, asset, folder };
                var result = new List<Object>(BetterTabManager.GetDistinctSupportedTargets(input));

                CollectionAssert.AreEqual(new[] { component, folder, gameObject, asset }, result);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FolderIdAdapterProducesExpectedProjectBrowserParameter()
        {
            CreateTestFolder(TestFolderPath);
            var folder = AssetDatabase.LoadMainAssetAtPath(TestFolderPath);
            Assert.NotNull(folder);

            var projectBrowserType = GetEditorType("UnityEditor.ProjectBrowser");
            var showFolderContents = projectBrowserType.GetMethod("ShowFolderContents", 
                InstanceFlags);
            
            var expectedType = showFolderContents.GetParameters()[0].ParameterType;
            var folderId = FolderTabs.CreateFolderId(folder);

            Assert.AreEqual(expectedType, folderId.GetType());
            if (expectedType == typeof(int))
            {
                Assert.AreEqual(folder.GetInstanceID(), folderId);
                return;
            }

            var equalsInstanceId = expectedType.GetMethod("Equals", InstanceFlags, 
                null, new[] { typeof(int) }, null);
            
            Assert.NotNull(equalsInstanceId);
            Assert.IsTrue((bool)equalsInstanceId.Invoke(folderId, new object[] { folder.GetInstanceID() }));
        }

        [Test]
        public void FolderMarkerResolvesAfterFolderMove()
        {
            CreateTestFolder(TestFolderPath);
            var marker = FolderTabs.CreateFolderMarker(TestFolderPath);

            Assert.IsEmpty(AssetDatabase.MoveAsset(TestFolderPath, MovedTestFolderPath));
            Assert.IsTrue(FolderTabs.TryResolveFolder(marker, out var folder));
            Assert.AreEqual(MovedTestFolderPath, AssetDatabase.GetAssetPath(folder));
        }

        private static Type GetEditorType(string name)
        {
            var type = typeof(EditorWindow).Assembly.GetType(name);
            Assert.NotNull(type, $"{name} was not found.");
            return type;
        }

        private static void CreateTestFolder(string path)
        {
            var folderName = path["Assets/".Length..];
            var guid = AssetDatabase.CreateFolder("Assets", folderName);
            Assert.IsFalse(string.IsNullOrEmpty(guid), $"Could not create {path}.");
        }

        private static void DeleteTestFolders()
        {
            if (AssetDatabase.IsValidFolder(TestFolderPath)) AssetDatabase.DeleteAsset(TestFolderPath);
            if (AssetDatabase.IsValidFolder(MovedTestFolderPath)) AssetDatabase.DeleteAsset(MovedTestFolderPath);
        }

        private sealed class TestAsset : ScriptableObject
        {
        }
    }
}
#endif
