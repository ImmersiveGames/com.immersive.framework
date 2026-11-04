using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>Point-in-time evidence for recognizing one admitted Object Entry occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Read-only physical Object Entry recognition result.")]
    public readonly struct ObjectEntryRecognitionResult
    {
        internal ObjectEntryRecognitionResult(ObjectEntryRecognitionStatus status, string diagnostic)
        {
            if (status == ObjectEntryRecognitionStatus.Resolved)
                throw new ArgumentOutOfRangeException(nameof(status), "Resolved recognition requires an identity and occurrence.");

            Status = status;
            ObjectEntryId = default;
            Occurrence = null;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal ObjectEntryRecognitionResult(ObjectEntryId objectEntryId, GameObject occurrence, string diagnostic)
        {
            if (!objectEntryId.IsValid) throw new ArgumentException("A resolved recognition requires a valid ObjectEntryId.", nameof(objectEntryId));
            if (occurrence == null) throw new ArgumentNullException(nameof(occurrence));

            Status = ObjectEntryRecognitionStatus.Resolved;
            ObjectEntryId = objectEntryId;
            Occurrence = occurrence;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public ObjectEntryRecognitionStatus Status { get; }
        public ObjectEntryId ObjectEntryId { get; }
        public GameObject Occurrence { get; }
        public string Diagnostic { get; }
        public bool Succeeded => Status == ObjectEntryRecognitionStatus.Resolved && ObjectEntryId.IsValid && Occurrence != null;
    }
}
