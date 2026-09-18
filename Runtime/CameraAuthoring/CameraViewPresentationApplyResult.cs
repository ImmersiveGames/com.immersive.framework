using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Unity.Cinemachine;

namespace Immersive.Framework.CameraAuthoring
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-D physical View-to-Cinemachine presentation result.")]
    public sealed class CameraViewPresentationApplyResult
    {
        internal CameraViewPresentationApplyResult(
            CameraViewPresentationApplyStatus status,
            CameraViewPresentationInput input,
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

        public CameraViewPresentationApplyStatus Status { get; }
        public CameraViewPresentationInput Input { get; }
        public CinemachineCamera CinemachineCamera { get; }
        public CinemachineTargetGroup TargetGroup { get; }
        public CinemachineGroupFraming GroupFraming { get; }
        public int MemberCount =>
            TargetGroup != null && TargetGroup.Targets != null
                ? TargetGroup.Targets.Count
                : 0;
        public string Diagnostic { get; }
        public bool Succeeded => Status is
            CameraViewPresentationApplyStatus.SucceededFixedNoTargets or
            CameraViewPresentationApplyStatus.SucceededSingleSubject or
            CameraViewPresentationApplyStatus.SucceededGroup or
            CameraViewPresentationApplyStatus.SucceededCleared;
    }
}
