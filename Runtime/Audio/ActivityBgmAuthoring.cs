using Immersive.Audio.Authoring;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Activity content authoring that publishes explicit BGM intent to
    /// the persistent FrameworkBgmDirector injected by the Audio assembly runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Audio/Activity BGM Authoring")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "BGM-CONTINUITY-1 Activity BGM intent adapter.")]
    public sealed class ActivityBgmAuthoring : ActivityContentBehaviour, IFrameworkBgmDirectorConsumer
    {
        [SerializeField] private ActivityAsset assignedActivity;
        [SerializeField] private AudioBgmCueAsset activityBgm;
        [SerializeField] private FrameworkBgmActivityPolicy policy = FrameworkBgmActivityPolicy.UseOwnOrRoute;

        [HideInInspector]
        [SerializeField] private FrameworkBgmDirector director;

        private FrameworkLogger _logger;

        public FrameworkBgmOperationResult LastOperationResult { get; private set; }

        public ActivityAsset AssignedActivity => assignedActivity;

        public AudioBgmCueAsset ActivityBgm => activityBgm;

        public FrameworkBgmActivityPolicy Policy => policy;

        public FrameworkBgmDirector Director => director;

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Activity BGM authoring requires an injected FrameworkBgmDirector.");
                return;
            }

            LastOperationResult = director.SetActivityBgm(activityBgm, policy);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Error("Activity BGM authoring requires an injected FrameworkBgmDirector.");
                return;
            }

            bool deferRefreshForActivityTransition = context.NextActivity != null
                && (context.Activity == null || !ReferenceEquals(context.NextActivity, context.Activity));

            LastOperationResult = director.ClearActivityBgm(activityBgm, deferRefreshForActivityTransition);
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
                    "Activity BGM authoring rejected a second FrameworkBgmDirector authority.",
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

        private void Error(string message, params LogField[] fields)
        {
            EnsureLogger();
            _logger.Error(message, fields);
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<ActivityBgmAuthoring>();
        }
    }
}
