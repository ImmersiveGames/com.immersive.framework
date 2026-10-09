using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.GlobalUi;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class PersistentContentSceneReferenceTests
    {
        private const string TestRoot = "Assets/__IFADR045PersistentContentTests";
        private EditorBuildSettingsScene[] _previousBuildScenes;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            EditorBuildSettingsScene[] currentBuildScenes =
                EditorBuildSettings.scenes;
            _previousBuildScenes = currentBuildScenes != null
                ? (EditorBuildSettingsScene[])currentBuildScenes.Clone()
                : Array.Empty<EditorBuildSettingsScene>();
            _folder = $"{TestRoot}_{Guid.NewGuid():N}";
            try
            {
                string folderGuid = AssetDatabase.CreateFolder(
                    "Assets",
                    _folder.Substring("Assets/".Length));
                Assert.IsNotEmpty(folderGuid, $"Could not create temporary test folder '{_folder}'.");
            }
            catch
            {
                try
                {
                    DeleteTemporaryAssets();
                    AssetDatabase.Refresh();
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }

                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_previousBuildScenes != null)
                    EditorBuildSettings.scenes = _previousBuildScenes;
            }
            finally
            {
                try
                {
                    DeleteTemporaryAssets();
                }
                finally
                {
                    AssetDatabase.Refresh();
                }
            }
        }

        private void DeleteTemporaryAssets()
        {
            if (string.IsNullOrWhiteSpace(_folder) ||
                !AssetDatabase.IsValidFolder(_folder))
            {
                return;
            }

            bool deleted = AssetDatabase.DeleteAsset(_folder);
            Assert.IsTrue(
                deleted || !AssetDatabase.IsValidFolder(_folder),
                $"Could not delete temporary test folder '{_folder}'.");
        }

        [Test]
        public void PathOnlyComposition_IsCompleteWithoutLegacyObjectReference()
        {
            GameApplicationAsset application = CreateApplication("PathOnly");
            SetComposition(application, "Assets/Scenes/Persistent.unity", "Persistent", null);

            PersistentContentComposition composition = application.PersistentContent;

            Assert.AreEqual("Assets/Scenes/Persistent.unity", composition.ContainerScenePath);
            Assert.AreEqual("Persistent", composition.ContainerSceneName);
            Assert.IsTrue(composition.HasContainerScene);
            Assert.IsTrue(composition.IsComplete);
            Assert.IsNull(composition.ContainerScene);
        }

        [Test]
        public void LegacyObjectOnlyComposition_IsIncompleteUntilExplicitMigration()
        {
            SceneAsset scene = CreateScene("Persistent");
            GameApplicationAsset application = CreateApplication("LegacyOnly");
            SetComposition(application, string.Empty, string.Empty, scene);

            Assert.IsFalse(application.PersistentContent.HasContainerScene);
            Assert.IsFalse(application.PersistentContent.IsComplete);
        }

        [Test]
        public void Migration_ConvertsLegacyReferenceAndPreservesStableGetter()
        {
            SceneAsset scene = CreateScene("Persistent");
            GameApplicationAsset application = CreateApplication("Migration");
            SetComposition(application, string.Empty, string.Empty, scene);

            PersistentContentSceneReferenceMigrationResult result =
                PersistentContentSceneReferenceMigration.MigrateAsset(application);

            Assert.AreEqual(PersistentContentSceneReferenceMigrationStatus.Migrated, result.Status);
            Assert.AreEqual(AssetDatabase.GetAssetPath(scene), application.PersistentContent.ContainerScenePath);
            Assert.AreEqual(scene.name, application.PersistentContent.ContainerSceneName);
            Assert.AreSame(scene, application.PersistentContent.ContainerScene);
            Assert.IsTrue(application.PersistentContent.IsComplete);
        }

        [Test]
        public void Migration_RepeatedRunDoesNotChangeMigratedAsset()
        {
            SceneAsset scene = CreateScene("Persistent");
            GameApplicationAsset application = CreateApplication("RepeatMigration");
            SetComposition(application, string.Empty, string.Empty, scene);
            PersistentContentSceneReferenceMigration.MigrateAsset(application);
            string pathBefore = application.PersistentContent.ContainerScenePath;
            string nameBefore = application.PersistentContent.ContainerSceneName;

            PersistentContentSceneReferenceMigrationResult result =
                PersistentContentSceneReferenceMigration.MigrateAsset(application);

            Assert.AreEqual(PersistentContentSceneReferenceMigrationStatus.Unchanged, result.Status);
            Assert.AreEqual(pathBefore, application.PersistentContent.ContainerScenePath);
            Assert.AreEqual(nameBefore, application.PersistentContent.ContainerSceneName);
        }

        [Test]
        public void BatchMigration_ProcessesAssetsWithoutInspectorInteraction()
        {
            SceneAsset scene = CreateScene("Persistent");
            GameApplicationAsset first = CreateApplication("FirstApplication");
            GameApplicationAsset second = CreateApplication("SecondApplication");
            SetComposition(first, string.Empty, string.Empty, scene);
            SetComposition(second, string.Empty, string.Empty, scene);

            IReadOnlyList<PersistentContentSceneReferenceMigrationResult> results =
                PersistentContentSceneReferenceMigration.MigrateAssets(
                    new[] { first, second });

            Assert.That(results.Count(result =>
                    result.Status == PersistentContentSceneReferenceMigrationStatus.Migrated &&
                    (result.AssetPath == AssetDatabase.GetAssetPath(first) ||
                     result.AssetPath == AssetDatabase.GetAssetPath(second))),
                Is.EqualTo(2));
            Assert.AreEqual(AssetDatabase.GetAssetPath(scene), first.PersistentContent.ContainerScenePath);
            Assert.AreEqual(AssetDatabase.GetAssetPath(scene), second.PersistentContent.ContainerScenePath);
        }

        [Test]
        public void Migration_InvalidLegacyReferenceIsReportedAndPreserved()
        {
            GameApplicationAsset application = CreateApplication("InvalidLegacy");
            RouteAsset invalidReference = ScriptableObject.CreateInstance<RouteAsset>();
            string invalidReferencePath = $"{_folder}/InvalidLegacyRoute.asset";
            AssetDatabase.CreateAsset(invalidReference, invalidReferencePath);
            SetComposition(application, string.Empty, string.Empty, invalidReference);
            AssetDatabase.SaveAssetIfDirty(application);

            PersistentContentSceneReferenceMigrationResult result =
                PersistentContentSceneReferenceMigration.MigrateAsset(application);

            var persistedApplication = new SerializedObject(application);
            SerializedProperty persistedComposition =
                persistedApplication.FindProperty("persistentContent");
            UnityEngine.Object persistedReference =
                persistedComposition.FindPropertyRelative("containerScene").objectReferenceValue;

            Assert.AreEqual(PersistentContentSceneReferenceMigrationStatus.Invalid, result.Status);
            Assert.AreSame(invalidReference, persistedReference);
            Assert.AreEqual(invalidReferencePath, AssetDatabase.GetAssetPath(persistedReference));
            Assert.AreSame(invalidReference, application.PersistentContent.ContainerScene);
            Assert.IsEmpty(application.PersistentContent.ContainerScenePath);
        }

        [Test]
        public void Migration_DoesNotOverwriteConflictingModernPath()
        {
            SceneAsset legacyScene = CreateScene("Legacy");
            SceneAsset modernScene = CreateScene("Modern");
            GameApplicationAsset application = CreateApplication("Conflict");
            string modernPath = AssetDatabase.GetAssetPath(modernScene);
            SetComposition(application, modernPath, modernScene.name, legacyScene);

            PersistentContentSceneReferenceMigrationResult result =
                PersistentContentSceneReferenceMigration.MigrateAsset(application);

            Assert.AreEqual(PersistentContentSceneReferenceMigrationStatus.Conflict, result.Status);
            Assert.AreEqual(modernPath, application.PersistentContent.ContainerScenePath);
            Assert.AreSame(legacyScene, application.PersistentContent.ContainerScene);
        }

        [Test]
        public void ExplicitInvalidPath_DoesNotFallbackToHomonymousBuildScene()
        {
            string homonymousPath = $"{_folder}/Other/Shared.unity";

            bool resolved = GlobalUiSceneRuntime.TryResolveSceneLoadIdentifierFromCandidates(
                $"{_folder}/Missing/Shared.unity",
                "Shared",
                new[] { homonymousPath },
                _ => true,
                out string identifier,
                out string diagnostic);

            Assert.IsFalse(resolved, diagnostic);
            Assert.IsEmpty(identifier);
            StringAssert.Contains("path", diagnostic.ToLowerInvariant());
        }

        [Test]
        public void NameOnlyReference_WithDuplicateBuildCandidatesIsRejected()
        {
            string firstPath = $"{_folder}/A/Shared.unity";
            string secondPath = $"{_folder}/B/Shared.unity";
            bool resolved = GlobalUiSceneRuntime.TryResolveSceneLoadIdentifierFromCandidates(
                string.Empty,
                "Shared",
                new[] { firstPath, secondPath },
                _ => true,
                out string identifier,
                out string diagnostic);

            Assert.IsFalse(resolved, diagnostic);
            Assert.IsEmpty(identifier);
            StringAssert.Contains("ambiguous", diagnostic.ToLowerInvariant());
        }

        [Test]
        public void NameOnlyReference_WithSingleCandidateResolvesToItsPath()
        {
            string expectedPath = $"{_folder}/Shared.unity";
            bool resolved = GlobalUiSceneRuntime.TryResolveSceneLoadIdentifierFromCandidates(
                string.Empty,
                "Shared",
                new[] { expectedPath },
                path => string.Equals(path, expectedPath, StringComparison.Ordinal),
                out string identifier,
                out string diagnostic);

            Assert.IsTrue(resolved, diagnostic);
            Assert.AreEqual(expectedPath, identifier);
        }

        [Test]
        public void NameOnlyReference_WithSingleUnavailableCandidateIsRejected()
        {
            const string candidatePath = "Assets/Scenes/Shared.unity";
            bool resolved = GlobalUiSceneRuntime.TryResolveSceneLoadIdentifierFromCandidates(
                string.Empty,
                "Shared",
                new[] { candidatePath },
                _ => false,
                out string identifier,
                out string diagnostic);

            Assert.IsFalse(resolved, diagnostic);
            Assert.IsEmpty(identifier);
            StringAssert.Contains("cannot be loaded", diagnostic);
        }

        [Test]
        public void Validator_ReportsMissingPersistentContentReference()
        {
            GameApplicationAsset application = CreateApplication("MissingReference");
            SetComposition(application, string.Empty, string.Empty, null);

            FrameworkAuthoringValidationReport report =
                FrameworkAuthoringValidator.ValidateGameApplication(application, false);

            Assert.That(report.Issues.Any(issue =>
                issue.Message.IndexOf("Persistent Content", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.True);
        }

        private SceneAsset CreateScene(string sceneName, string subdirectory = "")
        {
            string directory = string.IsNullOrWhiteSpace(subdirectory)
                ? _folder
                : $"{_folder}/{subdirectory}";
            if (directory != _folder)
            {
                string folderGuid = AssetDatabase.CreateFolder(_folder, subdirectory);
                Assert.IsNotEmpty(folderGuid, $"Could not create temporary scene folder '{directory}'.");
            }

            string path = $"{directory}/{sceneName}.unity";
            string packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(PersistentContentSceneReferenceTests).Assembly)?.assetPath;
            Assert.IsNotEmpty(packagePath, "Could not resolve the Framework package path for the scene fixture.");
            string templatePath =
                $"{packagePath}/Editor/SceneTemplates/PersistentContent/PersistentContentTemplateSource.unity";
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(templatePath),
                $"Persistent Content scene fixture template was not found at '{templatePath}'.");
            Assert.IsTrue(
                AssetDatabase.CopyAsset(templatePath, path),
                $"Could not copy scene fixture from '{templatePath}' to '{path}'.");

            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            Assert.IsNotNull(asset);
            return asset;
        }

        private GameApplicationAsset CreateApplication(string name)
        {
            GameApplicationAsset application = ScriptableObject.CreateInstance<GameApplicationAsset>();
            string path = $"{_folder}/{name}.asset";
            AssetDatabase.CreateAsset(application, path);
            return application;
        }

        private static void SetComposition(
            GameApplicationAsset application,
            string scenePath,
            string sceneName,
            UnityEngine.Object legacyReference)
        {
            var serialized = new SerializedObject(application);
            SerializedProperty composition = serialized.FindProperty("persistentContent");
            composition.FindPropertyRelative("scenePath").stringValue = scenePath;
            composition.FindPropertyRelative("sceneName").stringValue = sceneName;
            composition.FindPropertyRelative("containerScene").objectReferenceValue = legacyReference;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
        }
    }
}
