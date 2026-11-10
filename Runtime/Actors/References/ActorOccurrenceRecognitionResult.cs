using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>Point-in-time evidence for recognizing one current physical Actor occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Read-only physical Actor occurrence recognition result.")]
    public readonly struct ActorOccurrenceRecognitionResult
    {
        internal ActorOccurrenceRecognitionResult(ActorOccurrenceRecognitionStatus status, string diagnostic)
        {
            if (status == ActorOccurrenceRecognitionStatus.Resolved)
                throw new ArgumentOutOfRangeException(nameof(status), "Resolved recognition requires an Actor occurrence.");

            Status = status;
            Occurrence = default;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal ActorOccurrenceRecognitionResult(ActorOccurrence occurrence, string diagnostic)
        {
            if (!occurrence.IsValid)
                throw new ArgumentException("Resolved recognition requires a valid Actor occurrence.", nameof(occurrence));

            Status = ActorOccurrenceRecognitionStatus.Resolved;
            Occurrence = occurrence;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ActorOccurrenceRecognitionStatus Status { get; }
        public ActorOccurrence Occurrence { get; }
        public string Diagnostic { get; }
        public bool Succeeded => Status == ActorOccurrenceRecognitionStatus.Resolved && Occurrence.IsValid;
    }

    /// <summary>Explicit outcomes for read-only physical Actor occurrence recognition.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Outcomes for physical Actor occurrence recognition.")]
    public enum ActorOccurrenceRecognitionStatus
    {
        Invalid = 0,
        Unrecognized = 10,
        Resolved = 20,
        Unavailable = 30,
        Ambiguous = 40
    }
}
