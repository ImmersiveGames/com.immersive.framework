using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable camera intent submitted by Route, Activity, LocalPlayer or another typed owner.
    /// It does not admit itself, select a winner or mutate Cinemachine state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public readonly struct CameraRequest
    {
        internal CameraRequest(
            CameraRequestId requestId,
            CameraOutputId outputId,
            CameraRequestOwner owner,
            CameraRequestLifetime lifetime,
            CameraRigReference rig,
            CameraTargetSourceDescriptor targetSource,
            CameraRequestPolicy policy,
            CameraRequestReleaseCondition releaseCondition,
            string diagnosticSource,
            string diagnosticReason)
        {
            RequestId = requestId;
            OutputId = outputId;
            Owner = owner;
            Lifetime = lifetime;
            Rig = rig;
            TargetSource = targetSource;
            Policy = policy;
            ReleaseCondition = releaseCondition;
            DiagnosticSource = diagnosticSource.NormalizeText();
            DiagnosticReason = diagnosticReason.NormalizeText();
        }

        public CameraRequestId RequestId { get; }

        public CameraOutputId OutputId { get; }

        public CameraRequestOwner Owner { get; }

        public CameraRequestLifetime Lifetime { get; }

        public CameraRigReference Rig { get; }

        public CameraTargetSourceDescriptor TargetSource { get; }

        public CameraRequestPolicy Policy { get; }

        public CameraRequestReleaseCondition ReleaseCondition { get; }

        public string DiagnosticSource { get; }

        public string DiagnosticReason { get; }

        public bool IsValid =>
            RequestId.IsValid &&
            OutputId.IsValid &&
            Owner.IsValid &&
            Lifetime.IsValid &&
            Rig.IsValid &&
            !TargetSource.IsNone &&
            ReleaseCondition != CameraRequestReleaseCondition.Undefined &&
            !string.IsNullOrWhiteSpace(DiagnosticSource) &&
            !string.IsNullOrWhiteSpace(DiagnosticReason);
    }
}
