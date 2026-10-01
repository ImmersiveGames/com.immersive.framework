using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Reset
{
    /// <summary>Resolves semantic Reset intent to the existing executor's subject IDs.</summary>
    internal static class ResetTargetResolver
    {
        internal static ResetSelectionResolution Resolve(FrameworkRuntimeHost runtimeHost, ResetTarget target, string source, string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(ResetTargetResolver));
            string resolvedReason = reason.NormalizeText();
            if (runtimeHost == null)
            {
                return Failed(target.Kind, ResetSelectionResolutionStatus.RejectedRuntimeUnavailable,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, "Reset target resolution requires an active FrameworkRuntimeHost."),
                    resolvedSource, resolvedReason, "Reset target resolution failed because the runtime host is unavailable.");
            }

            if (!target.IsValid)
            {
                return Failed(target.Kind, ResetSelectionResolutionStatus.RejectedInvalidRequest,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Reset target '{target.Kind}' is missing or invalid."),
                    resolvedSource, resolvedReason, "Reset target resolution failed because the target is invalid.");
            }

            switch (target.Kind)
            {
                case ResetTargetKind.Object:
                    return ResolveObject(runtimeHost, target.Object, resolvedSource, resolvedReason);
                case ResetTargetKind.Composition:
                    return ResolveComposition(runtimeHost, target.Composition, resolvedSource, resolvedReason);
                case ResetTargetKind.StableReference:
                    return ResolveStableReference(runtimeHost, target.StableReference, resolvedSource, resolvedReason);
                case ResetTargetKind.CurrentActivity:
                    return ResolveCurrentActivity(runtimeHost, resolvedSource, resolvedReason);
                case ResetTargetKind.CurrentRoute:
                    return ResolveCurrentRoute(runtimeHost, resolvedSource, resolvedReason);
                default:
                    return Failed(target.Kind, ResetSelectionResolutionStatus.RejectedInvalidRequest,
                        ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Reset target kind '{target.Kind}' is not supported."),
                        resolvedSource, resolvedReason, "Reset target resolution failed because the target kind is unsupported.");
            }
        }

        /// <summary>Activity Restart resets only Route-owned state that survives Activity Clear/Reenter.</summary>
        internal static ResetSelectionResolution ResolveForActivityRestart(
            FrameworkRuntimeHost runtimeHost, ResetTarget target, string source, string reason)
        {
            ResetSelectionResolution resolution = Resolve(runtimeHost, target, source, reason);
            if (resolution.Failed || runtimeHost == null) return resolution;

            if (!runtimeHost.TryResolveCurrentResetOwner(ResetSubjectScope.Route, out RuntimeContentOwner routeOwner, out _))
            {
                return ResetSelectionResolution.SucceededResult(resolution.Mode, System.Array.Empty<ResetSubjectId>(),
                    resolution.Issues, resolution.Source, resolution.Reason,
                    "Activity Restart found no Route-owned Reset subjects that survive Activity Clear/Reenter.");
            }

            IReadOnlyList<ResetSubjectId> survivors = FilterActivityRestartSurvivors(
                resolution.SubjectIds, runtimeHost.ResetRegistry, routeOwner, target.Kind);

            return ResetSelectionResolution.SucceededResult(resolution.Mode, survivors, resolution.Issues,
                resolution.Source, resolution.Reason,
                survivors.Count == 0
                    ? "Activity Restart found no surviving Reset subjects for its target."
                    : "Activity Restart resolved surviving Route-owned Reset subjects.");
        }

        internal static IReadOnlyList<ResetSubjectId> FilterActivityRestartSurvivors(
            IReadOnlyList<ResetSubjectId> resolvedIds,
            ResetRegistry registry,
            RuntimeContentOwner currentRouteOwner,
            ResetTargetKind targetKind)
        {
            if (resolvedIds == null || registry == null || !currentRouteOwner.IsValid
                || currentRouteOwner.Scope != RuntimeContentScope.Route)
                return System.Array.Empty<ResetSubjectId>();

            return resolvedIds.Where(id => registry.TryGetSubject(id, out ResetSubject subject)
                    && subject.Owner.Equals(currentRouteOwner)
                    && (targetKind != ResetTargetKind.CurrentActivity || subject.EffectiveMembership == ResetMembership.Activity))
                .ToArray();
        }

        internal static IReadOnlyList<ResetSubjectId> ResolveCurrentActivitySubjects(
            ResetRegistry registry, RuntimeContentOwner activityOwner, RuntimeContentOwner routeOwner)
        {
            if (registry == null || !activityOwner.IsValid || activityOwner.Scope != RuntimeContentScope.Activity)
                return System.Array.Empty<ResetSubjectId>();

            return registry.SnapshotSubjects()
                .Where(subject => subject.EffectiveMembership == ResetMembership.Activity
                    && (subject.Owner.Equals(activityOwner)
                        || (routeOwner.IsValid && routeOwner.Scope == RuntimeContentScope.Route && subject.Owner.Equals(routeOwner))))
                .OrderBy(subject => subject.Owner.StableText, System.StringComparer.Ordinal)
                .ThenBy(subject => subject.SubjectId.StableText, System.StringComparer.Ordinal)
                .Select(subject => subject.SubjectId).ToArray();
        }

        internal static IReadOnlyList<ResetSubjectId> ResolveCurrentRouteSubjects(ResetRegistry registry, RuntimeContentOwner routeOwner)
        {
            if (registry == null || !routeOwner.IsValid || routeOwner.Scope != RuntimeContentScope.Route)
                return System.Array.Empty<ResetSubjectId>();

            return registry.SnapshotSubjects()
                .Where(subject => subject.EffectiveMembership == ResetMembership.Route && subject.Owner.Equals(routeOwner))
                .OrderBy(subject => subject.Owner.StableText, System.StringComparer.Ordinal)
                .ThenBy(subject => subject.SubjectId.StableText, System.StringComparer.Ordinal)
                .Select(subject => subject.SubjectId).ToArray();
        }

        internal static bool TryResolveObjectSubject(
            ResetRegistry registry,
            Resettable resettable,
            IReadOnlyList<RuntimeContentOwner> currentOwners,
            out ResetSubject subject,
            out string diagnostic)
        {
            subject = default;
            if (resettable == null)
            {
                diagnostic = "Object target requires a non-null Resettable reference.";
                return false;
            }

            if (!resettable.IsRegistered || registry == null
                || !registry.TryGetSubject(resettable.SubjectHandle, out subject)
                || !subject.SubjectId.Equals(resettable.RuntimeSubjectId)
                || !subject.Owner.Equals(resettable.Owner))
            {
                diagnostic = $"Object target Resettable '{resettable.name}' is not registered or has stale registration evidence.";
                return false;
            }

            if (!IsOwnedByCurrentContext(subject.Owner, currentOwners))
            {
                subject = default;
                diagnostic = $"Object target Resettable '{resettable.name}' belongs to an owner outside the current Activity/Route context.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        internal static bool TryResolveStableReferenceSubject(
            StableObjectBindingRegistry bindings,
            ResetRegistry registry,
            StableObjectReference reference,
            out ResetSubject subject,
            out string diagnostic)
        {
            subject = default;
            if (!reference.IsValid || !reference.TryGetObjectEntryId(out ObjectEntryId objectEntryId)
                || !reference.TryGetOwnerSelector(out var ownerSelector, out var ownerDefinitionSelector))
            {
                diagnostic = "StableReference requires a valid ObjectEntryId and typed owner selector.";
                return false;
            }

            if (bindings == null)
            {
                diagnostic = "StableReference cannot resolve because the StableObjectBinding authority is unavailable.";
                return false;
            }

            if (!bindings.TryResolve(objectEntryId, ownerSelector, ownerDefinitionSelector,
                    out StableObjectBinding binding, out StableObjectBindingResolutionStatus bindingStatus,
                    out string bindingDiagnostic))
            {
                diagnostic = bindingDiagnostic;
                return false;
            }

            if (bindingStatus != StableObjectBindingResolutionStatus.Resolved || binding == null
                || binding.PhysicalObject == null)
            {
                diagnostic = bindingDiagnostic;
                return false;
            }

            if (!binding.PhysicalObject.TryGetComponent(out Resettable resettable) || resettable == null)
            {
                diagnostic = $"StableReference ObjectEntryId='{objectEntryId.StableText}' resolved a physical occurrence without a Resettable component.";
                return false;
            }

            if (!resettable.IsRegistered || registry == null
                || !registry.TryGetSubject(resettable.SubjectHandle, out subject)
                || !subject.SubjectId.Equals(resettable.RuntimeSubjectId)
                || !subject.Owner.Equals(resettable.Owner)
                || !subject.Owner.Equals(binding.Owner))
            {
                subject = default;
                diagnostic = $"StableReference ObjectEntryId='{objectEntryId.StableText}' resolved a Resettable without a current registration for the bound owner.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        internal static bool TryResolveCompositionSubjects(
            ResetRegistry registry,
            ResetComposition composition,
            IReadOnlyList<RuntimeContentOwner> currentOwners,
            out IReadOnlyList<ResetSubjectId> subjectIds,
            out IReadOnlyList<ResetIssue> issues,
            out string diagnostic)
        {
            subjectIds = System.Array.Empty<ResetSubjectId>();
            issues = System.Array.Empty<ResetIssue>();
            ResetCompositionResolver.Resolution resolved = ResetCompositionResolver.Resolve(composition);
            if (!resolved.Succeeded)
            {
                diagnostic = resolved.Diagnostic;
                return false;
            }

            var result = new List<ResetSubjectId>(resolved.Members.Count);
            var seen = new HashSet<ResetSubjectId>();
            for (int index = 0; index < resolved.Members.Count; index++)
            {
                if (!TryResolveObjectSubject(registry, resolved.Members[index], currentOwners, out ResetSubject subject, out diagnostic))
                {
                    subjectIds = System.Array.Empty<ResetSubjectId>();
                    return false;
                }

                if (seen.Add(subject.SubjectId)) result.Add(subject.SubjectId);
            }

            subjectIds = result;
            issues = resolved.Diagnostics.Select(message => ResetIssue.Warning(ResetIssueKind.InvalidRequest, message)).ToArray();
            diagnostic = result.Count == 0
                ? "Composition target is empty; no subjects were selected."
                : "Composition target resolved registered members in deterministic composition order.";
            return true;
        }

        private static ResetSelectionResolution ResolveObject(FrameworkRuntimeHost host, Resettable resettable, string source, string reason)
        {
            if (!TryGetCurrentOwners(host, out RuntimeContentOwner[] owners, out string ownerDiagnostic))
                return Failed(ResetTargetKind.Object, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, ownerDiagnostic), source, reason, "Object target owner context could not be resolved.");
            if (!TryResolveObjectSubject(host.ResetRegistry, resettable, owners, out ResetSubject subject, out string diagnostic))
                return Failed(ResetTargetKind.Object, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, diagnostic), source, reason, "Object target did not resolve a registered current Resettable.");

            return ResetSelectionResolution.SucceededResult(ResetSelectionMode.ExplicitSubjects,
                new[] { subject.SubjectId }, System.Array.Empty<ResetIssue>(), source, reason,
                "Object target resolved exactly one registered Resettable.");
        }

        private static ResetSelectionResolution ResolveComposition(FrameworkRuntimeHost host, ResetComposition composition, string source, string reason)
        {
            if (!TryGetCurrentOwners(host, out RuntimeContentOwner[] owners, out string ownerDiagnostic))
                return Failed(ResetTargetKind.Composition, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, ownerDiagnostic), source, reason, "Composition target owner context could not be resolved.");
            if (!TryResolveCompositionSubjects(host.ResetRegistry, composition, owners,
                    out IReadOnlyList<ResetSubjectId> ids, out IReadOnlyList<ResetIssue> issues, out string diagnostic))
                return Failed(ResetTargetKind.Composition, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, diagnostic), source, reason, "Composition contains an invalid or unregistered member.");

            return ResetSelectionResolution.SucceededResult(ResetSelectionMode.ExplicitSubjects, ids, issues, source, reason, diagnostic);
        }

        private static ResetSelectionResolution ResolveStableReference(
            FrameworkRuntimeHost host,
            StableObjectReference reference,
            string source,
            string reason)
        {
            if (!TryResolveStableReferenceSubject(
                    host.StableObjectBindings,
                    host.ResetRegistry,
                    reference,
                    out ResetSubject subject,
                    out string diagnostic))
            {
                ResetIssueKind issueKind = diagnostic.Contains("ambiguous")
                    ? ResetIssueKind.InvalidSubject
                    : ResetIssueKind.SubjectNotFound;
                return Failed(ResetTargetKind.StableReference, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(issueKind, diagnostic), source, reason,
                    "StableReference did not resolve exactly one live occurrence with a currently registered Resettable.");
            }

            return ResetSelectionResolution.SucceededResult(
                ResetSelectionMode.ExplicitSubjects,
                new[] { subject.SubjectId },
                System.Array.Empty<ResetIssue>(),
                source,
                reason,
                "StableReference resolved the current physical occurrence and its current Reset subject.");
        }

        private static bool TryGetCurrentOwners(FrameworkRuntimeHost host, out RuntimeContentOwner[] owners, out string diagnostic)
        {
            var result = new List<RuntimeContentOwner>(2);
            if (host.TryResolveCurrentResetOwner(ResetSubjectScope.Activity, out RuntimeContentOwner activity, out _)) result.Add(activity);
            if (host.TryResolveCurrentResetOwner(ResetSubjectScope.Route, out RuntimeContentOwner route, out _)) result.Add(route);
            owners = result.ToArray();
            diagnostic = owners.Length == 0 ? "No current Activity or Route owner is available." : string.Empty;
            return owners.Length > 0;
        }

        private static bool IsOwnedByCurrentContext(RuntimeContentOwner owner, IReadOnlyList<RuntimeContentOwner> owners)
        {
            if (owners == null) return false;
            for (int index = 0; index < owners.Count; index++)
                if (owners[index].IsValid && owners[index].Equals(owner)) return true;
            return false;
        }

        private static ResetSelectionResolution ResolveCurrentActivity(FrameworkRuntimeHost host, string source, string reason)
        {
            if (!host.TryResolveCurrentResetOwner(ResetSubjectScope.Activity, out RuntimeContentOwner activity, out string issue))
                return Failed(ResetTargetKind.CurrentActivity, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Current Activity owner could not be resolved. {issue}"), source, reason,
                    "CurrentActivity target resolution failed before execution.");

            host.TryResolveCurrentResetOwner(ResetSubjectScope.Route, out RuntimeContentOwner route, out _);
            IReadOnlyList<ResetSubjectId> ids = ResolveCurrentActivitySubjects(host.ResetRegistry, activity, route);
            return ResetSelectionResolution.SucceededResult(ResetSelectionMode.CurrentActivitySubjects, ids,
                System.Array.Empty<ResetIssue>(), source, reason, "CurrentActivity target resolved registered subjects.");
        }

        private static ResetSelectionResolution ResolveCurrentRoute(FrameworkRuntimeHost host, string source, string reason)
        {
            if (!host.TryResolveCurrentResetOwner(ResetSubjectScope.Route, out RuntimeContentOwner route, out string issue))
                return Failed(ResetTargetKind.CurrentRoute, ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Current Route owner could not be resolved. {issue}"), source, reason,
                    "CurrentRoute target resolution failed before execution.");

            IReadOnlyList<ResetSubjectId> ids = ResolveCurrentRouteSubjects(host.ResetRegistry, route);
            return ResetSelectionResolution.SucceededResult(ResetSelectionMode.CurrentRouteSubjects, ids,
                System.Array.Empty<ResetIssue>(), source, reason, "CurrentRoute target resolved registered subjects.");
        }

        private static ResetSelectionResolution Failed(ResetTargetKind kind, ResetSelectionResolutionStatus status,
            ResetIssue issue, string source, string reason, string message)
        {
            ResetSelectionMode mode = kind == ResetTargetKind.CurrentActivity
                ? ResetSelectionMode.CurrentActivitySubjects
                : kind == ResetTargetKind.CurrentRoute ? ResetSelectionMode.CurrentRouteSubjects : ResetSelectionMode.ExplicitSubjects;
            return ResetSelectionResolution.FailedResult(mode, status, issue, source, reason, message);
        }
    }
}
