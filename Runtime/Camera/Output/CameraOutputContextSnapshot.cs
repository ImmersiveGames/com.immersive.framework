using System;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Immutable diagnostic snapshot of one output-scoped camera authority.
    /// </summary>
    public readonly struct CameraOutputContextSnapshot
    {
        internal CameraOutputContextSnapshot(
            CameraOutputId outputId,
            int admittedRequestCount,
            bool hasWinner,
            CameraRequest winner,
            CameraRequestId[] admittedRequestIds)
        {
            OutputId = outputId;
            AdmittedRequestCount = admittedRequestCount;
            HasWinner = hasWinner;
            Winner = winner;
            AdmittedRequestIds = admittedRequestIds ?? Array.Empty<CameraRequestId>();
        }

        public CameraOutputId OutputId { get; }

        public int AdmittedRequestCount { get; }

        public bool HasWinner { get; }

        public CameraRequest Winner { get; }

        public CameraRequestId[] AdmittedRequestIds { get; }
    }
}
