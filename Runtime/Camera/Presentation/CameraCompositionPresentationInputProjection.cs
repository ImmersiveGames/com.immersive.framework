using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit projection of current Composition membership into immutable presentation input.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E Composition presentation-input projection.")]
    public static class CameraCompositionPresentationInputProjection
    {
        public static CameraCompositionPresentationInputResult TryCreate(
            CameraCompositionMembershipSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.ContextId.IsValid ||
                !snapshot.AvailabilityContextId.IsValid)
            {
                return new CameraCompositionPresentationInputResult(
                    CameraCompositionPresentationInputStatus.RejectedInvalidRequest,
                    null,
                    "Composition presentation input requires a valid membership snapshot.");
            }

            var subjects = new CameraSubjectAvailabilityEntry[snapshot.Count];
            for (int index = 0; index < subjects.Length; index++)
            {
                if (!snapshot.Entries[index].IsValid)
                {
                    return new CameraCompositionPresentationInputResult(
                        CameraCompositionPresentationInputStatus.RejectedDivergentSnapshot,
                        null,
                        "Composition membership contains invalid resolved Subject evidence.");
                }
                subjects[index] = snapshot.Entries[index].Subject;
            }

            var input = new CameraCompositionPresentationInput(
                snapshot.ContextId,
                snapshot.Revision,
                snapshot.AvailabilityContextId,
                snapshot.AvailabilityRevision,
                subjects);
            return new CameraCompositionPresentationInputResult(
                CameraCompositionPresentationInputStatus.Succeeded,
                input,
                $"Projected composition membership with '{subjects.Length}' resolved Subjects.");
        }
    }
}
