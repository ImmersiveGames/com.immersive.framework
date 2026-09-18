using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable input presented to Camera presentation. The legacy type name remains until
    /// CAMERA-029-E; Composition input carries no View identity or View currentness evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-B Composition membership presentation input with legacy View compatibility.")]
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

        internal CameraViewPresentationInput(
            CameraCompositionMembershipContextId membershipContextId,
            int membershipRevision,
            SubjectAvailabilityContextId availabilityContextId,
            int availabilityRevision,
            CameraSubjectAvailabilityEntry[] subjects)
        {
            MembershipContextId = membershipContextId;
            MembershipRevision = membershipRevision;
            AvailabilityContextId = availabilityContextId;
            AvailabilityRevision = availabilityRevision;
            _subjects = subjects != null
                ? (CameraSubjectAvailabilityEntry[])subjects.Clone()
                : Array.Empty<CameraSubjectAvailabilityEntry>();
            _subjectView = Array.AsReadOnly(_subjects);
            IsCompositionMembershipInput = true;
        }

        public CameraView View { get; }
        public CameraViewId ViewId => View.ViewId;
        public ViewAssignmentContextId AssignmentContextId { get; }
        public int AssignmentRevision { get; }
        public CameraCompositionMembershipContextId MembershipContextId { get; }
        public int MembershipRevision { get; }
        public bool IsCompositionMembershipInput { get; }
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
            (IsCompositionMembershipInput
                ? MembershipContextId.IsValid && MembershipRevision >= 0
                : View.IsValid && AssignmentContextId.IsValid && AssignmentRevision >= 0) &&
            AvailabilityContextId.IsValid &&
            AvailabilityRevision >= 0 &&
            AllSubjectsAreValid();

        public bool IsCurrentFor(CameraViewAssignmentSnapshot snapshot)
        {
            if (IsCompositionMembershipInput || snapshot == null ||
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

        public bool IsCurrentFor(CameraCompositionMembershipSnapshot snapshot)
        {
            if (!IsCompositionMembershipInput || snapshot == null ||
                MembershipContextId != snapshot.ContextId ||
                MembershipRevision != snapshot.Revision ||
                AvailabilityContextId != snapshot.AvailabilityContextId ||
                AvailabilityRevision != snapshot.AvailabilityRevision ||
                snapshot.Count != SubjectCount)
            {
                return false;
            }

            for (int index = 0; index < _subjects.Length; index++)
            {
                if (_subjects[index].Token != snapshot.Entries[index].Subject.Token)
                    return false;
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
