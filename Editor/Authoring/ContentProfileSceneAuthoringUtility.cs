using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Common;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    internal static class ContentProfileSceneAuthoringUtility
    {
        internal static bool TryAddActivityScene(
            ActivityContentProfileAsset profile,
            SceneAsset scene,
            out string result)
        {
            return TryAddScene(
                profile,
                scene,
                "scenes",
                "activity.content",
                true,
                out result);
        }

        internal static bool TryAddRouteScene(
            RouteContentProfileAsset profile,
            SceneAsset scene,
            out string result)
        {
            return TryAddScene(
                profile,
                scene,
                "additionalScenes",
                "route.content",
                false,
                out result);
        }

        private static bool TryAddScene(
            ScriptableObject profile,
            SceneAsset scene,
            string arrayPropertyName,
            string identityDomain,
            bool isActivityEntry,
            out string result)
        {
            if (profile == null)
            {
                result =
                    "Assign or create a Content Profile first.";
                return false;
            }

            if (scene == null)
            {
                result =
                    "Select a valid Scene asset.";
                return false;
            }

            string scenePath =
                AssetDatabase.GetAssetPath(scene);

            if (string.IsNullOrWhiteSpace(
                    scenePath))
            {
                result =
                    "The selected Scene has no project asset path.";
                return false;
            }

            var serializedProfile =
                new SerializedObject(profile);

            SerializedProperty entries =
                serializedProfile.FindProperty(
                    arrayPropertyName);

            if (entries == null)
            {
                result =
                    $"Profile '{profile.name}' has no supported Scene collection.";
                return false;
            }

            var existingIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            for (int index = 0;
                 index < entries.arraySize;
                 index++)
            {
                SerializedProperty existingEntry =
                    entries.GetArrayElementAtIndex(index);

                SerializedProperty existingPath =
                    existingEntry.FindPropertyRelative(
                        "scenePath");

                SerializedProperty existingId =
                    existingEntry.FindPropertyRelative(
                        "contentId");

                if (existingPath != null &&
                    string.Equals(
                        existingPath.stringValue,
                        scenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result =
                        $"Scene '{scene.name}' is already declared by '{profile.name}'.";
                    return false;
                }

                if (existingId != null &&
                    !string.IsNullOrWhiteSpace(
                        existingId.stringValue))
                {
                    existingIds.Add(
                        existingId.stringValue.Trim());
                }
            }

            string suggestedId =
                FrameworkAuthoringSuggestionUtility
                    .SuggestIdentity(
                        scene,
                        identityDomain);

            string uniqueId =
                MakeUniqueIdentity(
                    suggestedId,
                    existingIds);

            Undo.RecordObject(
                profile,
                isActivityEntry
                    ? "Add Activity Content Scene"
                    : "Add Route Content Scene");

            int newIndex =
                entries.arraySize;

            entries.InsertArrayElementAtIndex(
                newIndex);

            SerializedProperty entry =
                entries.GetArrayElementAtIndex(
                    newIndex);

            ResetEntry(
                entry,
                isActivityEntry);

            entry.FindPropertyRelative("contentId")
                .stringValue =
                uniqueId;

            entry.FindPropertyRelative("scenePath")
                .stringValue =
                scenePath;

            entry.FindPropertyRelative("sceneName")
                .stringValue =
                scene.name;

            serializedProfile.ApplyModifiedProperties();

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);

            result =
                $"Added {scene.name} as {uniqueId}.";

            return true;
        }

        private static string MakeUniqueIdentity(
            string suggestedId,
            ISet<string> existingIds)
        {
            if (!existingIds.Contains(
                    suggestedId))
            {
                return suggestedId;
            }

            for (int suffix = 2;
                 suffix < int.MaxValue;
                 suffix++)
            {
                string candidate =
                    $"{suggestedId}-{suffix}";

                if (!existingIds.Contains(
                        candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"Could not create a unique Content Id from '{suggestedId}'.");
        }

        private static void ResetEntry(
            SerializedProperty entry,
            bool isActivityEntry)
        {
            entry.FindPropertyRelative("contentId")
                .stringValue = string.Empty;

            entry.FindPropertyRelative("scenePath")
                .stringValue = string.Empty;

            entry.FindPropertyRelative("sceneName")
                .stringValue = string.Empty;

            entry.FindPropertyRelative("requiredness")
                .enumValueIndex = 0;

            if (!isActivityEntry)
            {
                return;
            }

            entry.FindPropertyRelative("loadMode")
                .enumValueIndex = 0;

            entry.FindPropertyRelative("releasePolicy")
                .enumValueIndex = 0;
        }
    }
}
