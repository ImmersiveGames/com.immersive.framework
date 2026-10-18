using System;
using System.Collections.Generic;
using System.IO;
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
        Conflict = 4,
        Blocked = 5
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
        private const string MenuPath = "Tools/Immersive Framework/Migrate Actor Profile Visual Content";

        [MenuItem(MenuPath)]
        private static void MigrateAllMenu()
        {
            IReadOnlyList<ActorProfileVisualContentMigrationResult> results = MigrateAll();
            int migrated = Count(results, ActorProfileVisualContentMigrationStatus.Migrated);
            int unchanged = Count(results, ActorProfileVisualContentMigrationStatus.Unchanged);
            int ignored = Count(results, ActorProfileVisualContentMigrationStatus.Ignored);
            int invalid = Count(results, ActorProfileVisualContentMigrationStatus.Invalid);
            int conflicts = Count(results, ActorProfileVisualContentMigrationStatus.Conflict);
            int blocked = Count(results, ActorProfileVisualContentMigrationStatus.Blocked);

            for (int index = 0; index < results.Count; index++)
            {
                ActorProfileVisualContentMigrationResult result = results[index];
                if (result.Status is ActorProfileVisualContentMigrationStatus.Invalid or
                    ActorProfileVisualContentMigrationStatus.Conflict)
                {
                    Debug.LogError($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}");
                }
                else if (result.Status == ActorProfileVisualContentMigrationStatus.Blocked)
                {
                    Debug.LogWarning($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}");
                }
                else
                {
                    Debug.Log($"[IF-ADR-050] {result.Status} '{result.AssetPath}'. {result.Message}");
                }
            }

            Debug.Log(
                $"[IF-ADR-050] Actor Profile Base content migration finished. migrated='{migrated}' " +
                $"unchanged='{unchanged}' ignored='{ignored}' invalid='{invalid}' conflicts='{conflicts}' blocked='{blocked}'.");
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

        internal static ActorProfileVisualContentMigrationResult MigrateAsset(ActorProfile profile)
        {
            string assetPath = profile != null ? AssetDatabase.GetAssetPath(profile) : string.Empty;
            if (profile == null)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                    "Actor Profile asset is missing.");
            }

            if (profile.BaseContentParts.Count > 0 && !profile.HasLegacyCompositionData)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Unchanged,
                    "No inline or legacy data remains; current part asset references were left unchanged.");
            }

            IReadOnlyList<ActorCompositionPart> inlineParts =
                new List<ActorCompositionPart>(profile.LegacyCompositionParts).AsReadOnly();
            GameObject legacyPrefab = profile.LegacyPresentationPrefab;
            if (inlineParts.Count == 0 && legacyPrefab == null)
            {
                return Result(assetPath,
                    profile.BaseContentParts.Count == 0
                        ? ActorProfileVisualContentMigrationStatus.Ignored
                        : ActorProfileVisualContentMigrationStatus.Unchanged,
                    "No inline or legacy composition data remains; no placeholder asset was created.");
            }

            var sourceParts = new List<ActorCompositionPart>(inlineParts.Count + 1);
            for (int index = 0; index < inlineParts.Count; index++)
            {
                sourceParts.Add(inlineParts[index]);
            }

            if (!TryMergeLegacyVisual(profile.name, sourceParts, legacyPrefab, out string mergeIssue, out bool conflict))
            {
                return Result(assetPath,
                    conflict
                        ? ActorProfileVisualContentMigrationStatus.Conflict
                        : ActorProfileVisualContentMigrationStatus.Invalid,
                    mergeIssue);
            }

            if (!ActorCompositionProfileContract.TryValidateLegacyParts(
                    profile.name,
                    sourceParts,
                    out string validationIssue))
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                    $"Inline composition is invalid; no data was changed. {validationIssue}");
            }

            for (int index = 0; index < sourceParts.Count; index++)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(sourceParts[index].Prefab))
                {
                    return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                        $"Inline composition key '{sourceParts[index].Key}' does not reference a Prefab asset; no data was changed.");
                }
            }

            if (!TryResolvePaths(assetPath, sourceParts, out string partFolder, out string pathIssue))
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Blocked, pathIssue);
            }

            SerializedObject profileSerialized = new SerializedObject(profile);
            SerializedProperty baseReferences = profileSerialized.FindProperty("baseContentParts");
            if (baseReferences == null || !baseReferences.isArray)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid,
                    "Actor Profile Base content part reference list is incomplete; no data was changed.");
            }

            if (baseReferences.arraySize > sourceParts.Count)
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Conflict,
                    "The existing Base content part references do not match the inline migration input; no data was changed.");
            }

            var outputPaths = new string[sourceParts.Count];
            var outputAssets = new ActorCompositionPartAsset[sourceParts.Count];
            var createAsset = new bool[sourceParts.Count];
            for (int index = 0; index < sourceParts.Count; index++)
            {
                ActorCompositionPart source = sourceParts[index];
                string expectedPath = BuildPartAssetPath(partFolder, index, source.Key);
                outputPaths[index] = expectedPath;

                ActorCompositionPartAsset existingAtPath = AssetDatabase.LoadAssetAtPath<ActorCompositionPartAsset>(expectedPath);
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
                string physicalPath = Path.Combine(projectRoot, expectedPath);
                if ((AssetDatabase.LoadMainAssetAtPath(expectedPath) != null || File.Exists(physicalPath)) &&
                    existingAtPath == null)
                {
                    return Result(assetPath, ActorProfileVisualContentMigrationStatus.Conflict,
                        $"An unrelated asset already occupies generated path '{expectedPath}'; no data was changed.");
                }

                if (existingAtPath != null && !Matches(existingAtPath, source))
                {
                    return Result(assetPath, ActorProfileVisualContentMigrationStatus.Conflict,
                        $"Existing generated asset '{expectedPath}' has different serialized values; it was not overwritten.");
                }

                if (index < baseReferences.arraySize)
                {
                    ActorCompositionPartAsset existingReference =
                        baseReferences.GetArrayElementAtIndex(index).objectReferenceValue as ActorCompositionPartAsset;
                    if (existingReference != null &&
                        (existingReference != existingAtPath || !Matches(existingReference, source)))
                    {
                        return Result(assetPath, ActorProfileVisualContentMigrationStatus.Conflict,
                            $"Existing Base content reference '{index}' does not match its deterministic migration asset; no data was changed.");
                    }
                }

                outputAssets[index] = existingAtPath;
                createAsset[index] = existingAtPath == null;
            }

            if (!EnsureFolder(partFolder, out string folderIssue))
            {
                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Blocked, folderIssue);
            }

            var createdPaths = new List<string>();
            IReadOnlyList<ActorCompositionPartAsset> originalReferences = CopyBaseReferences(baseReferences);
            GameObject originalLegacyPrefab = legacyPrefab;
            var createdPartAssets = new List<ActorCompositionPartAsset>();
            bool profileReferencesChanged = false;
            try
            {
                for (int index = 0; index < sourceParts.Count; index++)
                {
                    if (!createAsset[index])
                    {
                        continue;
                    }

                    ActorCompositionPart source = sourceParts[index];
                    ActorCompositionPartAsset partAsset = ScriptableObject.CreateInstance<ActorCompositionPartAsset>();
                    partAsset.Initialize(source.Key, source.Prefab, source.Dependencies);
                    createdPartAssets.Add(partAsset);
                    createdPaths.Add(outputPaths[index]);
                    AssetDatabase.CreateAsset(partAsset, outputPaths[index]);
                    outputAssets[index] = partAsset;
                }

                Undo.RecordObject(profile, "Migrate Actor Profile Base Content Parts");
                profileSerialized.Update();
                baseReferences = profileSerialized.FindProperty("baseContentParts");
                baseReferences.arraySize = outputAssets.Length;
                for (int index = 0; index < outputAssets.Length; index++)
                {
                    baseReferences.GetArrayElementAtIndex(index).objectReferenceValue = outputAssets[index];
                }

                profileReferencesChanged = true;
                profileSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);

                if (!VerifySavedReferences(profile, outputAssets, sourceParts, out string verifyIssue))
                {
                    throw new InvalidOperationException(verifyIssue);
                }

                SerializedObject clearSources = new SerializedObject(profile);
                SerializedProperty inlineSource = clearSources.FindProperty("compositionParts");
                SerializedProperty legacySource = clearSources.FindProperty("legacyPresentationPrefab");
                if (inlineSource == null || !inlineSource.isArray || legacySource == null)
                {
                    throw new InvalidOperationException(
                        "Actor Profile migration sources could not be cleared after reference verification.");
                }

                inlineSource.arraySize = 0;
                legacySource.objectReferenceValue = null;
                clearSources.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                if (profile.HasLegacyCompositionData)
                {
                    throw new InvalidOperationException(
                        "Actor Profile inline or legacy migration data remained after saving the cleared source fields.");
                }

                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Migrated,
                    $"Created or verified {outputAssets.Length} ordered Base content part asset(s), saved the references, then cleared inline and legacy migration data.");
            }
            catch (Exception exception)
            {
                string rollbackIssue = string.Empty;
                bool restored = !profileReferencesChanged || TryRestoreProfile(
                    profile,
                    originalReferences,
                    inlineParts,
                    originalLegacyPrefab,
                    out rollbackIssue);
                var retainedAssets = new List<string>();
                if (restored)
                {
                    for (int index = createdPaths.Count - 1; index >= 0; index--)
                    {
                        string createdPath = createdPaths[index];
                        bool deleted = AssetDatabase.DeleteAsset(createdPath);
                        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
                        string physicalPath = Path.Combine(projectRoot, createdPath);
                        if (!deleted && (AssetDatabase.LoadMainAssetAtPath(createdPath) != null ||
                            File.Exists(physicalPath)))
                        {
                            retainedAssets.Add(createdPath);
                        }
                    }

                    for (int index = 0; index < createdPartAssets.Count; index++)
                    {
                        if (createdPartAssets[index] != null &&
                            !AssetDatabase.Contains(createdPartAssets[index]))
                        {
                            UnityEngine.Object.DestroyImmediate(createdPartAssets[index]);
                        }
                    }
                }

                string message = $"Actor Profile migration failed without clearing its source data. {exception.GetType().Name}: {exception.Message}";
                if (!restored)
                {
                    message += $" Profile rollback failed: {rollbackIssue} Generated assets were retained to avoid breaking possible references.";
                }
                else if (retainedAssets.Count > 0)
                {
                    message += $" These generated assets could not be removed after rollback and may be orphaned: {string.Join(", ", retainedAssets)}.";
                }

                return Result(assetPath, ActorProfileVisualContentMigrationStatus.Invalid, message);
            }
        }

        private static bool TryMergeLegacyVisual(
            string profileName,
            List<ActorCompositionPart> parts,
            GameObject legacyPrefab,
            out string issue,
            out bool conflict)
        {
            issue = string.Empty;
            conflict = false;
            if (legacyPrefab == null)
            {
                return true;
            }

            ActorCompositionPart visual = null;
            int visualCount = 0;
            for (int index = 0; index < parts.Count; index++)
            {
                ActorCompositionPart part = parts[index];
                if (part != null && string.Equals(part.Key, "visual", StringComparison.Ordinal))
                {
                    visual = part;
                    visualCount++;
                }
            }

            if (visualCount > 1)
            {
                conflict = true;
                issue = $"Actor Profile '{profileName}' has multiple inline Base 'visual' entries; no data was changed.";
                return false;
            }

            if (visualCount == 1)
            {
                if (visual.Prefab != legacyPrefab)
                {
                    conflict = true;
                    issue = $"Actor Profile '{profileName}' has conflicting legacy and inline Base 'visual' Prefabs; no data was changed.";
                    return false;
                }

                return true;
            }

            parts.Add(new ActorCompositionPart("visual", legacyPrefab));
            return true;
        }

        private static bool TryResolvePaths(
            string profilePath,
            IReadOnlyList<ActorCompositionPart> parts,
            out string partFolder,
            out string issue)
        {
            partFolder = string.Empty;
            if (string.IsNullOrWhiteSpace(profilePath) ||
                !profilePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !AssetDatabase.IsOpenForEdit(profilePath))
            {
                issue = "Migration is blocked because the Actor Profile is not under an editable project Assets/ path. No data was changed.";
                return false;
            }

            string directory = Path.GetDirectoryName(profilePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                issue = "Migration is blocked because the Actor Profile's owning folder could not be resolved. No data was changed.";
                return false;
            }

            string profileName = SanitizeFileName(Path.GetFileNameWithoutExtension(profilePath));
            partFolder = $"{directory}/{profileName}_BaseContentParts";
            for (int index = 0; index < parts.Count; index++)
            {
                if (parts[index] == null || string.IsNullOrWhiteSpace(parts[index].Key))
                {
                    issue = $"Migration is blocked because source part '{index}' has no valid key. No data was changed.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool EnsureFolder(string folderPath, out string issue)
        {
            issue = string.Empty;
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            int separator = folderPath.LastIndexOf('/');
            string parent = separator > 0 ? folderPath.Substring(0, separator) : string.Empty;
            string folderName = separator >= 0 ? folderPath.Substring(separator + 1) : folderPath;
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent) ||
                string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, folderName)))
            {
                issue = $"Migration is blocked because the profile-scoped output folder '{folderPath}' could not be created. No source data was changed.";
                return false;
            }

            return true;
        }

        private static string BuildPartAssetPath(string folder, int index, string key)
        {
            return $"{folder}/{index:D2}_{SanitizeFileName(key)}.asset";
        }

        private static string SanitizeFileName(string value)
        {
            string sanitized = string.IsNullOrWhiteSpace(value) ? "Part" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalid, '_');
            }

            return sanitized;
        }

        private static bool Matches(ActorCompositionPartAsset asset, ActorCompositionPart source)
        {
            if (asset == null || source == null ||
                !string.Equals(asset.Key, source.Key, StringComparison.Ordinal) ||
                asset.Prefab != source.Prefab ||
                !asset.HasDependenciesList ||
                asset.Dependencies.Count != source.Dependencies.Count)
            {
                return false;
            }

            for (int index = 0; index < source.Dependencies.Count; index++)
            {
                if (!string.Equals(asset.Dependencies[index], source.Dependencies[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<ActorCompositionPartAsset> CopyBaseReferences(SerializedProperty references)
        {
            var copy = new List<ActorCompositionPartAsset>(references.arraySize);
            for (int index = 0; index < references.arraySize; index++)
            {
                copy.Add(references.GetArrayElementAtIndex(index).objectReferenceValue as ActorCompositionPartAsset);
            }

            return copy.AsReadOnly();
        }

        private static bool VerifySavedReferences(
            ActorProfile profile,
            IReadOnlyList<ActorCompositionPartAsset> expectedAssets,
            IReadOnlyList<ActorCompositionPart> expectedValues,
            out string issue)
        {
            var verify = new SerializedObject(profile);
            SerializedProperty references = verify.FindProperty("baseContentParts");
            if (references == null || !references.isArray || references.arraySize != expectedAssets.Count)
            {
                issue = "Saved Base content part references do not match the prepared migration output.";
                return false;
            }

            for (int index = 0; index < expectedAssets.Count; index++)
            {
                ActorCompositionPartAsset actual =
                    references.GetArrayElementAtIndex(index).objectReferenceValue as ActorCompositionPartAsset;
                if (actual != expectedAssets[index] || !Matches(actual, expectedValues[index]))
                {
                    issue = $"Saved Base content part reference '{index}' failed exact value verification.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryRestoreProfile(
            ActorProfile profile,
            IReadOnlyList<ActorCompositionPartAsset> originalReferences,
            IReadOnlyList<ActorCompositionPart> originalInlineParts,
            GameObject originalLegacyPrefab,
            out string issue)
        {
            try
            {
                var serialized = new SerializedObject(profile);
                SerializedProperty references = serialized.FindProperty("baseContentParts");
                SerializedProperty inline = serialized.FindProperty("compositionParts");
                SerializedProperty legacy = serialized.FindProperty("legacyPresentationPrefab");
                if (references == null || !references.isArray || inline == null || !inline.isArray || legacy == null)
                {
                    issue = "One or more profile serialized fields are missing.";
                    return false;
                }

                references.arraySize = originalReferences.Count;
                for (int index = 0; index < originalReferences.Count; index++)
                {
                    references.GetArrayElementAtIndex(index).objectReferenceValue = originalReferences[index];
                }

                inline.arraySize = originalInlineParts.Count;
                for (int index = 0; index < originalInlineParts.Count; index++)
                {
                    ActorCompositionPart source = originalInlineParts[index];
                    SerializedProperty item = inline.GetArrayElementAtIndex(index);
                    item.FindPropertyRelative("key").stringValue = source.Key;
                    item.FindPropertyRelative("prefab").objectReferenceValue = source.Prefab;
                    SerializedProperty dependencies = item.FindPropertyRelative("dependencies");
                    dependencies.arraySize = source.Dependencies.Count;
                    for (int dependencyIndex = 0; dependencyIndex < source.Dependencies.Count; dependencyIndex++)
                    {
                        dependencies.GetArrayElementAtIndex(dependencyIndex).stringValue =
                            source.Dependencies[dependencyIndex];
                    }
                }

                legacy.objectReferenceValue = originalLegacyPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                issue = $"{exception.GetType().Name}: {exception.Message}";
                return false;
            }
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
