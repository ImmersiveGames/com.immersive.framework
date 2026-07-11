using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed publisher identity for a camera request.
    /// </summary>
    public readonly struct CameraRequestOwner
    {
        public CameraRequestOwner(CameraRequestOwnerKind kind, string logicalOwnerId)
        {
            Kind = kind;
            LogicalOwnerId = logicalOwnerId.NormalizeText();
        }

        public CameraRequestOwnerKind Kind { get; }

        public string LogicalOwnerId { get; }

        public bool IsValid =>
            Kind != CameraRequestOwnerKind.Undefined &&
            !string.IsNullOrWhiteSpace(LogicalOwnerId);

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{LogicalOwnerId}" : "Undefined";
        }
    }
}
