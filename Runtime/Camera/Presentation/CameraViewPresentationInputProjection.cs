using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit projection from one caller-selected logical View snapshot into immutable
    /// presentation input. It never chooses a View or Subject implicitly.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-C logical View presentation-input projection.")]
    public static class CameraViewPresentationInputProjection
    {
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
