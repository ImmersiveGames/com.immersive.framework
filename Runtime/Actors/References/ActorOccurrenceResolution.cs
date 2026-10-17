using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 explicit Actor reference outcomes.")]
    public enum ActorOccurrenceResolutionStatus
    {
        InvalidReference = 0,
        Resolved = 10,
        Unavailable = 20,
        Stale = 30,
        Ambiguous = 40
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 immutable Actor reference resolution result.")]
    public readonly struct ActorOccurrenceResolution
    {
        internal ActorOccurrenceResolution(
            ActorOccurrenceResolutionStatus status,
            ActorOccurrence occurrence,
            string diagnostic)
        {
            Status = status;
            Occurrence = occurrence;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ActorOccurrenceResolutionStatus Status { get; }
        public ActorOccurrence Occurrence { get; }
        public string Diagnostic { get; }
        public bool Succeeded => Status == ActorOccurrenceResolutionStatus.Resolved && Occurrence.IsValid;

        internal bool SameState(ActorOccurrenceResolution other) =>
            Status == other.Status && Occurrence.Id == other.Occurrence.Id &&
            ReferenceEquals(Occurrence.ActorRoot, other.Occurrence.ActorRoot);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 read-only occurrence change notification.")]
    public readonly struct ActorOccurrenceReferenceChange
    {
        internal ActorOccurrenceReferenceChange(
            ActorOccurrenceResolution previous,
            ActorOccurrenceResolution current,
            ActorOccurrenceReferenceChangeKind kind)
        {
            Previous = previous;
            Current = current;
            Kind = kind;
        }

        public ActorOccurrenceResolution Previous { get; }
        public ActorOccurrenceResolution Current { get; }
        public ActorOccurrenceReferenceChangeKind Kind { get; }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 occurrence change vocabulary.")]
    public enum ActorOccurrenceReferenceChangeKind
    {
        Available = 10,
        Replaced = 20,
        Unavailable = 30,
        Invalidated = 40,
        BindingReleased = 50
    }
}
