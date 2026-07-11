using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    public readonly struct CameraLifecycleAdapterCreateResult
    {
        internal CameraLifecycleAdapterCreateResult(
            ICameraLifecycleAdapter adapter,
            CameraIssue[] issues,
            string diagnosticSummary)
        {
            Adapter = adapter;
            Issues = issues ?? Array.Empty<CameraIssue>();
            DiagnosticSummary = diagnosticSummary.NormalizeText();
        }

        public ICameraLifecycleAdapter Adapter { get; }
        public CameraIssue[] Issues { get; }
        public string DiagnosticSummary { get; }

        public bool Succeeded => Adapter != null;
        public bool IsBlocked => Adapter == null;
    }
}
