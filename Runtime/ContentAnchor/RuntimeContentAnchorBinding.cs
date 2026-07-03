using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Internal logical runtime for Content Anchor binding correlation.
    /// It resolves explicit binding requests against a passive ContentAnchorSet and registered RuntimeContent handles;
    /// it does not move transforms, instantiate prefabs, unload scenes, perform physical release or create fallback anchors.
    /// F9E makes this runtime owned by FrameworkRuntimeHost through controlled internal methods; automatic Route/Activity cleanup remains deferred.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9E host-owned RuntimeContentAnchorBinding logical runtime; no physical placement or automatic lifecycle cleanup.")]
    internal sealed class RuntimeContentAnchorBinding
    {
        private readonly Dictionary<string, ContentAnchorContentHandle> _bindings;

        public RuntimeContentAnchorBinding()
        {
            _bindings = new Dictionary<string, ContentAnchorContentHandle>(StringComparer.Ordinal);
        }

        public int BindingCount => _bindings.Count;

        public bool HasBindings => BindingCount > 0;

        public ContentAnchorContentHandle[] SnapshotBindings()
        {
            if (_bindings.Count == 0)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            var results = new ContentAnchorContentHandle[_bindings.Count];
            _bindings.Values.CopyTo(results, 0);
            return results;
        }

        public bool TryGetBinding(
            ContentAnchorBindingRequest request,
            out ContentAnchorContentHandle handle)
        {
            ValidateRequest(request);

            if (_bindings.TryGetValue(CreateBindingKey(request), out handle) && handle.IsValid)
            {
                return true;
            }

            handle = default(ContentAnchorContentHandle);
            return false;
        }

        public ContentAnchorBindingResult Bind(
            ContentAnchorSet anchorSet,
            RuntimeContentRuntime runtimeContentRuntime,
            ContentAnchorBindingRequest request,
            string source,
            string reason)
        {
            if (runtimeContentRuntime == null)
            {
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
            }

            ValidateRequest(request);

            if (!anchorSet.TryGetByBindingRequest(request, out var anchor))
            {
                return ContentAnchorBindingResult.MissingAnchor(
                    request,
                    source,
                    reason,
                    "Content Anchor binding failed because the requested anchor was not present in the supplied ContentAnchorSet.");
            }

            if (!request.Matches(anchor))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedAnchor,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the resolved anchor did not match the request.");
            }

            if (!runtimeContentRuntime.TryGetHandle(request.RuntimeContext, request.RuntimeIdentity, out var runtimeHandle))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.FailedRuntimeRegistration,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the runtime content handle is not registered in the request scope root.");
            }

            if (runtimeHandle == null || runtimeHandle.Identity != request.RuntimeIdentity)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedRuntimeContent,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the runtime content handle does not match the request identity.");
            }

            if (runtimeHandle.IsReleased)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedReleasedRuntimeContent,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the runtime content handle is already released.");
            }

            string bindingKey = CreateBindingKey(request);
            if (_bindings.TryGetValue(bindingKey, out var existingHandle))
            {
                return EvaluateExistingBinding(
                    request,
                    anchor,
                    runtimeHandle,
                    existingHandle,
                    source,
                    reason);
            }

            var result = ContentAnchorBindingResult.Success(
                request,
                anchor,
                runtimeHandle,
                source,
                reason,
                "Content Anchor binding succeeded logically.");

            _bindings.Add(bindingKey, result.Handle);
            return result;
        }

        public bool Unbind(ContentAnchorBindingRequest request)
        {
            ValidateRequest(request);
            return _bindings.Remove(CreateBindingKey(request));
        }

        public bool Unbind(ContentAnchorContentHandle handle)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException("Content Anchor binding handle must be valid.", nameof(handle));
            }

            return _bindings.Remove(CreateBindingKey(handle.Request));
        }

        public ContentAnchorBindingLifecycleResult UnbindRuntimeContent(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            ValidateIdentity(identity);

            int before = BindingCount;
            RemoveBindingsWhere(handle => handle.Request.RuntimeIdentity == identity);
            return ContentAnchorBindingLifecycleResult.FromCounts(
                before,
                BindingCount,
                identity.Owner,
                identity,
                ContentAnchorScope.Unknown,
                default(FrameworkIdentityKey),
                ContentAnchorKind.Unknown,
                default(ContentAnchorId),
                "runtime-content",
                source,
                reason,
                "Content Anchor bindings were cleaned up for one runtime content identity.");
        }

        public ContentAnchorBindingLifecycleResult UnbindRuntimeOwner(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            int before = BindingCount;
            RemoveBindingsWhere(handle => handle.Request.RuntimeOwner == owner);
            return ContentAnchorBindingLifecycleResult.FromCounts(
                before,
                BindingCount,
                owner,
                default(RuntimeContentIdentity),
                ContentAnchorScope.Unknown,
                default(FrameworkIdentityKey),
                ContentAnchorKind.Unknown,
                default(ContentAnchorId),
                "runtime-owner",
                source,
                reason,
                "Content Anchor bindings were cleaned up for one runtime owner.");
        }

        public ContentAnchorBindingLifecycleResult UnbindRuntimeScope(
            RuntimeContentScope scope,
            string source,
            string reason)
        {
            ValidateRuntimeScope(scope);

            int before = BindingCount;
            RemoveBindingsWhere(handle => handle.Request.RuntimeScope == scope);
            return ContentAnchorBindingLifecycleResult.FromCounts(
                before,
                BindingCount,
                default(RuntimeContentOwner),
                default(RuntimeContentIdentity),
                ContentAnchorScope.Unknown,
                default(FrameworkIdentityKey),
                ContentAnchorKind.Unknown,
                default(ContentAnchorId),
                "runtime-scope",
                source,
                reason,
                "Content Anchor bindings were cleaned up for one runtime scope.");
        }

        public ContentAnchorBindingLifecycleResult UnbindAnchor(
            ContentAnchorDeclaration anchor,
            string source,
            string reason)
        {
            ValidateAnchor(anchor);

            int before = BindingCount;
            RemoveBindingsWhere(handle => handle.Request.Matches(anchor));
            return ContentAnchorBindingLifecycleResult.FromCounts(
                before,
                BindingCount,
                default(RuntimeContentOwner),
                default(RuntimeContentIdentity),
                anchor.Scope,
                anchor.Owner,
                anchor.Kind,
                anchor.AnchorId,
                "anchor",
                source,
                reason,
                "Content Anchor bindings were cleaned up for one anchor.");
        }

        public ContentAnchorBindingLifecycleResult UnbindAnchorOwner(
            ContentAnchorScope scope,
            FrameworkIdentityKey owner,
            string source,
            string reason)
        {
            ValidateAnchorOwner(scope, owner);

            int before = BindingCount;
            RemoveBindingsWhere(handle => handle.Request.AnchorScope == scope && handle.Request.AnchorOwner == owner);
            return ContentAnchorBindingLifecycleResult.FromCounts(
                before,
                BindingCount,
                default(RuntimeContentOwner),
                default(RuntimeContentIdentity),
                scope,
                owner,
                ContentAnchorKind.Unknown,
                default(ContentAnchorId),
                "anchor-owner",
                source,
                reason,
                "Content Anchor bindings were cleaned up for one anchor owner.");
        }

        public ContentAnchorBindingLifecycleResult UnbindAnchorScope(
            ContentAnchorScope scope,
            string source,
            string reason)
        {
            ValidateAnchorScope(scope);

            int before = BindingCount;
            RemoveBindingsWhere(handle => handle.Request.AnchorScope == scope);
            return ContentAnchorBindingLifecycleResult.FromCounts(
                before,
                BindingCount,
                default(RuntimeContentOwner),
                default(RuntimeContentIdentity),
                scope,
                default(FrameworkIdentityKey),
                ContentAnchorKind.Unknown,
                default(ContentAnchorId),
                "anchor-scope",
                source,
                reason,
                "Content Anchor bindings were cleaned up for one anchor scope.");
        }

        public ContentAnchorContentHandle[] SnapshotBindingsForRuntimeContent(RuntimeContentIdentity identity)
        {
            ValidateIdentity(identity);
            return SnapshotWhere(handle => handle.Request.RuntimeIdentity == identity);
        }

        public ContentAnchorContentHandle[] SnapshotBindingsForRuntimeOwner(RuntimeContentOwner owner)
        {
            ValidateOwner(owner);
            return SnapshotWhere(handle => handle.Request.RuntimeOwner == owner);
        }

        public ContentAnchorContentHandle[] SnapshotBindingsForRuntimeScope(RuntimeContentScope scope)
        {
            ValidateRuntimeScope(scope);
            return SnapshotWhere(handle => handle.Request.RuntimeScope == scope);
        }

        public ContentAnchorContentHandle[] SnapshotBindingsForAnchor(ContentAnchorDeclaration anchor)
        {
            ValidateAnchor(anchor);
            return SnapshotWhere(handle => handle.Request.Matches(anchor));
        }

        public ContentAnchorContentHandle[] SnapshotBindingsForAnchorOwner(ContentAnchorScope scope, FrameworkIdentityKey owner)
        {
            ValidateAnchorOwner(scope, owner);
            return SnapshotWhere(handle => handle.Request.AnchorScope == scope && handle.Request.AnchorOwner == owner);
        }

        public ContentAnchorContentHandle[] SnapshotBindingsForAnchorScope(ContentAnchorScope scope)
        {
            ValidateAnchorScope(scope);
            return SnapshotWhere(handle => handle.Request.AnchorScope == scope);
        }

        public int RemoveBindingsForRuntimeContent(RuntimeContentIdentity identity)
        {
            ValidateIdentity(identity);

            if (_bindings.Count == 0)
            {
                return 0;
            }

            var keys = new List<string>();
            foreach (KeyValuePair<string, ContentAnchorContentHandle> pair in _bindings)
            {
                if (pair.Value.RuntimeIdentity == identity)
                {
                    keys.Add(pair.Key);
                }
            }

            return RemoveKeys(keys);
        }

        public int RemoveBindingsForRuntimeOwner(RuntimeContentOwner owner)
        {
            ValidateOwner(owner);

            if (_bindings.Count == 0)
            {
                return 0;
            }

            var keys = new List<string>();
            foreach (KeyValuePair<string, ContentAnchorContentHandle> pair in _bindings)
            {
                if (pair.Value.RuntimeOwner == owner)
                {
                    keys.Add(pair.Key);
                }
            }

            return RemoveKeys(keys);
        }

        public string ToDiagnosticString()
        {
            if (_bindings.Count == 0)
            {
                return "contentAnchorBindings='0'";
            }

            var builder = new StringBuilder();
            builder.Append("contentAnchorBindings='");
            builder.Append(_bindings.Count);
            builder.Append("'");

            foreach (var binding in _bindings.Values)
            {
                builder.Append(" [");
                builder.Append(binding.ToDiagnosticString());
                builder.Append(']');
            }

            return builder.ToString();
        }

        private static ContentAnchorBindingResult EvaluateExistingBinding(
            ContentAnchorBindingRequest request,
            ContentAnchorDeclaration anchor,
            RuntimeContentHandle runtimeHandle,
            ContentAnchorContentHandle existingHandle,
            string source,
            string reason)
        {
            if (!existingHandle.IsValid)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.FailedRuntimeRegistration,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists, but the stored binding handle is no longer valid.");
            }

            if (!request.Matches(existingHandle.Anchor))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedAnchor,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists for this key, but its stored anchor does not match the request.");
            }

            if (existingHandle.RuntimeIdentity != request.RuntimeIdentity)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedRuntimeContent,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists for this key, but its runtime identity does not match the request.");
            }

            if (!ReferenceEquals(existingHandle.RuntimeHandle, runtimeHandle))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.FailedRuntimeRegistration,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists, but its stored runtime handle is not the registered handle for the request identity.");
            }

            return ContentAnchorBindingResult.AlreadyBound(
                request,
                anchor,
                existingHandle,
                source,
                reason,
                "Content Anchor binding already exists and remains valid.");
        }

        private int RemoveBindingsWhere(Func<ContentAnchorContentHandle, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (_bindings.Count == 0)
            {
                return 0;
            }

            var keys = new List<string>();
            foreach (KeyValuePair<string, ContentAnchorContentHandle> pair in _bindings)
            {
                if (predicate(pair.Value))
                {
                    keys.Add(pair.Key);
                }
            }

            return RemoveKeys(keys);
        }

        private ContentAnchorContentHandle[] SnapshotWhere(Func<ContentAnchorContentHandle, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (_bindings.Count == 0)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            var results = new List<ContentAnchorContentHandle>();
            foreach (var binding in _bindings.Values)
            {
                if (predicate(binding))
                {
                    results.Add(binding);
                }
            }

            return results.Count == 0 ? Array.Empty<ContentAnchorContentHandle>() : results.ToArray();
        }

        private int RemoveKeys(List<string> keys)
        {
            int removed = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                if (_bindings.Remove(keys[i]))
                {
                    removed++;
                }
            }

            return removed;
        }

        private static string CreateBindingKey(ContentAnchorBindingRequest request)
        {
            return $"{request.AnchorStableText}|{request.RuntimeIdentity.StableText}";
        }

        private static void ValidateRequest(ContentAnchorBindingRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request must be valid.", nameof(request));
            }
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateRuntimeScope(RuntimeContentScope scope)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentScope), scope) || scope == RuntimeContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Runtime content scope must be explicit.");
            }
        }

        private static void ValidateAnchor(ContentAnchorDeclaration anchor)
        {
            if (!anchor.IsValid)
            {
                throw new ArgumentException("Content Anchor declaration must be valid.", nameof(anchor));
            }
        }

        private static void ValidateAnchorOwner(ContentAnchorScope scope, FrameworkIdentityKey owner)
        {
            ValidateAnchorScope(scope);

            if (!owner.IsValid)
            {
                throw new ArgumentException("Content Anchor owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateAnchorScope(ContentAnchorScope scope)
        {
            if (!Enum.IsDefined(typeof(ContentAnchorScope), scope) || scope == ContentAnchorScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Content Anchor scope must be explicit.");
            }
        }
    }
}
