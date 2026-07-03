using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// API status: Internal. One content handle known by Route scope with explicit ownership semantics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route content entry introduced by F3E; not game-facing API.")]
    internal readonly struct RouteContentEntry
    {
        public RouteContentEntry(
            FrameworkContentHandle handle,
            RouteContentOwnership ownership)
        {
            if (!handle.HasContentId)
            {
                throw new ArgumentException("Route content entry requires a valid content handle.", nameof(handle));
            }

            if (!Enum.IsDefined(typeof(RouteContentOwnership), ownership))
            {
                throw new ArgumentOutOfRangeException(nameof(ownership), ownership, "Route content ownership must be explicit.");
            }

            if (handle.Scope != FrameworkContentScope.Route)
            {
                throw new ArgumentException("Route content entry requires a handle with Route content scope.", nameof(handle));
            }

            Handle = handle;
            Ownership = ownership;
        }

        public FrameworkContentHandle Handle { get; }

        public RouteContentOwnership Ownership { get; }

        public FrameworkContentIdentity Identity => Handle.Identity;

        public bool IsOwned => Ownership == RouteContentOwnership.Owned;

        public bool IsDiagnosticOnly => Ownership == RouteContentOwnership.DiagnosticOnly;

        public string ToDiagnosticString()
        {
            return $"ownership='{Ownership}' {Handle.ToDiagnosticString()}";
        }
    }
}
