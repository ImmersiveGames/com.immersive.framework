using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-B deterministic Camera Presentation materialization result.")]
    internal readonly struct CameraPresentationMaterializationResult
    {
        internal CameraPresentationMaterializationResult(
            CameraPresentationMaterializationStatus status,
            CameraPresentationMaterializationHandle handle,
            string issue)
        {
            Status = status;
            Handle = handle;
            Issue = issue ?? string.Empty;
        }

        internal CameraPresentationMaterializationStatus Status { get; }

        internal CameraPresentationMaterializationHandle Handle { get; }

        internal string Issue { get; }

        internal bool Succeeded =>
            Status == CameraPresentationMaterializationStatus.Succeeded ||
            Status ==
                CameraPresentationMaterializationStatus.SucceededAlreadyReleased;

        internal static CameraPresentationMaterializationResult Success(
            CameraPresentationMaterializationHandle handle) =>
            new CameraPresentationMaterializationResult(
                CameraPresentationMaterializationStatus.Succeeded,
                handle,
                string.Empty);

        internal static CameraPresentationMaterializationResult AlreadyReleased(
            CameraPresentationMaterializationHandle handle) =>
            new CameraPresentationMaterializationResult(
                CameraPresentationMaterializationStatus.SucceededAlreadyReleased,
                handle,
                string.Empty);

        internal static CameraPresentationMaterializationResult Failure(
            CameraPresentationMaterializationStatus status,
            string issue,
            CameraPresentationMaterializationHandle handle = null) =>
            new CameraPresentationMaterializationResult(
                status,
                handle,
                issue);
    }
}
