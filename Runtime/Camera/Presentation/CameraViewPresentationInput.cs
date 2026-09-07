using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable View-scoped input presented to Camera presentation. It preserves the
    /// complete ordered resolved Subject collection and the source snapshot revisions.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C logical View-to-presentation input contract.")]
    public sealed class CameraViewPresentationInput
    {
        private readonly CameraSubjectAvailabilityEntry[] _subjects;
        private readonly IReadOnlyList<CameraSubjectAvailabilityEntry> _subjectView;

        internal CameraViewPresentationInput(
            CameraView view,
            string assignmentContextId,
            int assignmentRevision,
            string availabilityContextId,
            int availabilityRevision,
            CameraSubjectAvailabilityEntry[] subjects)
        {
            View = view;
            AssignmentContextId = assignmentContextId ?? string.Empty;
            AssignmentRevision = assignmentRevision;
            AvailabilityContextId = availabilityContextId ?? string.Empty;
            AvailabilityRevision = availabilityRevision;
            _subjects = subjects != null
                ? (CameraSubjectAvailabilityEntry[])subjects.Clone()
                : Array.Empty<CameraSubjectAvailabilityEntry>();
            _subjectView = Array.AsReadOnly(_subjects);
        }

        public CameraView View { get; }
        public CameraViewId ViewId => View.ViewId;
        public string AssignmentContextId { get; }
        public int AssignmentRevision { get; }
        public string AvailabilityContextId { get; }
        public int AvailabilityRevision { get; }
        public IReadOnlyList<CameraSubjectAvailabilityEntry> Subjects => _subjectView;
        public int SubjectCount => _subjects.Length;
        public CameraViewSubjectCardinality Cardinality =>
            SubjectCount == 0
                ? CameraViewSubjectCardinality.Zero
                : SubjectCount == 1
                    ? CameraViewSubjectCardinality.One
                    : CameraViewSubjectCardinality.Many;

        public bool IsValid =>
            View.IsValid &&
            !string.IsNullOrEmpty(AssignmentContextId) &&
            AssignmentRevision >= 0 &&
            !string.IsNullOrEmpty(AvailabilityContextId) &&
            AvailabilityRevision >= 0 &&
            AllSubjectsAreValid();

        public bool IsCurrentFor(CameraViewAssignmentSnapshot snapshot)
        {
            if (snapshot == null ||
                !string.Equals(
                    AssignmentContextId,
                    snapshot.ContextId,
                    StringComparison.Ordinal) ||
                AssignmentRevision != snapshot.Revision ||
                !string.Equals(
                    AvailabilityContextId,
                    snapshot.AvailabilityContextId,
                    StringComparison.Ordinal) ||
                AvailabilityRevision != snapshot.AvailabilityRevision ||
                !snapshot.TryGetView(ViewId, out CameraViewSubjectSnapshot current) ||
                current.ResolvedSubjectCount != SubjectCount)
            {
                return false;
            }

            for (int index = 0; index < _subjects.Length; index++)
            {
                if (_subjects[index].Token != current.ResolvedSubjects[index].Token)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllSubjectsAreValid()
        {
            for (int index = 0; index < _subjects.Length; index++)
            {
                if (!_subjects[index].IsValid ||
                    !string.Equals(
                        _subjects[index].Token.ContextId,
                        AvailabilityContextId,
                        StringComparison.Ordinal) ||
                    (index > 0 &&
                     string.CompareOrdinal(
                         _subjects[index - 1].Subject.SubjectId.Value,
                         _subjects[index].Subject.SubjectId.Value) >= 0))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
