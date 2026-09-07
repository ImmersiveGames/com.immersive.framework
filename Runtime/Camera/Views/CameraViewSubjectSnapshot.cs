using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable resolved logical membership for one View after availability reconciliation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B deterministic per-View Subject Assignment snapshot.")]
    public sealed class CameraViewSubjectSnapshot
    {
        private readonly CameraSubjectAssignment[] _assignments;
        private readonly CameraSubjectAvailabilityEntry[] _resolvedSubjects;
        private readonly IReadOnlyList<CameraSubjectAssignment> _assignmentView;
        private readonly IReadOnlyList<CameraSubjectAvailabilityEntry> _resolvedSubjectView;

        internal CameraViewSubjectSnapshot(
            CameraView view,
            CameraSubjectAssignment[] assignments,
            CameraSubjectAvailabilityEntry[] resolvedSubjects)
        {
            View = view;
            _assignments = assignments != null
                ? (CameraSubjectAssignment[])assignments.Clone()
                : Array.Empty<CameraSubjectAssignment>();
            _resolvedSubjects = resolvedSubjects != null
                ? (CameraSubjectAvailabilityEntry[])resolvedSubjects.Clone()
                : Array.Empty<CameraSubjectAvailabilityEntry>();
            _assignmentView = Array.AsReadOnly(_assignments);
            _resolvedSubjectView = Array.AsReadOnly(_resolvedSubjects);
        }

        public CameraView View { get; }
        public IReadOnlyList<CameraSubjectAssignment> Assignments => _assignmentView;
        public IReadOnlyList<CameraSubjectAvailabilityEntry> ResolvedSubjects =>
            _resolvedSubjectView;
        public int AssignmentCount => _assignments.Length;
        public int ResolvedSubjectCount => _resolvedSubjects.Length;
    }
}
