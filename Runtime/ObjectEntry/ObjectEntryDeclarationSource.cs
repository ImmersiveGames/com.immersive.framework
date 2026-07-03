using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
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
        public ObjectEntryDeclarationSource(bool includeInactiveDeclarations = true)
        {
            IncludeInactiveDeclarations = includeInactiveDeclarations;
        }

        public bool IncludeInactiveDeclarations { get; }

        /// <summary>
        /// Development diagnostics only. Runtime Host authority uses CollectScoped and explicit lifecycle owners.
        /// </summary>
        internal ObjectEntryDeclarationSourceResult CollectLoadedSceneDeclarations()
        {
            ObjectEntryDeclaration[] declarations = UnityEngine.Object.FindObjectsByType<ObjectEntryDeclaration>(
                IncludeInactiveDeclarations ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
            return Collect(declarations, "loaded-scenes");
        }

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
            int filteredDeclarationCount = 0;

            for (int i = 0; i < declarations.Count; i++)
            {
                var declaration = declarations[i];
                if (!declaration.HasRequiredAuthoredOwner)
                {
                    issues.Add(ObjectEntryIssue.Error(
                        ObjectEntryIssueKind.MissingOwner,
                        FormatDeclarationIssue(declaration, $"Scope '{declaration.Scope}' requires an explicit authored owner.")));
                    continue;
                }

                if (declaration.Scope == ObjectEntryScope.Route && !declaration.MatchesRouteOwner(context.Route))
                {
                    filteredDeclarationCount++;
                    continue;
                }

                if (declaration.Scope == ObjectEntryScope.Activity
                    && (!context.HasActiveActivity || !declaration.MatchesActivityOwner(context.Activity)))
                {
                    filteredDeclarationCount++;
                    continue;
                }

                if (!context.TryResolveOwnerIdentity(declaration.Scope, out var ownerIdentity))
                {
                    issues.Add(ObjectEntryIssue.Error(
                        ObjectEntryIssueKind.MissingOwner,
                        FormatDeclarationIssue(declaration, $"No active typed owner is available for scope '{declaration.Scope}'.")));
                    continue;
                }

                if (!declaration.TryCreateScopedDescriptor(ownerIdentity, out var descriptor, out string issue))
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
            int rejectedDeclarations = declarations.Count - filteredDeclarationCount - acceptedDeclarations;

            return new ObjectEntryDeclarationSourceResult(
                set,
                status,
                declarations.Count,
                descriptors.Count,
                acceptedDeclarations,
                rejectedDeclarations,
                filteredDeclarationCount,
                issues);
        }

        public ObjectEntryDeclarationSourceResult Collect(
            IEnumerable<ObjectEntryDeclaration> declarations,
            string source = null)
        {
            ObjectEntryDeclaration[] materializedDeclarations = declarations == null
                ? Array.Empty<ObjectEntryDeclaration>()
                : declarations.Where(declaration => declaration != null).ToArray();

            var descriptors = new List<ObjectEntryDescriptor>(materializedDeclarations.Length);
            var issues = new List<ObjectEntryIssue>();
            for (int i = 0; i < materializedDeclarations.Length; i++)
            {
                var declaration = materializedDeclarations[i];
                if (!declaration.TryCreateDescriptor(out var descriptor, out string issue))
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
                    $"Object Entry declaration source '{ResolveSource(source)}' rejected duplicate identity. {exception.Message}"));
            }

            var status = ResolveStatus(issues);
            int acceptedDeclarations = aggregateRejected || status == ObjectEntryResultStatus.Rejected
                ? 0
                : set.Count;
            int rejectedDeclarations = materializedDeclarations.Length - acceptedDeclarations;

            return new ObjectEntryDeclarationSourceResult(
                set,
                status,
                materializedDeclarations.Length,
                descriptors.Count,
                acceptedDeclarations,
                rejectedDeclarations,
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
            var declarations = new List<ObjectEntryDeclaration>();
            var scannedScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<RouteSceneCompositionResultEntry> entries = context.RouteSceneCompositionResult.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Loaded)
                {
                    continue;
                }

                string sceneKey = !string.IsNullOrWhiteSpace(entry.ScenePath)
                    ? entry.ScenePath.Trim()
                    : entry.SceneName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sceneKey) || !scannedScenes.Add(sceneKey))
                {
                    continue;
                }

                IReadOnlyList<ObjectEntryDeclaration> sceneDeclarations = SceneScopedComponentQuery.GetComponentsInLoadedScene<ObjectEntryDeclaration>(
                    entry.ScenePath,
                    entry.SceneName);
                for (int declarationIndex = 0; declarationIndex < sceneDeclarations.Count; declarationIndex++)
                {
                    var declaration = sceneDeclarations[declarationIndex];
                    if (declaration != null)
                    {
                        declarations.Add(declaration);
                    }
                }
            }

            return declarations;
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

        private static string ResolveSource(string source)
        {
            return source.NormalizeTextOrFallback(nameof(ObjectEntryDeclarationSource));
        }
    }
}
