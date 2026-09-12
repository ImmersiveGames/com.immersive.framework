using System.Collections.Generic;
using Immersive.Framework.Camera;
using Unity.Cinemachine;

namespace Immersive.Framework.CameraAuthoring
{
    internal sealed class CameraViewPresentationAdapter
    {
        private readonly CameraRigComposer _composer;
        private ViewAssignmentContextId appliedViewAssignmentContextId;
        private int appliedViewAssignmentRevision = -1;
        private SubjectAvailabilityContextId appliedViewAvailabilityContextId;
        private int appliedViewAvailabilityRevision = -1;

        internal CameraViewPresentationAdapter(CameraRigComposer composer)
        {
            _composer = composer;
        }

        public CameraViewPresentationApplyResult ApplyViewPresentation(
            CameraViewPresentationInput input,
            CameraViewAssignmentSnapshot currentSnapshot)
        {
            if (input == null || currentSnapshot == null || !input.IsValid)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedInvalidInput,
                    input,
                    "View presentation apply requires valid input and its current logical snapshot.");
            }

            if (!input.IsCurrentFor(currentSnapshot) || IsOlderThanAppliedEvidence(input))
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedStaleInput,
                    input,
                    "Stale View presentation input cannot overwrite newer applied membership.");
            }

            if (!_composer.TryValidateForApply(out string settingsIssue))
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedInvalidSettings,
                    input,
                    settingsIssue);
            }

            if (_composer.CinemachineCamera == null)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedMissingCinemachineCamera,
                    input,
                    "View presentation requires the Composer's existing materialized Cinemachine Camera.");
            }

            CameraViewTargetProjectionResult projection =
                _composer.ResolveViewPresentationTargets(input);

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededSharedFollow)
            {
                if (!CameraSharedFollowProvenance.Validate(_composer, true, out string ownershipIssue))
                {
                    return ViewApplyResult(
                        CameraViewPresentationApplyStatus.RejectedOwnershipConflict,
                        input,
                        ownershipIssue);
                }

                ReconcileSharedFollowMembers(input);
                ConfigureSharedFollowFraming();
                _composer.FrameworkOwnedSharedFollowGroupFraming.enabled = true;
                _composer.CinemachineCamera.Follow = _composer.FrameworkOwnedSharedFollowTargetGroup.transform;
                _composer.CinemachineCamera.LookAt =
                    _composer.EffectiveLookAtRequirement == CameraTargetRequirement.NotUsed
                        ? null
                        : _composer.FrameworkOwnedSharedFollowTargetGroup.transform;
                RecordAppliedEvidence(input);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededSharedFollow,
                    input,
                    $"Applied shared Follow presentation with '{input.SubjectCount}' ordered Subjects.");
            }

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededSingleSubject)
            {
                ClearOwnedSharedFollowProjection();
                _composer.CinemachineCamera.Follow = projection.Targets.FollowTarget;
                _composer.CinemachineCamera.LookAt = projection.Targets.LookAtTarget;
                RecordAppliedEvidence(input);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededSingleSubject,
                    input,
                    "Applied direct single-Subject presentation to the existing Cinemachine Camera.");
            }

            ClearOwnedSharedFollowProjection();
            _composer.CinemachineCamera.Follow = null;
            _composer.CinemachineCamera.LookAt = null;
            RecordAppliedEvidence(input);

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededNoTargets)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededFixedNoTargets,
                    input,
                    "Applied target-independent Fixed presentation.");
            }

            return ViewApplyResult(
                projection.Status == CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing
                    ? CameraViewPresentationApplyStatus.BlockedRequiredSubjectMissing
                    : CameraViewPresentationApplyStatus.BlockedUnsupportedPresentation,
                input,
                projection.BlockingIssue);
        }

        public CameraViewPresentationApplyResult ClearViewPresentation()
        {
            ClearOwnedSharedFollowProjection();
            if (_composer.CinemachineCamera != null)
            {
                _composer.CinemachineCamera.Follow = null;
                _composer.CinemachineCamera.LookAt = null;
            }

            appliedViewAssignmentContextId = default;
            appliedViewAssignmentRevision = -1;
            appliedViewAvailabilityContextId = default;
            appliedViewAvailabilityRevision = -1;
            return ViewApplyResult(
                CameraViewPresentationApplyStatus.SucceededCleared,
                null,
                "Cleared explicit View presentation without releasing the Composer, Cinemachine Camera or output.");
        }

        private void ReconcileSharedFollowMembers(CameraViewPresentationInput input)
        {
            _composer.FrameworkOwnedSharedFollowTargetGroup.Targets ??=
                new List<CinemachineTargetGroup.Target>();
            _composer.FrameworkOwnedSharedFollowTargetGroup.Targets.Clear();
            for (int index = 0; index < input.SubjectCount; index++)
            {
                _composer.FrameworkOwnedSharedFollowTargetGroup.Targets.Add(
                    new CinemachineTargetGroup.Target
                    {
                        Object = input.Subjects[index].Subject.Observation,
                        Weight = _composer.SharedFollowMemberWeight,
                        Radius = _composer.SharedFollowMemberRadius
                    });
            }

            _composer.FrameworkOwnedSharedFollowTargetGroup.PositionMode =
                CinemachineTargetGroup.PositionModes.GroupCenter;
            _composer.FrameworkOwnedSharedFollowTargetGroup.RotationMode =
                CinemachineTargetGroup.RotationModes.Manual;
            _composer.FrameworkOwnedSharedFollowTargetGroup.UpdateMethod =
                CinemachineTargetGroup.UpdateMethods.LateUpdate;
        }

        private void ConfigureSharedFollowFraming()
        {
            _composer.FrameworkOwnedSharedFollowGroupFraming.FramingMode =
                CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            _composer.FrameworkOwnedSharedFollowGroupFraming.SizeAdjustment =
                CinemachineGroupFraming.SizeAdjustmentModes.DollyThenZoom;
            _composer.FrameworkOwnedSharedFollowGroupFraming.LateralAdjustment =
                CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition;
            _composer.FrameworkOwnedSharedFollowGroupFraming.FramingSize =
                _composer.SharedFollowFramingSize;
            _composer.FrameworkOwnedSharedFollowGroupFraming.Damping =
                _composer.SharedFollowDamping;
            _composer.FrameworkOwnedSharedFollowGroupFraming.FovRange =
                _composer.SharedFollowFovRange;
            _composer.FrameworkOwnedSharedFollowGroupFraming.DollyRange =
                _composer.SharedFollowDollyRange;
            _composer.FrameworkOwnedSharedFollowGroupFraming.OrthoSizeRange =
                _composer.SharedFollowOrthoSizeRange;
        }

        private void ClearOwnedSharedFollowProjection()
        {
            if (_composer.FrameworkOwnedSharedFollowTargetGroup != null &&
                _composer.FrameworkOwnedSharedFollowTargetGroup.transform.IsChildOf(_composer.transform))
            {
                _composer.FrameworkOwnedSharedFollowTargetGroup.Targets?.Clear();
            }

            if (_composer.FrameworkOwnedSharedFollowGroupFraming != null &&
                _composer.CinemachineCamera != null &&
                _composer.FrameworkOwnedSharedFollowGroupFraming.gameObject == _composer.CinemachineCamera.gameObject)
            {
                _composer.FrameworkOwnedSharedFollowGroupFraming.enabled = false;
            }
        }

        private bool IsOlderThanAppliedEvidence(CameraViewPresentationInput input)
        {
            if (appliedViewAssignmentContextId != input.AssignmentContextId)
            {
                return false;
            }

            return input.AssignmentRevision < appliedViewAssignmentRevision ||
                   ((appliedViewAvailabilityContextId == input.AvailabilityContextId) &&
                    input.AvailabilityRevision < appliedViewAvailabilityRevision);
        }

        private void RecordAppliedEvidence(CameraViewPresentationInput input)
        {
            appliedViewAssignmentContextId = input.AssignmentContextId;
            appliedViewAssignmentRevision = input.AssignmentRevision;
            appliedViewAvailabilityContextId = input.AvailabilityContextId;
            appliedViewAvailabilityRevision = input.AvailabilityRevision;
        }

        private CameraViewPresentationApplyResult ViewApplyResult(
            CameraViewPresentationApplyStatus status,
            CameraViewPresentationInput input,
            string diagnostic)
        {
            return new CameraViewPresentationApplyResult(
                status,
                input,
                _composer.CinemachineCamera,
                _composer.FrameworkOwnedSharedFollowTargetGroup,
                _composer.FrameworkOwnedSharedFollowGroupFraming,
                diagnostic);
        }
    }
}
