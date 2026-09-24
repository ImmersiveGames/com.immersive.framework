using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public readonly struct CameraOutputApplyResult
    {
        internal CameraOutputApplyResult(
            CameraOutputApplyKind kind,
            CameraRequest request,
            CinemachineCamera previousCamera,
            CinemachineCamera currentCamera,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            Kind = kind;
            Request = request;
            PreviousCamera = previousCamera;
            CurrentCamera = currentCamera;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public CameraOutputApplyKind Kind { get; }
        public CameraRequest Request { get; }
        public CinemachineCamera PreviousCamera { get; }
        public CinemachineCamera CurrentCamera { get; }
        public CameraIssue[] Issues { get; }
        public string DiagnosticSummary { get; }

        public bool Succeeded =>
            Kind == CameraOutputApplyKind.Applied ||
            Kind == CameraOutputApplyKind.Cleared ||
            Kind == CameraOutputApplyKind.Preserved;

        public bool IsBlocked => Kind == CameraOutputApplyKind.Blocked;
    }
}
