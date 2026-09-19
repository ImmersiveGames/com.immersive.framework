using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable presentation input derived from one current Camera Composition membership.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E Composition-owned presentation input.")]
    public sealed class CameraCompositionPresentationInput
    {
        private readonly CameraSubjectAvailabilityEntry[] _subjects;
        private readonly IReadOnlyList<CameraSubjectAvailabilityEntry> _subjectView;

        internal CameraCompositionPresentationInput(
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
        }

        public CameraCompositionMembershipContextId MembershipContextId { get; }
        public int MembershipRevision { get; }
        public SubjectAvailabilityContextId AvailabilityContextId { get; }
        public int AvailabilityRevision { get; }
        public IReadOnlyList<CameraSubjectAvailabilityEntry> Subjects => _subjectView;
        public int SubjectCount => _subjects.Length;
        public CameraSubjectCardinality Cardinality =>
            SubjectCount == 0
                ? CameraSubjectCardinality.Zero
                : SubjectCount == 1
                    ? CameraSubjectCardinality.One
                    : CameraSubjectCardinality.Many;

        public bool IsValid =>
            MembershipContextId.IsValid &&
            MembershipRevision >= 0 &&
            AvailabilityContextId.IsValid &&
            AvailabilityRevision >= 0 &&
            AllSubjectsAreValid();

        public bool IsCurrentFor(CameraCompositionMembershipSnapshot snapshot)
        {
            if (snapshot == null ||
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
