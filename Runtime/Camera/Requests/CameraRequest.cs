using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable camera intent submitted by Route, Activity, Session or another explicit typed owner.
    /// It does not admit itself, select a winner or mutate Cinemachine state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequest
    {
        internal CameraRequest(
            CameraRequestId requestId,
            CameraOutputId outputId,
            CameraRequestOwner owner,
            CameraRequestLifetime lifetime,
            CameraRigReference rig,
            CameraRequestPolicy policy,
            CameraPresentationTransitionMode presentationTransitionMode,
            CameraRequestReleaseCondition releaseCondition,
            string diagnosticSource,
            string diagnosticReason)
        {
            RequestId = requestId;
            OutputId = outputId;
            Owner = owner;
            Lifetime = lifetime;
            Rig = rig;
            Policy = policy;
            PresentationTransitionMode = presentationTransitionMode;
            ReleaseCondition = releaseCondition;
            DiagnosticSource = diagnosticSource.NormalizeText();
            DiagnosticReason = diagnosticReason.NormalizeText();
        }

        public CameraRequestId RequestId { get; }

        public CameraOutputId OutputId { get; }

        public CameraRequestOwner Owner { get; }

        public CameraRequestLifetime Lifetime { get; }

        public CameraRigReference Rig { get; }


        public CameraRequestPolicy Policy { get; }

        public CameraPresentationTransitionMode PresentationTransitionMode { get; }

        public CameraRequestReleaseCondition ReleaseCondition { get; }

        public string DiagnosticSource { get; }

        public string DiagnosticReason { get; }

        public bool IsValid =>
            RequestId.IsValid &&
            OutputId.IsValid &&
            Owner.IsValid &&
            Lifetime.IsValid &&
            Rig.IsValid &&
            (PresentationTransitionMode ==
                 CameraPresentationTransitionMode.Blend ||
             PresentationTransitionMode ==
                 CameraPresentationTransitionMode.Cut) &&
            ReleaseCondition != CameraRequestReleaseCondition.Undefined &&
            !string.IsNullOrWhiteSpace(DiagnosticSource) &&
            !string.IsNullOrWhiteSpace(DiagnosticReason);
    }
}
