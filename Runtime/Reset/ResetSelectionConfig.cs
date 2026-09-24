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
        [SerializeField] private ResetSelectionMode mode = ResetSelectionMode.ExplicitSubjects;
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
                    if (!TryAddSubjectsForScope(runtimeHost, ResetSubjectScope.Activity, subjects, out ResetIssue activityIssue))
                    {
                        issues.Add(activityIssue);
                    }
                    break;
                case ResetSelectionMode.CurrentRouteSubjects:
                    if (!TryAddSubjectsForScope(runtimeHost, ResetSubjectScope.Route, subjects, out ResetIssue routeIssue))
                    {
                        issues.Add(routeIssue);
                    }
                    break;
                case ResetSelectionMode.CurrentRouteAndActivitySubjects:
                    AddCurrentRouteActivityAndRuntimeSubjects(runtimeHost, subjects, issues);
                    break;
                case ResetSelectionMode.AllCurrentSubjects:
                    AddCurrentRouteActivityAndRuntimeSubjects(runtimeHost, subjects, issues);
                    break;
                case ResetSelectionMode.RuntimeOnlySubjects:
                    subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByOrigin(ResetSubjectOrigin.RuntimeRegistered).Select(subject => subject.SubjectId));
                    break;
                case ResetSelectionMode.SceneOnlySubjects:
                    subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByOrigin(ResetSubjectOrigin.SceneAuthored).Select(subject => subject.SubjectId));
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            ResetSelectionMode qaMode,
            IReadOnlyList<ResetSubjectReference> qaExplicitSubjects,
            bool qaAllowNoSubjects,
            bool qaAllowNoParticipants,
            bool qaStopOnFailure,
            bool qaYieldBetweenSubjects)
        {
            mode = qaMode;
            explicitSubjects = qaExplicitSubjects == null
                ? new List<ResetSubjectReference>()
                : qaExplicitSubjects.Where(reference => reference != null).ToList();
            allowNoSubjects = qaAllowNoSubjects;
            allowNoParticipants = qaAllowNoParticipants;
            stopOnFailure = qaStopOnFailure;
            yieldBetweenSubjects = qaYieldBetweenSubjects;
        }
#endif

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

        private static bool TryAddSubjectsForScope(
            FrameworkRuntimeHost runtimeHost,
            ResetSubjectScope scope,
            List<ResetSubjectId> subjects,
            out ResetIssue issue)
        {
            if (!runtimeHost.TryResolveCurrentResetOwner(scope, out RuntimeContentOwner owner, out string ownerIssue))
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidRequest,
                    $"Reset selection could not resolve current owner for scope '{scope}'. {ownerIssue}");
                return false;
            }

            subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByScopeAndOwner(scope, owner).Select(subject => subject.SubjectId));
            issue = default;
            return true;
        }

        private static void AddCurrentRouteActivityAndRuntimeSubjects(
            FrameworkRuntimeHost runtimeHost,
            List<ResetSubjectId> subjects,
            List<ResetIssue> issues)
        {
            RuntimeContentOwner routeOwner = default;
            RuntimeContentOwner activityOwner = default;
            bool hasRouteOwner = runtimeHost.TryResolveCurrentResetOwner(ResetSubjectScope.Route, out routeOwner, out string routeIssue);
            bool hasActivityOwner = runtimeHost.TryResolveCurrentResetOwner(ResetSubjectScope.Activity, out activityOwner, out string activityIssue);

            if (hasRouteOwner)
            {
                subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByScopeAndOwner(ResetSubjectScope.Route, routeOwner).Select(subject => subject.SubjectId));
                subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByScopeAndOwner(ResetSubjectScope.Runtime, routeOwner).Select(subject => subject.SubjectId));
            }
            else
            {
                issues.Add(ResetIssue.Warning(ResetIssueKind.InvalidRequest, $"Reset selection could not resolve current route owner. {routeIssue}"));
            }

            if (hasActivityOwner)
            {
                subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByScopeAndOwner(ResetSubjectScope.Activity, activityOwner).Select(subject => subject.SubjectId));
                subjects.AddRange(runtimeHost.ResetRegistry.GetSubjectsByScopeAndOwner(ResetSubjectScope.Runtime, activityOwner).Select(subject => subject.SubjectId));
            }
            else
            {
                issues.Add(ResetIssue.Warning(ResetIssueKind.InvalidRequest, $"Reset selection could not resolve current activity owner. {activityIssue}"));
            }
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
