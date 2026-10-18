using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    internal enum ActorProfileVisualContentMigrationStatus
    {
        Migrated = 0,
        Unchanged = 1,
        Ignored = 2,
        Invalid = 3,
        Conflict = 4
    }

    internal readonly struct ActorProfileVisualContentMigrationResult
    {
        internal ActorProfileVisualContentMigrationResult(
            string assetPath,
            ActorProfileVisualContentMigrationStatus status,
            string message)
        {
            AssetPath = assetPath ?? string.Empty;
            Status = status;
            Message = message ?? string.Empty;
        }

        internal string AssetPath { get; }
        internal ActorProfileVisualContentMigrationStatus Status { get; }
        internal string Message { get; }
    }

    internal static class ActorProfileVisualContentMigration
    {
        private const string MenuPath =
            "Tools/Immersive Framework/Migrate Actor Profile Visual Content";

        [MenuItem(MenuPath)]
        private static void MigrateAllMenu()
        {
            IReadOnlyList<ActorProfileVisualContentMigrationResult> results = MigrateAll();
            int migrated = Count(results, ActorProfileVisualContentMigrationStatus.Migrated);
            int unchanged = Count(results, ActorProfileVisualContentMigrationStatus.Unchanged);
            int ignored = Count(results, ActorProfileVisualContentMigrationStatus.Ignored);
            int invalid = Count(results, ActorProfileVisualContentMigrationStatus.Invalid);
            int conflicts = Count(results, ActorProfileVisualContentMigrationStatus.Conflict);

            for (int index = 0; index < results.Count; index++)
            {
                ActorProfileVisualContentMigrationResult result = results[index];
                if (result.Status is ActorProfileVisualContentMigrationStatus.Invalid or ActorProfileVisualContentMigrationStatus.Conflict)
                {
                    Debug.LogError($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}");
                }
                else
                {
                    Debug.Log($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}");
                }
            }

            Debug.Log(
                $"[IF-ADR-050] Actor Profile visual migration finished. migrated='{migrated}' " +
                $"unchanged='{unchanged}' ignored='{ignored}' invalid='{invalid}' conflicts='{conflicts}'.");
        }

        internal static IReadOnlyList<ActorProfileVisualContentMigrationResult> MigrateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActorProfile");
            var profiles = new List<ActorProfile>(guids.Length);
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ActorProfile profile = AssetDatabase.LoadAssetAtPath<ActorProfile>(path);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            return MigrateAssets(profiles);
        }

        internal static IReadOnlyList<ActorProfileVisualContentMigrationResult> MigrateAssets(
            IEnumerable<ActorProfile> profiles)
        {
            var results = new List<ActorProfileVisualContentMigrationResult>();
            if (profiles != null)
            {
                foreach (ActorProfile profile in profiles)
                {
                    results.Add(MigrateAsset(profile));
                }
            }

            return results.AsReadOnly();
        }

        internal static ActorProfileVisualContentMigrationResult MigrateAsset(
            ActorProfile profile)
        {
            string assetPath = profile != null ? AssetDatabase.GetAssetPath(profile) : string.Empty;
            if (profile == null)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                    "Actor Profile asset is missing.");
            }

            var serialized = new SerializedObject(profile);
            SerializedProperty legacy = serialized.FindProperty("legacyPresentationPrefab");
            SerializedProperty parts = serialized.FindProperty("compositionParts");
            if (legacy == null || parts == null || !parts.isArray)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                    "Actor Profile serialized fields are incomplete; no data was changed.");
            }

            GameObject legacyPrefab = legacy.objectReferenceValue as GameObject;
            if (legacyPrefab == null)
            {
                return Result(assetPath,
                    parts.arraySize == 0
                        ? ActorProfileVisualContentMigrationStatus.Ignored
                        : ActorProfileVisualContentMigrationStatus.Unchanged,
                    "No legacy visual reference remains; composition entries and all asset references were left unchanged.");
            }

            SerializedProperty matchingVisual = null;
            int visualCount = 0;
            for (int index = 0; index < parts.arraySize; index++)
            {
                SerializedProperty part = parts.GetArrayElementAtIndex(index);
                SerializedProperty key = part.FindPropertyRelative("key");
                SerializedProperty prefab = part.FindPropertyRelative("prefab");
                if (key == null || prefab == null)
                {
                    return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                        $"Composition entry '{index}' is incomplete; no data was changed.");
                }

                if (string.Equals(key.stringValue, "visual", StringComparison.Ordinal))
                {
                    visualCount++;
                    matchingVisual = part;
                }
            }

            if (visualCount > 1)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Conflict,
                    "More than one Base 'visual' entry exists; no data was changed.");
            }

            if (visualCount == 1)
            {
                SerializedProperty visualPrefab = matchingVisual.FindPropertyRelative("prefab");
                if (visualPrefab.objectReferenceValue != legacyPrefab)
                {
                    return Result(assetPath, ActorProfileVisualContentMigrationStatus.Conflict,
                        "The existing Base 'visual' entry references a different Prefab from the legacy field; no data was changed.");
                }

                Undo.RecordObject(profile, "Migrate Actor Profile Visual Content");
                legacy.objectReferenceValue = null;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Migrated,
                    $"Cleared the legacy field after verifying the Base 'visual' entry already references '{legacyPrefab.name}'.");
            }

            Undo.RecordObject(profile, "Migrate Actor Profile Visual Content");
            int newIndex = parts.arraySize;
            parts.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newPart = parts.GetArrayElementAtIndex(newIndex);
            SerializedProperty newKey = newPart.FindPropertyRelative("key");
            SerializedProperty newPrefab = newPart.FindPropertyRelative("prefab");
            SerializedProperty newDependencies = newPart.FindPropertyRelative("dependencies");
            if (newKey == null || newPrefab == null || newDependencies == null)
            {
                serialized.Update();
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                    "The new composition entry could not be initialized; no data was saved.");
            }

            newKey.stringValue = "visual";
            newPrefab.objectReferenceValue = legacyPrefab;
            newDependencies.arraySize = 0;
            legacy.objectReferenceValue = null;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return Result(assetPath, ActorProfileVisualContentMigrationStatus.Migrated,
                $"Copied '{legacyPrefab.name}' into the Base 'visual' entry and cleared the migration source. Profile, Prefab and consumer reference GUIDs are unchanged.");
        }

        private static ActorProfileVisualContentMigrationResult Result(
            string path,
            ActorProfileVisualContentMigrationStatus status,
            string message)
        {
            return new ActorProfileVisualContentMigrationResult(path, status, message);
        }

        private static int Count(
            IReadOnlyList<ActorProfileVisualContentMigrationResult> results,
            ActorProfileVisualContentMigrationStatus status)
        {
            int count = 0;
            for (int index = 0; index < results.Count; index++)
            {
                if (results[index].Status == status)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
