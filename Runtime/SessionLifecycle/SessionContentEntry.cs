using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.SessionLifecycle
{
    /// <summary>
    /// API status: Internal. One content handle known by Session scope with explicit ownership semantics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session content entry introduced by F2C; not game-facing API.")]
    internal readonly struct SessionContentEntry
    {
        public SessionContentEntry(
            FrameworkContentHandle handle,
            SessionContentOwnership ownership)
        {
            if (!handle.HasContentId)
            {
                throw new ArgumentException("Session content entry requires a valid content handle.", nameof(handle));
            }

            if (!Enum.IsDefined(typeof(SessionContentOwnership), ownership))
            {
                throw new ArgumentOutOfRangeException(nameof(ownership), ownership, "Session content ownership must be explicit.");
            }

            if (handle.Scope != FrameworkContentScope.Session)
            {
                throw new ArgumentException("Session content entry requires a handle with Session content scope.", nameof(handle));
            }

            Handle = handle;
            Ownership = ownership;
        }

        public FrameworkContentHandle Handle { get; }

        public SessionContentOwnership Ownership { get; }

        public FrameworkContentIdentity Identity => Handle.Identity;

        public bool IsOwned => Ownership == SessionContentOwnership.Owned;

        public bool IsDiagnosticOnly => Ownership == SessionContentOwnership.DiagnosticOnly;

        public string ToDiagnosticString()
        {
            return $"ownership='{Ownership}' {Handle.ToDiagnosticString()}";
        }
    }
}
