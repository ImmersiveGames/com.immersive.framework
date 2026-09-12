using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored Session-scoped Camera override.
    ///
    /// Session ownership is expressed by the explicit Scope ID inherited from
    /// ScopedCameraOverride. Output identity comes from the exact authored
    /// CameraOutputDefinition; physical output binding remains scene-owned.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Session Camera Override")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Session request bound to one explicit Camera Output ID.")]
    public sealed class SessionCameraOverride :
        ScopedCameraOverride
    {
        protected override CameraRequestOwnerKind OwnerKind =>
            CameraRequestOwnerKind.Session;

        protected override CameraRequestLifetimeKind LifetimeKind =>
            CameraRequestLifetimeKind.Session;

        protected override string OwnerDiagnosticName =>
            !string.IsNullOrWhiteSpace(ScopeId)
                ? ScopeId
                : "<session>";

        private void Reset()
        {
            EnsureMissingAuthoringIds();
        }

        private void OnEnable()
        {
            SetOwnerActive(
                $"Session camera override is available. " +
                $"scope='{OwnerDiagnosticName}'.");
        }

        protected override void OnDisable()
        {
            EndOwnerScope("SessionBindingDisabled");
        }

        protected override void OnDestroy()
        {
            EndOwnerScope("SessionBindingDestroyed");
        }

        protected override bool TryValidateOwner(
            out string diagnostic)
        {
            // The base configuration validates the explicit Session Scope ID.
            diagnostic = string.Empty;
            return true;
        }

        protected override CameraRequestPublisherCreateResult
            CreatePublisher(
                CameraOutputSession session,
                CameraRequest request)
        {
            return SessionCameraRequestPublisher.Create(
                session,
                request);
        }
    }
}
