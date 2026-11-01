using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>Point-in-time evidence for resolving an Object Entry to one current physical GameObject.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Read-only point-in-time Object Entry occurrence result.")]
    public readonly struct ObjectEntryOccurrenceResolution
    {
        internal ObjectEntryOccurrenceResolution(
            ObjectEntryOccurrenceResolutionStatus status,
            GameObject occurrence,
            string diagnostic)
        {
            Status = status;
            Occurrence = occurrence;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ObjectEntryOccurrenceResolutionStatus Status { get; }
        public GameObject Occurrence { get; }
        public string Diagnostic { get; }
        public bool Succeeded => Status == ObjectEntryOccurrenceResolutionStatus.Resolved && Occurrence != null;
    }
}
