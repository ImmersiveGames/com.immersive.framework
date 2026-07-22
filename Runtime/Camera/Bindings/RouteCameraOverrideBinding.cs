using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Route Camera Override")]
    public sealed class RouteCameraOverrideBinding : ScopedCameraOverrideBinding, IRouteContentLifecycleReceiver
    {
        [SerializeField] private RouteAsset assignedRoute;
        public RouteAsset AssignedRoute => assignedRoute;

        protected override CameraRequestOwnerKind OwnerKind => CameraRequestOwnerKind.Route;
        protected override CameraRequestLifetimeKind LifetimeKind => CameraRequestLifetimeKind.Route;
        protected override string OwnerDiagnosticName => assignedRoute != null ? assignedRoute.RouteName : "<missing-route>";

        void IRouteContentLifecycleReceiver.OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (!TryValidateContext(context, out string diagnostic)) { EndOwnerScope(diagnostic); return; }
            SetOwnerActive($"Route camera override is available. route='{OwnerDiagnosticName}'.");
        }

        void IRouteContentLifecycleReceiver.OnRouteContentExited(RouteContentLifecycleContext context)
        {
            EndOwnerScope("RouteExited");
        }

        protected override bool TryValidateOwner(out string diagnostic)
        {
            if (assignedRoute == null) { diagnostic = "Route Camera Override requires an assigned RouteAsset."; return false; }
            diagnostic = string.Empty; return true;
        }

        protected override CameraRequestPublisherCreateResult CreatePublisher(CameraOutputSession session, CameraRequest request) => RouteCameraRequestPublisher.Create(session, request);

        private bool TryValidateContext(RouteContentLifecycleContext context, out string diagnostic)
        {
            if (!TryValidateOwner(out diagnostic)) return false;
            if (context.Route == null || !context.Route.HasSameIdentity(assignedRoute)) { diagnostic = "Route camera override lifecycle owner does not match the assigned RouteId."; return false; }
            return true;
        }
    }
}
