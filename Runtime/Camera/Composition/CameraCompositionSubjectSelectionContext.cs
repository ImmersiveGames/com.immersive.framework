using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Outcome of one explicit Subject-selection write.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-A explicit Camera Composition Subject selection write status.")]
    public enum CameraCompositionSubjectSelectionStatus
    {
        None = 0,
        SucceededChanged = 10,
        SucceededUnchanged = 20,
        RejectedInvalidRequest = 100
    }

    /// <summary>Result of one explicit Subject-selection write. The snapshot is immutable.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-A explicit Camera Composition Subject selection write result.")]
    public sealed class CameraCompositionSubjectSelectionResult
    {
        internal CameraCompositionSubjectSelectionResult(
            CameraCompositionSubjectSelectionStatus status,
            CameraCompositionSubjectSelectionSnapshot snapshot,
            string message)
        {
            Status = status;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public CameraCompositionSubjectSelectionStatus Status { get; }

        public CameraCompositionSubjectSelectionSnapshot Snapshot { get; }

        public string Message { get; }

        public bool Succeeded => Status is CameraCompositionSubjectSelectionStatus.SucceededChanged
            or CameraCompositionSubjectSelectionStatus.SucceededUnchanged;
    }

    /// <summary>
    /// Single writer for one Composition's explicit Subject selection. Reads are exposed
    /// through <see cref="ICameraCompositionSubjectSelectionSource"/>. Writes are idempotent:
    /// an equal ordered set, including clear of an already empty set, does not advance the
    /// revision and does not raise <see cref="SelectionChanged"/>.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-031-A explicit Camera Composition Subject selection authority.")]
    public sealed class CameraCompositionSubjectSelectionContext :
        ICameraCompositionSubjectSelectionSource
    {
        private static readonly CameraSubjectIdComparer IdComparer = new CameraSubjectIdComparer();

        private CameraSubjectId[] _subjectIds = Array.Empty<CameraSubjectId>();
        private int _revision;
        private CameraCompositionSubjectSelectionSnapshot _current;

        public CameraCompositionSubjectSelectionContext(
            CameraCompositionSubjectSelectionContextId contextId)
        {
            if (!contextId.IsValid)
            {
                throw new ArgumentException(
                    "Camera Composition Subject selection requires an explicit context id.",
                    nameof(contextId));
            }

            ContextId = contextId;
            _current = CreateSnapshot();
        }

        public CameraCompositionSubjectSelectionContextId ContextId { get; }

        public int Revision => _revision;

        public CameraCompositionSubjectSelectionSnapshot CurrentSnapshot => _current;

        public event Action<CameraCompositionSubjectSelectionSnapshot> SelectionChanged;

        public CameraCompositionSubjectSelectionResult Replace(
            IReadOnlyList<CameraSubjectId> subjectIds)
        {
            if (subjectIds == null)
            {
                throw new ArgumentNullException(nameof(subjectIds));
            }

            if (!TryNormalize(subjectIds, out CameraSubjectId[] ordered))
            {
                return Result(
                    CameraCompositionSubjectSelectionStatus.RejectedInvalidRequest,
                    "Explicit Camera Subject selection requires unique valid Subject ids.");
            }

            if (SameAsCurrent(ordered))
            {
                return Result(
                    CameraCompositionSubjectSelectionStatus.SucceededUnchanged,
                    ordered.Length == 0
                        ? "Explicit Camera Subject selection is already empty."
                        : "Explicit Camera Subject selection is already current.");
            }

            _revision++;
            _subjectIds = ordered;
            _current = CreateSnapshot();
            SelectionChanged?.Invoke(_current);
            return Result(
                CameraCompositionSubjectSelectionStatus.SucceededChanged,
                ordered.Length == 0
                    ? "Explicit Camera Subject selection cleared."
                    : "Explicit Camera Subject selection changed.");
        }

        public CameraCompositionSubjectSelectionResult Clear() =>
            Replace(Array.Empty<CameraSubjectId>());

        private bool SameAsCurrent(CameraSubjectId[] ordered)
        {
            if (ordered.Length != _subjectIds.Length)
            {
                return false;
            }

            for (int index = 0; index < ordered.Length; index++)
            {
                if (ordered[index] != _subjectIds[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryNormalize(
            IReadOnlyList<CameraSubjectId> subjectIds,
            out CameraSubjectId[] ordered)
        {
            ordered = Array.Empty<CameraSubjectId>();
            if (subjectIds.Count == 0)
            {
                return true;
            }

            var unique = new HashSet<CameraSubjectId>();
            var buffer = new List<CameraSubjectId>(subjectIds.Count);
            for (int index = 0; index < subjectIds.Count; index++)
            {
                CameraSubjectId subjectId = subjectIds[index];
                if (!subjectId.IsValid || !unique.Add(subjectId))
                {
                    return false;
                }

                buffer.Add(subjectId);
            }

            buffer.Sort(IdComparer);
            ordered = buffer.ToArray();
            return true;
        }

        private CameraCompositionSubjectSelectionSnapshot CreateSnapshot() =>
            new CameraCompositionSubjectSelectionSnapshot(ContextId, _revision, _subjectIds);

        private CameraCompositionSubjectSelectionResult Result(
            CameraCompositionSubjectSelectionStatus status,
            string message) =>
            new CameraCompositionSubjectSelectionResult(status, _current, message);

        private sealed class CameraSubjectIdComparer : IComparer<CameraSubjectId>
        {
            public int Compare(CameraSubjectId left, CameraSubjectId right) =>
                string.CompareOrdinal(left.Value, right.Value);
        }
    }
}
