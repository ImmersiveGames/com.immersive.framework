using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    internal static class FrameworkIdentityAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateProjectAssets(FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            var firstAssetById = new Dictionary<ActivityId, string>();
            string[] guids = AssetDatabase.FindAssets("t:ActivityAsset");

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(path);
                if (activity == null)
                {
                    report.AddError($"Activity asset at '{path}' could not be loaded.", null);
                    continue;
                }

                var serialized = new SerializedObject(activity);
                string rawId = serialized.FindProperty("activityId").stringValue;
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    report.AddError($"Activity ID is missing. asset='{path}'.", activity);
                    continue;
                }

                if (!activity.HasValidActivityId)
                {
                    report.AddError($"Activity ID is invalid. id='{rawId}' asset='{path}'.", activity);
                    continue;
                }

                ActivityId id = activity.ActivityId;
                if (firstAssetById.TryGetValue(id, out string firstPath))
                {
                    report.AddError($"Duplicate Activity ID '{id.StableText}'. firstAsset='{firstPath}' secondAsset='{path}' context='Project Activity assets'.", activity);
                    continue;
                }

                firstAssetById.Add(id, path);
            }

            var firstRouteAssetById = new Dictionary<RouteId, string>();
            string[] routeGuids = AssetDatabase.FindAssets("t:RouteAsset");
            for (int index = 0; index < routeGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(routeGuids[index]);
                RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(path);
                if (route == null)
                {
                    report.AddError($"Route asset at '{path}' could not be loaded.", null);
                    continue;
                }

                var serialized = new SerializedObject(route);
                string rawId = serialized.FindProperty("routeId").stringValue;
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    report.AddError($"Route ID is missing. asset='{path}'.", route);
                    continue;
                }

                if (!route.HasValidRouteId)
                {
                    report.AddError($"Route ID is invalid. id='{rawId}' asset='{path}'.", route);
                    continue;
                }

                RouteId id = route.RouteId;
                if (firstRouteAssetById.TryGetValue(id, out string firstPath))
                {
                    report.AddError($"Duplicate Route ID '{id.StableText}'. firstAsset='{firstPath}' secondAsset='{path}' context='Project Route assets'.", route);
                    continue;
                }

                firstRouteAssetById.Add(id, path);
            }

            return report;
        }
    }
}
