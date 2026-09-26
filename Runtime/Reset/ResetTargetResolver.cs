using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// Internal IF-ADR-035 bridge from semantic Reset targets to the existing Reset execution input.
    /// It does not own registration, lifecycle ownership or execution.
    /// </summary>
    internal static class ResetTargetResolver
    {
        internal static ResetSelectionResolution Resolve(
            FrameworkRuntimeHost runtimeHost,
            ResetTarget target,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(ResetTargetResolver));
            string resolvedReason = reason.NormalizeText();

            if (runtimeHost == null)
            {
                return ResetSelectionResolution.FailedResult(
                    ToLegacySelectionMode(target.Kind),
                    ResetSelectionResolutionStatus.RejectedRuntimeUnavailable,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, "Reset target resolution requires an active FrameworkRuntimeHost."),
                    resolvedSource,
                    resolvedReason,
                    "Reset target resolution failed because the runtime host is unavailable.");
            }

            if (!target.IsValid)
            {
                return ResetSelectionResolution.FailedResult(
                    ToLegacySelectionMode(target.Kind),
                    ResetSelectionResolutionStatus.RejectedInvalidRequest,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Reset target '{target.Kind}' is not resolvable in the current architecture cut."),
                    resolvedSource,
                    resolvedReason,
                    "Reset target resolution failed because the target is invalid or not implemented yet.");
            }

            switch (target.Kind)
            {
                case ResetTargetKind.CurrentActivity:
                    return ResolveCurrentOwner(
                        runtimeHost,
                        ResetSubjectScope.Activity,
                        ResetSelectionMode.CurrentActivitySubjects,
                        resolvedSource,
                        resolvedReason);

                case ResetTargetKind.CurrentRoute:
                    return ResolveCurrentOwner(
                        runtimeHost,
                        ResetSubjectScope.Route,
                        ResetSelectionMode.CurrentRouteSubjects,
                        resolvedSource,
                        resolvedReason);

                case ResetTargetKind.StableReference:
                    return ResetSelectionResolution.SucceededResult(
                        ResetSelectionMode.ExplicitSubjects,
                        new[] { target.StableReference },
                        System.Array.Empty<ResetIssue>(),
                        resolvedSource,
                        resolvedReason,
                        "Reset stable target resolved through the legacy ResetSubjectId bridge.");

                default:
                    return ResetSelectionResolution.FailedResult(
                        ToLegacySelectionMode(target.Kind),
                        ResetSelectionResolutionStatus.RejectedInvalidRequest,
                        ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Reset target kind '{target.Kind}' is not supported by RESET-035-A."),
                        resolvedSource,
                        resolvedReason,
                        "Reset target resolution failed because the target kind is not supported by this architecture cut.");
            }
        }

        private static ResetSelectionResolution ResolveCurrentOwner(
            FrameworkRuntimeHost runtimeHost,
            ResetSubjectScope lifecycleScope,
            ResetSelectionMode legacyMode,
            string source,
            string reason)
        {
            if (!runtimeHost.TryResolveCurrentResetOwner(
                    lifecycleScope,
                    out RuntimeContentOwner owner,
                    out string ownerIssue))
            {
                return ResetSelectionResolution.FailedResult(
                    legacyMode,
                    ResetSelectionResolutionStatus.Failed,
                    ResetIssue.Error(
                        ResetIssueKind.InvalidRequest,
                        $"Reset target could not resolve current owner for scope '{lifecycleScope}'. {ownerIssue}"),
                    source,
                    reason,
                    "Reset target resolution failed before execution.");
            }

            var subjects = new List<ResetSubjectId>();
            subjects.AddRange(
                runtimeHost.ResetRegistry
                    .GetSubjectsByScopeAndOwner(lifecycleScope, owner)
                    .Select(subject => subject.SubjectId));
            subjects.AddRange(
                runtimeHost.ResetRegistry
                    .GetSubjectsByScopeAndOwner(ResetSubjectScope.Runtime, owner)
                    .Select(subject => subject.SubjectId));

            return ResetSelectionResolution.SucceededResult(
                legacyMode,
                subjects,
                System.Array.Empty<ResetIssue>(),
                source,
                reason,
                subjects.Count == 0
                    ? "Reset target resolved no subjects."
                    : "Reset target resolved subjects.");
        }

        private static ResetSelectionMode ToLegacySelectionMode(ResetTargetKind kind)
        {
            switch (kind)
            {
                case ResetTargetKind.CurrentActivity:
                    return ResetSelectionMode.CurrentActivitySubjects;
                case ResetTargetKind.CurrentRoute:
                    return ResetSelectionMode.CurrentRouteSubjects;
                default:
                    return ResetSelectionMode.ExplicitSubjects;
            }
        }
    }
}
