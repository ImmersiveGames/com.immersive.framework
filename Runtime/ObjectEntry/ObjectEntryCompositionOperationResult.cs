using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C2 operation outcome.")]
    public enum ObjectEntryCompositionOperationStatus
    {
        Committed = 0,
        NoChange = 1,
        Rejected = 2,
        Stale = 3,
        Cancelled = 4,
        Faulted = 5,
        Degraded = 6
    }

    /// <summary>Inspectable outcome of one consumer-owned Object Entry composition operation.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 003 C2 operation result.")]
    public sealed class ObjectEntryCompositionOperationResult
    {
        private readonly ReadOnlyCollection<string> _changedKeys;

        internal ObjectEntryCompositionOperationResult(
            ObjectEntryCompositionOperationStatus status,
            long revision,
            IEnumerable<string> changedKeys,
            string diagnostic,
            bool effectsMayHaveApplied)
        {
            Status = status;
            Revision = revision;
            _changedKeys = Array.AsReadOnly((changedKeys ?? Enumerable.Empty<string>()).ToArray());
            Diagnostic = diagnostic ?? string.Empty;
            EffectsMayHaveApplied = effectsMayHaveApplied;
        }

        public ObjectEntryCompositionOperationStatus Status { get; }
        public long Revision { get; }
        public IReadOnlyList<string> ChangedKeys => _changedKeys;
        public string Diagnostic { get; }
        public bool EffectsMayHaveApplied { get; }

        public bool IsSuccessful => Status is ObjectEntryCompositionOperationStatus.Committed or
            ObjectEntryCompositionOperationStatus.NoChange;
    }
}
