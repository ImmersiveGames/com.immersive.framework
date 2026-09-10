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
            ViewAssignmentContextId assignmentContextId,
            int assignmentRevision,
            SubjectAvailabilityContextId availabilityContextId,
            int availabilityRevision,
            CameraSubjectAvailabilityEntry[] subjects)
        {
            View = view;
            AssignmentContextId = assignmentContextId;
            AssignmentRevision = assignmentRevision;
            AvailabilityContextId = availabilityContextId;
            AvailabilityRevision = availabilityRevision;
            _subjects = subjects != null
                ? (CameraSubjectAvailabilityEntry[])subjects.Clone()
                : Array.Empty<CameraSubjectAvailabilityEntry>();
            _subjectView = Array.AsReadOnly(_subjects);
        }

        public CameraView View { get; }
        public CameraViewId ViewId => View.ViewId;
        public ViewAssignmentContextId AssignmentContextId { get; }
        public int AssignmentRevision { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
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
            AssignmentContextId.IsValid &&
            AssignmentRevision >= 0 &&
            AvailabilityContextId.IsValid &&
            AvailabilityRevision >= 0 &&
            AllSubjectsAreValid();

        public bool IsCurrentFor(CameraViewAssignmentSnapshot snapshot)
        {
            if (snapshot == null ||
                AssignmentContextId != snapshot.ContextId ||
                AssignmentRevision != snapshot.Revision ||
                AvailabilityContextId != snapshot.AvailabilityContextId ||
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
                    _subjects[index].Token.ContextId != AvailabilityContextId ||
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
