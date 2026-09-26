using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Inline authoring config that resolves ResetSubjects from ResetRegistry.
    /// It does not consult ObjectEntry snapshots.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12D inline ResetSelectionPolicy config.")]
    public sealed class ResetSelectionConfig : IEquatable<ResetSelectionConfig>
    {
        [SerializeField] private ResetSelectionMode mode = ResetSelectionMode.CurrentActivitySubjects;
        [SerializeField] private List<ResetSubjectReference> explicitSubjects = new List<ResetSubjectReference>();
        [SerializeField] private bool allowNoSubjects;
        [SerializeField] private bool allowNoParticipants = true;
        [SerializeField] private bool stopOnFailure = true;
        [SerializeField] private bool yieldBetweenSubjects;

        public ResetSelectionMode Mode => mode;

        public IReadOnlyList<ResetSubjectReference> ExplicitSubjects => explicitSubjects != null ? explicitSubjects : Array.Empty<ResetSubjectReference>();

        public bool AllowNoSubjects => allowNoSubjects;

        public bool AllowNoParticipants => allowNoParticipants;

        public bool StopOnFailure => stopOnFailure;

        public bool YieldBetweenSubjects => yieldBetweenSubjects;

        internal ResetSelectionResolution Resolve(
            FrameworkRuntimeHost runtimeHost,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(ResetSelectionConfig));
            string resolvedReason = reason.NormalizeText();

            if (runtimeHost == null)
            {
                return ResetSelectionResolution.FailedResult(
                    ResolveMode(),
                    ResetSelectionResolutionStatus.RejectedRuntimeUnavailable,
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, "Reset selection requires an active FrameworkRuntimeHost."),
                    resolvedSource,
                    resolvedReason,
                    "Reset selection failed because the runtime host is unavailable.");
            }

            ResetSelectionMode resolvedMode = ResolveMode();
            var issues = new List<ResetIssue>();
            var subjects = new List<ResetSubjectId>();
            switch (resolvedMode)
            {
                case ResetSelectionMode.ExplicitSubjects:
                    AddExplicitSubjects(subjects, issues);
                    break;
                case ResetSelectionMode.CurrentActivitySubjects:
                    return ResetTargetResolver.Resolve(
                        runtimeHost,
                        ResetTarget.CurrentActivity(),
                        resolvedSource,
                        resolvedReason);
                case ResetSelectionMode.CurrentRouteSubjects:
                    return ResetTargetResolver.Resolve(
                        runtimeHost,
                        ResetTarget.CurrentRoute(),
                        resolvedSource,
                        resolvedReason);
                case ResetSelectionMode.CurrentRouteAndActivitySubjects:
                    AddCurrentOwnerSubjects(
                        runtimeHost,
                        ResetSubjectScope.Route,
                        subjects,
                        issues);
                    AddCurrentOwnerSubjects(
                        runtimeHost,
                        ResetSubjectScope.Activity,
                        subjects,
                        issues);
                    break;
                case ResetSelectionMode.AllCurrentSubjects:
                    subjects.AddRange(
                        runtimeHost.ResetRegistry
                            .SnapshotSubjects()
                            .Select(subject => subject.SubjectId));
                    break;
                default:
                    return ResetSelectionResolution.FailedResult(
                        ResetSelectionMode.ExplicitSubjects,
                        ResetSelectionResolutionStatus.RejectedInvalidRequest,
                        ResetIssue.Error(ResetIssueKind.InvalidRequest, $"Unsupported reset selection mode '{resolvedMode}'."),
                        resolvedSource,
                        resolvedReason,
                        "Reset selection failed because the selection mode is invalid.");
            }

            var normalized = NormalizeSubjectIds(subjects);
            ResetIssue[] blocking = issues.Where(issue => issue.IsBlocking).ToArray();
            if (blocking.Length > 0 && normalized.Count == 0)
            {
                return new ResetSelectionResolution(
                    ResetSelectionResolutionStatus.Failed,
                    resolvedMode,
                    normalized,
                    issues,
                    resolvedSource,
                    resolvedReason,
                    "Reset selection failed before execution.");
            }

            return ResetSelectionResolution.SucceededResult(
                resolvedMode,
                normalized,
                issues,
                resolvedSource,
                resolvedReason,
                normalized.Count == 0
                    ? "Reset selection resolved no subjects."
                    : "Reset selection resolved subjects.");
        }

        internal ResetExecutionRequest CreateExecutionRequest(ResetSelectionResolution resolution)
        {
            return resolution.ToExecutionRequest(
                allowNoSubjects,
                allowNoParticipants,
                stopOnFailure,
                yieldBetweenSubjects);
        }

        public bool Equals(ResetSelectionConfig other)
        {
            if (other == null)
            {
                return false;
            }

            return mode == other.mode
                && ExplicitSubjects.SequenceEqual(other.ExplicitSubjects)
                && allowNoSubjects == other.allowNoSubjects
                && allowNoParticipants == other.allowNoParticipants
                && stopOnFailure == other.stopOnFailure
                && yieldBetweenSubjects == other.yieldBetweenSubjects;
        }

        public override bool Equals(object obj)
        {
            return obj is ResetSelectionConfig other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)mode;
                for (int i = 0; i < ExplicitSubjects.Count; i++)
                {
                    hashCode = hashCode * 397 ^ (ExplicitSubjects[i] != null ? ExplicitSubjects[i].GetHashCode() : 0);
                }

                hashCode = hashCode * 397 ^ allowNoSubjects.GetHashCode();
                hashCode = hashCode * 397 ^ allowNoParticipants.GetHashCode();
                hashCode = hashCode * 397 ^ stopOnFailure.GetHashCode();
                hashCode = hashCode * 397 ^ yieldBetweenSubjects.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"mode='{ResolveMode()}' explicitSubjects='{ExplicitSubjects.Count}' allowNoSubjects='{allowNoSubjects}' allowNoParticipants='{allowNoParticipants}' stopOnFailure='{stopOnFailure}' yieldBetweenSubjects='{yieldBetweenSubjects}'";
        }

        private ResetSelectionMode ResolveMode()
        {
            return Enum.IsDefined(typeof(ResetSelectionMode), mode) && mode != ResetSelectionMode.Unknown
                ? mode
                : ResetSelectionMode.ExplicitSubjects;
        }

        private void AddExplicitSubjects(List<ResetSubjectId> subjects, List<ResetIssue> issues)
        {
            IReadOnlyList<ResetSubjectReference> references = ExplicitSubjects;
            for (int i = 0; i < references.Count; i++)
            {
                ResetSubjectReference reference = references[i];
                if (reference == null)
                {
                    issues.Add(ResetIssue.Warning(ResetIssueKind.InvalidSubject, $"Reset explicit subject reference is null. index='{i}'."));
                    continue;
                }

                if (!reference.TryResolve(out ResetSubjectId subjectId, out ResetIssue issue))
                {
                    issues.Add(issue);
                    continue;
                }

                subjects.Add(subjectId);
            }
        }

        private static void AddCurrentOwnerSubjects(
            FrameworkRuntimeHost runtimeHost,
            ResetSubjectScope lifecycleScope,
            List<ResetSubjectId> subjects,
            List<ResetIssue> issues)
        {
            if (!runtimeHost.TryResolveCurrentResetOwner(
                    lifecycleScope,
                    out RuntimeContentOwner owner,
                    out string ownerIssue))
            {
                issues.Add(ResetIssue.Error(
                    ResetIssueKind.InvalidRequest,
                    $"Reset selection could not resolve current owner for scope '{lifecycleScope}'. {ownerIssue}"));
                return;
            }

            subjects.AddRange(
                runtimeHost.ResetRegistry
                    .GetSubjectsByScopeAndOwner(lifecycleScope, owner)
                    .Select(subject => subject.SubjectId));
            subjects.AddRange(
                runtimeHost.ResetRegistry
                    .GetSubjectsByScopeAndOwner(ResetSubjectScope.Runtime, owner)
                    .Select(subject => subject.SubjectId));
        }

        private static IReadOnlyList<ResetSubjectId> NormalizeSubjectIds(IEnumerable<ResetSubjectId> subjectIds)
        {
            if (subjectIds == null)
            {
                return Array.Empty<ResetSubjectId>();
            }

            var normalized = new List<ResetSubjectId>();
            var seen = new HashSet<ResetSubjectId>();
            foreach (ResetSubjectId subjectId in subjectIds)
            {
                if (!subjectId.IsValid || !seen.Add(subjectId))
                {
                    continue;
                }

                normalized.Add(subjectId);
            }

            return normalized.Count == 0 ? Array.Empty<ResetSubjectId>() : normalized.ToArray();
        }
    }
}
