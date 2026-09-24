using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal immutable diagnostic result for runtime scope root registry operations.
    /// It records what happened without exposing a global registry or executing materialization/release side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8F internal runtime root registry operation result; includes logical root lifecycle diagnostics, no materialization or release execution.")]
    internal sealed class RuntimeRootRegistryOperationResult
    {
        private RuntimeRootRegistryOperationResult(
            RuntimeContentOwner owner,
            RuntimeContentIdentity identity,
            bool hasIdentity,
            RuntimeScopeRoot root,
            RuntimeContentHandle handle,
            RuntimeRootRegistryOperationStatus status,
            string source,
            string reason,
            string message)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime root registry operation owner must be valid.", nameof(owner));
            }

            if (hasIdentity && !identity.IsValid)
            {
                throw new ArgumentException("Runtime root registry operation identity must be valid when present.", nameof(identity));
            }

            if (!Enum.IsDefined(typeof(RuntimeRootRegistryOperationStatus), status)
                || status == RuntimeRootRegistryOperationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Runtime root registry operation status must be explicit.");
            }

            Owner = owner;
            Identity = identity;
            HasIdentity = hasIdentity;
            Root = root;
            Handle = handle;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public RuntimeContentOwner Owner { get; }

        public RuntimeContentScope Scope => Owner.Scope;

        public RuntimeContentIdentity Identity { get; }

        public bool HasIdentity { get; }

        public RuntimeContentId ContentId => HasIdentity ? Identity.ContentId : default(RuntimeContentId);

        public RuntimeScopeRoot Root { get; }

        public RuntimeContentHandle Handle { get; }

        public RuntimeRootRegistryOperationStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Applied => Status is RuntimeRootRegistryOperationStatus.RootCreated or RuntimeRootRegistryOperationStatus.RootRemoved or RuntimeRootRegistryOperationStatus.HandleRegistered or RuntimeRootRegistryOperationStatus.HandleUnregistered;

        public bool Ignored => Status is RuntimeRootRegistryOperationStatus.RootAlreadyExists or RuntimeRootRegistryOperationStatus.RootMissing or RuntimeRootRegistryOperationStatus.HandleAlreadyRegistered or RuntimeRootRegistryOperationStatus.HandleMissing;

        public bool Rejected => Status is RuntimeRootRegistryOperationStatus.RejectedMissingRoot or RuntimeRootRegistryOperationStatus.RejectedMismatchedOwner or RuntimeRootRegistryOperationStatus.RejectedDuplicateHandle or RuntimeRootRegistryOperationStatus.RejectedRootHasHandles;

        public string ToDiagnosticString()
        {
            string identityText = HasIdentity ? Identity.StableText : "<none>";
            string contentIdText = HasIdentity ? ContentId.StableText : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();

            return $"owner='{Owner.StableText}' scope='{Scope}' identity='{identityText}' contentId='{contentIdText}' status='{Status}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static RuntimeRootRegistryOperationResult RootCreated(
            RuntimeContentOwner owner,
            RuntimeScopeRoot root,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                owner,
                default(RuntimeContentIdentity),
                false,
                root,
                null,
                RuntimeRootRegistryOperationStatus.RootCreated,
                source,
                reason,
                "Runtime scope root created.");
        }

        public static RuntimeRootRegistryOperationResult RootAlreadyExists(
            RuntimeContentOwner owner,
            RuntimeScopeRoot root,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                owner,
                default(RuntimeContentIdentity),
                false,
                root,
                null,
                RuntimeRootRegistryOperationStatus.RootAlreadyExists,
                source,
                reason,
                "Runtime scope root already exists.");
        }


        public static RuntimeRootRegistryOperationResult RootRemoved(
            RuntimeContentOwner owner,
            RuntimeScopeRoot root,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                owner,
                default(RuntimeContentIdentity),
                false,
                root,
                null,
                RuntimeRootRegistryOperationStatus.RootRemoved,
                source,
                reason,
                "Runtime scope root removed.");
        }

        public static RuntimeRootRegistryOperationResult RootMissing(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                owner,
                default(RuntimeContentIdentity),
                false,
                null,
                null,
                RuntimeRootRegistryOperationStatus.RootMissing,
                source,
                reason,
                "Runtime scope root was already absent.");
        }

        public static RuntimeRootRegistryOperationResult RejectedRootHasHandles(
            RuntimeScopeRoot root,
            string source,
            string reason)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            return new RuntimeRootRegistryOperationResult(
                root.Owner,
                default(RuntimeContentIdentity),
                false,
                root,
                null,
                RuntimeRootRegistryOperationStatus.RejectedRootHasHandles,
                source,
                reason,
                "Runtime scope root still has registered handles. Release or unregister handles before removing the root.");
        }

        public static RuntimeRootRegistryOperationResult HandleRegistered(
            RuntimeScopeRoot root,
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            return ForHandle(
                root,
                handle,
                RuntimeRootRegistryOperationStatus.HandleRegistered,
                source,
                reason,
                "Runtime content handle registered in scope root.");
        }

        public static RuntimeRootRegistryOperationResult HandleAlreadyRegistered(
            RuntimeScopeRoot root,
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            return ForHandle(
                root,
                handle,
                RuntimeRootRegistryOperationStatus.HandleAlreadyRegistered,
                source,
                reason,
                "Runtime content handle is already registered in scope root.");
        }

        public static RuntimeRootRegistryOperationResult HandleUnregistered(
            RuntimeScopeRoot root,
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            return ForHandle(
                root,
                handle,
                RuntimeRootRegistryOperationStatus.HandleUnregistered,
                source,
                reason,
                "Runtime content handle unregistered from scope root.");
        }

        public static RuntimeRootRegistryOperationResult HandleMissing(
            RuntimeScopeRoot root,
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                root.Owner,
                identity,
                true,
                root,
                null,
                RuntimeRootRegistryOperationStatus.HandleMissing,
                source,
                reason,
                "Runtime content handle is not registered in scope root.");
        }

        public static RuntimeRootRegistryOperationResult RejectedMissingRoot(
            RuntimeContentOwner owner,
            RuntimeContentIdentity identity,
            bool hasIdentity,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                owner,
                identity,
                hasIdentity,
                null,
                null,
                RuntimeRootRegistryOperationStatus.RejectedMissingRoot,
                source,
                reason,
                "Runtime scope root is missing. Create the root explicitly before registering handles.");
        }

        public static RuntimeRootRegistryOperationResult RejectedMismatchedOwner(
            RuntimeScopeRoot root,
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            return new RuntimeRootRegistryOperationResult(
                root.Owner,
                identity,
                true,
                root,
                null,
                RuntimeRootRegistryOperationStatus.RejectedMismatchedOwner,
                source,
                reason,
                "Runtime content identity owner does not match the target scope root owner.");
        }

        public static RuntimeRootRegistryOperationResult RejectedDuplicateHandle(
            RuntimeScopeRoot root,
            RuntimeContentHandle existingHandle,
            string source,
            string reason)
        {
            return ForHandle(
                root,
                existingHandle,
                RuntimeRootRegistryOperationStatus.RejectedDuplicateHandle,
                source,
                reason,
                "Another runtime content handle with the same identity is already registered in this scope root.");
        }

        private static RuntimeRootRegistryOperationResult ForHandle(
            RuntimeScopeRoot root,
            RuntimeContentHandle handle,
            RuntimeRootRegistryOperationStatus status,
            string source,
            string reason,
            string message)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return new RuntimeRootRegistryOperationResult(
                root.Owner,
                handle.Identity,
                true,
                root,
                handle,
                status,
                source,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
