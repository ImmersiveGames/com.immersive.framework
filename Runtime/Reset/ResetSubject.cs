using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for one resettable subject.
    /// It does not reference ObjectEntryDeclaration, ObjectEntryId or ObjectEntry runtime snapshots.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A ResetSubject descriptor independent from ObjectEntry.")]
    public readonly struct ResetSubject : IEquatable<ResetSubject>
    {
        public ResetSubject(
            ResetSubjectId subjectId,
            ResetSubjectScope scope,
            ResetSubjectOrigin origin,
            RuntimeContentOwner owner,
            string displayName,
            string diagnosticTag)
        {
            if (!subjectId.IsValid)
            {
                throw new ArgumentException("Reset subject requires a valid subject id.", nameof(subjectId));
            }

            if (!Enum.IsDefined(typeof(ResetSubjectScope), scope) || scope == ResetSubjectScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Reset subject scope must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ResetSubjectOrigin), origin) || origin == ResetSubjectOrigin.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "Reset subject origin must be explicit.");
            }

            if ((scope == ResetSubjectScope.Route || scope == ResetSubjectScope.Activity) && !owner.IsValid)
            {
                throw new ArgumentException($"Reset subject scope '{scope}' requires a valid runtime owner.", nameof(owner));
            }

            SubjectId = subjectId;
            Scope = scope;
            Origin = origin;
            Owner = owner;
            DisplayName = displayName.NormalizeText();
            DiagnosticTag = diagnosticTag.NormalizeText();
        }

        public ResetSubject(
            ResetSubjectId subjectId,
            ResetSubjectScope scope,
            ResetSubjectOrigin origin,
            string displayName,
            string diagnosticTag)
            : this(subjectId, scope, origin, default, displayName, diagnosticTag)
        {
        }

        public ResetSubjectId SubjectId { get; }

        public ResetSubjectScope Scope { get; }

        public ResetSubjectOrigin Origin { get; }

        public RuntimeContentOwner Owner { get; }

        public string DisplayName { get; }

        public string DiagnosticTag { get; }

        public bool HasOwner => Owner.IsValid;

        public bool IsValid => SubjectId.IsValid && Scope != ResetSubjectScope.Unknown && Origin != ResetSubjectOrigin.Unknown;

        public string OwnerStableText => HasOwner ? Owner.StableText : string.Empty;

        public bool Equals(ResetSubject other)
        {
            return SubjectId.Equals(other.SubjectId)
                && Scope == other.Scope
                && Origin == other.Origin
                && Owner.Equals(other.Owner)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(DiagnosticTag, other.DiagnosticTag, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetSubject other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SubjectId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)Origin;
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DiagnosticTag ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string ownerText = HasOwner ? Owner.StableText : "<none>";
            string displayNameText = DisplayName.ToDiagnosticText("<unnamed>");
            string diagnosticTagText = DiagnosticTag.ToDiagnosticText("<none>");
            return $"subjectId='{SubjectId.StableText}' scope='{Scope}' origin='{Origin}' owner='{ownerText}' displayName='{displayNameText}' diagnosticTag='{diagnosticTagText}'";
        }

        public static ResetSubject SceneRoute(
            string subjectId,
            RuntimeContentOwner owner,
            string displayName,
            string diagnosticTag)
        {
            return new ResetSubject(ResetSubjectId.From(subjectId), ResetSubjectScope.Route, ResetSubjectOrigin.SceneAuthored, owner, displayName, diagnosticTag);
        }

        public static ResetSubject SceneActivity(
            string subjectId,
            RuntimeContentOwner owner,
            string displayName,
            string diagnosticTag)
        {
            return new ResetSubject(ResetSubjectId.From(subjectId), ResetSubjectScope.Activity, ResetSubjectOrigin.SceneAuthored, owner, displayName, diagnosticTag);
        }

        public static ResetSubject Runtime(
            ResetSubjectId subjectId,
            RuntimeContentOwner owner,
            string displayName,
            string diagnosticTag)
        {
            return new ResetSubject(subjectId, ResetSubjectScope.Runtime, ResetSubjectOrigin.RuntimeRegistered, owner, displayName, diagnosticTag);
        }
    }
}
