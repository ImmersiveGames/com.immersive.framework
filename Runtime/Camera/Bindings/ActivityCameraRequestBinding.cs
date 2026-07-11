using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored Activity lifecycle receiver that publishes one Activity camera request.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Activity Camera Request Binding")]
    public sealed class ActivityCameraRequestBinding : ActivityContentBehaviour
    {
        [Header("Lifecycle Identity")]
        [SerializeField] private ActivityAsset assignedActivity;
        [SerializeField] private string scopeId;
        [SerializeField] private string requestId;

        [Header("Output and Rig")]
        [SerializeField] private CameraOutputSessionBinding outputSession;
        [SerializeField] private CameraRigComposer rigComposer;
        [SerializeField] private Transform targetSource;

        [Header("Arbitration")]
        [SerializeField] private int precedence = 100;
        [SerializeField] private string tieBreakerId;

        [Header("Diagnostics")]
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private string lastStatus = "NotEntered";
        [SerializeField] private string lastDiagnostic;

        private ActivityCameraRequestPublisher publisher;

        public ActivityAsset AssignedActivity => assignedActivity;
        public string ScopeId => scopeId.NormalizeText();
        public string RequestIdText => requestId.NormalizeText();
        public bool IsPublished => publisher != null && publisher.IsPublished;
        public string LastStatus => lastStatus ?? string.Empty;
        public string LastDiagnostic => lastDiagnostic ?? string.Empty;

        protected override void OnActivityContentEntered(
            ActivityContentLifecycleContext context)
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
                    $"Activity camera request is already published. activity='{assignedActivity.ActivityName}' scope='{ScopeId}'.",
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
                ActivityCameraRequestPublisher.Create(session, request);

            if (!creation.Succeeded)
            {
                SetDiagnostic("Blocked", creation.DiagnosticSummary, true);
                return;
            }

            publisher = creation.Publisher as ActivityCameraRequestPublisher;

            if (publisher == null)
            {
                SetDiagnostic(
                    "Blocked",
                    "Activity camera publisher creation returned an unexpected publisher type.",
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
                $"Activity camera request published. activity='{assignedActivity.ActivityName}' scope='{ScopeId}' request='{request.RequestId}'.",
                false);
        }

        protected override void OnActivityContentExited(
            ActivityContentLifecycleContext context)
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
                    $"Activity camera request was already released. activity='{assignedActivity.ActivityName}' scope='{ScopeId}'.",
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
                $"Activity camera request released. activity='{assignedActivity.ActivityName}' scope='{ScopeId}'.",
                false);
        }

        private bool TryValidateContext(
            ActivityContentLifecycleContext context,
            out string diagnostic)
        {
            if (assignedActivity == null)
            {
                diagnostic =
                    "Activity Camera Request Binding requires an assigned ActivityAsset.";
                return false;
            }

            if (context.Activity == null)
            {
                diagnostic =
                    "Activity lifecycle context has no ActivityAsset.";
                return false;
            }

            if (!ReferenceEquals(context.Activity, assignedActivity))
            {
                diagnostic =
                    $"Activity lifecycle mismatch. expected='{assignedActivity.ActivityName}' actual='{context.Activity.ActivityName}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ScopeId))
            {
                diagnostic =
                    "Activity Camera Request Binding requires an explicit stable scope id. ActivityName is not used as identity.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(RequestIdText))
            {
                diagnostic =
                    "Activity Camera Request Binding requires an explicit request id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(tieBreakerId.NormalizeText()))
            {
                diagnostic =
                    "Activity Camera Request Binding requires an explicit tie-breaker id.";
                return false;
            }

            if (outputSession == null)
            {
                diagnostic =
                    "Activity Camera Request Binding requires an explicit CameraOutputSessionBinding.";
                return false;
            }

            if (rigComposer == null)
            {
                diagnostic =
                    "Activity Camera Request Binding requires a CameraRigComposer.";
                return false;
            }

            if (targetSource == null)
            {
                diagnostic =
                    "Activity Camera Request Binding requires an explicit target source.";
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
                        CameraRequestOwnerKind.Activity,
                        ScopeId),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.Activity,
                        ScopeId),
                    CameraRigReference.FromComposer(rigComposer),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        targetSource,
                        $"Activity Camera Target {assignedActivity.ActivityName}"),
                    new CameraRequestPolicy(
                        precedence,
                        tieBreakerId.NormalizeText()),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(ActivityCameraRequestBinding),
                    $"Scene-authored Activity camera request for '{assignedActivity.ActivityName}'.");

            if (!result.IsSucceeded)
            {
                request = default;
                diagnostic =
                    $"Activity camera request creation failed. {result.BlockingIssue}";
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
                $"[FRAMEWORK_CAMERA] Activity Camera Request Binding status='{lastStatus}' diagnostic='{lastDiagnostic}'.";

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
