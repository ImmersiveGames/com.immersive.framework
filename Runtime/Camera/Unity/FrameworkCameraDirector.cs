using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Framework-owned Route/Activity camera precedence director.
    /// It selects camera rigs and delegates concrete priority/target application to optional adapters.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Director")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public sealed class FrameworkCameraDirector : MonoBehaviour
    {
        private const string LogPrefix = "[FRAMEWORK_CAMERA]";

        [Header("Default Fallback")]
        [SerializeField] private GameObject defaultCameraRig;
        [SerializeField] private FrameworkCameraAnchorHost defaultAnchors;

        [Header("Priority")]
        [SerializeField] private int routePriority = 20;
        [SerializeField] private int activityPriority = 100;

        [Header("Application")]
        [SerializeField] private bool setRigActiveState = true;
        [SerializeField] private MonoBehaviour rigApplier;

        [Header("Diagnostics")]
        [SerializeField] private bool logTransitions = true;

        private GameObject currentRouteRig;
        private FrameworkCameraAnchorHost currentRouteAnchors;
        private GameObject currentActivityRig;
        private FrameworkCameraAnchorHost currentActivityAnchors;
        private GameObject retainedActivityRigForCurrentRoute;
        private FrameworkCameraAnchorHost retainedActivityAnchorsForCurrentRoute;
        private GameObject currentEffectiveRig;
        private FrameworkCameraRigDescriptor currentEffectiveDescriptor;
        private FrameworkCameraActivityPolicy currentActivityPolicy = FrameworkCameraActivityPolicy.UseOwnOrRoute;
        private bool hasActiveActivityCameraBinding;

        public GameObject CurrentRouteRig => currentRouteRig;

        public GameObject CurrentActivityRig => currentActivityRig;

        public GameObject RetainedActivityRigForCurrentRoute => retainedActivityRigForCurrentRoute;

        public GameObject CurrentEffectiveRig => currentEffectiveRig;

        public FrameworkCameraRigDescriptor CurrentEffectiveDescriptor => currentEffectiveDescriptor;

        public FrameworkCameraActivityPolicy CurrentActivityPolicy => currentActivityPolicy;

        public bool HasActiveActivityCameraBinding => hasActiveActivityCameraBinding;

        public bool HasDefaultCameraRig => defaultCameraRig != null;

        public void SetRouteCamera(GameObject cameraRig, FrameworkCameraAnchorHost anchors)
        {
            SetRouteCamera(cameraRig, anchors, false);
        }

        public void SetRouteCamera(
            GameObject cameraRig,
            FrameworkCameraAnchorHost anchors,
            bool deferRefreshForStartupActivity)
        {
            currentRouteRig = cameraRig;
            currentRouteAnchors = anchors;
            currentActivityRig = null;
            currentActivityAnchors = null;
            retainedActivityRigForCurrentRoute = null;
            retainedActivityAnchorsForCurrentRoute = null;
            hasActiveActivityCameraBinding = false;
            currentActivityPolicy = FrameworkCameraActivityPolicy.UseOwnOrRoute;

            Log($"Route camera set. routeRig='{FormatRig(cameraRig)}' retainedActivityRig='<cleared>' deferRefreshForStartupActivity='{deferRefreshForStartupActivity}'.");

            if (!deferRefreshForStartupActivity)
            {
                Refresh();
            }
        }

        public void ClearRouteCamera(GameObject cameraRig)
        {
            if (cameraRig != null && !ReferenceEquals(currentRouteRig, cameraRig))
            {
                Log($"Route camera clear ignored as stale. requested='{FormatRig(cameraRig)}' currentRouteRig='{FormatRig(currentRouteRig)}'.");
                return;
            }

            currentRouteRig = null;
            currentRouteAnchors = null;
            currentActivityRig = null;
            currentActivityAnchors = null;
            retainedActivityRigForCurrentRoute = null;
            retainedActivityAnchorsForCurrentRoute = null;
            hasActiveActivityCameraBinding = false;
            currentActivityPolicy = FrameworkCameraActivityPolicy.UseOwnOrRoute;

            Log("Route camera cleared. Activity camera retention cleared with Route scope.");
            Refresh();
        }

        public void SetActivityCamera(
            GameObject cameraRig,
            FrameworkCameraActivityPolicy policy,
            FrameworkCameraAnchorHost anchors)
        {
            hasActiveActivityCameraBinding = true;
            currentActivityPolicy = NormalizeActivityPolicy(policy);

            if (currentActivityPolicy == FrameworkCameraActivityPolicy.UseRoute)
            {
                currentActivityRig = null;
                currentActivityAnchors = null;
                Log($"Activity camera policy UseRoute applied. retainedActivityRig='{FormatRig(retainedActivityRigForCurrentRoute)}'.");
                Refresh();
                return;
            }

            currentActivityRig = cameraRig;
            currentActivityAnchors = anchors;

            if (currentActivityPolicy == FrameworkCameraActivityPolicy.UseOwnOrRetainActivityUntilRouteExit && cameraRig != null)
            {
                retainedActivityRigForCurrentRoute = cameraRig;
                retainedActivityAnchorsForCurrentRoute = anchors;
            }

            Log($"Activity camera set. activityRig='{FormatRig(cameraRig)}' policy='{currentActivityPolicy}' retainedActivityRig='{FormatRig(retainedActivityRigForCurrentRoute)}'.");
            Refresh();
        }

        public void ClearActivityCamera(GameObject cameraRig)
        {
            ClearActivityCamera(cameraRig, false);
        }

        public void ClearActivityCamera(GameObject cameraRig, bool deferRefreshForActivityTransition)
        {
            if (cameraRig != null && !ReferenceEquals(currentActivityRig, cameraRig))
            {
                Log($"Activity camera clear ignored as stale. requested='{FormatRig(cameraRig)}' currentActivityRig='{FormatRig(currentActivityRig)}'.");
                return;
            }

            currentActivityRig = null;
            currentActivityAnchors = null;
            hasActiveActivityCameraBinding = false;
            currentActivityPolicy = FrameworkCameraActivityPolicy.UseOwnOrRoute;

            Log($"Activity camera cleared. retainedActivityRig='{FormatRig(retainedActivityRigForCurrentRoute)}' deferRefresh='{deferRefreshForActivityTransition}'.");

            if (!deferRefreshForActivityTransition)
            {
                Refresh();
            }
        }

        public FrameworkCameraRigDescriptor Refresh()
        {
            FrameworkCameraRigDescriptor next = ResolveEffectiveRig();
            if (!next.IsValid)
            {
                Debug.LogError(
                    $"{LogPrefix} No effective camera rig is available. Configure a default, route, or activity camera rig.",
                    this);
                currentEffectiveDescriptor = next;
                return next;
            }

            currentEffectiveDescriptor = next;

            if (ReferenceEquals(currentEffectiveRig, next.Rig))
            {
                ApplyRig(next);
                Log($"Camera refresh skipped. {next.ToDiagnosticString()}.");
                return next;
            }

            if (setRigActiveState)
            {
                SetRigActive(currentEffectiveRig, false);
            }

            currentEffectiveRig = next.Rig;

            if (setRigActiveState)
            {
                SetRigActive(currentEffectiveRig, true);
            }

            ApplyRig(next);
            Log($"Camera applied. {next.ToDiagnosticString()}.");
            return next;
        }

        private FrameworkCameraRigDescriptor ResolveEffectiveRig()
        {
            if (!hasActiveActivityCameraBinding)
            {
                return CreateDescriptor(
                    currentRouteRig != null ? currentRouteRig : defaultCameraRig,
                    currentRouteRig != null ? FrameworkCameraRigRole.Route : FrameworkCameraRigRole.DefaultFallback,
                    currentRouteRig != null ? FrameworkCameraScope.Route : FrameworkCameraScope.DefaultFallback,
                    currentRouteRig != null ? currentRouteAnchors : defaultAnchors,
                    routePriority,
                    nameof(FrameworkCameraDirector),
                    "route-or-default");
            }

            switch (currentActivityPolicy)
            {
                case FrameworkCameraActivityPolicy.UseRoute:
                    return CreateDescriptor(
                        currentRouteRig != null ? currentRouteRig : defaultCameraRig,
                        currentRouteRig != null ? FrameworkCameraRigRole.Route : FrameworkCameraRigRole.DefaultFallback,
                        currentRouteRig != null ? FrameworkCameraScope.Route : FrameworkCameraScope.DefaultFallback,
                        currentRouteRig != null ? currentRouteAnchors : defaultAnchors,
                        routePriority,
                        nameof(FrameworkCameraDirector),
                        "activity-policy-use-route");

                case FrameworkCameraActivityPolicy.UseOwnOrRetainActivityUntilRouteExit:
                    if (currentActivityRig != null)
                    {
                        return CreateDescriptor(
                            currentActivityRig,
                            FrameworkCameraRigRole.Activity,
                            FrameworkCameraScope.Activity,
                            currentActivityAnchors,
                            activityPriority,
                            nameof(FrameworkCameraDirector),
                            "activity-own");
                    }

                    if (retainedActivityRigForCurrentRoute != null)
                    {
                        return CreateDescriptor(
                            retainedActivityRigForCurrentRoute,
                            FrameworkCameraRigRole.RetainedActivity,
                            FrameworkCameraScope.Activity,
                            retainedActivityAnchorsForCurrentRoute,
                            activityPriority,
                            nameof(FrameworkCameraDirector),
                            "activity-retained-until-route-exit");
                    }

                    return CreateDescriptor(
                        currentRouteRig != null ? currentRouteRig : defaultCameraRig,
                        currentRouteRig != null ? FrameworkCameraRigRole.Route : FrameworkCameraRigRole.DefaultFallback,
                        currentRouteRig != null ? FrameworkCameraScope.Route : FrameworkCameraScope.DefaultFallback,
                        currentRouteRig != null ? currentRouteAnchors : defaultAnchors,
                        routePriority,
                        nameof(FrameworkCameraDirector),
                        "activity-fallback-route-or-default");

                default:
                    if (currentActivityRig != null)
                    {
                        return CreateDescriptor(
                            currentActivityRig,
                            FrameworkCameraRigRole.Activity,
                            FrameworkCameraScope.Activity,
                            currentActivityAnchors,
                            activityPriority,
                            nameof(FrameworkCameraDirector),
                            "activity-own");
                    }

                    return CreateDescriptor(
                        currentRouteRig != null ? currentRouteRig : defaultCameraRig,
                        currentRouteRig != null ? FrameworkCameraRigRole.Route : FrameworkCameraRigRole.DefaultFallback,
                        currentRouteRig != null ? FrameworkCameraScope.Route : FrameworkCameraScope.DefaultFallback,
                        currentRouteRig != null ? currentRouteAnchors : defaultAnchors,
                        routePriority,
                        nameof(FrameworkCameraDirector),
                        "activity-fallback-route-or-default");
            }
        }

        private static FrameworkCameraRigDescriptor CreateDescriptor(
            GameObject rig,
            FrameworkCameraRigRole role,
            FrameworkCameraScope scope,
            FrameworkCameraAnchorHost anchors,
            int priority,
            string source,
            string reason)
        {
            return new FrameworkCameraRigDescriptor(
                rig,
                role,
                scope,
                anchors != null ? anchors.ToDescriptor() : FrameworkCameraAnchorDescriptor.Empty,
                new FrameworkCameraPriorityState(role, priority, source, reason),
                source,
                reason);
        }

        private void ApplyRig(FrameworkCameraRigDescriptor descriptor)
        {
            if (!TryResolveRigApplier(out IFrameworkCameraRigApplier applier))
            {
                return;
            }

            if (!applier.Supports(descriptor))
            {
                Debug.LogWarning($"{LogPrefix} Camera rig applier skipped unsupported descriptor. {descriptor.ToDiagnosticString()}.", this);
                return;
            }

            applier.Apply(descriptor);
        }

        private bool TryResolveRigApplier(out IFrameworkCameraRigApplier applier)
        {
            applier = null;
            if (rigApplier == null)
            {
                return false;
            }

            applier = rigApplier as IFrameworkCameraRigApplier;
            if (applier != null)
            {
                return true;
            }

            Debug.LogError(
                $"{LogPrefix} Rig applier must implement {nameof(IFrameworkCameraRigApplier)}. object='{rigApplier.name.NormalizeTextOrFallback("<unnamed>")}'.",
                rigApplier);
            return false;
        }

        private void Log(string message)
        {
            if (logTransitions)
            {
                Debug.Log($"{LogPrefix} {message}", this);
            }
        }

        private static FrameworkCameraActivityPolicy NormalizeActivityPolicy(FrameworkCameraActivityPolicy policy)
        {
            return policy == FrameworkCameraActivityPolicy.UseOwnOrRetainActivityUntilRouteExit
                || policy == FrameworkCameraActivityPolicy.UseRoute
                ? policy
                : FrameworkCameraActivityPolicy.UseOwnOrRoute;
        }

        private static void SetRigActive(GameObject rig, bool isActive)
        {
            if (rig != null && rig.activeSelf != isActive)
            {
                rig.SetActive(isActive);
            }
        }

        private static string FormatRig(GameObject rig)
        {
            return rig != null ? rig.name : "<none>";
        }
    }
}
