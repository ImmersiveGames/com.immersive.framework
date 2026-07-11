using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerAuthoring;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit Player-to-camera request binding.
    ///
    /// PlayerComposer supplies stable Player identity and typed camera targets.
    /// This component translates local-player eligibility into one scoped camera
    /// request. It does not discover a Player, decide local ownership, select a
    /// winner or mutate Cinemachine directly.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Local Player Camera Request Binding")]
    public sealed class LocalPlayerCameraRequestBinding : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerComposer playerComposer;

        [Header("Request Identity")]
        [Tooltip("Explicit stable lifetime scope for this local-player eligibility instance.")]
        [SerializeField] private string eligibilityScopeId;
        [Tooltip("Explicit stable camera request id.")]
        [SerializeField] private string requestId;

        [Header("Output and Rig")]
        [SerializeField] private CameraOutputSessionBinding outputSession;
        [SerializeField] private CameraRigComposer rigComposer;

        [Header("Arbitration")]
        [SerializeField] private int precedence = 50;
        [SerializeField] private string tieBreakerId;

        [Header("Eligibility")]
        [Tooltip("Publishes during OnEnable. Disable this when another runtime system explicitly owns local-player eligibility.")]
        [SerializeField] private bool eligibleOnEnable = true;
        [Tooltip("Releases the request during OnDisable.")]
        [SerializeField] private bool releaseOnDisable = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logDiagnostics = true;
        [SerializeField] private bool isLocallyEligible;
        [SerializeField] private string lastStatus = "NotEligible";
        [SerializeField] private string lastDiagnostic;

        private LocalPlayerCameraRequestPublisher publisher;

        public PlayerComposer PlayerComposer => playerComposer;
        public string EligibilityScopeId => eligibilityScopeId.NormalizeText();
        public string RequestIdText => requestId.NormalizeText();
        public bool IsLocallyEligible => isLocallyEligible;
        public bool IsPublished => publisher != null && publisher.IsPublished;
        public string LastStatus => lastStatus.NormalizeText();
        public string LastDiagnostic => lastDiagnostic.NormalizeText();

        private void OnEnable()
        {
            if (eligibleOnEnable)
            {
                SetLocalPlayerEligible(true);
            }
        }

        private void OnDisable()
        {
            if (!releaseOnDisable)
            {
                return;
            }

            SetLocalPlayerEligible(false);
        }

        /// <summary>
        /// Explicit runtime boundary for local-player camera eligibility.
        /// Repeated calls are idempotent.
        /// </summary>
        public bool SetLocalPlayerEligible(bool eligible)
        {
            if (eligible)
            {
                return TryPublish();
            }

            return TryRelease();
        }

        public bool TryPublish()
        {
            if (publisher != null && publisher.IsPublished)
            {
                isLocallyEligible = true;
                SetDiagnostic(
                    "Preserved",
                    $"Local Player camera request is already published. player='{GetPlayerDiagnosticName()}' scope='{EligibilityScopeId}'.",
                    false);
                return true;
            }

            if (!TryValidateConfiguration(out string diagnostic))
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (!outputSession.TryGetSession(
                    out CameraOutputSession session,
                    out diagnostic))
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (!TryCreateRequest(
                    session.OutputId,
                    out CameraRequest request,
                    out diagnostic))
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            CameraRequestPublisherCreateResult creation =
                LocalPlayerCameraRequestPublisher.Create(session, request);

            if (!creation.Succeeded)
            {
                isLocallyEligible = false;
                SetDiagnostic("Blocked", creation.DiagnosticSummary, true);
                return false;
            }

            publisher =
                creation.Publisher as LocalPlayerCameraRequestPublisher;

            if (publisher == null)
            {
                isLocallyEligible = false;
                SetDiagnostic(
                    "Blocked",
                    "Local Player camera publisher creation returned an unexpected publisher type.",
                    true);
                return false;
            }

            CameraRequestPublisherResult publishResult =
                publisher.Publish();

            if (!publishResult.Succeeded)
            {
                publisher = null;
                isLocallyEligible = false;
                SetDiagnostic("Blocked", publishResult.DiagnosticSummary, true);
                return false;
            }

            isLocallyEligible = true;

            SetDiagnostic(
                "Published",
                $"Local Player camera request published. player='{GetPlayerDiagnosticName()}' " +
                $"scope='{EligibilityScopeId}' request='{request.RequestId}'.",
                false);

            return true;
        }

        public bool TryRelease()
        {
            if (publisher == null)
            {
                isLocallyEligible = false;
                SetDiagnostic(
                    "Preserved",
                    $"Local Player camera request was already released. player='{GetPlayerDiagnosticName()}' scope='{EligibilityScopeId}'.",
                    false);
                return true;
            }

            CameraRequestPublisherResult releaseResult =
                publisher.Release();

            if (!releaseResult.Succeeded)
            {
                SetDiagnostic("Blocked", releaseResult.DiagnosticSummary, true);
                return false;
            }

            publisher = null;
            isLocallyEligible = false;

            SetDiagnostic(
                "Released",
                $"Local Player camera request released. player='{GetPlayerDiagnosticName()}' scope='{EligibilityScopeId}'.",
                false);

            return true;
        }

        private bool TryValidateConfiguration(out string diagnostic)
        {
            if (playerComposer == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicit PlayerComposer.";
                return false;
            }

            if (!playerComposer.CameraBindingRequired)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires PlayerComposer Camera Binding Required to be enabled.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(playerComposer.ActorId))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires PlayerComposer ActorId.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(playerComposer.PlayerSlotId))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires PlayerComposer PlayerSlotId.";
                return false;
            }

            if (playerComposer.CameraTarget == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires PlayerComposer CameraTarget.";
                return false;
            }

            if (playerComposer.LookAtTarget == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires PlayerComposer LookAtTarget.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EligibilityScopeId))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicit stable eligibility scope id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(RequestIdText))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicit request id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(tieBreakerId.NormalizeText()))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicit tie-breaker id.";
                return false;
            }

            if (outputSession == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicit CameraOutputSessionBinding.";
                return false;
            }

            if (rigComposer == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires a CameraRigComposer.";
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
            string ownerId =
                playerComposer.PlayerSlotId.NormalizeText();

            CameraRequestCreateResult result =
                CameraRequestCreateResult.Create(
                    new CameraRequestId(RequestIdText),
                    outputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.LocalPlayer,
                        ownerId),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.LocalPlayerEligibility,
                        EligibilityScopeId),
                    CameraRigReference.FromComposer(rigComposer),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        playerComposer.CameraTarget,
                        $"Local Player Camera Target {ownerId}"),
                    new CameraRequestPolicy(
                        precedence,
                        tieBreakerId.NormalizeText()),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(LocalPlayerCameraRequestBinding),
                    $"Local Player camera request for slot '{ownerId}' actor='{playerComposer.ActorId}'.");

            if (!result.IsSucceeded)
            {
                request = default;
                diagnostic =
                    $"Local Player camera request creation failed. {result.BlockingIssue}";
                return false;
            }

            request = result.Request;
            diagnostic = string.Empty;
            return true;
        }

        private string GetPlayerDiagnosticName()
        {
            if (playerComposer == null)
            {
                return "<missing>";
            }

            string slot = playerComposer.PlayerSlotId.NormalizeText();
            string actor = playerComposer.ActorId.NormalizeText();
            return $"slot:{slot}|actor:{actor}";
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
                $"[FRAMEWORK_CAMERA] Local Player Camera Request Binding status='{lastStatus}' diagnostic='{lastDiagnostic}'.";

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
