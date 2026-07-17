using System;
using Immersive.Framework.Actors;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit prepared-Player-to-camera request binding.
    ///
    /// Player identity comes from the admitted Local Player Host and prepared
    /// PlayerActorDeclaration. Camera targets come from CameraRigComposer's explicit transforms or typed
    /// ICameraTargetSource. This component does not provision a Player, discover
    /// locality, select a winner or mutate Cinemachine directly.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Camera/Local Player Camera Request Binding")]
    public sealed class LocalPlayerCameraRequestBinding :
        MonoBehaviour,
        ICameraOutputSessionConsumer
    {
        [Header("Player")]
        [SerializeField] private LocalPlayerHostAuthoring localPlayerHost;
        [SerializeField] private PlayerActorDeclaration playerActor;

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

        public LocalPlayerHostAuthoring LocalPlayerHost => localPlayerHost;
        public PlayerActorDeclaration PlayerActor => playerActor;
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
            if (releaseOnDisable)
            {
                SetLocalPlayerEligible(false);
            }
        }

        public bool SetLocalPlayerEligible(bool eligible)
        {
            return eligible ? TryPublish() : TryRelease();
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

            if (outputSession == null)
            {
                isLocallyEligible = true;
                SetDiagnostic(
                    "AwaitingOutputSession",
                    "Local Player camera eligibility is active and is waiting for the session-scoped camera output injection.",
                    false);
                return true;
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

            publisher = creation.Publisher as LocalPlayerCameraRequestPublisher;
            if (publisher == null)
            {
                isLocallyEligible = false;
                SetDiagnostic(
                    "Blocked",
                    "Local Player camera publisher creation returned an unexpected publisher type.",
                    true);
                return false;
            }

            CameraRequestPublisherResult publishResult = publisher.Publish();
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
                $"Local Player camera request published. player='{GetPlayerDiagnosticName()}' scope='{EligibilityScopeId}' request='{request.RequestId}'.",
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

            CameraRequestPublisherResult releaseResult = publisher.Release();
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
            if (localPlayerHost == null || !localPlayerHost.HasJoinedSlot)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicitly joined LocalPlayerHostAuthoring.";
                return false;
            }

            if (playerActor == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an explicit prepared PlayerActorDeclaration.";
                return false;
            }

            if (!TryGetActorId(out _, out diagnostic))
            {
                return false;
            }

            if (rigComposer == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding requires a CameraRigComposer.";
                return false;
            }

            if (rigComposer.TargetSourceBehaviour != null &&
                !IsOwnedByPlayerActor(
                    playerActor.transform,
                    rigComposer.TargetSourceBehaviour.transform))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an Actor-owned camera target source.";
                return false;
            }

            CameraTargetResolveResult targets = rigComposer.ResolveCameraTargets(
                rigComposer.FollowRequirement,
                rigComposer.LookAtRequirement);
            if (!targets.IsSucceeded)
            {
                diagnostic =
                    $"Local Player camera target resolution failed. {targets.BlockingIssue}";
                return false;
            }

            if (!IsOwnedByPlayerActor(
                    playerActor.transform,
                    targets.Targets.FollowTarget))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an Actor-owned Follow target.";
                return false;
            }

            if (targets.Targets.LookAtTarget != null &&
                !IsOwnedByPlayerActor(
                    playerActor.transform,
                    targets.Targets.LookAtTarget))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires an Actor-owned Look At target.";
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

            diagnostic = string.Empty;
            return true;
        }

        private bool TryCreateRequest(
            CameraOutputId outputId,
            out CameraRequest request,
            out string diagnostic)
        {
            request = default;

            if (!TryGetActorId(out string actorId, out diagnostic))
            {
                return false;
            }

            CameraTargetResolveResult targets = rigComposer.ResolveCameraTargets(
                rigComposer.FollowRequirement,
                rigComposer.LookAtRequirement);
            if (!targets.IsSucceeded)
            {
                diagnostic =
                    $"Local Player camera target resolution failed. {targets.BlockingIssue}";
                return false;
            }

            string ownerId = localPlayerHost.JoinedPlayerSlotId.StableText;
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
                    targets.Source,
                    new CameraRequestPolicy(
                        precedence,
                        tieBreakerId.NormalizeText()),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(LocalPlayerCameraRequestBinding),
                    $"Local Player camera request for slot '{ownerId}' actor='{actorId}'.");

            if (!result.IsSucceeded)
            {
                diagnostic =
                    $"Local Player camera request creation failed. {result.BlockingIssue}";
                return false;
            }

            request = result.Request;
            diagnostic = string.Empty;
            return true;
        }

        private bool TryGetActorId(
            out string actorId,
            out string diagnostic)
        {
            actorId = string.Empty;
            diagnostic = string.Empty;

            if (playerActor == null)
            {
                diagnostic =
                    "Local Player Camera Request Binding has no PlayerActorDeclaration.";
                return false;
            }

            try
            {
                actorId = playerActor.ActorId.StableText;
            }
            catch (Exception exception)
            {
                diagnostic =
                    $"Local Player Camera Request Binding has an invalid Player Actor identity. {exception.Message}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(actorId))
            {
                diagnostic =
                    "Local Player Camera Request Binding requires a valid Player Actor identity.";
                return false;
            }

            return true;
        }

        private string GetPlayerDiagnosticName()
        {
            string slot = localPlayerHost != null && localPlayerHost.HasJoinedSlot
                ? localPlayerHost.JoinedPlayerSlotId.StableText
                : "<unjoined>";
            string actor = TryGetActorId(out string actorId, out _)
                ? actorId
                : "<missing>";
            return $"slot:{slot}|actor:{actor}";
        }

        private static bool IsOwnedByPlayerActor(
            Transform actorRoot,
            Transform target)
        {
            return actorRoot != null &&
                target != null &&
                (ReferenceEquals(actorRoot, target) || target.IsChildOf(actorRoot));
        }

        void ICameraOutputSessionConsumer.AttachOutputSession(
            CameraOutputSessionBinding binding)
        {
            outputSession = binding;
            if (binding == null)
            {
                SetDiagnostic(
                    "Blocked",
                    "Local Player camera output injection is missing.",
                    true);
                return;
            }

            if (isLocallyEligible && !IsPublished)
            {
                TryPublish();
                return;
            }

            SetDiagnostic(
                "OutputAttached",
                $"Local Player camera output attached. output='{binding.OutputIdText}'.",
                false);
        }

        void ICameraOutputSessionConsumer.DetachOutputSession(string reason)
        {
            TryRelease();
            outputSession = null;
            SetDiagnostic(
                "OutputDetached",
                $"Local Player camera output detached. reason='{reason}'.",
                false);
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
