using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Immutable release plan entry for one known content handle.
    /// The entry declares release intent only; it does not execute side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release plan entry; no physical release side effects.")]
    internal readonly struct ContentReleasePlanEntry
    {
        public ContentReleasePlanEntry(
            FrameworkContentHandle handle,
            ContentReleaseOwnership ownership,
            ContentReleaseAction action,
            string message)
        {
            if (!handle.HasContentId)
            {
                throw new ArgumentException("Content release plan entry requires a valid content handle.", nameof(handle));
            }

            if (!Enum.IsDefined(typeof(ContentReleaseOwnership), ownership))
            {
                throw new ArgumentOutOfRangeException(nameof(ownership), ownership, "Release ownership must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ContentReleaseAction), action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Release action must be explicit.");
            }

            Handle = handle;
            Ownership = ownership;
            Action = action;
            Message = Normalize(message);
        }

        public FrameworkContentHandle Handle { get; }

        public ContentReleaseOwnership Ownership { get; }

        public ContentReleaseAction Action { get; }

        public string Message { get; }

        public FrameworkContentIdentity Identity => Handle.Identity;

        public string ContentId => Handle.ContentId;

        public FrameworkContentScope Scope => Handle.Scope;

        public FrameworkContentKind Kind => Handle.Kind;

        public FrameworkContentRequiredness Requiredness => Handle.Requiredness;

        public string OwnerId => Handle.OwnerId;

        public string OwnerName => Handle.OwnerName;

        public string ResourceName => Handle.ResourceName;

        public string ResourcePath => Handle.ResourcePath;

        public bool IsOwned => Ownership == ContentReleaseOwnership.Owned;

        public bool HasReleaseAction => Action != ContentReleaseAction.None;

        public bool IsReleasable => IsOwned && HasReleaseAction;

        public string ToDiagnosticString()
        {
            string message = Message.ToDiagnosticText();
            return $"action='{Action}' ownership='{Ownership}' releasable='{IsReleasable}' message='{message}' {Handle.ToDiagnosticString()}";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
