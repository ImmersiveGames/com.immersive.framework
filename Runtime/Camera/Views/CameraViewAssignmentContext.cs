using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scoped logical authority for explicitly composed Views and their Subject membership.
    /// Views live for the context scope; Subject disappearance prunes Assignments, not Views.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B scoped logical Camera View and Subject Assignment authority.")]
    public sealed class CameraViewAssignmentContext
    {
        private readonly struct AssignmentKey : IEquatable<AssignmentKey>
        {
            internal AssignmentKey(CameraViewId viewId, CameraSubjectId subjectId)
            {
                ViewId = viewId;
                SubjectId = subjectId;
            }

            internal CameraViewId ViewId { get; }
            internal CameraSubjectId SubjectId { get; }

            public bool Equals(AssignmentKey other)
            {
                return ViewId == other.ViewId && SubjectId == other.SubjectId;
            }

            public override bool Equals(object obj)
            {
                return obj is AssignmentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ViewId.GetHashCode() * 397 ^ SubjectId.GetHashCode();
                }
            }
        }

        private readonly Dictionary<CameraViewId, CameraView> _views =
            new Dictionary<CameraViewId, CameraView>();
        private readonly Dictionary<AssignmentKey, CameraSubjectAssignment> _assignments =
            new Dictionary<AssignmentKey, CameraSubjectAssignment>();
        private int _revision;
        private int _lastAvailabilityRevision;

        public CameraViewAssignmentContext(
            ViewAssignmentContextId contextId,
            SubjectAvailabilityContextId subjectAvailabilityContextId,
            params CameraView[] views)
        {
            ContextId = contextId;
            SubjectAvailabilityContextId = subjectAvailabilityContextId;
            if (!ContextId.IsValid ||
                !SubjectAvailabilityContextId.IsValid)
            {
                throw new ArgumentException(
                    "Camera View Assignment requires explicit context and Subject availability context ids.");
            }

            CameraView[] source = views ?? Array.Empty<CameraView>();
            for (int index = 0; index < source.Length; index++)
            {
                CameraView view = source[index];
                if (!view.IsValid)
                {
                    throw new ArgumentException(
                        "Camera View Assignment received an invalid View.",
                        nameof(views));
                }
                if (_views.ContainsKey(view.ViewId))
                {
                    throw new ArgumentException(
                        $"Camera View '{view.ViewId}' is duplicated in the logical composition.",
                        nameof(views));
                }
                _views.Add(view.ViewId, view);
            }
        }

        public ViewAssignmentContextId ContextId { get; }
        public SubjectAvailabilityContextId SubjectAvailabilityContextId { get; }
        public int Revision => _revision;
        public int ViewCount => _views.Count;
        public int AssignmentCount => _assignments.Count;

        public CameraViewAssignmentResult TryAssign(
            CameraViewId viewId,
            CameraSubjectId subjectId,
            CameraSubjectAssignmentOwnerId ownerId,
            CameraSubjectAvailabilitySnapshot availability)
        {
            if (!TryValidateAvailability(availability, out CameraViewAssignmentResult rejected))
            {
                return rejected;
            }

            if (!viewId.IsValid || !subjectId.IsValid || !ownerId.IsValid)
            {
                return Result(
                    CameraViewAssignmentStatus.RejectedInvalidRequest,
                    default,
                    default,
                    0,
                    availability,
                    "Camera Subject Assignment requires valid View, Subject and owner identities.");
            }
            if (!_views.ContainsKey(viewId))
            {
                return Result(
                    CameraViewAssignmentStatus.RejectedViewUnavailable,
                    default,
                    default,
                    0,
                    availability,
                    $"Logical Camera View '{viewId}' is not composed in this context.");
            }

            int prunedCount = PruneUnavailableAssignments(availability);
            if (!availability.TryGet(subjectId, out _))
            {
                return Result(
                    CameraViewAssignmentStatus.RejectedSubjectUnavailable,
                    default,
                    default,
                    prunedCount,
                    availability,
                    $"Camera Subject '{subjectId}' is not currently available.");
            }

            var key = new AssignmentKey(viewId, subjectId);
            if (_assignments.TryGetValue(key, out CameraSubjectAssignment current))
            {
                if (current.OwnerId == ownerId)
                {
                    return Result(
                        CameraViewAssignmentStatus.SucceededAlreadyAssigned,
                        current,
                        current.Token,
                        0,
                        availability,
                        "The exact logical Camera Subject Assignment is already current.");
                }

                return Result(
                    CameraViewAssignmentStatus.RejectedAssignmentConflict,
                    current,
                    current.Token,
                    0,
                    availability,
                    "The View-to-Subject relation is already owned by another scope.");
            }

            _revision++;
            var token = new CameraSubjectAssignmentToken(
                ContextId,
                viewId,
                subjectId,
                ownerId,
                _revision);
            var assignment = new CameraSubjectAssignment(
                viewId,
                subjectId,
                ownerId,
                token);
            _assignments.Add(key, assignment);
            return Result(
                CameraViewAssignmentStatus.SucceededAssigned,
                assignment,
                token,
                0,
                availability,
                "Camera Subject was assigned to the logical View.");
        }

        public CameraViewAssignmentResult TryRelease(
            CameraSubjectAssignmentToken expectedToken,
            CameraSubjectAvailabilitySnapshot availability)
        {
            if (!TryValidateAvailability(availability, out CameraViewAssignmentResult rejected))
            {
                return rejected;
            }

            if (!expectedToken.IsValid ||
                expectedToken.ContextId != ContextId)
            {
                return Result(
                    CameraViewAssignmentStatus.RejectedForeignOrStaleToken,
                    default,
                    expectedToken,
                    0,
                    availability,
                    "Camera Subject Assignment release rejected a foreign or invalid token.");
            }

            PruneUnavailableAssignments(availability);

            var key = new AssignmentKey(expectedToken.ViewId, expectedToken.SubjectId);
            if (!_assignments.TryGetValue(key, out CameraSubjectAssignment current))
            {
                return Result(
                    CameraViewAssignmentStatus.SucceededAlreadyReleased,
                    default,
                    expectedToken,
                    0,
                    availability,
                    "The exact Camera Subject Assignment is already released.");
            }
            if (current.Token != expectedToken)
            {
                return Result(
                    CameraViewAssignmentStatus.RejectedForeignOrStaleToken,
                    current,
                    expectedToken,
                    0,
                    availability,
                    "Camera Subject Assignment release rejected a stale owner occurrence token.");
            }

            _assignments.Remove(key);
            _revision++;
            return Result(
                CameraViewAssignmentStatus.SucceededReleased,
                current,
                expectedToken,
                1,
                availability,
                "Camera Subject Assignment was released.");
        }

        public CameraViewAssignmentResult ReleaseOwner(
            CameraSubjectAssignmentOwnerId ownerId,
            CameraSubjectAvailabilitySnapshot availability)
        {
            if (!TryValidateAvailability(availability, out CameraViewAssignmentResult rejected))
            {
                return rejected;
            }

            if (!ownerId.IsValid)
            {
                return Result(
                    CameraViewAssignmentStatus.RejectedInvalidRequest,
                    default,
                    default,
                    0,
                    availability,
                    "Camera Subject Assignment owner teardown requires a valid owner id.");
            }

            int removedCount = PruneUnavailableAssignments(availability);
            List<AssignmentKey> owned = FindKeys(
                assignment => assignment.OwnerId == ownerId);
            for (int index = 0; index < owned.Count; index++)
            {
                _assignments.Remove(owned[index]);
                _revision++;
            }

            removedCount += owned.Count;
            return Result(
                CameraViewAssignmentStatus.SucceededOwnerReleased,
                default,
                default,
                removedCount,
                availability,
                $"Released '{owned.Count}' Camera Subject Assignments owned by '{ownerId}'.");
        }

        public CameraViewAssignmentResult Reconcile(
            CameraSubjectAvailabilitySnapshot availability)
        {
            if (!TryValidateAvailability(availability, out CameraViewAssignmentResult rejected))
            {
                return rejected;
            }

            int removedCount = PruneUnavailableAssignments(availability);
            return Result(
                CameraViewAssignmentStatus.SucceededReconciled,
                default,
                default,
                removedCount,
                availability,
                removedCount == 0
                    ? "Logical Camera View Assignments are reconciled with Subject availability."
                    : $"Removed '{removedCount}' unavailable Camera Subject Assignments during reconciliation.");
        }

        private bool TryValidateAvailability(
            CameraSubjectAvailabilitySnapshot availability,
            out CameraViewAssignmentResult rejected)
        {
            if (availability == null ||
                availability.ContextId != SubjectAvailabilityContextId)
            {
                rejected = new CameraViewAssignmentResult(
                    CameraViewAssignmentStatus.RejectedAvailabilityContextMismatch,
                    default,
                    default,
                    0,
                    null,
                    "Camera View Assignment requires a snapshot from its explicitly bound Subject availability context.");
                return false;
            }

            if (availability.Revision < _lastAvailabilityRevision)
            {
                rejected = new CameraViewAssignmentResult(
                    CameraViewAssignmentStatus.RejectedStaleAvailabilitySnapshot,
                    default,
                    default,
                    0,
                    null,
                    $"Camera View Assignment rejected stale Subject availability revision " +
                    $"'{availability.Revision}'; current observed revision is " +
                    $"'{_lastAvailabilityRevision}'.");
                return false;
            }

            _lastAvailabilityRevision = availability.Revision;
            rejected = null;
            return true;
        }

        private int PruneUnavailableAssignments(
            CameraSubjectAvailabilitySnapshot availability)
        {
            List<AssignmentKey> unavailable = FindKeys(
                assignment => !availability.TryGet(assignment.SubjectId, out _));
            for (int index = 0; index < unavailable.Count; index++)
            {
                _assignments.Remove(unavailable[index]);
                _revision++;
            }
            return unavailable.Count;
        }

        private List<AssignmentKey> FindKeys(
            Predicate<CameraSubjectAssignment> predicate)
        {
            var keys = new List<AssignmentKey>();
            foreach (KeyValuePair<AssignmentKey, CameraSubjectAssignment> pair in _assignments)
            {
                if (predicate(pair.Value))
                {
                    keys.Add(pair.Key);
                }
            }
            keys.Sort(CompareKeys);
            return keys;
        }

        private CameraViewAssignmentResult Result(
            CameraViewAssignmentStatus status,
            CameraSubjectAssignment assignment,
            CameraSubjectAssignmentToken token,
            int removedCount,
            CameraSubjectAvailabilitySnapshot availability,
            string message)
        {
            return new CameraViewAssignmentResult(
                status,
                assignment,
                token,
                removedCount,
                CreateSnapshot(availability),
                message);
        }

        private CameraViewAssignmentSnapshot CreateSnapshot(
            CameraSubjectAvailabilitySnapshot availability)
        {
            var views = new List<CameraView>(_views.Values);
            views.Sort((left, right) => string.CompareOrdinal(
                left.ViewId.Value,
                right.ViewId.Value));

            var snapshots = new CameraViewSubjectSnapshot[views.Count];
            for (int viewIndex = 0; viewIndex < views.Count; viewIndex++)
            {
                CameraView view = views[viewIndex];
                var assignments = new List<CameraSubjectAssignment>();
                foreach (CameraSubjectAssignment assignment in _assignments.Values)
                {
                    if (assignment.ViewId == view.ViewId)
                    {
                        assignments.Add(assignment);
                    }
                }
                assignments.Sort((left, right) => string.CompareOrdinal(
                    left.SubjectId.Value,
                    right.SubjectId.Value));

                var resolved = new CameraSubjectAvailabilityEntry[assignments.Count];
                for (int assignmentIndex = 0;
                     assignmentIndex < assignments.Count;
                     assignmentIndex++)
                {
                    availability.TryGet(
                        assignments[assignmentIndex].SubjectId,
                        out resolved[assignmentIndex]);
                }

                snapshots[viewIndex] = new CameraViewSubjectSnapshot(
                    view,
                    assignments.ToArray(),
                    resolved);
            }

            return new CameraViewAssignmentSnapshot(
                ContextId,
                SubjectAvailabilityContextId,
                availability.Revision,
                _revision,
                snapshots);
        }

        private static int CompareKeys(AssignmentKey left, AssignmentKey right)
        {
            int viewComparison = string.CompareOrdinal(
                left.ViewId.Value,
                right.ViewId.Value);
            return viewComparison != 0
                ? viewComparison
                : string.CompareOrdinal(left.SubjectId.Value, right.SubjectId.Value);
        }
    }
}
