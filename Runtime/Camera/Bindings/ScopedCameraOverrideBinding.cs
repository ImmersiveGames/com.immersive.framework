using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Shared explicit-request behavior for camera overrides owned by a lifecycle scope.
    /// Derived bindings validate their own owner identity and decide when the scope is available.
    /// </summary>
    public abstract class ScopedCameraOverrideBinding : MonoBehaviour, ICameraOutputSessionConsumer
    {
        [Header("Override Identity")]
        [SerializeField] private string scopeId;
        [SerializeField] private string requestId;

        [Header("Rig")]
        [SerializeField] private CameraRigComposer rigComposer;
        [SerializeField] private Transform targetSource;

        [Header("Arbitration")]
        [SerializeField] private int precedence;
        [SerializeField] private string tieBreakerId;

        [Header("Diagnostics")]
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool overrideActive;
        [SerializeField] private bool ownerActive;
        [SerializeField] private string lastStatus = "Unavailable";
        [SerializeField] private string lastDiagnostic;

        private CameraOutputSessionBinding outputSession;
        private ICameraRequestPublisher publisher;

        public string ScopeId => scopeId.NormalizeText();
        public string RequestIdText => requestId.NormalizeText();
        public CameraRigComposer RigComposer => rigComposer;
        public Transform TargetSource => targetSource;
        public int Precedence => precedence;
        public string TieBreakerId => tieBreakerId.NormalizeText();
        public CameraOutputSessionBinding OutputSession => outputSession;
        public bool IsOverrideActive => overrideActive;
        public bool IsPublished => overrideActive;
        public bool IsOwnerActive => ownerActive;
        public string LastStatus => lastStatus ?? string.Empty;
        public string LastDiagnostic => lastDiagnostic ?? string.Empty;

        public CameraOverrideResult RequestOverride()
        {
            if (overrideActive)
            {
                return Result(CameraOverrideOperationKind.Preserved, true,
                    "Override request preserved because it is already active.", false);
            }

            if (!ownerActive)
            {
                return Result(CameraOverrideOperationKind.Blocked, false,
                    "Camera override request is blocked because its owner scope is not active.", true);
            }

            if (!TryValidateOwner(out string diagnostic) || !TryValidateConfiguration(out diagnostic))
            {
                return Result(CameraOverrideOperationKind.Blocked, false, diagnostic, true);
            }

            if (!outputSession.TryGetSession(out CameraOutputSession session, out diagnostic))
            {
                return Result(CameraOverrideOperationKind.Blocked, false, diagnostic, true);
            }

            CameraRequestCreateResult requestResult = CameraRequestCreateResult.Create(
                new CameraRequestId(RequestIdText),
                session.OutputId,
                new CameraRequestOwner(OwnerKind, ScopeId),
                new CameraRequestLifetime(LifetimeKind, ScopeId),
                CameraRigReference.FromComposer(rigComposer),
                CameraTargetSourceDescriptor.ExplicitTransform(targetSource, OwnerDiagnosticName),
                new CameraRequestPolicy(precedence, TieBreakerId),
                CameraRequestReleaseCondition.ExplicitRelease,
                GetType().Name,
                $"Explicit camera override for '{OwnerDiagnosticName}'.");

            if (!requestResult.IsSucceeded)
            {
                return Result(CameraOverrideOperationKind.Blocked, false,
                    $"Camera override request creation failed. {requestResult.BlockingIssue}", true);
            }

            CameraRequestPublisherCreateResult creation = CreatePublisher(session, requestResult.Request);
            if (!creation.Succeeded || creation.Publisher == null)
            {
                return Result(CameraOverrideOperationKind.Blocked, false, creation.DiagnosticSummary, true);
            }

            CameraRequestPublisherResult publication = creation.Publisher.Publish();
            if (!publication.Succeeded)
            {
                return Result(CameraOverrideOperationKind.Blocked, false, publication.DiagnosticSummary, true);
            }

            publisher = creation.Publisher;
            overrideActive = true;
            return Result(CameraOverrideOperationKind.Requested, true,
                $"Camera override requested. owner='{OwnerDiagnosticName}' scope='{ScopeId}' request='{requestResult.Request.RequestId}' output='{session.OutputId}' precedence='{precedence}'.", false);
        }

        public CameraOverrideResult ReleaseOverride()
        {
            return ReleaseInternal(CameraOverrideOperationKind.Released, "ExplicitRelease", false);
        }

        protected void SetOwnerActive(string diagnostic)
        {
            ownerActive = true;
            SetDiagnostic("Available", diagnostic, false);
        }

        protected CameraOverrideResult EndOwnerScope(string reason)
        {
            ownerActive = false;
            return ReleaseInternal(CameraOverrideOperationKind.CleanedUp, reason, false);
        }

        void ICameraOutputSessionConsumer.AttachOutputSession(CameraOutputSessionBinding binding)
        {
            SetOutputSession(binding);
        }

        void ICameraOutputSessionConsumer.DetachOutputSession(string reason)
        {
            EndOwnerScope(reason.NormalizeTextOrFallback("OutputDetached"));
            outputSession = null;
        }

        protected void SetOutputSession(CameraOutputSessionBinding binding)
        {
            outputSession = binding;
            SetDiagnostic("OutputAttached", binding == null
                ? "Camera output injection was empty."
                : $"Camera output session attached. output='{binding.OutputIdText}'.", binding == null);
        }

        private CameraOverrideResult ReleaseInternal(
            CameraOverrideOperationKind operation,
            string reason,
            bool errorOnFailure)
        {
            if (publisher == null || !overrideActive)
            {
                overrideActive = false;
                return Result(CameraOverrideOperationKind.Preserved, true,
                    $"Camera override is already released. reason='{reason}'.", false);
            }

            CameraRequestPublisherResult release = publisher.Release();
            if (!release.Succeeded)
            {
                return Result(CameraOverrideOperationKind.Blocked, false, release.DiagnosticSummary, errorOnFailure);
            }

            publisher = null;
            overrideActive = false;
            return Result(operation, true,
                $"Camera override released. reason='{reason}'.", false);
        }

        private bool TryValidateConfiguration(out string diagnostic)
        {
            if (string.IsNullOrWhiteSpace(ScopeId)) { diagnostic = "Camera override requires an explicit scope id."; return false; }
            if (string.IsNullOrWhiteSpace(RequestIdText)) { diagnostic = "Camera override requires an explicit request id."; return false; }
            if (outputSession == null) { diagnostic = "Camera override requires an injected CameraOutputSessionBinding."; return false; }
            if (rigComposer == null) { diagnostic = "Camera override requires a CameraRigComposer."; return false; }
            if (targetSource == null) { diagnostic = "Camera override requires an explicit target source."; return false; }
            if (string.IsNullOrWhiteSpace(TieBreakerId)) { diagnostic = "Camera override requires an explicit tie-breaker id."; return false; }
            diagnostic = string.Empty;
            return true;
        }

        private CameraOverrideResult Result(CameraOverrideOperationKind operation, bool succeeded, string diagnostic, bool error)
        {
            SetDiagnostic(operation.ToString(), diagnostic, error);
            return new CameraOverrideResult(operation, succeeded, overrideActive, diagnostic);
        }

        private void SetDiagnostic(string status, string diagnostic, bool error)
        {
            lastStatus = status.NormalizeTextOrFallback("Unknown");
            lastDiagnostic = diagnostic.NormalizeText();
            if (!logDiagnostics) return;
            string message = $"[FRAMEWORK_CAMERA] override='{GetType().Name}' status='{lastStatus}' owner='{OwnerDiagnosticName}' scope='{ScopeId}' request='{RequestIdText}' precedence='{precedence}' diagnostic='{lastDiagnostic}'.";
            if (error) Debug.LogError(message, this); else Debug.Log(message, this);
        }

        protected abstract CameraRequestOwnerKind OwnerKind { get; }
        protected abstract CameraRequestLifetimeKind LifetimeKind { get; }
        protected abstract string OwnerDiagnosticName { get; }
        protected abstract bool TryValidateOwner(out string diagnostic);
        protected abstract CameraRequestPublisherCreateResult CreatePublisher(CameraOutputSession session, CameraRequest request);
    }
}
