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

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededGroup)
            {
                if (!CameraGroupProvenance.Validate(_composer, true, out string ownershipIssue))
                {
                    return ViewApplyResult(
                        CameraViewPresentationApplyStatus.RejectedOwnershipConflict,
                        input,
                        ownershipIssue);
                }

                ReconcileGroupMembers(input);
                ConfigureGroupFraming();
                _composer.FrameworkOwnedGroupFraming.enabled = true;
                _composer.CinemachineCamera.Follow = _composer.FrameworkOwnedGroupTargetGroup.transform;
                _composer.CinemachineCamera.LookAt =
                    _composer.EffectiveLookAtRequirement == CameraTargetRequirement.NotUsed
                        ? null
                        : _composer.FrameworkOwnedGroupTargetGroup.transform;
                RecordAppliedEvidence(input);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededGroup,
                    input,
                    $"Applied Group presentation with '{input.SubjectCount}' ordered Subjects.");
            }

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededSingleSubject)
            {
                ClearOwnedGroupProjection();
                _composer.CinemachineCamera.Follow = projection.Targets.FollowTarget;
                _composer.CinemachineCamera.LookAt = projection.Targets.LookAtTarget;
                RecordAppliedEvidence(input);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededSingleSubject,
                    input,
                    "Applied direct single-Subject presentation to the existing Cinemachine Camera.");
            }

            ClearOwnedGroupProjection();
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
            ClearOwnedGroupProjection();
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

        private void ReconcileGroupMembers(CameraViewPresentationInput input)
        {
            _composer.FrameworkOwnedGroupTargetGroup.Targets ??=
                new List<CinemachineTargetGroup.Target>();
            _composer.FrameworkOwnedGroupTargetGroup.Targets.Clear();
            for (int index = 0; index < input.SubjectCount; index++)
            {
                _composer.FrameworkOwnedGroupTargetGroup.Targets.Add(
                    new CinemachineTargetGroup.Target
                    {
                        Object = input.Subjects[index].Subject.Observation,
                        Weight = _composer.GroupMemberWeight,
                        Radius = _composer.GroupMemberRadius
                    });
            }

            _composer.FrameworkOwnedGroupTargetGroup.PositionMode =
                CinemachineTargetGroup.PositionModes.GroupCenter;
            _composer.FrameworkOwnedGroupTargetGroup.RotationMode =
                CinemachineTargetGroup.RotationModes.Manual;
            _composer.FrameworkOwnedGroupTargetGroup.UpdateMethod =
                CinemachineTargetGroup.UpdateMethods.LateUpdate;
        }

        private void ConfigureGroupFraming()
        {
            _composer.FrameworkOwnedGroupFraming.FramingMode =
                CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            _composer.FrameworkOwnedGroupFraming.SizeAdjustment =
                CinemachineGroupFraming.SizeAdjustmentModes.DollyThenZoom;
            _composer.FrameworkOwnedGroupFraming.LateralAdjustment =
                CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition;
            _composer.FrameworkOwnedGroupFraming.FramingSize =
                _composer.GroupFramingSize;
            _composer.FrameworkOwnedGroupFraming.Damping =
                _composer.GroupDamping;
            _composer.FrameworkOwnedGroupFraming.FovRange =
                _composer.GroupFovRange;
            _composer.FrameworkOwnedGroupFraming.DollyRange =
                _composer.GroupDollyRange;
            _composer.FrameworkOwnedGroupFraming.OrthoSizeRange =
                _composer.GroupOrthoSizeRange;
        }

        private void ClearOwnedGroupProjection()
        {
            if (_composer.FrameworkOwnedGroupTargetGroup != null &&
                _composer.FrameworkOwnedGroupTargetGroup.transform.IsChildOf(_composer.transform))
            {
                _composer.FrameworkOwnedGroupTargetGroup.Targets?.Clear();
            }

            if (_composer.FrameworkOwnedGroupFraming != null &&
                _composer.CinemachineCamera != null &&
                _composer.FrameworkOwnedGroupFraming.gameObject == _composer.CinemachineCamera.gameObject)
            {
                _composer.FrameworkOwnedGroupFraming.enabled = false;
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
                _composer.FrameworkOwnedGroupTargetGroup,
                _composer.FrameworkOwnedGroupFraming,
                diagnostic);
        }
    }
}
