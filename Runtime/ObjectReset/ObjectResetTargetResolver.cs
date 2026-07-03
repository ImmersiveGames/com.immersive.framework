using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Resolves Object Reset logical targets against the current Object Entry snapshot.
    /// This resolver does not find Unity objects, discover participants or perform reset side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset target resolver against Object Entry runtime snapshots only.")]
    public static class ObjectResetTargetResolver
    {
        public static ObjectResetResult ResolveTarget(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetRequest request)
        {
            if (!request.IsValid)
            {
                return ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedInvalidRequest,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.InvalidRequest,
                        "Object Reset request is invalid."),
                    "Object Reset target resolution rejected because the request is invalid.");
            }

            if (snapshot == null || !snapshot.IsAvailable)
            {
                return ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedRuntimeContextUnavailable,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.RuntimeContextUnavailable,
                        "Object Reset requires the current Object Entry runtime context snapshot."),
                    "Object Reset target resolution rejected because Object Entry runtime context is unavailable.");
            }

            if (!snapshot.TryGet(request.Target.ObjectEntryId, out var descriptor))
            {
                return ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedTargetNotFound,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.TargetNotFound,
                        $"Object Reset target '{request.Target.ObjectEntryId.StableText}' was not found in the current Object Entry snapshot."),
                    "Object Reset target resolution rejected because the target ObjectEntryId is not in the current snapshot.");
            }

            if (descriptor.Scope != request.Target.Scope)
            {
                return ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedForeignTarget,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.TargetScopeMismatch,
                        $"Object Reset target scope '{request.Target.Scope}' does not match descriptor scope '{descriptor.Scope}'."),
                    "Object Reset target resolution rejected because the target scope does not match the current descriptor.");
            }

            if (!descriptor.HasOwnerIdentity)
            {
                return ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedForeignTarget,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.TargetOwnerMissing,
                        $"Object Reset target '{request.Target.ObjectEntryId.StableText}' has no owner identity in the current descriptor."),
                    "Object Reset target resolution rejected because the current descriptor has no owner identity.");
            }

            if (descriptor.OwnerIdentity.Value != request.Target.OwnerIdentity)
            {
                return ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedForeignTarget,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.ForeignOrStaleTarget,
                        $"Object Reset target owner '{request.Target.OwnerIdentity.StableText}' does not match current descriptor owner '{descriptor.OwnerIdentity.Value.StableText}'."),
                    "Object Reset target resolution rejected because the target belongs to a foreign or stale owner.");
            }

            return ObjectResetResult.TargetResolved(request, descriptor);
        }
    }
}
