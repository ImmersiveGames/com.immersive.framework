using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    internal sealed class CameraRigPresentationState
    {
        private readonly CinemachineTargetGroup.Target[] _groupTargets;

        internal CameraRigPresentationState(
            CameraRigComposer composer,
            Transform follow,
            Transform lookAt,
            bool hasGroupTargetGroup,
            bool groupTargetsWereNull,
            CinemachineTargetGroup.Target[] groupTargets,
            CinemachineTargetGroup.PositionModes groupPositionMode,
            CinemachineTargetGroup.RotationModes groupRotationMode,
            CinemachineTargetGroup.UpdateMethods groupUpdateMethod,
            bool hasGroupFraming,
            bool groupFramingEnabled,
            CinemachineGroupFraming.FramingModes framingMode,
            CinemachineGroupFraming.SizeAdjustmentModes sizeAdjustment,
            CinemachineGroupFraming.LateralAdjustmentModes lateralAdjustment,
            float framingSize,
            float damping,
            Vector2 fovRange,
            Vector2 dollyRange,
            Vector2 orthoSizeRange,
            CameraCompositionMembershipContextId membershipContextId,
            int membershipRevision,
            SubjectAvailabilityContextId membershipAvailabilityContextId,
            int membershipAvailabilityRevision)
        {
            Composer = composer;
            Follow = follow;
            LookAt = lookAt;
            HasGroupTargetGroup = hasGroupTargetGroup;
            GroupTargetsWereNull = groupTargetsWereNull;
            _groupTargets = groupTargets != null
                ? (CinemachineTargetGroup.Target[])groupTargets.Clone()
                : Array.Empty<CinemachineTargetGroup.Target>();
            GroupPositionMode = groupPositionMode;
            GroupRotationMode = groupRotationMode;
            GroupUpdateMethod = groupUpdateMethod;
            HasGroupFraming = hasGroupFraming;
            GroupFramingEnabled = groupFramingEnabled;
            FramingMode = framingMode;
            SizeAdjustment = sizeAdjustment;
            LateralAdjustment = lateralAdjustment;
            FramingSize = framingSize;
            Damping = damping;
            FovRange = fovRange;
            DollyRange = dollyRange;
            OrthoSizeRange = orthoSizeRange;
            MembershipContextId = membershipContextId;
            MembershipRevision = membershipRevision;
            MembershipAvailabilityContextId = membershipAvailabilityContextId;
            MembershipAvailabilityRevision = membershipAvailabilityRevision;
        }

        internal CameraRigComposer Composer { get; }
        internal Transform Follow { get; }
        internal Transform LookAt { get; }
        internal bool HasGroupTargetGroup { get; }
        internal bool GroupTargetsWereNull { get; }
        internal IReadOnlyList<CinemachineTargetGroup.Target> GroupTargets => _groupTargets;
        internal CinemachineTargetGroup.PositionModes GroupPositionMode { get; }
        internal CinemachineTargetGroup.RotationModes GroupRotationMode { get; }
        internal CinemachineTargetGroup.UpdateMethods GroupUpdateMethod { get; }
        internal bool HasGroupFraming { get; }
        internal bool GroupFramingEnabled { get; }
        internal CinemachineGroupFraming.FramingModes FramingMode { get; }
        internal CinemachineGroupFraming.SizeAdjustmentModes SizeAdjustment { get; }
        internal CinemachineGroupFraming.LateralAdjustmentModes LateralAdjustment { get; }
        internal float FramingSize { get; }
        internal float Damping { get; }
        internal Vector2 FovRange { get; }
        internal Vector2 DollyRange { get; }
        internal Vector2 OrthoSizeRange { get; }
        internal CameraCompositionMembershipContextId MembershipContextId { get; }
        internal int MembershipRevision { get; }
        internal SubjectAvailabilityContextId MembershipAvailabilityContextId { get; }
        internal int MembershipAvailabilityRevision { get; }
    }

    internal enum CameraRigPresentationRestoreStatus
    {
        None = 0,
        Succeeded = 10,
        RejectedInvalidState = 100,
        RejectedStaleTransaction = 110,
        CriticalRestoreFailure = 120
    }

    internal readonly struct CameraRigPresentationRestoreResult
    {
        internal CameraRigPresentationRestoreResult(
            CameraRigPresentationRestoreStatus status,
            string diagnostic)
        {
            Status = status;
            Diagnostic = diagnostic.NormalizeText();
        }

        internal CameraRigPresentationRestoreStatus Status { get; }
        internal string Diagnostic { get; }
        internal bool Succeeded => Status == CameraRigPresentationRestoreStatus.Succeeded;
    }
}
