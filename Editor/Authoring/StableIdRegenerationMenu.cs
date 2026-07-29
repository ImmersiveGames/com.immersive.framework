using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    /// <summary>
    /// Provides the explicit Project-window workflow for replacing a duplicated
    /// stable identity without changing the remaining asset authoring.
    /// </summary>
    internal static class StableIdRegenerationMenu
    {
        private const string MenuPath =
            "Assets/Immersive Framework/Identity/Regenerate Stable ID...";

        [MenuItem(MenuPath, false, 2000)]
        private static void RegenerateStableId()
        {
            if (!TryGetSelectedAsset(out UnityEngine.Object asset))
            {
                return;
            }

            if (asset is RouteAsset route)
            {
                RegenerateRouteId(route);
                return;
            }

            if (asset is ActivityAsset activity)
            {
                RegenerateActivityId(activity);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRegenerateStableId()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Selection.objects == null ||
                Selection.objects.Length != 1)
            {
                return false;
            }

            UnityEngine.Object selected = Selection.activeObject;
            return selected is RouteAsset || selected is ActivityAsset;
        }

        private static bool TryGetSelectedAsset(out UnityEngine.Object asset)
        {
            asset = null;
            if (!ValidateRegenerateStableId())
            {
                return false;
            }

            asset = Selection.activeObject;
            return asset != null;
        }

        private static void RegenerateRouteId(RouteAsset route)
        {
            string currentId = GetSerializedId(route, "routeId");
            if (!ConfirmRegeneration(route, "Route", currentId))
            {
                return;
            }

            if (!TryGenerateUnusedRouteId(route, currentId, out string newId))
            {
                ShowGenerationFailure(route, "Route");
                return;
            }

            ReplaceId(route, "routeId", newId, "Regenerate Route Stable ID");
        }

        private static void RegenerateActivityId(ActivityAsset activity)
        {
            string currentId = GetSerializedId(activity, "activityId");
            if (!ConfirmRegeneration(activity, "Activity", currentId))
            {
                return;
            }

            if (!TryGenerateUnusedActivityId(activity, currentId, out string newId))
            {
                ShowGenerationFailure(activity, "Activity");
                return;
            }

            ReplaceId(activity, "activityId", newId, "Regenerate Activity Stable ID");
        }

        private static bool ConfirmRegeneration(
            UnityEngine.Object asset,
            string assetKind,
            string currentId)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string displayedId = string.IsNullOrWhiteSpace(currentId)
                ? "<missing>"
                : currentId;
            return EditorUtility.DisplayDialog(
                "Regenerate Stable ID",
                $"Asset: {asset.name}\nPath: {path}\nCurrent {assetKind} ID: {displayedId}\n\n" +
                "This replaces only the stable ID. All other configuration is preserved. " +
                "References that identify this asset by its current ID must be updated manually.",
                "Regenerate",
                "Cancel");
        }

        private static string GetSerializedId(
            UnityEngine.Object asset,
            string propertyName)
        {
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.stringValue : string.Empty;
        }

        private static void ReplaceId(
            UnityEngine.Object asset,
            string propertyName,
            string newId,
            string undoName)
        {
            Undo.RecordObject(asset, undoName);
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Stable ID property '{propertyName}' was not found on '{asset.name}'.");
            }

            property.stringValue = newId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static bool TryGenerateUnusedRouteId(
            RouteAsset selected,
            string currentId,
            out string newId)
        {
            return TryGenerateUnusedId(
                selected,
                currentId,
                "t:RouteAsset",
                "routeId",
                ImmersiveFrameworkEditorSettingsUtility.GenerateRouteIdText,
                out newId);
        }

        private static bool TryGenerateUnusedActivityId(
            ActivityAsset selected,
            string currentId,
            out string newId)
        {
            return TryGenerateUnusedId(
                selected,
                currentId,
                "t:ActivityAsset",
                "activityId",
                ImmersiveFrameworkEditorSettingsUtility.GenerateActivityIdText,
                out newId);
        }

        private static bool TryGenerateUnusedId(
            UnityEngine.Object selected,
            string currentId,
            string assetFilter,
            string propertyName,
            Func<string> generateId,
            out string newId)
        {
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets(assetFilter);
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null || asset == selected)
                {
                    continue;
                }

                string id = GetSerializedId(asset, propertyName);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    usedIds.Add(id);
                }
            }

            for (int attempt = 0; attempt < 1024; attempt++)
            {
                string candidate = generateId();
                if (!string.Equals(candidate, currentId, StringComparison.Ordinal) &&
                    !usedIds.Contains(candidate))
                {
                    newId = candidate;
                    return true;
                }
            }

            newId = string.Empty;
            return false;
        }

        private static void ShowGenerationFailure(
            UnityEngine.Object asset,
            string assetKind)
        {
            EditorUtility.DisplayDialog(
                "Stable ID Was Not Regenerated",
                $"A unique {assetKind} ID could not be generated for '{AssetDatabase.GetAssetPath(asset)}'. No changes were made.",
                "OK");
        }
    }
}
