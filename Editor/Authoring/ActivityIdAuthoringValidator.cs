using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    /// <summary>
    /// IF-ADR-014 / IF-ID-06: identity validation with explicit scopes.
    /// Definition-local findings block the selected asset; project audit findings are labeled separately.
    /// </summary>
    internal static class FrameworkIdentityAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateObjectEntryDeclaration(
            ObjectEntryDeclaration declaration,
            FrameworkValidationMode validationMode = FrameworkValidationMode.Standard)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            if (declaration == null)
            {
                report.AddError("Object Entry Declaration is missing.", null);
                return report;
            }
            if (!declaration.TryGetObjectEntryId(out ObjectEntryId id))
            {
                report.AddError("Object Entry ID is missing or invalid. Generate it explicitly under Advanced / Debug.", declaration);
                return report;
            }

            ObjectEntryDeclaration[] loaded = Resources.FindObjectsOfTypeAll<ObjectEntryDeclaration>();
            for (int index = 0; index < loaded.Length; index++)
            {
                ObjectEntryDeclaration other = loaded[index];
                if (other == null || ReferenceEquals(other, declaration)
                    || !other.TryGetObjectEntryId(out ObjectEntryId otherId)
                    || otherId != id || !other.gameObject.scene.IsValid() || !other.gameObject.scene.isLoaded
                    || !TryGetAuthoringOwner(declaration, out Object owner)
                    || !TryGetAuthoringOwner(other, out Object otherOwner)
                    || !ReferenceEquals(owner, otherOwner))
                    continue;

                report.AddWarning(
                    $"Duplicate Object Entry ID '{id.StableText}' exists in loaded content. Reuse across different owners is allowed; duplicates within one owner are rejected, and an unqualified StableReference may be ambiguous.",
                    other);
            }
            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateProjectAssets(
            FrameworkValidationMode validationMode)
        {
            return ValidateProjectIdentityAudit(validationMode);
        }

        /// <summary>
        /// Project-wide identity audit. Collisions are labeled as project-level evidence.
        /// Does not substitute for definition-local or Game Application validation.
        /// </summary>
        internal static FrameworkAuthoringValidationReport ValidateProjectIdentityAudit(
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            CollectIdentityIndex(
                report,
                out Dictionary<ActivityId, List<AssetIdentityEntry>> activitiesById,
                out Dictionary<RouteId, List<AssetIdentityEntry>> routesById);

            ReportProjectCollisions(report, activitiesById, "Activity");
            ReportProjectCollisions(report, routesById, "Route");

            if (!report.HasIssues)
            {
                report.AddInfo(
                    "Project identity audit: no duplicate Route or Activity stable IDs found.",
                    null);
            }

            return report;
        }

        /// <summary>
        /// Definition-local validation for one Activity: missing/invalid ID and collisions
        /// that involve this asset. Unrelated project collisions are excluded.
        /// </summary>
        internal static FrameworkAuthoringValidationReport ValidateActivityDefinitionLocal(
            ActivityAsset activity,
            FrameworkValidationMode validationMode = FrameworkValidationMode.Standard)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            if (activity == null)
            {
                report.AddError("Activity is missing.", null);
                return report;
            }

            string path = AssetDatabase.GetAssetPath(activity);
            string rawId = ReadSerializedId(activity, "activityId");

            if (string.IsNullOrWhiteSpace(rawId))
            {
                report.AddError(
                    "Activity ID is missing. Stable identity must be authored explicitly.",
                    activity);
                return report;
            }

            if (!activity.HasValidActivityId)
            {
                report.AddError(
                    $"Activity ID is invalid. id='{rawId}'.",
                    activity);
                return report;
            }

            ActivityId id = activity.ActivityId;
            CollectIdentityIndex(
                null,
                out Dictionary<ActivityId, List<AssetIdentityEntry>> activitiesById,
                out _);

            if (activitiesById.TryGetValue(id, out List<AssetIdentityEntry> entries) &&
                entries.Count > 1)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    AssetIdentityEntry entry = entries[index];
                    if (entry.Asset == activity)
                    {
                        continue;
                    }

                    // Context is the conflicting asset so Inspectors can open it directly.
                    report.AddError(
                        $"Stable ID collision involving this Activity. " +
                        $"id='{id.StableText}' thisAsset='{FormatPath(path)}' " +
                        $"otherAsset='{entry.Path}' scope='Definition-local'. " +
                        "Use Regenerate Stable ID on the duplicated asset.",
                        entry.Asset);
                }
            }

            return report;
        }

        /// <summary>
        /// Definition-local validation for one Route: missing/invalid ID and collisions
        /// that involve this asset. Unrelated project collisions are excluded.
        /// </summary>
        internal static FrameworkAuthoringValidationReport ValidateRouteDefinitionLocal(
            RouteAsset route,
            FrameworkValidationMode validationMode = FrameworkValidationMode.Standard)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            if (route == null)
            {
                report.AddError("Route is missing.", null);
                return report;
            }

            string path = AssetDatabase.GetAssetPath(route);
            string rawId = ReadSerializedId(route, "routeId");

            if (string.IsNullOrWhiteSpace(rawId))
            {
                report.AddError(
                    "Route ID is missing. Stable identity must be authored explicitly.",
                    route);
                return report;
            }

            if (!route.HasValidRouteId)
            {
                report.AddError(
                    $"Route ID is invalid. id='{rawId}'.",
                    route);
                return report;
            }

            RouteId id = route.RouteId;
            CollectIdentityIndex(
                null,
                out _,
                out Dictionary<RouteId, List<AssetIdentityEntry>> routesById);

            if (routesById.TryGetValue(id, out List<AssetIdentityEntry> entries) &&
                entries.Count > 1)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    AssetIdentityEntry entry = entries[index];
                    if (entry.Asset == route)
                    {
                        continue;
                    }

                    // Context is the conflicting asset so Inspectors can open it directly.
                    report.AddError(
                        $"Stable ID collision involving this Route. " +
                        $"id='{id.StableText}' thisAsset='{FormatPath(path)}' " +
                        $"otherAsset='{entry.Path}' scope='Definition-local'. " +
                        "Use Regenerate Stable ID on the duplicated asset.",
                        entry.Asset);
                }
            }

            return report;
        }

        /// <summary>
        /// Startup identity chain validation for one Game Application.
        /// Covers only Startup Route and its Startup Activity — not a full application graph.
        /// </summary>
        internal static FrameworkAuthoringValidationReport ValidateGameApplicationIdentity(
            GameApplicationAsset gameApplication,
            FrameworkValidationMode validationMode = FrameworkValidationMode.Standard)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);
            if (gameApplication == null)
            {
                report.AddError("Game Application is missing.", null);
                return report;
            }

            var routes = new List<RouteAsset>();
            var activities = new List<ActivityAsset>();
            CollectStartupIdentityChain(gameApplication, routes, activities);

            var routeIds = new Dictionary<RouteId, RouteAsset>();
            for (int index = 0; index < routes.Count; index++)
            {
                RouteAsset route = routes[index];
                if (route == null || !route.HasValidRouteId)
                {
                    continue;
                }

                RouteId id = route.RouteId;
                if (routeIds.TryGetValue(id, out RouteAsset first))
                {
                    report.AddError(
                        $"Startup identity chain has colliding Route IDs. " +
                        $"id='{id.StableText}' first='{AssetDatabase.GetAssetPath(first)}' " +
                        $"second='{AssetDatabase.GetAssetPath(route)}' scope='Startup identity chain'.",
                        first);
                }
                else
                {
                    routeIds.Add(id, route);
                }
            }

            var activityIds = new Dictionary<ActivityId, ActivityAsset>();
            for (int index = 0; index < activities.Count; index++)
            {
                ActivityAsset activity = activities[index];
                if (activity == null || !activity.HasValidActivityId)
                {
                    continue;
                }

                ActivityId id = activity.ActivityId;
                if (activityIds.TryGetValue(id, out ActivityAsset first))
                {
                    report.AddError(
                        $"Startup identity chain has colliding Activity IDs. " +
                        $"id='{id.StableText}' first='{AssetDatabase.GetAssetPath(first)}' " +
                        $"second='{AssetDatabase.GetAssetPath(activity)}' scope='Startup identity chain'.",
                        first);
                }
                else
                {
                    activityIds.Add(id, activity);
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    "Startup identity chain has no Route/Activity stable-ID collisions " +
                    "(Startup Route and its Startup Activity only).",
                    gameApplication);
            }

            return report;
        }

        internal static bool TryRegenerateStableId(
            RouteAsset route,
            out string previousId,
            out string newId,
            out string issue)
        {
            previousId = string.Empty;
            newId = string.Empty;
            issue = string.Empty;

            if (route == null)
            {
                issue = "Route is missing.";
                return false;
            }

            var serialized = new SerializedObject(route);
            SerializedProperty property = serialized.FindProperty("routeId");
            if (property == null)
            {
                issue = "Route ID property was not found.";
                return false;
            }

            previousId = property.stringValue ?? string.Empty;
            newId = ImmersiveFrameworkEditorSettingsUtility.GenerateRouteIdText();
            Undo.RecordObject(route, "Regenerate Route Stable ID");
            property.stringValue = newId;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(route);
            return true;
        }

        internal static bool TryRegenerateStableId(
            ActivityAsset activity,
            out string previousId,
            out string newId,
            out string issue)
        {
            previousId = string.Empty;
            newId = string.Empty;
            issue = string.Empty;

            if (activity == null)
            {
                issue = "Activity is missing.";
                return false;
            }

            var serialized = new SerializedObject(activity);
            SerializedProperty property = serialized.FindProperty("activityId");
            if (property == null)
            {
                issue = "Activity ID property was not found.";
                return false;
            }

            previousId = property.stringValue ?? string.Empty;
            newId = ImmersiveFrameworkEditorSettingsUtility.GenerateActivityIdText();
            Undo.RecordObject(activity, "Regenerate Activity Stable ID");
            property.stringValue = newId;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(activity);
            return true;
        }

        internal static bool TryRegenerateStableId(
            ObjectEntryDeclaration declaration,
            out string previousId,
            out string newId,
            out string issue)
        {
            previousId = string.Empty;
            newId = string.Empty;
            issue = string.Empty;
            if (declaration == null)
            {
                issue = "Object Entry Declaration is missing.";
                return false;
            }

            var serialized = new SerializedObject(declaration);
            SerializedProperty property = serialized.FindProperty("objectEntryId");
            if (property == null)
            {
                issue = "Object Entry ID property was not found.";
                return false;
            }

            previousId = property.stringValue ?? string.Empty;
            newId = ImmersiveFrameworkEditorSettingsUtility.GenerateObjectEntryIdText();
            Undo.RecordObject(declaration, "Regenerate Object Entry Stable ID");
            property.stringValue = newId;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(declaration);
            PrefabUtility.RecordPrefabInstancePropertyModifications(declaration);
            if (declaration.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(declaration.gameObject.scene);
            return true;
        }

        private static bool TryGetAuthoringOwner(
            ObjectEntryDeclaration declaration,
            out UnityEngine.Object owner)
        {
            owner = null;
            if (declaration == null) return false;

            Transform current = declaration.transform;
            while (current != null)
            {
                if (current.TryGetComponent(out RouteContentContribution routeContribution))
                {
                    owner = routeContribution.Route;
                    return owner != null;
                }
                if (current.TryGetComponent(out ActivityContentContribution activityContribution))
                {
                    owner = activityContribution.Activity;
                    return owner != null;
                }
                current = current.parent;
            }

            return false;
        }

        private static void CollectStartupIdentityChain(
            GameApplicationAsset gameApplication,
            List<RouteAsset> routes,
            List<ActivityAsset> activities)
        {
            RouteAsset startupRoute = gameApplication.StartupRoute;
            if (startupRoute == null)
            {
                return;
            }

            routes.Add(startupRoute);
            if (startupRoute.StartupActivity != null)
            {
                activities.Add(startupRoute.StartupActivity);
            }
        }

        private static void CollectIdentityIndex(
            FrameworkAuthoringValidationReport projectAuditReport,
            out Dictionary<ActivityId, List<AssetIdentityEntry>> activitiesById,
            out Dictionary<RouteId, List<AssetIdentityEntry>> routesById)
        {
            activitiesById = new Dictionary<ActivityId, List<AssetIdentityEntry>>();
            routesById = new Dictionary<RouteId, List<AssetIdentityEntry>>();

            string[] activityGuids = AssetDatabase.FindAssets("t:ActivityAsset");
            for (int index = 0; index < activityGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(activityGuids[index]);
                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(path);
                if (activity == null)
                {
                    projectAuditReport?.AddError(
                        $"Project identity audit: Activity asset at '{path}' could not be loaded.",
                        null);
                    continue;
                }

                string rawId = ReadSerializedId(activity, "activityId");
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    projectAuditReport?.AddError(
                        $"Project identity audit: Activity ID is missing. asset='{path}' scope='Project audit'.",
                        activity);
                    continue;
                }

                if (!activity.HasValidActivityId)
                {
                    projectAuditReport?.AddError(
                        $"Project identity audit: Activity ID is invalid. id='{rawId}' asset='{path}' scope='Project audit'.",
                        activity);
                    continue;
                }

                ActivityId id = activity.ActivityId;
                if (!activitiesById.TryGetValue(id, out List<AssetIdentityEntry> list))
                {
                    list = new List<AssetIdentityEntry>();
                    activitiesById.Add(id, list);
                }

                list.Add(new AssetIdentityEntry(activity, path));
            }

            string[] routeGuids = AssetDatabase.FindAssets("t:RouteAsset");
            for (int index = 0; index < routeGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(routeGuids[index]);
                RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(path);
                if (route == null)
                {
                    projectAuditReport?.AddError(
                        $"Project identity audit: Route asset at '{path}' could not be loaded.",
                        null);
                    continue;
                }

                string rawId = ReadSerializedId(route, "routeId");
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    projectAuditReport?.AddError(
                        $"Project identity audit: Route ID is missing. asset='{path}' scope='Project audit'.",
                        route);
                    continue;
                }

                if (!route.HasValidRouteId)
                {
                    projectAuditReport?.AddError(
                        $"Project identity audit: Route ID is invalid. id='{rawId}' asset='{path}' scope='Project audit'.",
                        route);
                    continue;
                }

                RouteId id = route.RouteId;
                if (!routesById.TryGetValue(id, out List<AssetIdentityEntry> list))
                {
                    list = new List<AssetIdentityEntry>();
                    routesById.Add(id, list);
                }

                list.Add(new AssetIdentityEntry(route, path));
            }
        }

        private static void ReportProjectCollisions<TId>(
            FrameworkAuthoringValidationReport report,
            Dictionary<TId, List<AssetIdentityEntry>> byId,
            string kind)
            where TId : struct
        {
            foreach (KeyValuePair<TId, List<AssetIdentityEntry>> pair in byId)
            {
                List<AssetIdentityEntry> entries = pair.Value;
                if (entries.Count < 2)
                {
                    continue;
                }

                for (int index = 1; index < entries.Count; index++)
                {
                    report.AddError(
                        $"Project identity audit: duplicate {kind} ID '{pair.Key}'. " +
                        $"firstAsset='{entries[0].Path}' secondAsset='{entries[index].Path}' " +
                        "scope='Project audit' (not definition-local).",
                        entries[index].Asset);
                }
            }
        }

        private static string ReadSerializedId(UnityEngine.Object asset, string propertyName)
        {
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.stringValue ?? string.Empty : string.Empty;
        }

        private static string FormatPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? "<unsaved>" : path;
        }

        private readonly struct AssetIdentityEntry
        {
            internal AssetIdentityEntry(UnityEngine.Object asset, string path)
            {
                Asset = asset;
                Path = path ?? string.Empty;
            }

            internal UnityEngine.Object Asset { get; }
            internal string Path { get; }
        }
    }
}
