using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Unity.Cinemachine;

namespace Immersive.Framework.CameraAuthoring
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-E physical Rig presentation result.")]
    public sealed class CameraRigPresentationApplyResult
    {
        internal CameraRigPresentationApplyResult(
            CameraRigPresentationApplyStatus status,
            CameraCompositionPresentationInput input,
            CinemachineCamera cinemachineCamera,
            CinemachineTargetGroup targetGroup,
            CinemachineGroupFraming groupFraming,
            string diagnostic)
        {
            Status = status;
            Input = input;
            CinemachineCamera = cinemachineCamera;
            TargetGroup = targetGroup;
            GroupFraming = groupFraming;
            Diagnostic = diagnostic.NormalizeText();
        }

        public CameraRigPresentationApplyStatus Status { get; }
        public CameraCompositionPresentationInput Input { get; }
        public CinemachineCamera CinemachineCamera { get; }
        public CinemachineTargetGroup TargetGroup { get; }
        public CinemachineGroupFraming GroupFraming { get; }
        public int MemberCount =>
            TargetGroup != null && TargetGroup.Targets != null
                ? TargetGroup.Targets.Count
                : 0;
        public string Diagnostic { get; }
        public bool Succeeded => Status is
            CameraRigPresentationApplyStatus.SucceededFixedNoTargets or
            CameraRigPresentationApplyStatus.SucceededSingleSubject or
            CameraRigPresentationApplyStatus.SucceededGroup or
            CameraRigPresentationApplyStatus.SucceededCleared;
    }
}
