using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal scoped registry for logical runtime roots in the current framework runtime.
    /// It is not a service locator and does not create hierarchy objects, materialize prefabs, destroy objects or bind Content Anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8F internal minimal runtime root registry; explicit root lifecycle only, no GameObject.Find or fallback creation.")]
    internal sealed class RuntimeRootRegistry
    {
        private readonly Dictionary<RuntimeContentOwner, RuntimeScopeRoot> _roots = new Dictionary<RuntimeContentOwner, RuntimeScopeRoot>();

        public int RootCount => _roots.Count;

        public bool HasRoots => _roots.Count > 0;

        public RuntimeScopeRoot[] SnapshotRoots()
        {
            var snapshot = new RuntimeScopeRoot[_roots.Count];
            _roots.Values.CopyTo(snapshot, 0);
            return snapshot;
        }

        public RuntimeRootRegistryOperationResult CreateRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            if (_roots.TryGetValue(owner, out var existingRoot))
            {
                return RuntimeRootRegistryOperationResult.RootAlreadyExists(
                    owner,
                    existingRoot,
                    source,
                    reason);
            }

            var root = new RuntimeScopeRoot(owner, source, reason);
            _roots.Add(owner, root);

            return RuntimeRootRegistryOperationResult.RootCreated(
                owner,
                root,
                source,
                reason);
        }

        public bool TryGetRoot(RuntimeContentOwner owner, out RuntimeScopeRoot root)
        {
            ValidateOwner(owner);
            return _roots.TryGetValue(owner, out root);
        }
        public RuntimeRootRegistryOperationResult RemoveRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            if (!_roots.TryGetValue(owner, out var root))
            {
                return RuntimeRootRegistryOperationResult.RootMissing(owner, source, reason);
            }

            if (root.HasHandles)
            {
                return RuntimeRootRegistryOperationResult.RejectedRootHasHandles(root, source, reason);
            }

            _roots.Remove(owner);
            return RuntimeRootRegistryOperationResult.RootRemoved(owner, root, source, reason);
        }


        public RuntimeRootRegistryOperationResult RegisterHandle(
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (!_roots.TryGetValue(handle.Owner, out var root))
            {
                return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                    handle.Owner,
                    handle.Identity,
                    true,
                    source,
                    reason);
            }

            return root.RegisterHandle(handle, source, reason);
        }

        public RuntimeRootRegistryOperationResult UnregisterHandle(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            ValidateIdentity(identity);

            if (!_roots.TryGetValue(identity.Owner, out var root))
            {
                return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                    identity.Owner,
                    identity,
                    true,
                    source,
                    reason);
            }

            return root.UnregisterHandle(identity, source, reason);
        }

        public bool TryGetHandle(RuntimeContentIdentity identity, out RuntimeContentHandle handle)
        {
            ValidateIdentity(identity);

            if (!_roots.TryGetValue(identity.Owner, out var root))
            {
                handle = null;
                return false;
            }

            return root.TryGetHandle(identity, out handle);
        }

        public RuntimeContentHandle[] SnapshotHandles(RuntimeContentOwner owner)
        {
            ValidateOwner(owner);

            if (!_roots.TryGetValue(owner, out var root))
            {
                return new RuntimeContentHandle[0];
            }

            return root.SnapshotHandles();
        }

        public RuntimeScopeRoot[] SnapshotRoots(RuntimeContentScope scope)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentScope), scope) || scope == RuntimeContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Runtime content scope must be explicit.");
            }

            var scopedRoots = new List<RuntimeScopeRoot>();
            foreach (var root in _roots.Values)
            {
                if (root.Scope == scope)
                {
                    scopedRoots.Add(root);
                }
            }

            return scopedRoots.ToArray();
        }

        public string ToDiagnosticString()
        {
            return $"rootCount='{RootCount}'";
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }
    }
}
