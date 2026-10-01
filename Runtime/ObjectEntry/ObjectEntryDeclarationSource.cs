using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Passive source that converts scene-authored ObjectEntryDeclaration components into an ObjectEntrySet.
    /// It does not bind GameObjects, materialize prefabs, spawn actors, register services, perform reset or create lifecycle authority.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Passive Object Entry declaration source introduced by F13D; F13J adds owner-aware collection scoped to active Route scenes.")]
    public sealed class ObjectEntryDeclarationSource
    {
        internal ObjectEntryDeclarationSourceResult CollectScoped(ObjectEntryScopedCollectionContext context)
        {
            if (!context.TryValidate(out string contextIssue))
            {
                return new ObjectEntryDeclarationSourceResult(
                    ObjectEntrySet.Empty(),
                    ObjectEntryResultStatus.Rejected,
                    0,
                    0,
                    0,
                    0,
                    0,
                    new[]
                    {
                        ObjectEntryIssue.Error(ObjectEntryIssueKind.InvalidRequest, contextIssue)
                    });
            }

            IReadOnlyList<ObjectEntryDeclaration> declarations = CollectScopedSceneDeclarations(context);
            var descriptors = new List<ObjectEntryDescriptor>(declarations.Count);
            var issues = new List<ObjectEntryIssue>();

            for (int i = 0; i < declarations.Count; i++)
            {
                var declaration = declarations[i];
                bool activityOwned = context.HasActiveActivity
                    && IsInActivityOwnedScene(declaration, context);
                ObjectEntryScope scope = activityOwned ? ObjectEntryScope.Activity : ObjectEntryScope.Route;
                FrameworkIdentityKey ownerIdentity = activityOwned ? context.ActivityOwnerIdentity : context.RouteOwnerIdentity;
                if (!declaration.TryCreateDescriptor(scope, ownerIdentity, out var descriptor, out string issue))
                {
                    issues.Add(ObjectEntryIssue.Error(
                        ObjectEntryIssueKind.InvalidRequest,
                        FormatDeclarationIssue(declaration, issue)));
                    continue;
                }

                descriptors.Add(descriptor);
            }

            ObjectEntrySet set;
            bool aggregateRejected = false;
            try
            {
                set = new ObjectEntrySet(descriptors);
            }
            catch (ArgumentException exception)
            {
                aggregateRejected = true;
                set = ObjectEntrySet.Empty();
                issues.Add(ObjectEntryIssue.Error(
                    ObjectEntryIssueKind.DuplicateIdentity,
                    $"Scoped Object Entry declaration source rejected duplicate identity. {exception.Message}"));
            }

            var status = ResolveStatus(issues);
            if (status == ObjectEntryResultStatus.Rejected && !set.IsEmpty)
            {
                set = ObjectEntrySet.Empty();
            }

            int acceptedDeclarations = aggregateRejected || status == ObjectEntryResultStatus.Rejected
                ? 0
                : set.Count;
            int rejectedDeclarations = declarations.Count - acceptedDeclarations;

            return new ObjectEntryDeclarationSourceResult(
                set,
                status,
                declarations.Count,
                descriptors.Count,
                acceptedDeclarations,
                rejectedDeclarations,
                0,
                issues);
        }

        private static ObjectEntryResultStatus ResolveStatus(IReadOnlyCollection<ObjectEntryIssue> issues)
        {
            if (issues.Any(issue => issue.IsBlocking))
            {
                return ObjectEntryResultStatus.Rejected;
            }

            return issues.Count == 0
                ? ObjectEntryResultStatus.Accepted
                : ObjectEntryResultStatus.AcceptedWithWarnings;
        }

        private static IReadOnlyList<ObjectEntryDeclaration> CollectScopedSceneDeclarations(
            ObjectEntryScopedCollectionContext context)
        {
            RouteContentDiscoveryScope scope =
                RouteContentDiscoveryScope.FromCompositionResult(
                    context.RouteSceneCompositionResult);
            var result = new List<ObjectEntryDeclaration>(SceneCompositionComponentQuery.GetComponents<ObjectEntryDeclaration>(scope));
            if (context.HasActiveActivity)
                result.AddRange(SceneCompositionComponentQuery.GetActivityOwnedComponents<ObjectEntryDeclaration>(context.ActivityContentDiscoveryScope, context.Activity));
            return result;
        }

        private static bool IsInActivityOwnedScene(ObjectEntryDeclaration declaration, ObjectEntryScopedCollectionContext context)
        {
            if (declaration == null || !declaration.gameObject.scene.IsValid()) return false;
            IReadOnlyList<ActivityContentDiscoveryScene> scenes = context.ActivityContentDiscoveryScope.ActivityOwnedScenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].MatchesActivity(context.Activity)
                    && string.Equals(declaration.gameObject.scene.path, scenes[i].ScenePath, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string FormatDeclarationIssue(ObjectEntryDeclaration declaration, string issue)
        {
            string objectName = declaration != null && declaration.gameObject != null
                ? declaration.gameObject.name
                : "<missing>";
            string sceneName = declaration != null && declaration.gameObject != null && declaration.gameObject.scene.IsValid()
                ? declaration.gameObject.scene.name
                : "<no-scene>";
            string message = issue.NormalizeTextOrFallback("Invalid Object Entry declaration.");
            return $"ObjectEntryDeclaration object='{objectName}' scene='{sceneName}' issue='{message}'.";
        }
    }
}
