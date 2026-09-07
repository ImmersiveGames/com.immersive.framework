using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit scoped authority for available Subjects; owns no request, rig, presentation or output.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A scoped Camera Subject availability authority.")]
    public sealed class CameraSubjectAvailabilityContext :
        ICameraSubjectAvailabilitySource
    {
        private readonly Dictionary<CameraSubjectId, CameraSubjectAvailabilityEntry> _entries = new Dictionary<CameraSubjectId, CameraSubjectAvailabilityEntry>();
        private int _revision;

        public CameraSubjectAvailabilityContext(string contextId)
        {
            ContextId = contextId.NormalizeText();
            if (string.IsNullOrEmpty(ContextId))
            {
                throw new ArgumentException("Camera Subject availability requires an explicit context id.", nameof(contextId));
            }
        }

        public string ContextId { get; }
        public int Revision => _revision;
        public int AvailableCount => _entries.Count;

        /// <summary>
        /// Explicit Camera-domain change notification fired only after a Subject availability
        /// mutation actually changes membership. It carries the resulting immutable snapshot.
        /// This is not a global static event bus: each context instance raises only its own
        /// event, and the producer (e.g. Player Actor preparation) does not know who consumes it.
        /// </summary>
        public event Action<CameraSubjectAvailabilitySnapshot> AvailabilityChanged;

        public CameraSubjectAvailabilityResult TryMakeAvailable(CameraSubject subject, CameraSubjectAvailabilityOwnerId ownerId)
        {
            if (!subject.IsValid || !ownerId.IsValid)
            {
                return Result(CameraSubjectAvailabilityStatus.RejectedInvalidRequest, subject, default, "Camera Subject availability requires a valid Subject, observation and owner.");
            }

            if (_entries.TryGetValue(subject.SubjectId, out CameraSubjectAvailabilityEntry current))
            {
                if (current.OwnerId == ownerId && current.Subject.HasSameDescription(subject))
                {
                    return Result(CameraSubjectAvailabilityStatus.SucceededAlreadyAvailable, current.Subject, current.Token, "The exact Camera Subject availability is already current.");
                }
                return Result(CameraSubjectAvailabilityStatus.RejectedSubjectConflict, subject, current.Token, "Camera Subject identity is already available with different owner or observation evidence.");
            }

            _revision++;
            var token = new CameraSubjectAvailabilityToken(ContextId, subject.SubjectId, ownerId, _revision);
            _entries.Add(subject.SubjectId, new CameraSubjectAvailabilityEntry(subject, ownerId, token));
            CameraSubjectAvailabilityResult result = Result(CameraSubjectAvailabilityStatus.SucceededAvailable, subject, token, "Camera Subject became available.");
            RaiseAvailabilityChanged(result.Snapshot);
            return result;
        }

        public CameraSubjectAvailabilityResult TryMakeUnavailable(CameraSubjectAvailabilityToken expectedToken)
        {
            if (!expectedToken.IsValid || !string.Equals(expectedToken.ContextId, ContextId, StringComparison.Ordinal))
            {
                return Result(CameraSubjectAvailabilityStatus.RejectedForeignOrStaleToken, default, expectedToken, "Camera Subject removal rejected a foreign or invalid availability token.");
            }
            if (!_entries.TryGetValue(expectedToken.SubjectId, out CameraSubjectAvailabilityEntry current))
            {
                return Result(CameraSubjectAvailabilityStatus.SucceededAlreadyUnavailable, default, expectedToken, "The exact Camera Subject is already unavailable.");
            }
            if (current.Token != expectedToken)
            {
                return Result(CameraSubjectAvailabilityStatus.RejectedForeignOrStaleToken, current.Subject, expectedToken, "Camera Subject removal rejected a stale owner occurrence token.");
            }

            _entries.Remove(expectedToken.SubjectId);
            _revision++;
            CameraSubjectAvailabilityResult result = Result(CameraSubjectAvailabilityStatus.SucceededUnavailable, current.Subject, expectedToken, "Camera Subject became unavailable.");
            RaiseAvailabilityChanged(result.Snapshot);
            return result;
        }

        public int ReleaseOwner(CameraSubjectAvailabilityOwnerId ownerId)
        {
            if (!ownerId.IsValid) return 0;
            var subjectIds = new List<CameraSubjectId>();
            foreach (KeyValuePair<CameraSubjectId, CameraSubjectAvailabilityEntry> pair in _entries)
            {
                if (pair.Value.OwnerId == ownerId) subjectIds.Add(pair.Key);
            }
            for (int index = 0; index < subjectIds.Count; index++)
            {
                _entries.Remove(subjectIds[index]);
                _revision++;
            }
            if (subjectIds.Count > 0)
            {
                RaiseAvailabilityChanged(CreateSnapshot());
            }
            return subjectIds.Count;
        }

        private void RaiseAvailabilityChanged(CameraSubjectAvailabilitySnapshot snapshot)
        {
            AvailabilityChanged?.Invoke(snapshot);
        }

        public CameraSubjectAvailabilitySnapshot CreateSnapshot()
        {
            var entries = new List<CameraSubjectAvailabilityEntry>(_entries.Values);
            entries.Sort((left, right) => string.CompareOrdinal(left.Subject.SubjectId.Value, right.Subject.SubjectId.Value));
            return new CameraSubjectAvailabilitySnapshot(ContextId, _revision, entries.ToArray());
        }

        private CameraSubjectAvailabilityResult Result(CameraSubjectAvailabilityStatus status, CameraSubject subject, CameraSubjectAvailabilityToken token, string message) =>
            new CameraSubjectAvailabilityResult(status, subject, token, CreateSnapshot(), message);
    }
}
