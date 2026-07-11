using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed lifetime evidence carried by a camera request.
    /// </summary>
    public readonly struct CameraRequestLifetime
    {
        public CameraRequestLifetime(CameraRequestLifetimeKind kind, string scopeId)
        {
            Kind = kind;
            ScopeId = scopeId.NormalizeText();
        }

        public CameraRequestLifetimeKind Kind { get; }

        public string ScopeId { get; }

        public bool IsValid =>
            Kind != CameraRequestLifetimeKind.Undefined &&
            !string.IsNullOrWhiteSpace(ScopeId);

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{ScopeId}" : "Undefined";
        }
    }
}
