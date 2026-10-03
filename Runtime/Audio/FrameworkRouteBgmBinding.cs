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
    /// API status: Experimental. Route content contribution that publishes explicit BGM intent to the
    /// persistent FrameworkBgmDirector injected by the Audio assembly runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/Route BGM Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "BGM-CONTINUITY-1 Route BGM intent adapter.")]
    public sealed class RouteBgmAuthoring : RouteContentBehaviour, IFrameworkBgmDirectorConsumer, ISerializationCallbackReceiver
    {
        private const int CurrentRoutePolicySerializationVersion = 1;

        [SerializeField] private AudioBgmCueAsset routeBgm;
        [SerializeField] private FrameworkBgmRoutePolicy policy = FrameworkBgmRoutePolicy.PlayOwn;

        [HideInInspector]
        [SerializeField] private int routePolicySerializationVersion = CurrentRoutePolicySerializationVersion;

        [HideInInspector]
        [SerializeField] private FrameworkBgmDirector director;

        private FrameworkLogger _logger;

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

        bool IFrameworkBgmDirectorConsumer.TryAttachBgmDirector(
            FrameworkBgmDirector nextDirector,
            out bool wasAlreadyAttached,
            out string issue)
        {
            if (nextDirector == null)
            {
                wasAlreadyAttached = false;
                issue = "Route BGM binding requires a non-null FrameworkBgmDirector.";
                return false;
            }

            if (director != null && !ReferenceEquals(director, nextDirector))
            {
                wasAlreadyAttached = false;
                issue = "Route BGM binding rejected a different FrameworkBgmDirector authority.";
                Error(
                    issue,
                    LogFields.Of(
                        LogFields.Field("currentDirector", director.name),
                        LogFields.Field("rejectedDirector", nextDirector.name)));
                return false;
            }

            wasAlreadyAttached = ReferenceEquals(director, nextDirector);
            director = nextDirector;
            issue = string.Empty;
            return true;
        }

        bool IFrameworkBgmDirectorConsumer.TryDetachBgmDirector(
            FrameworkBgmDirector detachedDirector,
            out string issue)
        {
            if (detachedDirector == null)
            {
                issue = "Route BGM release requires the exact non-null FrameworkBgmDirector authority.";
                return false;
            }

            if (director == null)
            {
                issue = string.Empty;
                return true;
            }

            if (!ReferenceEquals(director, detachedDirector))
            {
                issue = "Route BGM release rejected a foreign or stale FrameworkBgmDirector authority.";
                return false;
            }

            director = null;
            issue = string.Empty;
            return true;
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
            _logger.Error(message, fields);
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<RouteBgmAuthoring>();
        }
    }
}
