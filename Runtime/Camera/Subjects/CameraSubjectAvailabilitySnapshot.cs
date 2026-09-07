using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Immutable ordered view of currently available Camera Subjects.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A immutable Camera Subject availability snapshot.")]
    public sealed class CameraSubjectAvailabilitySnapshot
    {
        private readonly CameraSubjectAvailabilityEntry[] _entries;
        private readonly IReadOnlyList<CameraSubjectAvailabilityEntry> _view;

        internal CameraSubjectAvailabilitySnapshot(string contextId, int revision, CameraSubjectAvailabilityEntry[] entries)
        {
            ContextId = contextId ?? string.Empty;
            Revision = revision;
            _entries = entries != null ? (CameraSubjectAvailabilityEntry[])entries.Clone() : Array.Empty<CameraSubjectAvailabilityEntry>();
            _view = Array.AsReadOnly(_entries);
        }

        public string ContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<CameraSubjectAvailabilityEntry> Entries => _view;
        public int Count => _entries.Length;

        public bool TryGet(CameraSubjectId subjectId, out CameraSubjectAvailabilityEntry entry)
        {
            for (int index = 0; index < _entries.Length; index++)
            {
                if (_entries[index].Subject.SubjectId == subjectId)
                {
                    entry = _entries[index];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}
