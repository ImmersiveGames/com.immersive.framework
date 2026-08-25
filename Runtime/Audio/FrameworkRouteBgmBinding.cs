using Immersive.Audio.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RouteLifecycle;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Route content binding that publishes explicit BGM intent to the
    /// persistent FrameworkBgmDirector injected by the Audio assembly runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/Route BGM Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "BGM-CONTINUITY-1 Route BGM intent adapter.")]
    public sealed class FrameworkRouteBgmBinding : RouteContentBehaviour, IFrameworkBgmDirectorConsumer, ISerializationCallbackReceiver
    {
        private const int CurrentRoutePolicySerializationVersion = 1;

        [SerializeField] private AudioBgmCueAsset routeBgm;
        [SerializeField] private FrameworkBgmRoutePolicy policy = FrameworkBgmRoutePolicy.PlayOwn;

        [HideInInspector]
        [SerializeField] private int routePolicySerializationVersion = CurrentRoutePolicySerializationVersion;

        [HideInInspector]
        [SerializeField] private FrameworkBgmDirector director;

        private FrameworkLogger logger;

        public FrameworkBgmOperationResult LastOperationResult { get; private set; }

        public AudioBgmCueAsset RouteBgm => routeBgm;

        public FrameworkBgmRoutePolicy Policy => policy;

        public FrameworkBgmDirector Director => director;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Route BGM binding requires an injected FrameworkBgmDirector.");
                return;
            }

            bool deferRefreshForStartupActivity =
                context.Route != null && context.Route.HasStartupActivity;
            LastOperationResult = director.SetRouteBgm(
                routeBgm,
                policy,
                deferRefreshForStartupActivity);
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Route BGM binding requires an injected FrameworkBgmDirector.");
                return;
            }

            LastOperationResult = director.ClearRouteBgm(routeBgm, policy);
        }

        void IFrameworkBgmDirectorConsumer.AttachBgmDirector(FrameworkBgmDirector nextDirector)
        {
            if (nextDirector == null)
            {
                return;
            }

            if (director != null && !ReferenceEquals(director, nextDirector))
            {
                Error(
                    "Route BGM binding rejected a second FrameworkBgmDirector authority.",
                    LogFields.Of(
                        LogFields.Field("currentDirector", director.name),
                        LogFields.Field("rejectedDirector", nextDirector.name)));
                return;
            }

            director = nextDirector;
        }

        void IFrameworkBgmDirectorConsumer.DetachBgmDirector(FrameworkBgmDirector detachedDirector)
        {
            if (ReferenceEquals(director, detachedDirector))
            {
                director = null;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            MigrateRoutePolicyIfRequired();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            MigrateRoutePolicyIfRequired();
        }

        private void MigrateRoutePolicyIfRequired()
        {
            if (routePolicySerializationVersion >= CurrentRoutePolicySerializationVersion)
            {
                return;
            }

            // Migration BGM-ROUTE-POLICY-1: old bindings encoded Play/Preserve only by cue presence.
            policy = routeBgm != null
                ? FrameworkBgmRoutePolicy.PlayOwn
                : FrameworkBgmRoutePolicy.PreserveCurrent;
            routePolicySerializationVersion = CurrentRoutePolicySerializationVersion;
        }

        private void Error(string message, params LogField[] fields)
        {
            EnsureLogger();
            logger.Error(message, fields);
        }

        private void EnsureLogger()
        {
            logger ??= FrameworkLogger.Create<FrameworkRouteBgmBinding>();
        }
    }
}
