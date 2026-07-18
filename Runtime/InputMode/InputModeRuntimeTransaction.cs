using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Exact transaction evidence produced by one scoped InputMode runtime context.
    /// It carries no Unity side effects and is accepted only by its owning context.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 exact InputMode runtime transaction evidence.")]
    public readonly struct InputModeRuntimeTransaction :
        IEquatable<InputModeRuntimeTransaction>
    {
        internal InputModeRuntimeTransaction(
            string contextId,
            long sequence,
            InputModeRequest request,
            InputModeState previousState,
            InputModeState nextState)
        {
            ContextId = contextId.NormalizeText();
            Sequence = sequence;
            Request = request;
            PreviousState = previousState;
            NextState = nextState;
        }

        public string ContextId { get; }
        public long Sequence { get; }
        public InputModeRequest Request { get; }
        public InputModeState PreviousState { get; }
        public InputModeState NextState { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(ContextId) &&
            Sequence > 0 &&
            Request.IsValid &&
            PreviousState.IsValid &&
            NextState.IsValid &&
            NextState.Revision == PreviousState.Revision + 1 &&
            NextState.CurrentKind == Request.TargetMode;

        public bool Equals(InputModeRuntimeTransaction other)
        {
            return string.Equals(
                       ContextId,
                       other.ContextId,
                       StringComparison.Ordinal) &&
                   Sequence == other.Sequence &&
                   Request.Equals(other.Request) &&
                   PreviousState.Equals(other.PreviousState) &&
                   NextState.Equals(other.NextState);
        }

        public override bool Equals(object obj) =>
            obj is InputModeRuntimeTransaction other && Equals(other);

        public string ToDiagnosticString()
        {
            return
                $"context='{ContextId}' sequence='{Sequence}' " +
                $"requester='{Request.Requester}' target='{Request.TargetMode}' " +
                $"previous='{PreviousState.CurrentKind}' " +
                $"previousRevision='{PreviousState.Revision}' " +
                $"next='{NextState.CurrentKind}' " +
                $"nextRevision='{NextState.Revision}'.";
        }

        public override string ToString() => ToDiagnosticString();

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(
                    ContextId ?? string.Empty);
                hash = (hash * 397) ^ Sequence.GetHashCode();
                hash = (hash * 397) ^ Request.GetHashCode();
                hash = (hash * 397) ^ PreviousState.GetHashCode();
                hash = (hash * 397) ^ NextState.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(
            InputModeRuntimeTransaction left,
            InputModeRuntimeTransaction right) => left.Equals(right);

        public static bool operator !=(
            InputModeRuntimeTransaction left,
            InputModeRuntimeTransaction right) => !left.Equals(right);
    }
}
