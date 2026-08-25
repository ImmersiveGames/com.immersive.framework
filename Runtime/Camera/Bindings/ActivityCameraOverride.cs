using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Activity Camera Override")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public sealed class ActivityCameraOverride : ScopedCameraOverride, IActivityContentLifecycleReceiver
    {
        [SerializeField] private ActivityAsset assignedActivity;
        public ActivityAsset AssignedActivity => assignedActivity;

        protected override CameraRequestOwnerKind OwnerKind => CameraRequestOwnerKind.Activity;
        protected override CameraRequestLifetimeKind LifetimeKind => CameraRequestLifetimeKind.Activity;
        protected override string OwnerDiagnosticName => assignedActivity != null ? assignedActivity.ActivityName : "<missing-activity>";

        void IActivityContentLifecycleReceiver.OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (!TryValidateContext(context, out string diagnostic)) { EndOwnerScope(diagnostic); return; }
            SetOwnerActive($"Activity camera override is available. activity='{OwnerDiagnosticName}'.");
        }

        void IActivityContentLifecycleReceiver.OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            EndOwnerScope("ActivityExited");
        }

        protected override bool TryValidateOwner(out string diagnostic)
        {
            if (assignedActivity == null) { diagnostic = "Activity Camera Override requires an assigned ActivityAsset."; return false; }
            diagnostic = string.Empty; return true;
        }

        protected override CameraRequestPublisherCreateResult CreatePublisher(CameraOutputSession session, CameraRequest request) => ActivityCameraRequestPublisher.Create(session, request);

        private bool TryValidateContext(ActivityContentLifecycleContext context, out string diagnostic)
        {
            if (!TryValidateOwner(out diagnostic)) return false;
            if (context.Activity == null || !ReferenceEquals(context.Activity, assignedActivity)) { diagnostic = "Activity camera override lifecycle owner does not match the assigned Activity asset reference."; return false; }
            return true;
        }
    }
}
