using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public readonly struct CameraRequestPublisherCreateResult
    {
        internal CameraRequestPublisherCreateResult(
            ICameraRequestPublisher publisher,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            Publisher = publisher;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public ICameraRequestPublisher Publisher { get; }
        public CameraIssue[] Issues { get; }
        public string DiagnosticSummary { get; }

        public bool Succeeded => Publisher != null;
        public bool IsBlocked => Publisher == null;
    }
}
