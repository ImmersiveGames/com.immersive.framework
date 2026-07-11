namespace Immersive.Framework.Camera
{
    public sealed class ActivityCameraLifecycleAdapter :
        ScopedCameraLifecycleAdapter
    {
        private ActivityCameraLifecycleAdapter(
            ICameraRequestPublisher publisher,
            string scopeId)
            : base(
                publisher,
                scopeId,
                nameof(ActivityCameraLifecycleAdapter))
        {
        }

        public static CameraLifecycleAdapterCreateResult Create(
            ActivityCameraRequestPublisher publisher)
        {
            if (!CameraLifecycleAdapterFactory.TryValidate(
                    publisher,
                    CameraRequestOwnerKind.Activity,
                    CameraRequestLifetimeKind.Activity,
                    nameof(ActivityCameraLifecycleAdapter),
                    out CameraLifecycleAdapterCreateResult blockedResult))
            {
                return blockedResult;
            }

            var adapter = new ActivityCameraLifecycleAdapter(
                publisher,
                publisher.Request.Lifetime.ScopeId);

            return CameraLifecycleAdapterFactory.Succeeded(
                adapter,
                $"Activity camera lifecycle adapter created for scope '{adapter.ScopeId}'.");
        }
    }
}
