using Immersive.Framework.Authoring;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Session Camera Override")]
    public sealed class SessionCameraOverrideBinding :
        ScopedCameraOverrideBinding
    {
        [SerializeField]
        private GameApplicationAsset assignedGameApplication;

        [SerializeField]
        private CameraOutputSessionBinding persistentOutputSession;

        public GameApplicationAsset AssignedGameApplication =>
            assignedGameApplication;

        public CameraOutputSessionBinding PersistentOutputSession =>
            persistentOutputSession;

        protected override CameraRequestOwnerKind OwnerKind =>
            CameraRequestOwnerKind.Session;

        protected override CameraRequestLifetimeKind LifetimeKind =>
            CameraRequestLifetimeKind.Session;

        protected override string OwnerDiagnosticName =>
            assignedGameApplication != null
                ? assignedGameApplication.ApplicationName
                : "<missing-session>";

        private void OnEnable()
        {
            SetOutputSession(persistentOutputSession);

            SetOwnerActive(
                $"Session camera override is available. " +
                $"application='{OwnerDiagnosticName}'.");
        }

        private void OnDisable()
        {
            EndOwnerScope("SessionBindingDisabled");
        }

        protected override bool TryValidateOwner(
            out string diagnostic)
        {
            if (assignedGameApplication == null)
            {
                diagnostic =
                    "Session Camera Override requires an assigned " +
                    "GameApplicationAsset.";
                return false;
            }

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
