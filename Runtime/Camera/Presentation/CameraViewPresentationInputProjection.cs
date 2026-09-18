using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit projection into immutable presentation input. Composition membership is the
    /// productive path; the View overload remains as compatibility until CAMERA-029-E.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-B Composition presentation-input projection with legacy View compatibility.")]
    public static class CameraViewPresentationInputProjection
    {
        public static CameraViewPresentationInputResult TryCreate(
            CameraCompositionMembershipSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.ContextId.IsValid ||
                !snapshot.AvailabilityContextId.IsValid)
            {
                return new CameraViewPresentationInputResult(
                    CameraViewPresentationInputStatus.RejectedInvalidRequest,
                    null,
                    "Composition presentation input requires a valid membership snapshot.");
            }

            var subjects = new CameraSubjectAvailabilityEntry[snapshot.Count];
            for (int index = 0; index < subjects.Length; index++)
            {
                if (!snapshot.Entries[index].IsValid)
                {
                    return new CameraViewPresentationInputResult(
                        CameraViewPresentationInputStatus.RejectedDivergentSnapshot,
                        null,
                        "Composition membership contains invalid resolved Subject evidence.");
                }
                subjects[index] = snapshot.Entries[index].Subject;
            }

            var input = new CameraViewPresentationInput(
                snapshot.ContextId,
                snapshot.Revision,
                snapshot.AvailabilityContextId,
                snapshot.AvailabilityRevision,
                subjects);
            return new CameraViewPresentationInputResult(
                CameraViewPresentationInputStatus.Succeeded,
                input,
                $"Projected composition membership with '{subjects.Length}' resolved Subjects.");
        }

        public static CameraViewPresentationInputResult TryCreate(
            CameraViewAssignmentSnapshot snapshot,
            CameraViewId viewId)
        {
            if (snapshot == null || !viewId.IsValid)
            {
                return new CameraViewPresentationInputResult(
                    CameraViewPresentationInputStatus.RejectedInvalidRequest,
                    null,
                    "View presentation input requires a snapshot and explicit valid View id.");
            }

            if (!snapshot.TryGetView(viewId, out CameraViewSubjectSnapshot view))
            {
                return new CameraViewPresentationInputResult(
                    CameraViewPresentationInputStatus.RejectedViewNotFound,
                    null,
                    $"Logical Camera View '{viewId}' does not exist in the supplied snapshot.");
            }

            if (view.AssignmentCount != view.ResolvedSubjectCount)
            {
                return new CameraViewPresentationInputResult(
                    CameraViewPresentationInputStatus.RejectedDivergentSnapshot,
                    null,
                    $"Logical Camera View '{viewId}' contains unresolved Assignment evidence.");
            }

            var subjects = new CameraSubjectAvailabilityEntry[view.ResolvedSubjectCount];
            for (int index = 0; index < subjects.Length; index++)
            {
                subjects[index] = view.ResolvedSubjects[index];
            }

            var input = new CameraViewPresentationInput(
                view.View,
                snapshot.ContextId,
                snapshot.Revision,
                snapshot.AvailabilityContextId,
                snapshot.AvailabilityRevision,
                subjects);
            return new CameraViewPresentationInputResult(
                CameraViewPresentationInputStatus.Succeeded,
                input,
                $"Projected logical Camera View '{viewId}' with '{subjects.Length}' resolved Subjects.");
        }
    }
}
