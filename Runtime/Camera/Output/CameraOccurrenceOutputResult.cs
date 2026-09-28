using Unity.Cinemachine;

namespace Immersive.Framework.Camera
{
    internal enum CameraOccurrenceOutputStatus
    {
        Rejected = 0,
        Applied = 1,
        Preserved = 2
    }

    internal readonly struct CameraOccurrenceOutputResult
    {
        private CameraOccurrenceOutputResult(
            CameraOccurrenceOutputStatus status,
            CinemachineCamera previousCamera,
            CinemachineCamera currentCamera,
            string diagnostic)
        {
            Status = status;
            PreviousCamera = previousCamera;
            CurrentCamera = currentCamera;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal CameraOccurrenceOutputStatus Status { get; }
        internal CinemachineCamera PreviousCamera { get; }
        internal CinemachineCamera CurrentCamera { get; }
        internal string Diagnostic { get; }
        internal bool Succeeded => Status == CameraOccurrenceOutputStatus.Applied ||
            Status == CameraOccurrenceOutputStatus.Preserved;

        internal static CameraOccurrenceOutputResult Applied(
            CinemachineCamera previous,
            CinemachineCamera current) => new CameraOccurrenceOutputResult(
                CameraOccurrenceOutputStatus.Applied,
                previous,
                current,
                string.Empty);

        internal static CameraOccurrenceOutputResult Preserved(
            CinemachineCamera current) => new CameraOccurrenceOutputResult(
                CameraOccurrenceOutputStatus.Preserved,
                current,
                current,
                string.Empty);

        internal static CameraOccurrenceOutputResult Rejected(
            CinemachineCamera current,
            string diagnostic) => new CameraOccurrenceOutputResult(
                CameraOccurrenceOutputStatus.Rejected,
                current,
                current,
                diagnostic);
    }
}
