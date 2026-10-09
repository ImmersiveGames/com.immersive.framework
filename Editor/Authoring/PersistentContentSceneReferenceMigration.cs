using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    internal enum PersistentContentSceneReferenceMigrationStatus
    {
        Migrated = 0,
        Unchanged = 1,
        Ignored = 2,
        Invalid = 3,
        Conflict = 4
    }

    internal readonly struct PersistentContentSceneReferenceMigrationResult
    {
        internal PersistentContentSceneReferenceMigrationResult(
            string assetPath,
            PersistentContentSceneReferenceMigrationStatus status,
            string message)
        {
            AssetPath = assetPath ?? string.Empty;
            Status = status;
            Message = message ?? string.Empty;
        }

        internal string AssetPath { get; }

        internal PersistentContentSceneReferenceMigrationStatus Status { get; }

        internal string Message { get; }
    }

    internal static class PersistentContentSceneReferenceMigration
    {
        private const string MenuPath =
            "Tools/Immersive Framework/Migrate Persistent Content Scene References";

        [MenuItem(MenuPath)]
        private static void MigrateAllMenu()
        {
            IReadOnlyList<PersistentContentSceneReferenceMigrationResult> results =
                MigrateAll();

            int migrated = Count(results, PersistentContentSceneReferenceMigrationStatus.Migrated);
            int unchanged = Count(results, PersistentContentSceneReferenceMigrationStatus.Unchanged);
            int ignored = Count(results, PersistentContentSceneReferenceMigrationStatus.Ignored);
            int invalid = Count(results, PersistentContentSceneReferenceMigrationStatus.Invalid);
            int conflicts = Count(results, PersistentContentSceneReferenceMigrationStatus.Conflict);

            for (int index = 0; index < results.Count; index++)
            {
                PersistentContentSceneReferenceMigrationResult result = results[index];
                if (result.Status == PersistentContentSceneReferenceMigrationStatus.Migrated)
                    Debug.Log($"[IF-ADR-045] Migrated '{result.AssetPath}'. {result.Message}");
                else if (result.Status == PersistentContentSceneReferenceMigrationStatus.Invalid ||
                         result.Status == PersistentContentSceneReferenceMigrationStatus.Conflict)
                    Debug.LogError($"[IF-ADR-045] {result.Status} '{result.AssetPath}'. {result.Message}");
                else
                    Debug.Log($"[IF-ADR-045] {result.Status} '{result.AssetPath}'. {result.Message}");
            }

            Debug.Log(
                $"[IF-ADR-045] Persistent Content scene-reference migration finished. " +
                $"migrated='{migrated}' unchanged='{unchanged}' ignored='{ignored}' " +
                $"invalid='{invalid}' conflicts='{conflicts}'.");
        }

        internal static IReadOnlyList<PersistentContentSceneReferenceMigrationResult> MigrateAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:GameApplicationAsset");
            var applications = new List<GameApplicationAsset>(assetGuids.Length);

            for (int index = 0; index < assetGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[index]);
                GameApplicationAsset application =
                    AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(assetPath);
                if (application == null)
                    continue;

                applications.Add(application);
            }

            return MigrateAssets(applications);
        }

        internal static IReadOnlyList<PersistentContentSceneReferenceMigrationResult> MigrateAssets(
            IEnumerable<GameApplicationAsset> applications)
        {
            var results = new List<PersistentContentSceneReferenceMigrationResult>();
            if (applications != null)
            {
                foreach (GameApplicationAsset application in applications)
                    results.Add(MigrateAsset(application));
            }

            return results.AsReadOnly();
        }

        internal static PersistentContentSceneReferenceMigrationResult MigrateAsset(
            GameApplicationAsset application)
        {
            string applicationPath = application != null
                ? AssetDatabase.GetAssetPath(application)
                : string.Empty;
            if (application == null)
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Invalid,
                    "Game Application asset is missing.");
            }

            var serializedApplication = new SerializedObject(application);
            SerializedProperty composition =
                serializedApplication.FindProperty("persistentContent");
            SerializedProperty pathProperty = composition?.FindPropertyRelative("scenePath");
            SerializedProperty nameProperty = composition?.FindPropertyRelative("sceneName");
            SerializedProperty legacyProperty = composition?.FindPropertyRelative("containerScene");
            if (pathProperty == null || nameProperty == null || legacyProperty == null)
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Invalid,
                    "Persistent Content serialized fields are incomplete; no data was changed.");
            }

            string currentPath = pathProperty.stringValue?.Trim() ?? string.Empty;
            string currentName = nameProperty.stringValue?.Trim() ?? string.Empty;
            UnityEngine.Object legacyReference = legacyProperty.objectReferenceValue;

            if (legacyReference == null)
            {
                if (string.IsNullOrWhiteSpace(currentPath))
                {
                    return Result(applicationPath,
                        string.IsNullOrWhiteSpace(currentName)
                            ? PersistentContentSceneReferenceMigrationStatus.Ignored
                            : PersistentContentSceneReferenceMigrationStatus.Unchanged,
                        string.IsNullOrWhiteSpace(currentName)
                            ? "No Persistent Content scene reference is authored."
                            : "Name-only compatibility data was left unchanged because it has no legacy object path to convert.");
                }

                if (!TryLoadSceneAsset(currentPath, out SceneAsset pathScene))
                {
                    return Result(applicationPath,
                        PersistentContentSceneReferenceMigrationStatus.Invalid,
                        $"Serialized scenePath '{currentPath}' does not resolve to a Unity Scene asset; no data was changed.");
                }

                if (!string.IsNullOrWhiteSpace(currentName) &&
                    !string.Equals(currentName, pathScene.name, StringComparison.Ordinal))
                {
                    return Result(applicationPath,
                        PersistentContentSceneReferenceMigrationStatus.Conflict,
                        $"Serialized sceneName '{currentName}' conflicts with scenePath '{currentPath}' resolving to '{pathScene.name}'; no data was changed.");
                }

                if (!string.IsNullOrWhiteSpace(currentName))
                {
                    return Result(applicationPath,
                        PersistentContentSceneReferenceMigrationStatus.Unchanged,
                        "Path and cached name already identify the same Scene asset.");
                }

                nameProperty.stringValue = pathScene.name;
                serializedApplication.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(application);
                AssetDatabase.SaveAssetIfDirty(application);
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Migrated,
                    $"Filled the missing cached sceneName from scenePath '{currentPath}'.");
            }

            if (legacyReference is not SceneAsset legacyScene)
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Invalid,
                    $"Legacy reference '{legacyReference.name}' is not a Unity Scene asset; no data was changed.");
            }

            string legacyPath = AssetDatabase.GetAssetPath(legacyScene);
            if (!TryLoadSceneAsset(legacyPath, out SceneAsset resolvedLegacyScene))
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Invalid,
                    $"Legacy reference path '{legacyPath}' is not a valid .unity Scene asset; no data was changed.");
            }

            if (!string.IsNullOrWhiteSpace(currentPath) &&
                !string.Equals(currentPath, legacyPath, StringComparison.OrdinalIgnoreCase))
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Conflict,
                    $"Modern scenePath '{currentPath}' conflicts with legacy Scene '{legacyPath}'; no data was changed.");
            }

            if (!string.IsNullOrWhiteSpace(currentName) &&
                !string.Equals(currentName, resolvedLegacyScene.name, StringComparison.Ordinal))
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Conflict,
                    $"Modern sceneName '{currentName}' conflicts with legacy Scene name '{resolvedLegacyScene.name}'; no data was changed.");
            }

            bool changed = false;
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                pathProperty.stringValue = legacyPath;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(currentName))
            {
                nameProperty.stringValue = resolvedLegacyScene.name;
                changed = true;
            }

            if (!changed)
            {
                return Result(applicationPath,
                    PersistentContentSceneReferenceMigrationStatus.Unchanged,
                    "Modern path/name values already agree with the retained legacy Scene reference.");
            }

            serializedApplication.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssetIfDirty(application);
            return Result(applicationPath,
                PersistentContentSceneReferenceMigrationStatus.Migrated,
                $"Stored scenePath '{legacyPath}' and sceneName '{resolvedLegacyScene.name}'; retained the legacy reference.");
        }

        private static bool TryLoadSceneAsset(string path, out SceneAsset scene)
        {
            scene = null;
            if (string.IsNullOrWhiteSpace(path) ||
                !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            return scene != null;
        }

        private static PersistentContentSceneReferenceMigrationResult Result(
            string assetPath,
            PersistentContentSceneReferenceMigrationStatus status,
            string message)
        {
            return new PersistentContentSceneReferenceMigrationResult(
                assetPath,
                status,
                message);
        }

        private static int Count(
            IReadOnlyList<PersistentContentSceneReferenceMigrationResult> results,
            PersistentContentSceneReferenceMigrationStatus status)
        {
            int count = 0;
            for (int index = 0; index < results.Count; index++)
            {
                if (results[index].Status == status)
                    count++;
            }

            return count;
        }
    }
}
