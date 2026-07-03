#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Development-only runner for F13 Object Entry contract, scoped ownership and snapshot lifecycle smokes.
    /// It does not bind physical GameObjects, materialize prefabs, create Player/Actor state or perform reset.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Shared QA runner for F13 Object Entry passive set, ownership and snapshot lifecycle smokes; no physical object entry or gameplay entry.")]
    internal static class ObjectEntryQaSmokeRunner
    {
        internal const string SyntheticSetSmokeName = "Object Entry Synthetic Set Smoke";

        internal const string DeclarationSourceSmokeName = "Object Entry Declaration Source Smoke";

        internal const string RuntimeIntegrationSmokeName = "Object Entry Runtime Integration Smoke";

        internal const string RuntimeContextSnapshotSmokeName = "Object Entry Runtime Context Snapshot Smoke";

        internal const string RuntimeHostSnapshotExposureSmokeName = "Object Entry Runtime Host Snapshot Exposure Smoke";

        internal const string ScopedOwnershipSmokeName = "Object Entry Scoped Ownership Smoke";

        internal const string SnapshotLifecycleSmokeName = "Object Entry Snapshot Lifecycle Smoke";

        internal const string FoundationClosureSmokeName = "Object Entry Foundation Closure Smoke";

        internal static Task<bool> RunSyntheticSetSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            try
            {
                var routeOwner = new FrameworkIdentityKey(FrameworkIdentityDomain.Route, new FrameworkIdentityValue("qa.route.canonical"));
                var activityOwner = new FrameworkIdentityKey(FrameworkIdentityDomain.Activity, new FrameworkIdentityValue("qa.activity.primary"));

                var routeRequired = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-entry.route.required"),
                    ObjectEntryScope.Route,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Route Required Object Entry",
                    routeOwner);

                var routeOptional = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-entry.route.optional"),
                    ObjectEntryScope.Route,
                    ObjectEntrySourceKind.RuntimeRegistered,
                    ObjectEntryRequiredness.Optional,
                    "QA Route Optional Object Entry",
                    routeOwner);

                var activityRequired = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-entry.activity.required"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Activity Required Object Entry",
                    activityOwner);

                var set = new ObjectEntrySet(new[] { routeRequired, routeOptional, activityRequired });
                if (!ValidateSet(logger, set, routeRequired, activityRequired))
                {
                    return Task.FromResult(false);
                }

                var accepted = ObjectEntryResult.Accepted(routeRequired);
                if (!ValidateAcceptedResult(logger, accepted))
                {
                    return Task.FromResult(false);
                }

                if (!ValidateDuplicateRejection(logger, routeRequired, routeOptional))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Synthetic Set Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "synthetic-set"),
                        LogFields.Field("source", source),
                        LogFields.Field("objectEntries", set.Count),
                        LogFields.Field("required", set.RequiredCount),
                        LogFields.Field("optional", set.OptionalCount),
                        LogFields.Field("route", set.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", set.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", set.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("resultStatus", accepted.Status.ToString()),
                        LogFields.Field("blockingIssues", accepted.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", accepted.NonBlockingIssueCount),
                        LogFields.Field("summary", set.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Synthetic Set Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "synthetic-set"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }


        internal static Task<bool> RunDeclarationSourceSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            GameObject routeObject = null;
            GameObject activityObject = null;
            GameObject duplicateObject = null;

            try
            {
                routeObject = new GameObject("QA_ObjectEntryDeclarationSource_Route");
                activityObject = new GameObject("QA_ObjectEntryDeclarationSource_Activity");
                duplicateObject = new GameObject("QA_ObjectEntryDeclarationSource_Duplicate");

                var routeDeclaration = routeObject.AddComponent<ObjectEntryDeclaration>();
                routeDeclaration.ConfigureForQa(
                    "qa.object-entry.declaration-source.route",
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Declaration Source Route Entry");

                var activityDeclaration = activityObject.AddComponent<ObjectEntryDeclaration>();
                activityDeclaration.ConfigureForQa(
                    "qa.object-entry.declaration-source.activity",
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Optional,
                    "QA Declaration Source Activity Entry");

                var duplicateDeclaration = duplicateObject.AddComponent<ObjectEntryDeclaration>();
                duplicateDeclaration.ConfigureForQa(
                    "qa.object-entry.declaration-source.route",
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Declaration Source Duplicate Entry");

                var declarationSource = new ObjectEntryDeclarationSource(includeInactiveDeclarations: true);
                var result = declarationSource.Collect(
                    new[] { routeDeclaration, activityDeclaration },
                    source);

                if (!ValidateDeclarationSourceResult(logger, result, routeDeclaration, activityDeclaration))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Declaration Source Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "declaration-source-set"),
                        LogFields.Field("source", source),
                        LogFields.Field("resultStatus", result.Status.ToString()),
                        LogFields.Field("declarations", result.DeclarationCount),
                        LogFields.Field("candidateDescriptors", result.CandidateDescriptorCount),
                        LogFields.Field("acceptedDeclarations", result.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", result.RejectedDeclarationCount),
                        LogFields.Field("objectEntries", result.ObjectEntries.Count),
                        LogFields.Field("required", result.ObjectEntries.RequiredCount),
                        LogFields.Field("optional", result.ObjectEntries.OptionalCount),
                        LogFields.Field("route", result.ObjectEntries.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", result.ObjectEntries.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", result.ObjectEntries.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("blockingIssues", result.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                        LogFields.Field("summary", result.Summary)));

                var duplicateResult = declarationSource.Collect(
                    new[] { routeDeclaration, duplicateDeclaration },
                    source);

                if (!ValidateDeclarationSourceDuplicateRejection(logger, duplicateResult))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Declaration Source Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "declaration-source-duplicate-identity"),
                        LogFields.Field("source", source),
                        LogFields.Field("duplicateRejected", true),
                        LogFields.Field("resultStatus", duplicateResult.Status.ToString()),
                        LogFields.Field("declarations", duplicateResult.DeclarationCount),
                        LogFields.Field("candidateDescriptors", duplicateResult.CandidateDescriptorCount),
                        LogFields.Field("acceptedDeclarations", duplicateResult.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", duplicateResult.RejectedDeclarationCount),
                        LogFields.Field("objectEntries", duplicateResult.ObjectEntries.Count),
                        LogFields.Field("blockingIssues", duplicateResult.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", duplicateResult.NonBlockingIssueCount),
                        LogFields.Field("summary", duplicateResult.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Declaration Source Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "declaration-source"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                DestroyQaObject(routeObject);
                DestroyQaObject(activityObject);
                DestroyQaObject(duplicateObject);
            }
        }



        internal static Task<bool> RunRuntimeIntegrationSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            const string routeEntryId = "qa.object-entry.runtime.route";
            const string inactiveActivityEntryId = "qa.object-entry.runtime.activity.inactive";

            GameObject routeObject = null;
            GameObject inactiveActivityObject = null;

            try
            {
                routeObject = new GameObject("QA_ObjectEntryRuntimeIntegration_Route");
                inactiveActivityObject = new GameObject("QA_ObjectEntryRuntimeIntegration_InactiveActivity");

                var routeDeclaration = routeObject.AddComponent<ObjectEntryDeclaration>();
                routeDeclaration.ConfigureForQa(
                    routeEntryId,
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Runtime Integration Route Entry");

                var inactiveActivityDeclaration = inactiveActivityObject.AddComponent<ObjectEntryDeclaration>();
                inactiveActivityDeclaration.ConfigureForQa(
                    inactiveActivityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Optional,
                    "QA Runtime Integration Inactive Activity Entry");
                inactiveActivityObject.SetActive(false);

                var declarationSource = new ObjectEntryDeclarationSource(includeInactiveDeclarations: true);
                var result = declarationSource.CollectLoadedSceneDeclarations();

                if (!ValidateRuntimeIntegrationResult(logger, result, routeEntryId, inactiveActivityEntryId))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Runtime Integration Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "loaded-scene-declarations"),
                        LogFields.Field("source", source),
                        LogFields.Field("resultStatus", result.Status.ToString()),
                        LogFields.Field("declarations", result.DeclarationCount),
                        LogFields.Field("candidateDescriptors", result.CandidateDescriptorCount),
                        LogFields.Field("acceptedDeclarations", result.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", result.RejectedDeclarationCount),
                        LogFields.Field("objectEntries", result.ObjectEntries.Count),
                        LogFields.Field("required", result.ObjectEntries.RequiredCount),
                        LogFields.Field("optional", result.ObjectEntries.OptionalCount),
                        LogFields.Field("route", result.ObjectEntries.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", result.ObjectEntries.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", result.ObjectEntries.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("qaRouteFound", true),
                        LogFields.Field("qaInactiveActivityFound", true),
                        LogFields.Field("blockingIssues", result.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                        LogFields.Field("summary", result.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Runtime Integration Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "loaded-scene-declarations"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                DestroyQaObject(routeObject);
                DestroyQaObject(inactiveActivityObject);
            }
        }

        internal static Task<bool> RunRuntimeContextSnapshotSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            const string routeEntryId = "qa.object-entry.runtime-context.route";
            const string activityEntryId = "qa.object-entry.runtime-context.activity";

            GameObject routeObject = null;
            GameObject activityObject = null;

            try
            {
                routeObject = new GameObject("QA_ObjectEntryRuntimeContext_Route");
                activityObject = new GameObject("QA_ObjectEntryRuntimeContext_Activity");

                var routeDeclaration = routeObject.AddComponent<ObjectEntryDeclaration>();
                routeDeclaration.ConfigureForQa(
                    routeEntryId,
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Runtime Context Route Entry");

                var activityDeclaration = activityObject.AddComponent<ObjectEntryDeclaration>();
                activityDeclaration.ConfigureForQa(
                    activityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Optional,
                    "QA Runtime Context Activity Entry");
                activityObject.SetActive(false);

                var declarationSource = new ObjectEntryDeclarationSource(includeInactiveDeclarations: true);
                var result = declarationSource.CollectLoadedSceneDeclarations();
                var snapshot = result.ToRuntimeContextSnapshot(source);

                if (!ValidateRuntimeContextSnapshot(logger, snapshot, result, routeEntryId, activityEntryId))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Runtime Context Snapshot Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-context-snapshot"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot.IsAvailable),
                        LogFields.Field("resultStatus", snapshot.Status.ToString()),
                        LogFields.Field("declarations", snapshot.DeclarationCount),
                        LogFields.Field("candidateDescriptors", snapshot.CandidateDescriptorCount),
                        LogFields.Field("acceptedDeclarations", snapshot.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", snapshot.RejectedDeclarationCount),
                        LogFields.Field("objectEntries", snapshot.Count),
                        LogFields.Field("required", snapshot.RequiredCount),
                        LogFields.Field("optional", snapshot.OptionalCount),
                        LogFields.Field("route", snapshot.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", snapshot.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", snapshot.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("qaRouteFound", true),
                        LogFields.Field("qaActivityFound", true),
                        LogFields.Field("blockingIssues", snapshot.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", snapshot.NonBlockingIssueCount),
                        LogFields.Field("summary", snapshot.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Runtime Context Snapshot Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-context-snapshot"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                DestroyQaObject(routeObject);
                DestroyQaObject(activityObject);
            }
        }


        internal static Task<bool> RunRuntimeHostSnapshotExposureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            const string routeEntryId = "qa.object-entry.runtime-host.route";
            const string activityEntryId = "qa.object-entry.runtime-host.activity";

            GameObject routeObject = null;
            GameObject activityObject = null;

            try
            {
                routeObject = new GameObject("QA_ObjectEntryRuntimeHost_Route");
                activityObject = new GameObject("QA_ObjectEntryRuntimeHost_Activity");

                var routeDeclaration = routeObject.AddComponent<ObjectEntryDeclaration>();
                routeDeclaration.ConfigureForQa(
                    routeEntryId,
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Runtime Host Route Entry",
                    runtimeHost.State.CurrentRoute);

                var activityDeclaration = activityObject.AddComponent<ObjectEntryDeclaration>();
                activityDeclaration.ConfigureForQa(
                    activityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Optional,
                    "QA Runtime Host Activity Entry",
                    null,
                    runtimeHost.State.CurrentActivity);
                activityObject.SetActive(false);

                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot(source);
                bool hostSnapshotAvailable = runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var hostedSnapshot);

                if (!hostSnapshotAvailable || !ReferenceEquals(snapshot, hostedSnapshot))
                {
                    logger.Warning("QA Object Entry Runtime Host Snapshot Exposure Smoke step failed. step='runtime-host-snapshot' reason='Runtime Host did not expose the refreshed Object Entry snapshot'.");
                    return Task.FromResult(false);
                }

                if (!ValidateRuntimeHostSnapshotExposure(logger, runtimeHost, hostedSnapshot, routeEntryId, activityEntryId))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Runtime Host Snapshot Exposure Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-host-snapshot"),
                        LogFields.Field("source", source),
                        LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                        LogFields.Field("snapshotAvailable", hostedSnapshot.IsAvailable),
                        LogFields.Field("resultStatus", hostedSnapshot.Status.ToString()),
                        LogFields.Field("declarations", hostedSnapshot.DeclarationCount),
                        LogFields.Field("candidateDescriptors", hostedSnapshot.CandidateDescriptorCount),
                        LogFields.Field("acceptedDeclarations", hostedSnapshot.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", hostedSnapshot.RejectedDeclarationCount),
                        LogFields.Field("filteredDeclarations", hostedSnapshot.FilteredDeclarationCount),
                        LogFields.Field("objectEntries", hostedSnapshot.Count),
                        LogFields.Field("required", hostedSnapshot.RequiredCount),
                        LogFields.Field("optional", hostedSnapshot.OptionalCount),
                        LogFields.Field("route", hostedSnapshot.GetByScope(ObjectEntryScope.Route).Count),
                        LogFields.Field("activity", hostedSnapshot.GetByScope(ObjectEntryScope.Activity).Count),
                        LogFields.Field("session", hostedSnapshot.GetByScope(ObjectEntryScope.Session).Count),
                        LogFields.Field("qaRouteFound", true),
                        LogFields.Field("qaActivityFound", true),
                        LogFields.Field("blockingIssues", hostedSnapshot.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", hostedSnapshot.NonBlockingIssueCount),
                        LogFields.Field("summary", hostedSnapshot.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Runtime Host Snapshot Exposure Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-host-snapshot"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                DestroyQaObject(routeObject);
                DestroyQaObject(activityObject);
            }
        }

        internal static Task<bool> RunScopedOwnershipSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            var currentRoute = runtimeHost.State.CurrentRoute;
            var currentActivity = runtimeHost.State.CurrentActivity;
            if (currentRoute == null || currentActivity == null)
            {
                logger.Warning("QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Active Route and Activity are required'.");
                return Task.FromResult(false);
            }

            const string routeEntryId = "qa.object-entry.scoped.route";
            const string activityEntryId = "qa.object-entry.scoped.activity";
            const string foreignActivityEntryId = "qa.object-entry.scoped.activity.foreign";

            GameObject routeObject = null;
            GameObject activityObject = null;
            GameObject foreignActivityObject = null;
            ActivityAsset foreignActivity = null;

            try
            {
                foreignActivity = ScriptableObject.CreateInstance<ActivityAsset>();
                foreignActivity.name = "QA Foreign Object Entry Activity";

                routeObject = new GameObject("QA_ObjectEntryScoped_Route");
                activityObject = new GameObject("QA_ObjectEntryScoped_Activity");
                foreignActivityObject = new GameObject("QA_ObjectEntryScoped_ForeignActivity");

                routeObject.AddComponent<ObjectEntryDeclaration>().ConfigureForQa(
                    routeEntryId,
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Scoped Route Entry",
                    currentRoute);

                activityObject.AddComponent<ObjectEntryDeclaration>().ConfigureForQa(
                    activityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Scoped Activity Entry",
                    null,
                    currentActivity);

                foreignActivityObject.AddComponent<ObjectEntryDeclaration>().ConfigureForQa(
                    foreignActivityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Optional,
                    "QA Scoped Foreign Activity Entry",
                    null,
                    foreignActivity);
                foreignActivityObject.SetActive(false);

                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot(source);
                if (!ValidateScopedOwnershipSnapshot(
                        logger,
                        runtimeHost,
                        snapshot,
                        routeEntryId,
                        activityEntryId,
                        foreignActivityEntryId))
                {
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Entry Scoped Ownership Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "scoped-ownership"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot.IsAvailable),
                        LogFields.Field("resultStatus", snapshot.Status.ToString()),
                        LogFields.Field("routeOwner", runtimeHost.State.RouteState.RouteIdentity.StableText),
                        LogFields.Field("activityOwner", runtimeHost.State.ActivityState.ActivityIdentity.StableText),
                        LogFields.Field("declarations", snapshot.DeclarationCount),
                        LogFields.Field("acceptedDeclarations", snapshot.AcceptedDeclarationCount),
                        LogFields.Field("rejectedDeclarations", snapshot.RejectedDeclarationCount),
                        LogFields.Field("filteredDeclarations", snapshot.FilteredDeclarationCount),
                        LogFields.Field("objectEntries", snapshot.Count),
                        LogFields.Field("qaRouteFound", true),
                        LogFields.Field("qaActivityFound", true),
                        LogFields.Field("foreignActivityFiltered", true),
                        LogFields.Field("blockingIssues", snapshot.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", snapshot.NonBlockingIssueCount),
                        LogFields.Field("summary", snapshot.Summary)));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Scoped Ownership Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "scoped-ownership"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
            finally
            {
                DestroyQaObject(routeObject);
                DestroyQaObject(activityObject);
                DestroyQaObject(foreignActivityObject);
                DestroyQaObject(foreignActivity);
            }
        }

        internal static async Task<bool> RunSnapshotLifecycleSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            ActivityAsset primaryActivity,
            ActivityAsset secondaryActivity)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            var currentRoute = runtimeHost.State.CurrentRoute;
            var originalActivity = runtimeHost.State.CurrentActivity;
            var targetActivity = ResolveTransitionTarget(originalActivity, primaryActivity, secondaryActivity);
            if (currentRoute == null || originalActivity == null || targetActivity == null)
            {
                logger.Warning("QA Object Entry Snapshot Lifecycle Smoke step failed. step='snapshot-lifecycle' reason='Active Route, original Activity and alternate Activity are required'.");
                return false;
            }

            const string routeEntryId = "qa.object-entry.snapshot-lifecycle.route";
            const string originalActivityEntryId = "qa.object-entry.snapshot-lifecycle.activity.original";
            const string targetActivityEntryId = "qa.object-entry.snapshot-lifecycle.activity.target";

            GameObject routeObject = null;
            GameObject originalActivityObject = null;
            GameObject targetActivityObject = null;
            bool targetApplied = false;

            try
            {
                routeObject = new GameObject("QA_ObjectEntrySnapshotLifecycle_Route");
                originalActivityObject = new GameObject("QA_ObjectEntrySnapshotLifecycle_OriginalActivity");
                targetActivityObject = new GameObject("QA_ObjectEntrySnapshotLifecycle_TargetActivity");

                routeObject.AddComponent<ObjectEntryDeclaration>().ConfigureForQa(
                    routeEntryId,
                    ObjectEntryScope.Route,
                    ObjectEntryRequiredness.Required,
                    "QA Snapshot Lifecycle Route Entry",
                    currentRoute);

                originalActivityObject.AddComponent<ObjectEntryDeclaration>().ConfigureForQa(
                    originalActivityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Snapshot Lifecycle Original Activity Entry",
                    null,
                    originalActivity);

                targetActivityObject.AddComponent<ObjectEntryDeclaration>().ConfigureForQa(
                    targetActivityEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Snapshot Lifecycle Target Activity Entry",
                    null,
                    targetActivity);

                runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:snapshot-lifecycle-baseline");
                int initialRevision = runtimeHost.ObjectEntryRuntimeContextRevision;
                int initialInvalidationCount = runtimeHost.ObjectEntryRuntimeContextInvalidationCount;

                var targetResult = await runtimeHost.RequestActivityAsync(
                    targetActivity,
                    source,
                    "Object Entry snapshot lifecycle target transition");
                if (!targetResult.Succeeded)
                {
                    logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='target-transition' reason='Target Activity request failed'. message='{targetResult.Message}'");
                    return false;
                }

                targetApplied = true;
                if (!runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var targetSnapshot)
                    || !ValidateLifecycleTransitionSnapshot(
                        logger,
                        runtimeHost,
                        targetSnapshot,
                        targetActivityEntryId,
                        originalActivityEntryId,
                        "target-transition"))
                {
                    return false;
                }

                int targetRevision = runtimeHost.ObjectEntryRuntimeContextRevision;
                if (targetRevision != initialRevision + 1
                    || runtimeHost.ObjectEntryRuntimeContextInvalidationCount != initialInvalidationCount + 1)
                {
                    logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='target-transition' reason='Snapshot revision/invalidation counters did not advance once' initialRevision='{initialRevision}' targetRevision='{targetRevision}' initialInvalidations='{initialInvalidationCount}' targetInvalidations='{runtimeHost.ObjectEntryRuntimeContextInvalidationCount}'.");
                    return false;
                }

                var restoreResult = await runtimeHost.RequestActivityAsync(
                    originalActivity,
                    source,
                    "Object Entry snapshot lifecycle restore transition");
                if (!restoreResult.Succeeded)
                {
                    logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='restore-transition' reason='Original Activity restore failed'. message='{restoreResult.Message}'");
                    return false;
                }

                targetApplied = false;
                if (!runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var restoredSnapshot)
                    || !ValidateLifecycleTransitionSnapshot(
                        logger,
                        runtimeHost,
                        restoredSnapshot,
                        originalActivityEntryId,
                        targetActivityEntryId,
                        "restore-transition"))
                {
                    return false;
                }

                int restoredRevision = runtimeHost.ObjectEntryRuntimeContextRevision;
                int restoredInvalidationCount = runtimeHost.ObjectEntryRuntimeContextInvalidationCount;
                if (restoredRevision != initialRevision + 2
                    || restoredInvalidationCount != initialInvalidationCount + 2)
                {
                    logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='restore-transition' reason='Snapshot revision/invalidation counters did not advance twice' initialRevision='{initialRevision}' restoredRevision='{restoredRevision}' initialInvalidations='{initialInvalidationCount}' restoredInvalidations='{restoredInvalidationCount}'.");
                    return false;
                }

                logger.Info(
                    "QA Object Entry Snapshot Lifecycle Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "activity-transition-refresh"),
                        LogFields.Field("source", source),
                        LogFields.Field("targetActivity", targetActivity.ActivityName),
                        LogFields.Field("restoredActivity", originalActivity.ActivityName),
                        LogFields.Field("targetSnapshotAvailable", targetSnapshot.IsAvailable),
                        LogFields.Field("restoredSnapshotAvailable", restoredSnapshot.IsAvailable),
                        LogFields.Field("initialRevision", initialRevision),
                        LogFields.Field("targetRevision", targetRevision),
                        LogFields.Field("restoredRevision", restoredRevision),
                        LogFields.Field("invalidationDelta", restoredInvalidationCount - initialInvalidationCount),
                        LogFields.Field("refreshDelta", restoredRevision - initialRevision),
                        LogFields.Field("lastInvalidationReason", runtimeHost.LastObjectEntryRuntimeContextInvalidationReason),
                        LogFields.Field("targetForeignFiltered", targetSnapshot.FilteredDeclarationCount > 0),
                        LogFields.Field("restoredForeignFiltered", restoredSnapshot.FilteredDeclarationCount > 0),
                        LogFields.Field("blockingIssues", restoredSnapshot.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", restoredSnapshot.NonBlockingIssueCount),
                        LogFields.Field("summary", restoredSnapshot.Summary)));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Entry Snapshot Lifecycle Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "snapshot-lifecycle"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                if (targetApplied
                    && originalActivity != null
                    && !ReferenceEquals(runtimeHost.State.CurrentActivity, originalActivity))
                {
                    await runtimeHost.RequestActivityAsync(
                        originalActivity,
                        source,
                        "Object Entry snapshot lifecycle emergency restore");
                }

                DestroyQaObject(routeObject);
                DestroyQaObject(originalActivityObject);
                DestroyQaObject(targetActivityObject);
            }
        }

        internal static Task<bool> RunFoundationClosureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectEntryQaSmokeRunner) : source;

            if (!runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var snapshot))
            {
                logger.Warning("QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Runtime Host snapshot is unavailable'. Start a new Play session or complete a successful Route/Activity transition first.");
                return Task.FromResult(false);
            }

            if (!ValidateFoundationClosureSnapshot(runtimeHost, logger, snapshot))
            {
                return Task.FromResult(false);
            }

            logger.Info(
                "QA Object Entry Foundation Closure Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", "foundation-closure"),
                    LogFields.Field("source", source),
                    LogFields.Field("snapshotAvailable", snapshot.IsAvailable),
                    LogFields.Field("lifecycleSource", true),
                    LogFields.Field("ownerDomainsValid", true),
                    LogFields.Field("activeOwnersValid", true),
                    LogFields.Field("countInvariant", true),
                    LogFields.Field("revision", runtimeHost.ObjectEntryRuntimeContextRevision),
                    LogFields.Field("invalidations", runtimeHost.ObjectEntryRuntimeContextInvalidationCount),
                    LogFields.Field("lastInvalidationReason", runtimeHost.LastObjectEntryRuntimeContextInvalidationReason),
                    LogFields.Field("resultStatus", snapshot.Status.ToString()),
                    LogFields.Field("declarations", snapshot.DeclarationCount),
                    LogFields.Field("candidateDescriptors", snapshot.CandidateDescriptorCount),
                    LogFields.Field("acceptedDeclarations", snapshot.AcceptedDeclarationCount),
                    LogFields.Field("rejectedDeclarations", snapshot.RejectedDeclarationCount),
                    LogFields.Field("filteredDeclarations", snapshot.FilteredDeclarationCount),
                    LogFields.Field("objectEntries", snapshot.Count),
                    LogFields.Field("route", snapshot.GetByScope(ObjectEntryScope.Route).Count),
                    LogFields.Field("activity", snapshot.GetByScope(ObjectEntryScope.Activity).Count),
                    LogFields.Field("session", snapshot.GetByScope(ObjectEntryScope.Session).Count),
                    LogFields.Field("blockingIssues", snapshot.BlockingIssueCount),
                    LogFields.Field("nonBlockingIssues", snapshot.NonBlockingIssueCount),
                    LogFields.Field("summary", snapshot.Summary)));

            return Task.FromResult(true);
        }

        private static bool ValidateSet(
            FrameworkLogger logger,
            ObjectEntrySet set,
            ObjectEntryDescriptor routeRequired,
            ObjectEntryDescriptor activityRequired)
        {
            if (set.Count != 3 || set.RequiredCount != 2 || set.OptionalCount != 1)
            {
                logger.Warning($"QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Unexpected aggregate counts'. {set.Summary}");
                return false;
            }

            if (set.GetByScope(ObjectEntryScope.Route).Count != 2 || set.GetByScope(ObjectEntryScope.Activity).Count != 1 || set.GetByScope(ObjectEntryScope.Session).Count != 0)
            {
                logger.Warning($"QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Unexpected scope counts'. {set.Summary}");
                return false;
            }

            if (!set.TryGet(routeRequired.Id, out var foundRoute) || !foundRoute.Equals(routeRequired))
            {
                logger.Warning("QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Required Route object entry was not retrievable by id'.");
                return false;
            }

            if (!set.TryGet(activityRequired.Id, out var foundActivity) || !foundActivity.Equals(activityRequired))
            {
                logger.Warning("QA Object Entry Synthetic Set Smoke step failed. step='synthetic-set' reason='Required Activity object entry was not retrievable by id'.");
                return false;
            }

            return true;
        }

        private static bool ValidateAcceptedResult(FrameworkLogger logger, ObjectEntryResult result)
        {
            if (!result.Succeeded || result.Failed || result.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Synthetic Set Smoke step failed. step='result' reason='Accepted result did not report success'. {result.Summary}");
                return false;
            }

            return true;
        }


        private static bool ValidateDeclarationSourceResult(
            FrameworkLogger logger,
            ObjectEntryDeclarationSourceResult result,
            ObjectEntryDeclaration routeDeclaration,
            ObjectEntryDeclaration activityDeclaration)
        {
            if (result == null || result.Failed || result.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Result failed'. summary='{result?.Summary ?? "<missing>"}'");
                return false;
            }

            if (result.Status != ObjectEntryResultStatus.Accepted || result.DeclarationCount != 2 || result.CandidateDescriptorCount != 2 || result.AcceptedDeclarationCount != 2 || result.RejectedDeclarationCount != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Unexpected declaration counts'. {result.Summary}");
                return false;
            }

            var set = result.ObjectEntries;
            if (set.Count != 2 || set.RequiredCount != 1 || set.OptionalCount != 1)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Unexpected ObjectEntrySet counts'. {result.Summary}");
                return false;
            }

            if (set.GetByScope(ObjectEntryScope.Route).Count != 1 || set.GetByScope(ObjectEntryScope.Activity).Count != 1 || set.GetByScope(ObjectEntryScope.Session).Count != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Unexpected ObjectEntrySet scope counts'. {result.Summary}");
                return false;
            }

            var routeDescriptor = routeDeclaration.CreateDescriptor();
            var activityDescriptor = activityDeclaration.CreateDescriptor();
            if (!set.TryGet(routeDescriptor.Id, out var foundRoute) || !foundRoute.Equals(routeDescriptor))
            {
                logger.Warning("QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Route declaration descriptor was not retrievable by id'.");
                return false;
            }

            if (!set.TryGet(activityDescriptor.Id, out var foundActivity) || !foundActivity.Equals(activityDescriptor))
            {
                logger.Warning("QA Object Entry Declaration Source Smoke step failed. step='declaration-source-set' reason='Activity declaration descriptor was not retrievable by id'.");
                return false;
            }

            return true;
        }

        private static bool ValidateDeclarationSourceDuplicateRejection(
            FrameworkLogger logger,
            ObjectEntryDeclarationSourceResult result)
        {
            if (result == null)
            {
                logger.Warning("QA Object Entry Declaration Source Smoke step failed. step='declaration-source-duplicate-identity' reason='Duplicate result was missing'.");
                return false;
            }

            bool hasDuplicateIssue = result.Issues.Any(issue => issue is { Kind: ObjectEntryIssueKind.DuplicateIdentity, IsBlocking: true });
            if (!result.Failed || result.Status != ObjectEntryResultStatus.Rejected || result.BlockingIssueCount == 0 || !hasDuplicateIssue)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-duplicate-identity' reason='Duplicate object entry identity was accepted'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.DeclarationCount != 2 || result.CandidateDescriptorCount != 2 || result.AcceptedDeclarationCount != 0 || result.RejectedDeclarationCount != 2 || result.ObjectEntries.Count != 0)
            {
                logger.Warning($"QA Object Entry Declaration Source Smoke step failed. step='declaration-source-duplicate-identity' reason='Duplicate rejection diagnostics were inconsistent'. {result.ToDiagnosticString()}");
                return false;
            }

            return true;
        }



        private static bool ValidateRuntimeIntegrationResult(
            FrameworkLogger logger,
            ObjectEntryDeclarationSourceResult result,
            string routeEntryId,
            string inactiveActivityEntryId)
        {
            if (result == null || result.Failed || result.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Runtime Integration Smoke step failed. step='loaded-scene-declarations' reason='Loaded scene declaration collection failed'. summary='{result?.ToDiagnosticString() ?? "<missing>"}'");
                return false;
            }

            if (result.DeclarationCount < 2 || result.CandidateDescriptorCount < 2 || result.ObjectEntries.Count < 2)
            {
                logger.Warning($"QA Object Entry Runtime Integration Smoke step failed. step='loaded-scene-declarations' reason='Loaded scene declaration counts were too low'. {result.Summary}");
                return false;
            }

            var routeId = ObjectEntryId.From(routeEntryId);
            if (!result.ObjectEntries.TryGet(routeId, out var routeDescriptor)
                || routeDescriptor.Scope != ObjectEntryScope.Route
                || routeDescriptor.Requiredness != ObjectEntryRequiredness.Required
                || routeDescriptor.SourceKind != ObjectEntrySourceKind.SceneAuthored)
            {
                logger.Warning($"QA Object Entry Runtime Integration Smoke step failed. step='loaded-scene-declarations' reason='QA Route declaration was not collected as expected'. {result.Summary}");
                return false;
            }

            var inactiveActivityId = ObjectEntryId.From(inactiveActivityEntryId);
            if (!result.ObjectEntries.TryGet(inactiveActivityId, out var inactiveActivityDescriptor)
                || inactiveActivityDescriptor.Scope != ObjectEntryScope.Activity
                || inactiveActivityDescriptor.Requiredness != ObjectEntryRequiredness.Optional
                || inactiveActivityDescriptor.SourceKind != ObjectEntrySourceKind.SceneAuthored)
            {
                logger.Warning($"QA Object Entry Runtime Integration Smoke step failed. step='loaded-scene-declarations' reason='QA inactive Activity declaration was not collected as expected'. {result.Summary}");
                return false;
            }

            return true;
        }

        private static bool ValidateRuntimeContextSnapshot(
            FrameworkLogger logger,
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectEntryDeclarationSourceResult result,
            string routeEntryId,
            string activityEntryId)
        {
            if (snapshot == null || result == null)
            {
                logger.Warning("QA Object Entry Runtime Context Snapshot Smoke step failed. step='runtime-context-snapshot' reason='Snapshot or source result was missing'.");
                return false;
            }

            if (!snapshot.IsAvailable || snapshot.Failed || snapshot.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Runtime Context Snapshot Smoke step failed. step='runtime-context-snapshot' reason='Snapshot was not available'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.Status != result.Status
                || snapshot.DeclarationCount != result.DeclarationCount
                || snapshot.CandidateDescriptorCount != result.CandidateDescriptorCount
                || snapshot.AcceptedDeclarationCount != result.AcceptedDeclarationCount
                || snapshot.RejectedDeclarationCount != result.RejectedDeclarationCount
                || snapshot.FilteredDeclarationCount != result.FilteredDeclarationCount
                || snapshot.Count != result.ObjectEntries.Count
                || snapshot.RequiredCount != result.ObjectEntries.RequiredCount
                || snapshot.OptionalCount != result.ObjectEntries.OptionalCount)
            {
                logger.Warning($"QA Object Entry Runtime Context Snapshot Smoke step failed. step='runtime-context-snapshot' reason='Snapshot did not mirror source result diagnostics'. snapshot='{snapshot.Summary}' result='{result.Summary}'");
                return false;
            }

            if (snapshot.Count < 2 || snapshot.DeclarationCount < 2 || snapshot.CandidateDescriptorCount < 2)
            {
                logger.Warning($"QA Object Entry Runtime Context Snapshot Smoke step failed. step='runtime-context-snapshot' reason='Snapshot counts were too low'. {snapshot.Summary}");
                return false;
            }

            var routeId = ObjectEntryId.From(routeEntryId);
            if (!snapshot.TryGet(routeId, out var routeDescriptor)
                || routeDescriptor.Scope != ObjectEntryScope.Route
                || routeDescriptor.Requiredness != ObjectEntryRequiredness.Required
                || routeDescriptor.SourceKind != ObjectEntrySourceKind.SceneAuthored)
            {
                logger.Warning($"QA Object Entry Runtime Context Snapshot Smoke step failed. step='runtime-context-snapshot' reason='QA Route declaration was not present in snapshot'. {snapshot.Summary}");
                return false;
            }

            var activityId = ObjectEntryId.From(activityEntryId);
            if (!snapshot.TryGet(activityId, out var activityDescriptor)
                || activityDescriptor.Scope != ObjectEntryScope.Activity
                || activityDescriptor.Requiredness != ObjectEntryRequiredness.Optional
                || activityDescriptor.SourceKind != ObjectEntrySourceKind.SceneAuthored)
            {
                logger.Warning($"QA Object Entry Runtime Context Snapshot Smoke step failed. step='runtime-context-snapshot' reason='QA Activity declaration was not present in snapshot'. {snapshot.Summary}");
                return false;
            }

            return true;
        }


        private static bool ValidateRuntimeHostSnapshotExposure(
            FrameworkLogger logger,
            FrameworkRuntimeHost runtimeHost,
            ObjectEntryRuntimeContextSnapshot snapshot,
            string routeEntryId,
            string activityEntryId)
        {
            if (snapshot == null)
            {
                logger.Warning("QA Object Entry Runtime Host Snapshot Exposure Smoke step failed. step='runtime-host-snapshot' reason='Snapshot was missing'.");
                return false;
            }

            if (!snapshot.IsAvailable || snapshot.Failed || snapshot.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Runtime Host Snapshot Exposure Smoke step failed. step='runtime-host-snapshot' reason='Snapshot was not available'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.Count < 2 || snapshot.DeclarationCount < 2 || snapshot.CandidateDescriptorCount < 2)
            {
                logger.Warning($"QA Object Entry Runtime Host Snapshot Exposure Smoke step failed. step='runtime-host-snapshot' reason='Snapshot counts were too low'. {snapshot.Summary}");
                return false;
            }

            var routeId = ObjectEntryId.From(routeEntryId);
            if (!snapshot.TryGet(routeId, out var routeDescriptor)
                || routeDescriptor.Scope != ObjectEntryScope.Route
                || routeDescriptor.Requiredness != ObjectEntryRequiredness.Required
                || routeDescriptor.SourceKind != ObjectEntrySourceKind.SceneAuthored
                || !routeDescriptor.HasOwnerIdentity
                || routeDescriptor.OwnerIdentity.Value != runtimeHost.State.RouteState.RouteIdentity)
            {
                logger.Warning($"QA Object Entry Runtime Host Snapshot Exposure Smoke step failed. step='runtime-host-snapshot' reason='QA Route declaration was not present in Runtime Host snapshot'. {snapshot.Summary}");
                return false;
            }

            var activityId = ObjectEntryId.From(activityEntryId);
            if (!snapshot.TryGet(activityId, out var activityDescriptor)
                || activityDescriptor.Scope != ObjectEntryScope.Activity
                || activityDescriptor.Requiredness != ObjectEntryRequiredness.Optional
                || activityDescriptor.SourceKind != ObjectEntrySourceKind.SceneAuthored
                || !activityDescriptor.HasOwnerIdentity
                || activityDescriptor.OwnerIdentity.Value != runtimeHost.State.ActivityState.ActivityIdentity)
            {
                logger.Warning($"QA Object Entry Runtime Host Snapshot Exposure Smoke step failed. step='runtime-host-snapshot' reason='QA Activity declaration was not present in Runtime Host snapshot'. {snapshot.Summary}");
                return false;
            }

            return true;
        }

        private static bool ValidateScopedOwnershipSnapshot(
            FrameworkLogger logger,
            FrameworkRuntimeHost runtimeHost,
            ObjectEntryRuntimeContextSnapshot snapshot,
            string routeEntryId,
            string activityEntryId,
            string foreignActivityEntryId)
        {
            if (snapshot == null || !snapshot.IsAvailable || snapshot.Failed || snapshot.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Scoped snapshot is unavailable'. summary='{snapshot?.Summary ?? "<missing>"}'");
                return false;
            }

            if (snapshot.FilteredDeclarationCount < 1)
            {
                logger.Warning($"QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Foreign Activity declaration was not counted as filtered'. {snapshot.Summary}");
                return false;
            }

            var routeId = ObjectEntryId.From(routeEntryId);
            if (!snapshot.TryGet(routeId, out var routeDescriptor)
                || !routeDescriptor.HasOwnerIdentity
                || routeDescriptor.OwnerIdentity.Value != runtimeHost.State.RouteState.RouteIdentity)
            {
                logger.Warning($"QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Route entry did not receive the active typed Route owner'. {snapshot.Summary}");
                return false;
            }

            var activityId = ObjectEntryId.From(activityEntryId);
            if (!snapshot.TryGet(activityId, out var activityDescriptor)
                || !activityDescriptor.HasOwnerIdentity
                || activityDescriptor.OwnerIdentity.Value != runtimeHost.State.ActivityState.ActivityIdentity)
            {
                logger.Warning($"QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Activity entry did not receive the active typed Activity owner'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.TryGet(ObjectEntryId.From(foreignActivityEntryId), out _))
            {
                logger.Warning($"QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Foreign Activity entry leaked into the active snapshot'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.Entries.Any(entry => !entry.HasOwnerIdentity))
            {
                logger.Warning($"QA Object Entry Scoped Ownership Smoke step failed. step='scoped-ownership' reason='Snapshot contains ownerless Object Entry descriptors'. {snapshot.Summary}");
                return false;
            }

            return true;
        }

        private static bool ValidateLifecycleTransitionSnapshot(
            FrameworkLogger logger,
            FrameworkRuntimeHost runtimeHost,
            ObjectEntryRuntimeContextSnapshot snapshot,
            string activeEntryId,
            string inactiveEntryId,
            string step)
        {
            if (snapshot == null || !snapshot.IsAvailable || snapshot.Failed || snapshot.BlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='{step}' reason='Automatic snapshot is unavailable'. summary='{snapshot?.Summary ?? "<missing>"}'");
                return false;
            }

            if (!snapshot.Source.StartsWith("FrameworkRuntimeHost:activity-request:", StringComparison.Ordinal))
            {
                logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='{step}' reason='Snapshot was not produced by the automatic Activity request boundary'. source='{snapshot.Source}'.");
                return false;
            }

            if (!snapshot.TryGet(ObjectEntryId.From(activeEntryId), out var activeDescriptor)
                || !activeDescriptor.HasOwnerIdentity
                || activeDescriptor.OwnerIdentity.Value != runtimeHost.State.ActivityState.ActivityIdentity)
            {
                logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='{step}' reason='Active Activity Object Entry is missing or has stale ownership'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.TryGet(ObjectEntryId.From(inactiveEntryId), out _))
            {
                logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='{step}' reason='Previous/foreign Activity Object Entry leaked into refreshed snapshot'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.FilteredDeclarationCount < 1)
            {
                logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='{step}' reason='Inactive Activity declaration was not reported as filtered'. {snapshot.Summary}");
                return false;
            }

            if (snapshot.Entries.Any(entry => !entry.HasOwnerIdentity))
            {
                logger.Warning($"QA Object Entry Snapshot Lifecycle Smoke step failed. step='{step}' reason='Automatic snapshot contains ownerless descriptors'. {snapshot.Summary}");
                return false;
            }

            return true;
        }

        private static bool ValidateFoundationClosureSnapshot(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            ObjectEntryRuntimeContextSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsAvailable || snapshot.Failed)
            {
                logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Snapshot is not available'. summary='{snapshot?.Summary ?? "<missing>"}'");
                return false;
            }

            if (snapshot.Status != ObjectEntryResultStatus.Accepted
                || snapshot.BlockingIssueCount != 0
                || snapshot.NonBlockingIssueCount != 0)
            {
                logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Snapshot contains rejected/warning state'. {snapshot.Summary}");
                return false;
            }

            if (!snapshot.Source.StartsWith("FrameworkRuntimeHost:", StringComparison.Ordinal))
            {
                logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Snapshot was not produced by a lifecycle boundary'. source='{snapshot.Source}'. Start a new Play session before running this smoke.");
                return false;
            }

            if (runtimeHost.ObjectEntryRuntimeContextRevision < 1
                || runtimeHost.ObjectEntryRuntimeContextInvalidationCount < 1)
            {
                logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Snapshot lifecycle counters were not initialized' revision='{runtimeHost.ObjectEntryRuntimeContextRevision}' invalidations='{runtimeHost.ObjectEntryRuntimeContextInvalidationCount}'.");
                return false;
            }

            if (snapshot.AcceptedDeclarationCount
                    + snapshot.RejectedDeclarationCount
                    + snapshot.FilteredDeclarationCount
                != snapshot.DeclarationCount
                || snapshot.CandidateDescriptorCount != snapshot.AcceptedDeclarationCount
                || snapshot.Count != snapshot.AcceptedDeclarationCount)
            {
                logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Declaration/set count invariant failed'. {snapshot.Summary}");
                return false;
            }

            var routeIdentity = runtimeHost.State.RouteState.RouteIdentity;
            var activityState = runtimeHost.State.ActivityState;
            for (int i = 0; i < snapshot.Entries.Count; i++)
            {
                var entry = snapshot.Entries[i];
                if (!entry.HasOwnerIdentity)
                {
                    logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Ownerless descriptor found' objectEntry='{entry.Id.StableText}'.");
                    return false;
                }

                var ownerIdentity = entry.OwnerIdentity.Value;
                if (ownerIdentity.Domain != ObjectEntryDescriptor.GetExpectedOwnerDomain(entry.Scope))
                {
                    logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Owner domain does not match scope' objectEntry='{entry.Id.StableText}' scope='{entry.Scope}' owner='{ownerIdentity.StableText}'.");
                    return false;
                }

                if (entry.Scope == ObjectEntryScope.Route && ownerIdentity != routeIdentity)
                {
                    logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Foreign/stale Route entry found' objectEntry='{entry.Id.StableText}' owner='{ownerIdentity.StableText}' activeRoute='{routeIdentity.StableText}'.");
                    return false;
                }

                if (entry.Scope == ObjectEntryScope.Activity
                    && (!activityState.IsActive || ownerIdentity != activityState.ActivityIdentity))
                {
                    logger.Warning($"QA Object Entry Foundation Closure Smoke step failed. step='foundation-closure' reason='Foreign/stale Activity entry found' objectEntry='{entry.Id.StableText}' owner='{ownerIdentity.StableText}' activeActivity='{activityState.DiagnosticIdentity}'.");
                    return false;
                }
            }

            return true;
        }

        private static ActivityAsset ResolveTransitionTarget(
            ActivityAsset currentActivity,
            ActivityAsset primaryActivity,
            ActivityAsset secondaryActivity)
        {
            if (secondaryActivity != null && !ReferenceEquals(currentActivity, secondaryActivity))
            {
                return secondaryActivity;
            }

            if (primaryActivity != null && !ReferenceEquals(currentActivity, primaryActivity))
            {
                return primaryActivity;
            }

            return null;
        }

        private static void DestroyQaObject(UnityEngine.Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(unityObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(unityObject);
            }
        }

        private static bool ValidateDuplicateRejection(
            FrameworkLogger logger,
            ObjectEntryDescriptor first,
            ObjectEntryDescriptor second)
        {
            try
            {
                _ = new ObjectEntrySet(new[] { first, first, second });
                logger.Warning("QA Object Entry Synthetic Set Smoke step failed. step='duplicate-identity' reason='Duplicate object entry identity was accepted'.");
                return false;
            }
            catch (ArgumentException)
            {
                logger.Info(
                    "QA Object Entry Synthetic Set Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "duplicate-identity"),
                        LogFields.Field("duplicateRejected", true),
                        LogFields.Field("objectEntry", first.Id.StableText)));
                return true;
            }
        }
    }
}
#endif
