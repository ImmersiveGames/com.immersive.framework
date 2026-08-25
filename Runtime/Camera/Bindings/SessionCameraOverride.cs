using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored Session-scoped Camera override.
    ///
    /// Session ownership is expressed by the explicit Scope ID inherited from
    /// ScopedCameraOverride. The binding intentionally has no
    /// consumer-project asset reference, so it can live in reusable package
    /// Scene Templates.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Session Camera Override")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public sealed class SessionCameraOverride :
        ScopedCameraOverride
    {
        [SerializeField]
        private CameraOutputAuthoring persistentOutputSession;

        public CameraOutputAuthoring PersistentOutputSession =>
            persistentOutputSession;

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
            SetOutputSession(persistentOutputSession);

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
