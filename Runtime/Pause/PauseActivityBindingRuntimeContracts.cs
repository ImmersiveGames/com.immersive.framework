using System;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Pause
{
    internal enum PauseActivityBindingRuntimeState
    {
        Inactive = 0,
        Activating = 10,
        Active = 20,
        Releasing = 30,
        Failed = 40
    }

    internal enum PauseActivityBindingOperationStatus
    {
        Unknown = 0,
        Absent = 10,
        Activated = 20,
        AlreadyActive = 30,
        Released = 40,
        AlreadyReleased = 50,
        Rejected = 60,
        Failed = 70
    }

    /// <summary>
    /// Immutable Pause-specific projection of one concrete Activity entry.
    /// The lifecycle supplies the canonical Activity owner and its entry sequence;
    /// this type does not generate either value or own Activity admission.
    /// </summary>
    internal readonly struct PauseActivityBindingScope : IEquatable<PauseActivityBindingScope>
    {
        internal PauseActivityBindingScope(
            RuntimeContentOwner owner,
            int activityEntrySequence)
        {
            if (!owner.IsValid || owner.Scope != RuntimeContentScope.Activity)
            {
                throw new ArgumentException(
                    "Pause Activity binding scope requires a valid Activity runtime owner.",
                    nameof(owner));
            }

            if (activityEntrySequence <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activityEntrySequence),
                    activityEntrySequence,
                    "Pause Activity binding scope entry sequence must be positive.");
            }

            Owner = owner;
            ActivityEntrySequence = activityEntrySequence;
        }

        internal RuntimeContentOwner Owner { get; }
        internal int ActivityEntrySequence { get; }
        internal bool IsValid =>
            Owner.IsValid &&
            Owner.Scope == RuntimeContentScope.Activity &&
            ActivityEntrySequence > 0;
        internal string StableText => IsValid
            ? $"pause-activity-binding:{Owner.StableText}:{ActivityEntrySequence}"
            : string.Empty;

        public bool Equals(PauseActivityBindingScope other)
        {
            return Owner == other.Owner &&
                ActivityEntrySequence == other.ActivityEntrySequence;
        }

        public override bool Equals(object obj)
        {
            return obj is PauseActivityBindingScope other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Owner.GetHashCode() * 397 ^ ActivityEntrySequence;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PauseActivityBindingScope left,
            PauseActivityBindingScope right) => left.Equals(right);

        public static bool operator !=(
            PauseActivityBindingScope left,
            PauseActivityBindingScope right) => !left.Equals(right);
    }

    internal readonly struct PauseActivityBindingRuntimeSnapshot
    {
        internal PauseActivityBindingRuntimeSnapshot(
            PauseActivityBindingRuntimeState state,
            PauseActivityBindingScope activeScope,
            PauseActivityBindingScope lastReleasedScope,
            bool hasActiveBinding,
            string diagnostic)
        {
            State = state;
            ActiveScope = activeScope;
            LastReleasedScope = lastReleasedScope;
            HasActiveBinding = hasActiveBinding;
            Diagnostic = diagnostic.NormalizeText();
        }

        internal PauseActivityBindingRuntimeState State { get; }
        internal PauseActivityBindingScope ActiveScope { get; }
        internal PauseActivityBindingScope LastReleasedScope { get; }
        internal bool HasActiveBinding { get; }
        internal string Diagnostic { get; }
    }

    internal readonly struct PauseActivityBindingOperationResult
    {
        internal PauseActivityBindingOperationResult(
            PauseActivityBindingOperationStatus status,
            string operation,
            PauseActivityBindingRuntimeSnapshot snapshot,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackDiagnostic,
            string diagnostic)
        {
            Status = status;
            Operation = operation.NormalizeText();
            Snapshot = snapshot;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            RollbackDiagnostic = rollbackDiagnostic.NormalizeText();
            Diagnostic = diagnostic.NormalizeText();
        }

        internal PauseActivityBindingOperationStatus Status { get; }
        internal string Operation { get; }
        internal PauseActivityBindingRuntimeSnapshot Snapshot { get; }
        internal bool RollbackAttempted { get; }
        internal bool RollbackSucceeded { get; }
        internal string RollbackDiagnostic { get; }
        internal string Diagnostic { get; }

        internal bool Succeeded => Status is
            PauseActivityBindingOperationStatus.Absent or
            PauseActivityBindingOperationStatus.Activated or
            PauseActivityBindingOperationStatus.AlreadyActive or
            PauseActivityBindingOperationStatus.Released or
            PauseActivityBindingOperationStatus.AlreadyReleased;
    }
}
