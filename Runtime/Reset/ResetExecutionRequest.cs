using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Explicit subject reset execution request.
    /// Selection modes are introduced later by ResetSelectionPolicy; preview.12C keeps the executor boundary small.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12C ResetExecutor explicit subject request.")]
    public readonly struct ResetExecutionRequest : IEquatable<ResetExecutionRequest>
    {
        private readonly IReadOnlyList<ResetSubjectId> _subjectIds;

        public ResetExecutionRequest(
            IReadOnlyList<ResetSubjectId> subjectIds,
            bool allowNoSubjects,
            bool allowNoParticipants,
            bool stopOnFailure,
            bool yieldBetweenSubjects,
            string source,
            string reason)
        {
            _subjectIds = NormalizeSubjectIds(subjectIds);
            AllowNoSubjects = allowNoSubjects;
            AllowNoParticipants = allowNoParticipants;
            StopOnFailure = stopOnFailure;
            YieldBetweenSubjects = yieldBetweenSubjects;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public IReadOnlyList<ResetSubjectId> SubjectIds => _subjectIds ?? Array.Empty<ResetSubjectId>();

        public bool AllowNoSubjects { get; }

        public bool AllowNoParticipants { get; }

        public bool StopOnFailure { get; }

        public bool YieldBetweenSubjects { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => _subjectIds != null && SubjectIds.All(subjectId => subjectId.IsValid);

        public bool HasSubjects => SubjectIds.Count > 0;

        public bool Equals(ResetExecutionRequest other)
        {
            return SubjectIds.SequenceEqual(other.SubjectIds)
                && AllowNoSubjects == other.AllowNoSubjects
                && AllowNoParticipants == other.AllowNoParticipants
                && StopOnFailure == other.StopOnFailure
                && YieldBetweenSubjects == other.YieldBetweenSubjects
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetExecutionRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                for (int i = 0; i < SubjectIds.Count; i++)
                {
                    hashCode = hashCode * 397 ^ SubjectIds[i].GetHashCode();
                }

                hashCode = hashCode * 397 ^ AllowNoSubjects.GetHashCode();
                hashCode = hashCode * 397 ^ AllowNoParticipants.GetHashCode();
                hashCode = hashCode * 397 ^ StopOnFailure.GetHashCode();
                hashCode = hashCode * 397 ^ YieldBetweenSubjects.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string subjectsText = string.Join(",", SubjectIds.Select(subjectId => subjectId.StableText));
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"subjects='{subjectsText}' allowNoSubjects='{AllowNoSubjects}' allowNoParticipants='{AllowNoParticipants}' stopOnFailure='{StopOnFailure}' yieldBetweenSubjects='{YieldBetweenSubjects}' source='{sourceText}' reason='{reasonText}'";
        }

        public static ResetExecutionRequest ForSubjectIds(
            IEnumerable<ResetSubjectId> subjectIds,
            bool allowNoSubjects,
            bool allowNoParticipants,
            bool stopOnFailure,
            string source,
            string reason,
            bool yieldBetweenSubjects = false)
        {
            return new ResetExecutionRequest(
                subjectIds == null ? Array.Empty<ResetSubjectId>() : subjectIds.ToArray(),
                allowNoSubjects,
                allowNoParticipants,
                stopOnFailure,
                yieldBetweenSubjects,
                source,
                reason);
        }

        public static ResetExecutionRequest ForSingleSubject(
            ResetSubjectId subjectId,
            bool allowNoParticipants,
            string source,
            string reason,
            bool stopOnFailure = true,
            bool yieldBetweenSubjects = false)
        {
            return ForSubjectIds(
                subjectId.IsValid ? new[] { subjectId } : Array.Empty<ResetSubjectId>(),
                allowNoSubjects: false,
                allowNoParticipants: allowNoParticipants,
                stopOnFailure: stopOnFailure,
                source: source,
                reason: reason,
                yieldBetweenSubjects: yieldBetweenSubjects);
        }

        public static ResetExecutionRequest Empty(
            bool allowNoSubjects,
            bool allowNoParticipants,
            bool stopOnFailure,
            string source,
            string reason)
        {
            return ForSubjectIds(
                Array.Empty<ResetSubjectId>(),
                allowNoSubjects,
                allowNoParticipants,
                stopOnFailure,
                source,
                reason);
        }

        private static IReadOnlyList<ResetSubjectId> NormalizeSubjectIds(IReadOnlyList<ResetSubjectId> subjectIds)
        {
            if (subjectIds == null || subjectIds.Count == 0)
            {
                return Array.Empty<ResetSubjectId>();
            }

            var normalized = new List<ResetSubjectId>(subjectIds.Count);
            var seen = new HashSet<ResetSubjectId>();
            for (int i = 0; i < subjectIds.Count; i++)
            {
                ResetSubjectId subjectId = subjectIds[i];
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
