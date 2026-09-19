using System;
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
        private CameraCompositionMembershipContextId appliedMembershipContextId;
        private int appliedMembershipRevision = -1;
        private SubjectAvailabilityContextId appliedMembershipAvailabilityContextId;
        private int appliedMembershipAvailabilityRevision = -1;

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

            return ApplyCurrent(input, false);
        }

        public CameraViewPresentationApplyResult ApplyCompositionPresentation(
            CameraViewPresentationInput input,
            CameraCompositionMembershipSnapshot currentSnapshot)
        {
            if (input == null || currentSnapshot == null || !input.IsValid ||
                !input.IsCompositionMembershipInput)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedInvalidInput,
                    input,
                    "Composition presentation apply requires valid composition membership input and its current snapshot.");
            }

            if (!input.IsCurrentFor(currentSnapshot) || IsOlderThanAppliedCompositionEvidence(input))
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedStaleInput,
                    input,
                    "Stale composition presentation input cannot overwrite newer applied membership.");
            }

            return ApplyCurrent(input, true);
        }

        private CameraViewPresentationApplyResult ApplyCurrent(
            CameraViewPresentationInput input,
            bool compositionInput)
        {
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
                RecordAppliedEvidence(input, compositionInput);
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
                RecordAppliedEvidence(input, compositionInput);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededSingleSubject,
                    input,
                    "Applied direct single-Subject presentation to the existing Cinemachine Camera.");
            }

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededNoTargets)
            {
                ClearOwnedGroupProjection();
                _composer.CinemachineCamera.Follow = null;
                _composer.CinemachineCamera.LookAt = null;
                RecordAppliedEvidence(input, compositionInput);
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
            appliedMembershipContextId = default;
            appliedMembershipRevision = -1;
            appliedMembershipAvailabilityContextId = default;
            appliedMembershipAvailabilityRevision = -1;
            return ViewApplyResult(
                CameraViewPresentationApplyStatus.SucceededCleared,
                null,
                "Cleared explicit presentation without releasing the Composer, Cinemachine Camera or output.");
        }

        internal CameraRigPresentationState CaptureState()
        {
            CinemachineTargetGroup group = _composer.FrameworkOwnedGroupTargetGroup;
            CinemachineGroupFraming framing = _composer.FrameworkOwnedGroupFraming;
            List<CinemachineTargetGroup.Target> targets = group != null ? group.Targets : null;

            return new CameraRigPresentationState(
                _composer,
                _composer.CinemachineCamera != null ? _composer.CinemachineCamera.Follow : null,
                _composer.CinemachineCamera != null ? _composer.CinemachineCamera.LookAt : null,
                group != null,
                targets == null,
                targets != null
                    ? new List<CinemachineTargetGroup.Target>(targets).ToArray()
                    : Array.Empty<CinemachineTargetGroup.Target>(),
                group != null ? group.PositionMode : default,
                group != null ? group.RotationMode : default,
                group != null ? group.UpdateMethod : default,
                framing != null,
                framing != null && framing.enabled,
                framing != null ? framing.FramingMode : default,
                framing != null ? framing.SizeAdjustment : default,
                framing != null ? framing.LateralAdjustment : default,
                framing != null ? framing.FramingSize : 0f,
                framing != null ? framing.Damping : 0f,
                framing != null ? framing.FovRange : default,
                framing != null ? framing.DollyRange : default,
                framing != null ? framing.OrthoSizeRange : default,
                appliedViewAssignmentContextId,
                appliedViewAssignmentRevision,
                appliedViewAvailabilityContextId,
                appliedViewAvailabilityRevision,
                appliedMembershipContextId,
                appliedMembershipRevision,
                appliedMembershipAvailabilityContextId,
                appliedMembershipAvailabilityRevision);
        }

        internal CameraRigPresentationRestoreResult RestoreState(
            CameraRigPresentationState previous,
            CameraRigPresentationState expectedCurrent)
        {
            if (previous == null || expectedCurrent == null ||
                !ReferenceEquals(previous.Composer, _composer) ||
                !ReferenceEquals(expectedCurrent.Composer, _composer))
            {
                return RestoreResult(
                    CameraRigPresentationRestoreStatus.RejectedInvalidState,
                    "Camera presentation rollback requires states captured from the exact same Composer.");
            }

            CameraRigPresentationState current = CaptureState();
            if (!Matches(current, expectedCurrent))
            {
                return RestoreResult(
                    CameraRigPresentationRestoreStatus.RejectedStaleTransaction,
                    "Camera presentation rollback rejected because newer or foreign presentation evidence is already applied.");
            }

            if (previous.HasGroupTargetGroup && _composer.FrameworkOwnedGroupTargetGroup == null)
            {
                return RestoreResult(
                    CameraRigPresentationRestoreStatus.CriticalRestoreFailure,
                    "Camera presentation rollback cannot restore Group members because the owned Target Group is missing.");
            }

            if (previous.HasGroupFraming && _composer.FrameworkOwnedGroupFraming == null)
            {
                return RestoreResult(
                    CameraRigPresentationRestoreStatus.CriticalRestoreFailure,
                    "Camera presentation rollback cannot restore Group framing because the owned Group Framing component is missing.");
            }

            try
            {
                if (_composer.CinemachineCamera != null)
                {
                    _composer.CinemachineCamera.Follow = previous.Follow;
                    _composer.CinemachineCamera.LookAt = previous.LookAt;
                }

                if (previous.HasGroupTargetGroup)
                {
                    CinemachineTargetGroup group = _composer.FrameworkOwnedGroupTargetGroup;
                    group.Targets = previous.GroupTargetsWereNull
                        ? null
                        : new List<CinemachineTargetGroup.Target>(previous.GroupTargets);
                    group.PositionMode = previous.GroupPositionMode;
                    group.RotationMode = previous.GroupRotationMode;
                    group.UpdateMethod = previous.GroupUpdateMethod;
                }

                if (previous.HasGroupFraming)
                {
                    CinemachineGroupFraming framing = _composer.FrameworkOwnedGroupFraming;
                    framing.FramingMode = previous.FramingMode;
                    framing.SizeAdjustment = previous.SizeAdjustment;
                    framing.LateralAdjustment = previous.LateralAdjustment;
                    framing.FramingSize = previous.FramingSize;
                    framing.Damping = previous.Damping;
                    framing.FovRange = previous.FovRange;
                    framing.DollyRange = previous.DollyRange;
                    framing.OrthoSizeRange = previous.OrthoSizeRange;
                    framing.enabled = previous.GroupFramingEnabled;
                }

                appliedViewAssignmentContextId = previous.ViewAssignmentContextId;
                appliedViewAssignmentRevision = previous.ViewAssignmentRevision;
                appliedViewAvailabilityContextId = previous.ViewAvailabilityContextId;
                appliedViewAvailabilityRevision = previous.ViewAvailabilityRevision;
                appliedMembershipContextId = previous.MembershipContextId;
                appliedMembershipRevision = previous.MembershipRevision;
                appliedMembershipAvailabilityContextId = previous.MembershipAvailabilityContextId;
                appliedMembershipAvailabilityRevision = previous.MembershipAvailabilityRevision;

                return RestoreResult(
                    CameraRigPresentationRestoreStatus.Succeeded,
                    "Camera presentation rollback restored the previous Composer state.");
            }
            catch (Exception exception)
            {
                return RestoreResult(
                    CameraRigPresentationRestoreStatus.CriticalRestoreFailure,
                    $"Camera presentation rollback failed while restoring Composer state: {exception.Message}");
            }
        }

        private static bool Matches(
            CameraRigPresentationState left,
            CameraRigPresentationState right)
        {
            if (left.Follow != right.Follow || left.LookAt != right.LookAt ||
                left.HasGroupTargetGroup != right.HasGroupTargetGroup ||
                left.GroupTargetsWereNull != right.GroupTargetsWereNull ||
                left.HasGroupFraming != right.HasGroupFraming ||
                left.GroupPositionMode != right.GroupPositionMode ||
                left.GroupRotationMode != right.GroupRotationMode ||
                left.GroupUpdateMethod != right.GroupUpdateMethod ||
                left.GroupFramingEnabled != right.GroupFramingEnabled ||
                left.FramingMode != right.FramingMode ||
                left.SizeAdjustment != right.SizeAdjustment ||
                left.LateralAdjustment != right.LateralAdjustment ||
                !left.FramingSize.Equals(right.FramingSize) ||
                !left.Damping.Equals(right.Damping) ||
                left.FovRange != right.FovRange ||
                left.DollyRange != right.DollyRange ||
                left.OrthoSizeRange != right.OrthoSizeRange ||
                left.ViewAssignmentContextId != right.ViewAssignmentContextId ||
                left.ViewAssignmentRevision != right.ViewAssignmentRevision ||
                left.ViewAvailabilityContextId != right.ViewAvailabilityContextId ||
                left.ViewAvailabilityRevision != right.ViewAvailabilityRevision ||
                left.MembershipContextId != right.MembershipContextId ||
                left.MembershipRevision != right.MembershipRevision ||
                left.MembershipAvailabilityContextId != right.MembershipAvailabilityContextId ||
                left.MembershipAvailabilityRevision != right.MembershipAvailabilityRevision ||
                left.GroupTargets.Count != right.GroupTargets.Count)
            {
                return false;
            }

            for (int index = 0; index < left.GroupTargets.Count; index++)
            {
                CinemachineTargetGroup.Target leftTarget = left.GroupTargets[index];
                CinemachineTargetGroup.Target rightTarget = right.GroupTargets[index];
                if (leftTarget.Object != rightTarget.Object ||
                    !leftTarget.Weight.Equals(rightTarget.Weight) ||
                    !leftTarget.Radius.Equals(rightTarget.Radius))
                {
                    return false;
                }
            }

            return true;
        }

        private static CameraRigPresentationRestoreResult RestoreResult(
            CameraRigPresentationRestoreStatus status,
            string diagnostic) =>
            new CameraRigPresentationRestoreResult(status, diagnostic);

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

        private bool IsOlderThanAppliedCompositionEvidence(CameraViewPresentationInput input)
        {
            if (appliedMembershipContextId != input.MembershipContextId)
                return false;

            return input.MembershipRevision < appliedMembershipRevision ||
                (appliedMembershipAvailabilityContextId == input.AvailabilityContextId &&
                 input.AvailabilityRevision < appliedMembershipAvailabilityRevision);
        }

        private void RecordAppliedEvidence(CameraViewPresentationInput input, bool compositionInput)
        {
            if (compositionInput)
            {
                appliedMembershipContextId = input.MembershipContextId;
                appliedMembershipRevision = input.MembershipRevision;
                appliedMembershipAvailabilityContextId = input.AvailabilityContextId;
                appliedMembershipAvailabilityRevision = input.AvailabilityRevision;
                return;
            }
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
