using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B composition membership identity.")]
    public readonly struct CameraCompositionMembershipContextId : IEquatable<CameraCompositionMembershipContextId>
    {
        public CameraCompositionMembershipContextId(string value) => Value = value.NormalizeText();
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(CameraCompositionMembershipContextId other) =>
            string.Equals(Value ?? string.Empty, other.Value ?? string.Empty, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CameraCompositionMembershipContextId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CameraCompositionMembershipContextId left, CameraCompositionMembershipContextId right) => left.Equals(right);
        public static bool operator !=(CameraCompositionMembershipContextId left, CameraCompositionMembershipContextId right) => !left.Equals(right);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B exact composition membership occurrence token.")]
    public readonly struct CameraCompositionMembershipToken : IEquatable<CameraCompositionMembershipToken>
    {
        internal CameraCompositionMembershipToken(
            CameraCompositionMembershipContextId contextId,
            CameraSubjectId subjectId,
            CameraSubjectAvailabilityToken availabilityToken,
            int revision)
        {
            ContextId = contextId;
            SubjectId = subjectId;
            AvailabilityToken = availabilityToken;
            Revision = revision;
        }

        public CameraCompositionMembershipContextId ContextId { get; }
        public CameraSubjectId SubjectId { get; }
        public CameraSubjectAvailabilityToken AvailabilityToken { get; }
        public int Revision { get; }
        public bool IsValid => ContextId.IsValid && SubjectId.IsValid && AvailabilityToken.IsValid &&
            AvailabilityToken.SubjectId == SubjectId && Revision > 0;
        public string StableText => IsValid
            ? $"camera-composition-membership:{ContextId}:{SubjectId.Value}:{AvailabilityToken.Revision}:{Revision}"
            : string.Empty;
        public bool Equals(CameraCompositionMembershipToken other) =>
            ContextId == other.ContextId && SubjectId == other.SubjectId &&
            AvailabilityToken == other.AvailabilityToken && Revision == other.Revision;
        public override bool Equals(object obj) => obj is CameraCompositionMembershipToken other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ContextId.GetHashCode();
                hash = hash * 397 ^ SubjectId.GetHashCode();
                hash = hash * 397 ^ AvailabilityToken.GetHashCode();
                hash = hash * 397 ^ Revision;
                return hash;
            }
        }
        public override string ToString() => StableText;
        public static bool operator ==(CameraCompositionMembershipToken left, CameraCompositionMembershipToken right) => left.Equals(right);
        public static bool operator !=(CameraCompositionMembershipToken left, CameraCompositionMembershipToken right) => !left.Equals(right);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B immutable resolved composition member.")]
    public readonly struct CameraCompositionMembershipEntry
    {
        internal CameraCompositionMembershipEntry(
            CameraSubjectAvailabilityEntry subject,
            CameraCompositionMembershipToken token)
        {
            Subject = subject;
            Token = token;
        }

        public CameraSubjectAvailabilityEntry Subject { get; }
        public CameraCompositionMembershipToken Token { get; }
        public CameraSubjectId SubjectId => Subject.Subject.SubjectId;
        public bool IsValid => Subject.IsValid && Token.IsValid &&
            Token.SubjectId == SubjectId && Token.AvailabilityToken == Subject.Token;
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B immutable ordered composition membership snapshot.")]
    public sealed class CameraCompositionMembershipSnapshot
    {
        private readonly CameraCompositionMembershipEntry[] _entries;
        private readonly IReadOnlyList<CameraCompositionMembershipEntry> _view;

        internal CameraCompositionMembershipSnapshot(
            CameraCompositionMembershipContextId contextId,
            SubjectAvailabilityContextId availabilityContextId,
            int availabilityRevision,
            int revision,
            CameraCompositionMembershipEntry[] entries)
        {
            ContextId = contextId;
            AvailabilityContextId = availabilityContextId;
            AvailabilityRevision = availabilityRevision;
            Revision = revision;
            _entries = entries != null
                ? (CameraCompositionMembershipEntry[])entries.Clone()
                : Array.Empty<CameraCompositionMembershipEntry>();
            _view = Array.AsReadOnly(_entries);
        }

        public CameraCompositionMembershipContextId ContextId { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
        public int AvailabilityRevision { get; }
        public int Revision { get; }
        public IReadOnlyList<CameraCompositionMembershipEntry> Entries => _view;
        public int Count => _entries.Length;

        public bool TryGet(CameraSubjectId subjectId, out CameraCompositionMembershipEntry entry)
        {
            for (int index = 0; index < _entries.Length; index++)
            {
                if (_entries[index].SubjectId == subjectId)
                {
                    entry = _entries[index];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B composition membership operation status.")]
    public enum CameraCompositionMembershipStatus
    {
        None = 0,
        SucceededChanged = 10,
        SucceededNoChange = 20,
        SucceededReleased = 30,
        SucceededCleared = 40,
        RejectedInvalidRequest = 100,
        RejectedForeignAvailabilityContext = 110,
        RejectedStaleAvailabilitySnapshot = 120,
        RejectedForeignOrStaleToken = 130,
        RejectedSubjectUnavailable = 140
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B composition membership result.")]
    public sealed class CameraCompositionMembershipResult
    {
        internal CameraCompositionMembershipResult(
            CameraCompositionMembershipStatus status,
            int addedCount,
            int removedCount,
            CameraCompositionMembershipSnapshot snapshot,
            string message)
        {
            Status = status;
            AddedCount = addedCount;
            RemovedCount = removedCount;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public CameraCompositionMembershipStatus Status { get; }
        public int AddedCount { get; }
        public int RemovedCount { get; }
        public CameraCompositionMembershipSnapshot Snapshot { get; }
        public string Message { get; }
        public bool Succeeded => Status is CameraCompositionMembershipStatus.SucceededChanged or
            CameraCompositionMembershipStatus.SucceededNoChange or
            CameraCompositionMembershipStatus.SucceededReleased or
            CameraCompositionMembershipStatus.SucceededCleared;
    }

    /// <summary>
    /// Scoped authority for one Composition's ordered Subject membership. It owns no View,
    /// request, rig or output identity and accepts only evidence from its bound availability context.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-029-B single-composition Subject membership authority.")]
    public sealed class CameraCompositionMembershipContext
    {
        private readonly Dictionary<CameraSubjectId, CameraCompositionMembershipEntry> _entries =
            new Dictionary<CameraSubjectId, CameraCompositionMembershipEntry>();
        private int _revision;
        private int _availabilityRevision = -1;

        public CameraCompositionMembershipContext(
            CameraCompositionMembershipContextId contextId,
            SubjectAvailabilityContextId availabilityContextId)
        {
            ContextId = contextId;
            AvailabilityContextId = availabilityContextId;
            if (!ContextId.IsValid || !AvailabilityContextId.IsValid)
            {
                throw new ArgumentException("Composition membership requires explicit membership and availability context ids.");
            }
        }

        public CameraCompositionMembershipContextId ContextId { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
        public int Revision => _revision;
        public int Count => _entries.Count;
        public CameraCompositionMembershipSnapshot Snapshot => CreateSnapshot();

        public CameraCompositionMembershipResult Reconcile(
            CameraSubjectAvailabilitySnapshot availability,
            IReadOnlyList<CameraSubjectId> desiredSubjects)
        {
            if (availability == null || desiredSubjects == null)
                return Result(CameraCompositionMembershipStatus.RejectedInvalidRequest, 0, 0,
                    "Composition membership requires availability evidence and an explicit desired Subject collection.");
            if (availability.ContextId != AvailabilityContextId)
                return Result(CameraCompositionMembershipStatus.RejectedForeignAvailabilityContext, 0, 0,
                    "Availability evidence belongs to another context.");
            if (availability.Revision < _availabilityRevision)
                return Result(CameraCompositionMembershipStatus.RejectedStaleAvailabilitySnapshot, 0, 0,
                    "Availability evidence regressed and cannot restore older membership.");

            var desired = new List<CameraSubjectId>(desiredSubjects.Count);
            var desiredSet = new HashSet<CameraSubjectId>();
            for (int index = 0; index < desiredSubjects.Count; index++)
            {
                CameraSubjectId id = desiredSubjects[index];
                if (!id.IsValid || !desiredSet.Add(id))
                    return Result(CameraCompositionMembershipStatus.RejectedInvalidRequest, 0, 0,
                        "Desired composition membership requires unique valid Subject ids.");
                if (!availability.TryGet(id, out _))
                    return Result(CameraCompositionMembershipStatus.RejectedSubjectUnavailable, 0, 0,
                        $"Desired Camera Subject '{id}' is not currently available.");
                desired.Add(id);
            }
            desired.Sort((left, right) => string.CompareOrdinal(left.Value, right.Value));

            int removed = 0;
            foreach (KeyValuePair<CameraSubjectId, CameraCompositionMembershipEntry> pair in _entries)
            {
                if (!desiredSet.Contains(pair.Key) ||
                    !availability.TryGet(pair.Key, out CameraSubjectAvailabilityEntry current) ||
                    current.Token != pair.Value.Subject.Token)
                {
                    removed++;
                }
            }

            int added = 0;
            for (int index = 0; index < desired.Count; index++)
            {
                availability.TryGet(desired[index], out CameraSubjectAvailabilityEntry current);
                if (!_entries.TryGetValue(desired[index], out CameraCompositionMembershipEntry existing) ||
                    existing.Subject.Token != current.Token)
                {
                    added++;
                }
            }

            bool changed = added > 0 || removed > 0;
            if (changed)
            {
                _revision++;
                var next = new Dictionary<CameraSubjectId, CameraCompositionMembershipEntry>();
                for (int index = 0; index < desired.Count; index++)
                {
                    CameraSubjectId id = desired[index];
                    availability.TryGet(id, out CameraSubjectAvailabilityEntry current);
                    if (_entries.TryGetValue(id, out CameraCompositionMembershipEntry existing) &&
                        existing.Subject.Token == current.Token)
                    {
                        next.Add(id, existing);
                    }
                    else
                    {
                        var token = new CameraCompositionMembershipToken(ContextId, id, current.Token, _revision);
                        next.Add(id, new CameraCompositionMembershipEntry(current, token));
                    }
                }
                _entries.Clear();
                foreach (KeyValuePair<CameraSubjectId, CameraCompositionMembershipEntry> pair in next)
                    _entries.Add(pair.Key, pair.Value);
            }

            _availabilityRevision = availability.Revision;
            return Result(
                changed ? CameraCompositionMembershipStatus.SucceededChanged : CameraCompositionMembershipStatus.SucceededNoChange,
                added,
                removed,
                changed ? "Composition membership reconciled." : "Composition membership is already current.");
        }

        public CameraCompositionMembershipResult TryRelease(
            CameraCompositionMembershipToken expectedToken,
            CameraSubjectAvailabilitySnapshot availability)
        {
            if (availability == null || availability.ContextId != AvailabilityContextId)
                return Result(CameraCompositionMembershipStatus.RejectedForeignAvailabilityContext, 0, 0,
                    "Membership release requires evidence from the bound availability context.");
            if (availability.Revision < _availabilityRevision)
                return Result(CameraCompositionMembershipStatus.RejectedStaleAvailabilitySnapshot, 0, 0,
                    "Membership release rejected stale availability evidence.");
            if (!expectedToken.IsValid || expectedToken.ContextId != ContextId ||
                !_entries.TryGetValue(expectedToken.SubjectId, out CameraCompositionMembershipEntry current) ||
                current.Token != expectedToken)
                return Result(CameraCompositionMembershipStatus.RejectedForeignOrStaleToken, 0, 0,
                    "Membership release rejected a foreign or stale occurrence token.");

            _entries.Remove(expectedToken.SubjectId);
            _revision++;
            _availabilityRevision = availability.Revision;
            return Result(CameraCompositionMembershipStatus.SucceededReleased, 0, 1,
                "Composition member released.");
        }

        public CameraCompositionMembershipResult Clear()
        {
            int removed = _entries.Count;
            if (removed > 0)
            {
                _entries.Clear();
                _revision++;
            }
            return Result(CameraCompositionMembershipStatus.SucceededCleared, 0, removed,
                removed > 0 ? "Composition membership cleared." : "Composition membership was already empty.");
        }

        private CameraCompositionMembershipSnapshot CreateSnapshot()
        {
            var entries = new List<CameraCompositionMembershipEntry>(_entries.Values);
            entries.Sort((left, right) => string.CompareOrdinal(left.SubjectId.Value, right.SubjectId.Value));
            return new CameraCompositionMembershipSnapshot(
                ContextId,
                AvailabilityContextId,
                Math.Max(_availabilityRevision, 0),
                _revision,
                entries.ToArray());
        }

        private CameraCompositionMembershipResult Result(
            CameraCompositionMembershipStatus status,
            int added,
            int removed,
            string message) =>
            new CameraCompositionMembershipResult(status, added, removed, CreateSnapshot(), message);
    }
}
