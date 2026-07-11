using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    public abstract class ScopedCameraLifecycleAdapter :
        ICameraLifecycleAdapter
    {
        private readonly ICameraRequestPublisher publisher;
        private readonly string scopeId;
        private readonly string adapterName;

        private bool isEntered;

        protected ScopedCameraLifecycleAdapter(
            ICameraRequestPublisher publisher,
            string scopeId,
            string adapterName)
        {
            this.publisher = publisher;
            this.scopeId = scopeId.NormalizeText();
            this.adapterName = adapterName.NormalizeTextOrFallback(
                nameof(ScopedCameraLifecycleAdapter));
        }

        public string ScopeId => scopeId;
        public bool IsEntered => isEntered;

        public CameraLifecycleAdapterResult Enter(string enteredScopeId)
        {
            CameraLifecycleAdapterResult identityFailure =
                ValidateScope(enteredScopeId, "enter");

            if (identityFailure.IsRejected)
            {
                return identityFailure;
            }

            if (isEntered)
            {
                return Preserved(
                    $"Camera lifecycle adapter '{adapterName}' preserved entered state for scope '{scopeId}'.");
            }

            CameraRequestPublisherResult publisherResult =
                publisher.Publish();

            if (!publisherResult.Succeeded)
            {
                return Rejected(
                    enteredScopeId,
                    publisherResult,
                    $"Camera lifecycle adapter '{adapterName}' failed to enter scope '{scopeId}'.");
            }

            isEntered = true;

            return new CameraLifecycleAdapterResult(
                CameraLifecycleAdapterOperationKind.Entered,
                scopeId,
                true,
                publisherResult,
                Array.Empty<CameraIssue>(),
                $"Camera lifecycle adapter '{adapterName}' entered scope '{scopeId}'.");
        }

        public CameraLifecycleAdapterResult Exit(string exitedScopeId)
        {
            CameraLifecycleAdapterResult identityFailure =
                ValidateScope(exitedScopeId, "exit");

            if (identityFailure.IsRejected)
            {
                return identityFailure;
            }

            if (!isEntered)
            {
                return Preserved(
                    $"Camera lifecycle adapter '{adapterName}' preserved exited state for scope '{scopeId}'.");
            }

            CameraRequestPublisherResult publisherResult =
                publisher.Release();

            if (!publisherResult.Succeeded)
            {
                return Rejected(
                    exitedScopeId,
                    publisherResult,
                    $"Camera lifecycle adapter '{adapterName}' failed to exit scope '{scopeId}'.");
            }

            isEntered = false;

            return new CameraLifecycleAdapterResult(
                CameraLifecycleAdapterOperationKind.Exited,
                scopeId,
                true,
                publisherResult,
                Array.Empty<CameraIssue>(),
                $"Camera lifecycle adapter '{adapterName}' exited scope '{scopeId}'.");
        }

        private CameraLifecycleAdapterResult ValidateScope(
            string receivedScopeId,
            string operation)
        {
            string normalized = receivedScopeId.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Rejected(
                    normalized,
                    "camera.lifecycle-adapter.scope.missing",
                    $"Camera lifecycle adapter '{adapterName}' requires a scope id for {operation}.");
            }

            if (!string.Equals(
                normalized,
                scopeId,
                StringComparison.Ordinal))
            {
                return Rejected(
                    normalized,
                    "camera.lifecycle-adapter.scope-mismatch",
                    $"Camera lifecycle adapter '{adapterName}' expected scope '{scopeId}' but received '{normalized}' for {operation}.");
            }

            return default;
        }

        private CameraLifecycleAdapterResult Preserved(string summary)
        {
            return new CameraLifecycleAdapterResult(
                CameraLifecycleAdapterOperationKind.Preserved,
                scopeId,
                false,
                default,
                Array.Empty<CameraIssue>(),
                summary);
        }

        private static CameraLifecycleAdapterResult Rejected(
            string receivedScopeId,
            string code,
            string message)
        {
            CameraIssue issue =
                CameraIssue.Blocking(code, message);

            return new CameraLifecycleAdapterResult(
                CameraLifecycleAdapterOperationKind.Rejected,
                receivedScopeId,
                false,
                default,
                new[] { issue },
                issue.Message);
        }

        private static CameraLifecycleAdapterResult Rejected(
            string receivedScopeId,
            CameraRequestPublisherResult publisherResult,
            string summary)
        {
            return new CameraLifecycleAdapterResult(
                CameraLifecycleAdapterOperationKind.Rejected,
                receivedScopeId,
                true,
                publisherResult,
                publisherResult.Issues,
                $"{summary} {publisherResult.DiagnosticSummary}".NormalizeText());
        }
    }
}
