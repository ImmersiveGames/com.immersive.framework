using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    internal sealed class ContentProfileSceneReferenceSynchronizer :
        AssetPostprocessor
    {
        private const string LogPrefix =
            "[IMMERSIVE_FRAMEWORK_SCENE_REFERENCE]";

        private static readonly Dictionary<string, string>
            PendingSceneMoves =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

        private static bool _synchronizationScheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (movedAssets == null ||
                movedFromAssetPaths == null)
            {
                return;
            }

            int pairCount =
                Math.Min(
                    movedAssets.Length,
                    movedFromAssetPaths.Length);

            for (int index = 0;
                 index < pairCount;
                 index++)
            {
                string oldPath =
                    movedFromAssetPaths[index];

                string newPath =
                    movedAssets[index];

                if (!IsScenePath(oldPath) ||
                    !IsScenePath(newPath))
                {
                    continue;
                }

                PendingSceneMoves[oldPath] =
                    newPath;
            }

            if (PendingSceneMoves.Count == 0 ||
                _synchronizationScheduled)
            {
                return;
            }

            _synchronizationScheduled = true;

            EditorApplication.delayCall +=
                SynchronizePendingSceneMoves;
        }

        private static void SynchronizePendingSceneMoves()
        {
            _synchronizationScheduled = false;

            if (PendingSceneMoves.Count == 0)
            {
                return;
            }

            var moves =
                new Dictionary<string, string>(
                    PendingSceneMoves,
                    StringComparer.OrdinalIgnoreCase);

            PendingSceneMoves.Clear();

            int activityProfilesUpdated =
                SynchronizeProfiles<ActivityContentProfileAsset>(
                    "scenes",
                    moves);

            int routeProfilesUpdated =
                SynchronizeProfiles<RouteContentProfileAsset>(
                    "additionalScenes",
                    moves);

            int routeAssetsUpdated =
                SynchronizeRoutePrimaryScenes(
                    moves);

            int totalUpdated =
                activityProfilesUpdated +
                routeProfilesUpdated +
                routeAssetsUpdated;

            if (totalUpdated <= 0)
            {
                return;
            }

            Debug.Log(
                $"{LogPrefix} Scene references synchronized after rename/move. " +
                $"activityProfiles='{activityProfilesUpdated}' " +
                $"routeProfiles='{routeProfilesUpdated}' " +
                $"routeAssets='{routeAssetsUpdated}'.");
        }

        private static int SynchronizeProfiles<TProfile>(
            string arrayPropertyName,
            IReadOnlyDictionary<string, string> moves)
            where TProfile : ScriptableObject
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    $"t:{typeof(TProfile).Name}");

            int profilesUpdated = 0;

            for (int profileIndex = 0;
                 profileIndex < guids.Length;
                 profileIndex++)
            {
                string profilePath =
                    AssetDatabase.GUIDToAssetPath(
                        guids[profileIndex]);

                TProfile profile =
                    AssetDatabase.LoadAssetAtPath<TProfile>(
                        profilePath);

                if (profile == null)
                {
                    continue;
                }

                var serializedProfile =
                    new SerializedObject(profile);

                SerializedProperty entries =
                    serializedProfile.FindProperty(
                        arrayPropertyName);

                if (entries == null)
                {
                    continue;
                }

                bool changed = false;

                for (int entryIndex = 0;
                     entryIndex < entries.arraySize;
                     entryIndex++)
                {
                    SerializedProperty entry =
                        entries.GetArrayElementAtIndex(
                            entryIndex);

                    SerializedProperty scenePath =
                        entry.FindPropertyRelative(
                            "scenePath");

                    SerializedProperty sceneName =
                        entry.FindPropertyRelative(
                            "sceneName");

                    if (scenePath == null ||
                        sceneName == null ||
                        !moves.TryGetValue(
                            scenePath.stringValue,
                            out string newPath))
                    {
                        continue;
                    }

                    scenePath.stringValue =
                        newPath;

                    sceneName.stringValue =
                        Path.GetFileNameWithoutExtension(
                            newPath);

                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                serializedProfile.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);

                profilesUpdated++;
            }

            return profilesUpdated;
        }


        private static int SynchronizeRoutePrimaryScenes(
            IReadOnlyDictionary<string, string> moves)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:RouteAsset");

            int routesUpdated = 0;

            for (int routeIndex = 0;
                 routeIndex < guids.Length;
                 routeIndex++)
            {
                string routePath =
                    AssetDatabase.GUIDToAssetPath(
                        guids[routeIndex]);

                RouteAsset route =
                    AssetDatabase.LoadAssetAtPath<RouteAsset>(
                        routePath);

                if (route == null)
                {
                    continue;
                }

                var serializedRoute =
                    new SerializedObject(route);

                SerializedProperty primaryScenePath =
                    serializedRoute.FindProperty(
                        "primaryScenePath");

                SerializedProperty primarySceneName =
                    serializedRoute.FindProperty(
                        "primarySceneName");

                if (primaryScenePath == null ||
                    primarySceneName == null ||
                    !moves.TryGetValue(
                        primaryScenePath.stringValue,
                        out string newPath))
                {
                    continue;
                }

                primaryScenePath.stringValue =
                    newPath;

                primarySceneName.stringValue =
                    Path.GetFileNameWithoutExtension(
                        newPath);

                serializedRoute
                    .ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(route);
                AssetDatabase.SaveAssetIfDirty(route);

                routesUpdated++;
            }

            return routesUpdated;
        }

        private static bool IsScenePath(
            string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.EndsWith(
                       ".unity",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
