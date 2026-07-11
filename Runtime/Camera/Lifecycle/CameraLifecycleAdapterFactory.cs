using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    internal static class CameraLifecycleAdapterFactory
    {
        public static bool TryValidate(
            ICameraRequestPublisher publisher,
            CameraRequestOwnerKind expectedOwnerKind,
            CameraRequestLifetimeKind expectedLifetimeKind,
            string adapterName,
            out CameraLifecycleAdapterCreateResult blockedResult)
        {
            if (publisher == null)
            {
                blockedResult = Blocked(
                    "camera.lifecycle-adapter.publisher.missing",
                    $"{adapterName} requires an ICameraRequestPublisher.");
                return false;
            }

            CameraRequest request = publisher.Request;

            if (!request.IsValid)
            {
                blockedResult = Blocked(
                    "camera.lifecycle-adapter.request.invalid",
                    $"{adapterName} requires a valid publisher request.");
                return false;
            }

            if (request.Owner.Kind != expectedOwnerKind)
            {
                blockedResult = Blocked(
                    "camera.lifecycle-adapter.owner-kind.invalid",
                    $"{adapterName} requires owner kind '{expectedOwnerKind}', found '{request.Owner.Kind}'.");
                return false;
            }

            if (request.Lifetime.Kind != expectedLifetimeKind)
            {
                blockedResult = Blocked(
                    "camera.lifecycle-adapter.lifetime-kind.invalid",
                    $"{adapterName} requires lifetime kind '{expectedLifetimeKind}', found '{request.Lifetime.Kind}'.");
                return false;
            }

            if (!string.Equals(
                request.Owner.LogicalOwnerId,
                request.Lifetime.ScopeId,
                StringComparison.Ordinal))
            {
                blockedResult = Blocked(
                    "camera.lifecycle-adapter.identity-mismatch",
                    $"{adapterName} requires owner id '{request.Owner.LogicalOwnerId}' and lifetime scope '{request.Lifetime.ScopeId}' to match.");
                return false;
            }

            blockedResult = default;
            return true;
        }

        public static CameraLifecycleAdapterCreateResult Succeeded(
            ICameraLifecycleAdapter adapter,
            string summary)
        {
            return new CameraLifecycleAdapterCreateResult(
                adapter,
                Array.Empty<CameraIssue>(),
                summary);
        }

        private static CameraLifecycleAdapterCreateResult Blocked(
            string code,
            string message)
        {
            string normalized =
                message.NormalizeTextOrFallback(
                    "Camera lifecycle adapter creation was blocked.");

            return new CameraLifecycleAdapterCreateResult(
                null,
                new[]
                {
                    CameraIssue.Blocking(code, normalized)
                },
                normalized);
        }
    }
}
