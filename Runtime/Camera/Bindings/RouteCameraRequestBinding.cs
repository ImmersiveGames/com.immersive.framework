using Immersive.Framework.Authoring;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored Route lifecycle receiver that publishes one Route camera request.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Route Camera Request Binding")]
    public sealed class RouteCameraRequestBinding : RouteContentBehaviour
    {
        [Header("Lifecycle Identity")]
        [SerializeField] private RouteAsset assignedRoute;
        [SerializeField] private string scopeId;
        [SerializeField] private string requestId;

        [Header("Output and Rig")]
        [SerializeField] private CameraOutputSessionBinding outputSession;
        [SerializeField] private CameraRigComposer rigComposer;
        [SerializeField] private Transform targetSource;

        [Header("Arbitration")]
        [SerializeField] private int precedence = 10;
        [SerializeField] private string tieBreakerId;

        [Header("Diagnostics")]
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private string lastStatus = "NotEntered";
        [SerializeField] private string lastDiagnostic;

        private RouteCameraRequestPublisher publisher;

        public RouteAsset AssignedRoute => assignedRoute;
        public string ScopeId => scopeId.NormalizeText();
        public string RequestIdText => requestId.NormalizeText();
        public bool IsPublished => publisher != null && publisher.IsPublished;
        public string LastStatus => lastStatus ?? string.Empty;
        public string LastDiagnostic => lastDiagnostic ?? string.Empty;

        protected override void OnRouteContentEntered(
            RouteContentLifecycleContext context)
        {
            if (!TryValidateContext(context, out string diagnostic))
            {
                SetDiagnostic("Blocked", diagnostic, true);
                return;
            }

            if (publisher != null && publisher.IsPublished)
            {
                SetDiagnostic(
                    "Preserved",
                    $"Route camera request is already published. route='{assignedRoute.RouteName}' scope='{ScopeId}'.",
                    false);
                return;
            }

            if (!outputSession.TryGetSession(
                    out CameraOutputSession session,
                    out diagnostic))
            {
                SetDiagnostic("Blocked", diagnostic, true);
                return;
            }

            if (!TryCreateRequest(
                    session.OutputId,
                    out CameraRequest request,
                    out diagnostic))
            {
                SetDiagnostic("Blocked", diagnostic, true);
                return;
            }

            CameraRequestPublisherCreateResult creation =
                RouteCameraRequestPublisher.Create(session, request);

            if (!creation.Succeeded)
            {
                SetDiagnostic("Blocked", creation.DiagnosticSummary, true);
                return;
            }

            publisher = creation.Publisher as RouteCameraRequestPublisher;

            if (publisher == null)
            {
                SetDiagnostic(
                    "Blocked",
                    "Route camera publisher creation returned an unexpected publisher type.",
                    true);
                return;
            }

            CameraRequestPublisherResult publishResult =
                publisher.Publish();

            if (!publishResult.Succeeded)
            {
                publisher = null;
                SetDiagnostic("Blocked", publishResult.DiagnosticSummary, true);
                return;
            }

            SetDiagnostic(
                "Published",
                $"Route camera request published. route='{assignedRoute.RouteName}' scope='{ScopeId}' request='{request.RequestId}'.",
                false);
        }

        protected override void OnRouteContentExited(
            RouteContentLifecycleContext context)
        {
            if (!TryValidateContext(context, out string diagnostic))
            {
                SetDiagnostic("Blocked", diagnostic, true);
                return;
            }

            if (publisher == null)
            {
                SetDiagnostic(
                    "Preserved",
                    $"Route camera request was already released. route='{assignedRoute.RouteName}' scope='{ScopeId}'.",
                    false);
                return;
            }

            CameraRequestPublisherResult releaseResult =
                publisher.Release();

            if (!releaseResult.Succeeded)
            {
                SetDiagnostic("Blocked", releaseResult.DiagnosticSummary, true);
                return;
            }

            publisher = null;

            SetDiagnostic(
                "Released",
                $"Route camera request released. route='{assignedRoute.RouteName}' scope='{ScopeId}'.",
                false);
        }

        private bool TryValidateContext(
            RouteContentLifecycleContext context,
            out string diagnostic)
        {
            if (assignedRoute == null)
            {
                diagnostic = "Route Camera Request Binding requires an assigned RouteAsset.";
                return false;
            }

            if (context.Route == null)
            {
                diagnostic = "Route lifecycle context has no RouteAsset.";
                return false;
            }

            if (!ReferenceEquals(context.Route, assignedRoute))
            {
                diagnostic =
                    $"Route lifecycle mismatch. expected='{assignedRoute.RouteName}' actual='{context.Route.RouteName}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ScopeId))
            {
                diagnostic =
                    "Route Camera Request Binding requires an explicit stable scope id. RouteName is not used as identity.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(RequestIdText))
            {
                diagnostic =
                    "Route Camera Request Binding requires an explicit request id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(tieBreakerId.NormalizeText()))
            {
                diagnostic =
                    "Route Camera Request Binding requires an explicit tie-breaker id.";
                return false;
            }

            if (outputSession == null)
            {
                diagnostic =
                    "Route Camera Request Binding requires an explicit CameraOutputSessionBinding.";
                return false;
            }

            if (rigComposer == null)
            {
                diagnostic =
                    "Route Camera Request Binding requires a CameraRigComposer.";
                return false;
            }

            if (targetSource == null)
            {
                diagnostic =
                    "Route Camera Request Binding requires an explicit target source.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        private bool TryCreateRequest(
            CameraOutputId outputId,
            out CameraRequest request,
            out string diagnostic)
        {
            CameraRequestCreateResult result =
                CameraRequestCreateResult.Create(
                    new CameraRequestId(RequestIdText),
                    outputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.Route,
                        ScopeId),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.Route,
                        ScopeId),
                    CameraRigReference.FromComposer(rigComposer),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        targetSource,
                        $"Route Camera Target {assignedRoute.RouteName}"),
                    new CameraRequestPolicy(
                        precedence,
                        tieBreakerId.NormalizeText()),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(RouteCameraRequestBinding),
                    $"Scene-authored Route camera request for '{assignedRoute.RouteName}'.");

            if (!result.IsSucceeded)
            {
                request = default;
                diagnostic =
                    $"Route camera request creation failed. {result.BlockingIssue}";
                return false;
            }

            request = result.Request;
            diagnostic = string.Empty;
            return true;
        }

        private void SetDiagnostic(
            string status,
            string diagnostic,
            bool error)
        {
            lastStatus = status.NormalizeTextOrFallback("Unknown");
            lastDiagnostic = diagnostic.NormalizeText();

            if (!logDiagnostics)
            {
                return;
            }

            string message =
                $"[FRAMEWORK_CAMERA] Route Camera Request Binding status='{lastStatus}' diagnostic='{lastDiagnostic}'.";

            if (error)
            {
                Debug.LogError(message, this);
            }
            else
            {
                Debug.Log(message, this);
            }
        }
    }
}
