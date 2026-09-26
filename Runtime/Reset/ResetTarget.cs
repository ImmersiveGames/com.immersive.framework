using System;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// Internal semantic Reset target introduced by IF-ADR-035.
    /// It deliberately stays outside the public authoring surface until Resettable and ResetComposition exist.
    /// </summary>
    internal readonly struct ResetTarget : IEquatable<ResetTarget>
    {
        private ResetTarget(ResetTargetKind kind, ResetSubjectId stableReference)
        {
            Kind = kind;
            StableReference = stableReference;
        }

        public ResetTargetKind Kind { get; }

        public ResetSubjectId StableReference { get; }

        public bool IsValid =>
            Kind == ResetTargetKind.CurrentActivity
            || Kind == ResetTargetKind.CurrentRoute
            || (Kind == ResetTargetKind.StableReference && StableReference.IsValid);

        public static ResetTarget CurrentActivity() =>
            new ResetTarget(ResetTargetKind.CurrentActivity, default);

        public static ResetTarget CurrentRoute() =>
            new ResetTarget(ResetTargetKind.CurrentRoute, default);

        public static ResetTarget ForLegacyStableReference(ResetSubjectId subjectId) =>
            new ResetTarget(ResetTargetKind.StableReference, subjectId);

        public bool Equals(ResetTarget other) =>
            Kind == other.Kind && StableReference.Equals(other.StableReference);

        public override bool Equals(object obj) =>
            obj is ResetTarget other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ StableReference.GetHashCode();
            }
        }

        public override string ToString() =>
            Kind == ResetTargetKind.StableReference
                ? $"kind='{Kind}' stableReference='{StableReference.StableText}'"
                : $"kind='{Kind}'";
    }
}
