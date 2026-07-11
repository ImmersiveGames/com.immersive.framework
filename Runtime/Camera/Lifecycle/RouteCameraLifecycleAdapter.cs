namespace Immersive.Framework.Camera
{
    public sealed class RouteCameraLifecycleAdapter :
        ScopedCameraLifecycleAdapter
    {
        private RouteCameraLifecycleAdapter(
            ICameraRequestPublisher publisher,
            string scopeId)
            : base(
                publisher,
                scopeId,
                nameof(RouteCameraLifecycleAdapter))
        {
        }

        public static CameraLifecycleAdapterCreateResult Create(
            RouteCameraRequestPublisher publisher)
        {
            if (!CameraLifecycleAdapterFactory.TryValidate(
                    publisher,
                    CameraRequestOwnerKind.Route,
                    CameraRequestLifetimeKind.Route,
                    nameof(RouteCameraLifecycleAdapter),
                    out CameraLifecycleAdapterCreateResult blockedResult))
            {
                return blockedResult;
            }

            var adapter = new RouteCameraLifecycleAdapter(
                publisher,
                publisher.Request.Lifetime.ScopeId);

            return CameraLifecycleAdapterFactory.Succeeded(
                adapter,
                $"Route camera lifecycle adapter created for scope '{adapter.ScopeId}'.");
        }
    }
}
