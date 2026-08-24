using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// FrameworkRuntimeHost-scoped composition adapter for Scene Local Player host/Slot
    /// transactions. It binds declared product surfaces from loaded scenes, but never discovers
    /// Players by name, tag, hierarchy convention or global service lookup.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3M4B1/P3M4B2A/P3M5A host-scoped Scene Local Player admission, Activity lifecycle composition and deterministic loaded-scene reconciliation.")]
    internal sealed partial class SceneLocalPlayerAdmissionRuntimeHostModule : MonoBehaviour
    {
        private enum ContextualReleaseAuthority
        {
            ActivityExit = 10,
            SessionPlayerLeave = 20,
            SessionTermination = 30
        }

        private sealed class ResolvedAutomaticAuthoring
        {
            internal ResolvedAutomaticAuthoring(
                SceneLocalPlayerAdmissionAuthoring authoring,
                PlayerSlotId playerSlotId,
                int configuredIndex)
            {
                Authoring = authoring;
                PlayerSlotId = playerSlotId;
                ConfiguredIndex = configuredIndex;
            }

            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal PlayerSlotId PlayerSlotId { get; }
            internal int ConfiguredIndex { get; }
        }

        private readonly List<SceneLocalPlayerAdmissionAuthoring> _boundAuthoring = new();
        private FrameworkRuntimeHost _runtimeHost;
        private PlayerParticipationRuntimeContext _participationContext;
        private PlayerActorPreparationRuntimeHostModule _hostEvidenceOwner;
        private SceneLocalPlayerAdmissionRuntime _runtime;
        private RouteAsset _activityLifecycleRouteContext;
        private ActivityAsset _activityLifecycleActivityContext;
        private string _diagnostic = "Scene Local Player admission runtime is not initialized.";
        private SceneLocalPlayerAdmissionDiagnosticsSnapshot _lastDiagnostics =
            SceneLocalPlayerAdmissionDiagnosticsSnapshot.Empty(
                "No Scene-Provided Player admission operation has been recorded.");
        private bool _shuttingDown;

        internal bool IsReady =>
            _runtimeHost != null &&
            _participationContext != null &&
            _runtime != null;

        internal string Diagnostic => _diagnostic;
        internal int BoundAuthoringCount => _boundAuthoring.Count;
        internal int ActiveAdmissionCount => _runtime?.ActiveAdmissionCount ?? 0;
        internal PlayerParticipationRuntimeContext ParticipationContext => _participationContext;
        internal SceneLocalPlayerAdmissionDiagnosticsSnapshot LastDiagnostics => _lastDiagnostics;

        internal void SetActivityLifecycleContext(
            RouteAsset route,
            ActivityAsset nextActivity)
        {
            _activityLifecycleRouteContext = route;
            _activityLifecycleActivityContext = nextActivity;
        }

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            PlayerParticipationRuntimeContext participationContext,
            out SceneLocalPlayerAdmissionRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue = "Scene Local Player admission requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            if (participationContext == null)
            {
                issue = "Scene Local Player admission requires the Session Player participation context.";
                return false;
            }

            module = runtimeHost.GetComponent<SceneLocalPlayerAdmissionRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<SceneLocalPlayerAdmissionRuntimeHostModule>();
            }

            return module.TryInitialize(runtimeHost, participationContext, out issue);
        }

        internal bool TryInitialize(
            FrameworkRuntimeHost targetRuntimeHost,
            PlayerParticipationRuntimeContext targetParticipationContext,
            out string issue)
        {
            issue = string.Empty;
            if (IsReady)
            {
                if (ReferenceEquals(_runtimeHost, targetRuntimeHost) &&
                    ReferenceEquals(_participationContext, targetParticipationContext))
                {
                    BindLoadedScenes();
                    return true;
                }

                issue = "Scene Local Player admission runtime is already bound to another Session authority.";
                return false;
            }

            if (targetRuntimeHost == null || targetParticipationContext == null)
            {
                issue = "Scene Local Player admission runtime initialization requires explicit host and participation authorities.";
                _diagnostic = issue;
                return false;
            }

            PlayerActorPreparationRuntimeHostModule targetHostEvidenceOwner =
                targetRuntimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (targetHostEvidenceOwner == null ||
                !targetHostEvidenceOwner.IsReady)
            {
                issue =
                    "Scene Local Player admission requires the ready host-scoped physical Host evidence projection.";
                _diagnostic = issue;
                return false;
            }

            _runtimeHost = targetRuntimeHost;
            _participationContext = targetParticipationContext;
            _hostEvidenceOwner = targetHostEvidenceOwner;
            _runtime = new SceneLocalPlayerAdmissionRuntime(targetParticipationContext);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            BindLoadedScenes();
            _diagnostic =
                $"Scene Local Player admission runtime is ready. surfaces='{BoundAuthoringCount}' activeAdmissions='{ActiveAdmissionCount}'.";
            return true;
        }

        internal bool IsReadyFor(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            if (!IsReady || authoring == null)
            {
                return false;
            }

            if (!ContainsAuthoring(authoring))
            {
                BindScene(authoring.gameObject.scene);
            }

            return ContainsAuthoring(authoring);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryAdmit(
            SceneLocalPlayerAdmissionAuthoring authoring,
            RuntimeContentOwner assignmentOwner,
            string source,
            string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "AdmitSceneLocalPlayer",
                    authoring,
                    source,
                    reason,
                    IsReady
                        ? "Scene Local Player authoring surface is not bound to this Session runtime."
                        : _diagnostic);
            }

            bool hadActiveAdmission = _runtime.TryGetActiveToken(
                authoring,
                out _);
            SceneLocalPlayerAdmissionRuntimeResult result = _runtime.TryAdmit(
                authoring,
                assignmentOwner,
                source,
                reason);
            if (result != null && result.Succeeded)
            {
                bool hasRetainedPhysicalHost =
                    _hostEvidenceOwner.TryGetRetainedHostEvidence(
                        result.Token.PlayerSlotId,
                        out _);
                PlayerHostEvidenceResult registration = hasRetainedPhysicalHost
                    ? _hostEvidenceOwner.ReprojectHostEvidence(
                        result.Token.PlayerSlotId,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        result.Token.AssignmentToken,
                        result.Token.AssignmentToken.HostBindingIdentity,
                        source,
                        reason)
                    : _hostEvidenceOwner.RegisterHostEvidence(
                        result.Token.PlayerSlotId,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        result.Token.AssignmentToken,
                        result.Token.AssignmentToken.HostBindingIdentity,
                        authoring.LocalPlayerHost,
                        source,
                        reason);
                if (!registration.Succeeded)
                {
                    SceneLocalPlayerAdmissionRuntimeResult rollback =
                        _runtime.TryRelease(
                            authoring,
                            result.Token,
                            source,
                            "scene-host-evidence-registration-failed");
                    result = HostEvidenceFailure(
                        "AdmitSceneLocalPlayer",
                        result,
                        rollback,
                        registration,
                        rollback != null && rollback.Succeeded
                            ? SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit
                            : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation,
                        source,
                        reason);
                }
                else if (hasRetainedPhysicalHost &&
                         authoring.SceneLogicalPlayerActor != null)
                {
                    authoring.SceneLogicalPlayerActor.BindPlayerInputEvidence(
                        authoring.LocalPlayerHost.PlayerInput);
                }
            }

            RecordOperation(result, hadActiveAdmission, false);
            _diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, _diagnostic);
            return result;
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryRelease(
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "ReleaseSceneLocalPlayer",
                    authoring,
                    source,
                    reason,
                    IsReady
                        ? "Scene Local Player authoring surface is not bound to this Session runtime."
                        : _diagnostic);
            }

            _runtime.TryGetActiveToken(authoring, out SceneLocalPlayerAdmissionToken token);
            SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                authoring,
                token,
                ContextualReleaseAuthority.ActivityExit,
                default,
                source,
                reason);
            RecordOperation(result, token.IsValid, true);
            _diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, _diagnostic);
            return result;
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryRetireContextualRepresentation(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            return TryRetireContextualRepresentationWithHostEvidence(
                authoring,
                expectedToken,
                ContextualReleaseAuthority.ActivityExit,
                default,
                source,
                reason);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult
            TryRetireContextualRepresentationForSessionPlayerLeave(
                SceneLocalPlayerAdmissionAuthoring authoring,
                SceneLocalPlayerAdmissionToken expectedToken,
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            return TryRetireContextualRepresentationWithHostEvidence(
                authoring,
                expectedToken,
                ContextualReleaseAuthority.SessionPlayerLeave,
                leaveToken,
                source,
                reason);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult
            TryRetireContextualRepresentationForSessionTermination(
                SceneLocalPlayerAdmissionAuthoring authoring,
                SceneLocalPlayerAdmissionToken expectedToken,
                string source,
                string reason)
        {
            return TryRetireContextualRepresentationWithHostEvidence(
                authoring,
                expectedToken,
                ContextualReleaseAuthority.SessionTermination,
                default,
                source,
                reason);
        }

        private SceneLocalPlayerAdmissionRuntimeResult
            TryRetireContextualRepresentationWithHostEvidence(
                SceneLocalPlayerAdmissionAuthoring authoring,
                SceneLocalPlayerAdmissionToken expectedToken,
                ContextualReleaseAuthority authority,
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "RetireSceneLocalPlayerContext", authoring, source, reason, _diagnostic);
            }

            if (authority == ContextualReleaseAuthority.SessionPlayerLeave &&
                !TryConfirmSessionPlayerLeaveContextualRelease(
                    authoring,
                    expectedToken,
                    leaveToken,
                    source,
                    reason,
                    out SceneLocalPlayerAdmissionRuntimeResult leaveRejection))
            {
                return leaveRejection;
            }

            LocalPlayerHostAuthoring expectedEvidenceHost = authoring != null
                ? authoring.LocalPlayerHost
                : null;
            PlayerHostEvidenceSnapshot retainedEvidence = default;
            bool hasRetainedEvidence = expectedToken.IsValid &&
                _hostEvidenceOwner.TryGetRetainedHostEvidence(
                    expectedToken.PlayerSlotId,
                    out retainedEvidence);
            if (hasRetainedEvidence)
            {
                expectedEvidenceHost = retainedEvidence.Host;
            }

            if (authority != ContextualReleaseAuthority.ActivityExit &&
                (!hasRetainedEvidence || !retainedEvidence.HasContextualProjection))
            {
                SceneLocalPlayerAdmissionRuntimeResult residual =
                    authority == ContextualReleaseAuthority.SessionPlayerLeave
                    ? _runtime.TryRetireContextualRepresentationForSessionPlayerLeave(
                        authoring, expectedToken, leaveToken, source, reason)
                    : _runtime.TryRetireContextualRepresentationForSessionTermination(
                        authoring, expectedToken, source, reason);
                RecordOperation(residual, expectedToken.IsValid, true);
                _diagnostic = residual != null
                    ? residual.ToDiagnosticString()
                    : "Scene Local Player contextual retirement returned no residual result.";
                authoring.SetRuntimeResult(residual, _diagnostic);
                return residual;
            }

            PlayerHostEvidenceResult evidenceRelease = expectedToken.IsValid
                ? _hostEvidenceOwner.ReleaseHostEvidence(
                    expectedToken.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    expectedToken.AssignmentToken.HostBindingIdentity,
                    expectedEvidenceHost,
                    source,
                    reason + "; release-contextual-host-evidence")
                : null;
            if (expectedToken.IsValid &&
                (evidenceRelease == null || !evidenceRelease.Succeeded))
            {
                return HostEvidenceFailure(
                    "RetireSceneLocalPlayerContext",
                    null,
                    null,
                    evidenceRelease,
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    source,
                    reason,
                    authoring,
                    expectedToken);
            }

            SceneLocalPlayerAdmissionRuntimeResult result = authority switch
            {
                ContextualReleaseAuthority.SessionPlayerLeave =>
                    _runtime.TryRetireContextualRepresentationForSessionPlayerLeave(
                        authoring, expectedToken, leaveToken, source, reason),
                ContextualReleaseAuthority.SessionTermination =>
                    _runtime.TryRetireContextualRepresentationForSessionTermination(
                        authoring, expectedToken, source, reason),
                _ => _runtime.TryRetireContextualRepresentation(
                    authoring, expectedToken, source, reason)
            };
            if (result != null && result.Succeeded)
            {
                RecordOperation(result, expectedToken.IsValid, true);
                _diagnostic = result.ToDiagnosticString();
                authoring.SetRuntimeResult(result, _diagnostic);
                return result;
            }

            if (authority != ContextualReleaseAuthority.ActivityExit)
            {
                return result;
            }

            PlayerHostEvidenceResult restoration = expectedToken.IsValid
                ? _hostEvidenceOwner.RegisterHostEvidence(
                    expectedToken.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    expectedToken.AssignmentToken,
                    expectedToken.AssignmentToken.HostBindingIdentity,
                    expectedEvidenceHost,
                    source,
                    "restore-contextual-host-evidence-after-retirement-failure")
                : null;
            result = result != null && restoration != null && restoration.Succeeded
                ? result
                : HostEvidenceFailure(
                    "RetireSceneLocalPlayerContext",
                    result,
                    null,
                    restoration,
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation,
                    source,
                    reason,
                    authoring,
                    expectedToken);
            RecordOperation(result, expectedToken.IsValid, true);
            _diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, _diagnostic);
            return result;
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryRelease(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "ReleaseSceneLocalPlayer",
                    authoring,
                    source,
                    reason,
                    IsReady
                        ? "Scene Local Player authoring surface is not bound to this Session runtime."
                        : _diagnostic);
            }

            SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                authoring,
                expectedToken,
                ContextualReleaseAuthority.ActivityExit,
                default,
                source,
                reason);
            RecordOperation(result, expectedToken.IsValid, true);
            _diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, _diagnostic);
            return result;
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryReleaseForSessionPlayerLeave(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "RetireSceneLocalPlayerForSessionLeave",
                    authoring,
                    source,
                    reason,
                    IsReady
                        ? "Scene Local Player authoring surface is not bound to this Session runtime."
                        : _diagnostic);
            }

            if (!TryConfirmSessionPlayerLeaveContextualRelease(
                    authoring,
                    expectedToken,
                    leaveToken,
                    source,
                    reason,
                    out SceneLocalPlayerAdmissionRuntimeResult leaveRejection))
            {
                return leaveRejection;
            }

            SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                authoring,
                expectedToken,
                ContextualReleaseAuthority.SessionPlayerLeave,
                leaveToken,
                source,
                reason);
            RecordOperation(result, expectedToken.IsValid, true);
            _diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, _diagnostic);
            return result;
        }

        private bool TryConfirmSessionPlayerLeaveContextualRelease(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason,
            out SceneLocalPlayerAdmissionRuntimeResult rejection)
        {
            rejection = null;
            SessionPlayerLeaveRuntimeResult confirmation =
                _participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    source,
                    reason + "; confirm-session-player-leave-before-contextual-retirement");
            if (confirmation != null && confirmation.Succeeded)
            {
                return true;
            }

            PlayerSlotRuntimeSnapshot slot = confirmation != null
                ? confirmation.CurrentSlot
                : default;
            rejection = new SceneLocalPlayerAdmissionRuntimeResult(
                SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                "RetireSceneLocalPlayerForSessionLeave",
                authoring,
                expectedToken,
                null,
                null,
                null,
                slot,
                slot,
                source,
                reason,
                confirmation != null
                    ? "Scene contextual retirement rejected a foreign or stale Session Player Leave correlation. " + confirmation.Message
                    : "Scene contextual retirement received no Session Player Leave confirmation result.");
            return false;
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryReleaseForSessionTermination(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "RetireSceneLocalPlayerForSessionTermination",
                    authoring,
                    source,
                    reason,
                    IsReady
                        ? "Scene Local Player authoring surface is not bound to this Session runtime."
                        : _diagnostic);
            }

            SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                authoring,
                expectedToken,
                ContextualReleaseAuthority.SessionTermination,
                default,
                source,
                reason);
            RecordOperation(result, expectedToken.IsValid, true);
            _diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, _diagnostic);
            return result;
        }

        private SceneLocalPlayerAdmissionRuntimeResult TryReleaseWithHostEvidence(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            ContextualReleaseAuthority authority,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            if (!expectedToken.IsValid)
            {
                return ReleaseRuntimeContextualRepresentation(
                    authoring,
                    expectedToken,
                    authority,
                    leaveToken,
                    source,
                    reason);
            }

            if (!_runtime.TryGetActiveToken(
                    authoring,
                    out SceneLocalPlayerAdmissionToken activeToken) ||
                activeToken != expectedToken)
            {
                return ReleaseRuntimeContextualRepresentation(
                    authoring,
                    expectedToken,
                    authority,
                    leaveToken,
                    source,
                    reason);
            }

            // A reprojected Activity owns its local admission, while the retained evidence
            // always references the Session physical Host. Retire the contextual projection
            // using that retained reference; never use the Activity Host as physical identity.
            LocalPlayerHostAuthoring expectedEvidenceHost = authoring.LocalPlayerHost;
            PlayerHostEvidenceSnapshot retainedEvidence = default;
            bool hasRetainedEvidence = _hostEvidenceOwner.TryGetRetainedHostEvidence(
                expectedToken.PlayerSlotId,
                out retainedEvidence);
            if (hasRetainedEvidence)
            {
                expectedEvidenceHost = retainedEvidence.Host;
            }

            if (authority != ContextualReleaseAuthority.ActivityExit &&
                (!hasRetainedEvidence || !retainedEvidence.HasContextualProjection))
            {
                return ReleaseRuntimeContextualRepresentation(
                    authoring,
                    expectedToken,
                    authority,
                    leaveToken,
                    source,
                    reason);
            }

            PlayerHostEvidenceResult evidenceRelease =
                _hostEvidenceOwner.ReleaseHostEvidence(
                    expectedToken.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    expectedToken.AssignmentToken.HostBindingIdentity,
                    expectedEvidenceHost,
                    source,
                    reason);
            if (!evidenceRelease.Succeeded)
            {
                return HostEvidenceFailure(
                    "ReleaseSceneLocalPlayer",
                    null,
                    null,
                    evidenceRelease,
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    source,
                    reason,
                    authoring,
                    expectedToken);
            }

            SceneLocalPlayerAdmissionRuntimeResult result = ReleaseRuntimeContextualRepresentation(
                authoring,
                expectedToken,
                authority,
                leaveToken,
                source,
                reason);
            if (result != null && result.Succeeded)
            {
                return result;
            }

            if (authority != ContextualReleaseAuthority.ActivityExit)
            {
                return result;
            }

            PlayerHostEvidenceResult restoration =
                _hostEvidenceOwner.RegisterHostEvidence(
                    expectedToken.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    expectedToken.AssignmentToken,
                    expectedToken.AssignmentToken.HostBindingIdentity,
                    expectedEvidenceHost,
                    source,
                    "restore-scene-host-evidence-after-release-failure");
            return result != null && restoration.Succeeded
                ? result
                : HostEvidenceFailure(
                    "ReleaseSceneLocalPlayer",
                    result,
                    null,
                    restoration,
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation,
                    source,
                    reason,
                    authoring,
                    expectedToken);
        }

        private SceneLocalPlayerAdmissionRuntimeResult ReleaseRuntimeContextualRepresentation(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            ContextualReleaseAuthority authority,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            return authority switch
            {
                ContextualReleaseAuthority.SessionPlayerLeave =>
                    _runtime.TryReleaseForSessionPlayerLeave(
                        authoring, expectedToken, leaveToken, source, reason),
                ContextualReleaseAuthority.SessionTermination =>
                    _runtime.TryReleaseForSessionTermination(
                        authoring, expectedToken, source, reason),
                _ => _runtime.TryRelease(authoring, expectedToken, source, reason)
            };
        }

        private static SceneLocalPlayerAdmissionRuntimeResult HostEvidenceFailure(
            string operation,
            SceneLocalPlayerAdmissionRuntimeResult primary,
            SceneLocalPlayerAdmissionRuntimeResult compensation,
            PlayerHostEvidenceResult evidence,
            SceneLocalPlayerAdmissionRuntimeStatus status,
            string source,
            string reason,
            SceneLocalPlayerAdmissionAuthoring authoring = null,
            SceneLocalPlayerAdmissionToken token = default)
        {
            SceneLocalPlayerAdmissionRuntimeResult basis = primary ?? compensation;
            return new SceneLocalPlayerAdmissionRuntimeResult(
                status,
                status,
                operation,
                authoring ?? basis?.Authoring,
                token.IsValid
                    ? token
                    : basis != null
                        ? basis.Token
                        : default,
                basis?.ReservationResult,
                basis?.SlotOperationResult,
                compensation?.CompensationResult,
                basis != null ? basis.PreviousSlot : default,
                compensation != null
                    ? compensation.CurrentSlot
                    : basis != null
                        ? basis.CurrentSlot
                        : default,
                source,
                reason,
                $"Physical Host evidence operation failed. {(evidence != null ? evidence.ToDiagnosticString() : "<no-result>")} " +
                $"compensationSucceeded='{(compensation != null && compensation.Succeeded)}'.",
                basis?.AssignmentResult,
                compensation?.AssignmentCompensationResult);
        }

        internal bool TryGetActiveToken(
            SceneLocalPlayerAdmissionAuthoring authoring,
            out SceneLocalPlayerAdmissionToken token)
        {
            token = default;
            return _runtime != null && _runtime.TryGetActiveToken(authoring, out token);
        }


        internal bool TryGetSlotSnapshot(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            snapshot = default;
            return _participationContext != null &&
                _participationContext.TryGetSlotSnapshot(playerSlotId, out snapshot);
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return _participationContext != null
                ? _participationContext.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    _diagnostic);
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return _participationContext != null
                ? _participationContext.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    _diagnostic);
        }

        internal bool TryResolveAutomaticActivityAuthoring(
            ActivityAsset activity,
            out IReadOnlyList<SceneLocalPlayerAdmissionAuthoring> authoring,
            out string issue)
        {
            var resolved = new List<ResolvedAutomaticAuthoring>();
            authoring = Array.Empty<SceneLocalPlayerAdmissionAuthoring>();
            issue = string.Empty;

            if (!IsReady)
            {
                issue = _diagnostic;
                return false;
            }

            if (activity == null)
            {
                issue = "Scene Local Player automatic admission requires an Activity.";
                return false;
            }

            ReconcileLoadedSceneAuthoring();

            RouteAsset routeContext =
                ResolveActivityLifecycleRouteContext(activity);

            PlayerParticipationSnapshot snapshot = _participationContext.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized)
            {
                issue = "Scene Local Player automatic admission requires an initialized Session participation snapshot.";
                return false;
            }

            var slotIds = new HashSet<PlayerSlotId>();
            var hosts = new List<LocalPlayerHostAuthoring>();
            var actors = new List<PlayerActorDeclaration>();

            for (int index = 0; index < _boundAuthoring.Count; index++)
            {
                SceneLocalPlayerAdmissionAuthoring candidate = _boundAuthoring[index];
                if (candidate == null ||
                    candidate.AdmissionTiming != SceneLocalPlayerAdmissionTiming.OnActivityEnter ||
                    !IsDeclaredByActivityOrRoute(
                        candidate,
                        activity,
                        routeContext))
                {
                    continue;
                }

                if (!candidate.TryValidateRuntimeEvidence(out string candidateIssue))
                {
                    issue = $"Scene Local Player Admission '{candidate.name}' is invalid. {candidateIssue}";
                    return false;
                }

                if (!candidate.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out candidateIssue))
                {
                    issue = candidateIssue;
                    return false;
                }

                int configuredIndex = -1;
                for (int slotIndex = 0; slotIndex < snapshot.Slots.Count; slotIndex++)
                {
                    if (snapshot.Slots[slotIndex].PlayerSlotId == playerSlotId)
                    {
                        configuredIndex = snapshot.Slots[slotIndex].ConfiguredIndex;
                        break;
                    }
                }

                if (configuredIndex < 0)
                {
                    issue = $"Scene Local Player Admission '{candidate.name}' references Slot '{playerSlotId.StableText}', which is not configured in the Session.";
                    return false;
                }

                if (!slotIds.Add(playerSlotId))
                {
                    issue = $"Activity '{activity.ActivityName}' declares more than one automatic Scene Local Player Admission for Slot '{playerSlotId.StableText}'.";
                    return false;
                }

                if (ContainsReference(hosts, candidate.LocalPlayerHost))
                {
                    issue = $"Activity '{activity.ActivityName}' reuses Local Player Host '{candidate.LocalPlayerHost.name}' across automatic Scene Local Player Admission surfaces.";
                    return false;
                }

                if (ContainsReference(actors, candidate.SceneLogicalPlayerActor))
                {
                    issue = $"Activity '{activity.ActivityName}' reuses Scene Logical Player Actor '{candidate.SceneLogicalPlayerActor.name}' across automatic admission surfaces.";
                    return false;
                }

                hosts.Add(candidate.LocalPlayerHost);
                actors.Add(candidate.SceneLogicalPlayerActor);
                resolved.Add(new ResolvedAutomaticAuthoring(
                    candidate,
                    playerSlotId,
                    configuredIndex));
            }

            resolved.Sort((left, right) =>
                left.ConfiguredIndex.CompareTo(right.ConfiguredIndex));
            var ordered = new SceneLocalPlayerAdmissionAuthoring[resolved.Count];
            for (int index = 0; index < resolved.Count; index++)
            {
                ordered[index] = resolved[index].Authoring;
            }

            authoring = ordered;
            return true;
        }

        internal void HandleAuthoringDestroyed(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            if (_shuttingDown || ReferenceEquals(authoring, null))
            {
                return;
            }

            if (_runtime != null && _runtime.TryGetActiveToken(authoring, out SceneLocalPlayerAdmissionToken token))
            {
                SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                    authoring,
                    token,
                    ContextualReleaseAuthority.ActivityExit,
                    default,
                    nameof(SceneLocalPlayerAdmissionRuntimeHostModule),
                    "authoring-destroyed-best-effort-release");
                RecordOperation(result, true, true);
                _diagnostic = result.ToDiagnosticString();
            }

            RemoveAuthoring(authoring);
            authoring.UnbindRuntime(this, "Scene Local Player authoring surface was destroyed.");
        }

        private void BindLoadedScenes()
        {
            PruneDestroyedAuthoring();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                BindScene(SceneManager.GetSceneAt(index));
            }
        }

        private void ReconcileLoadedSceneAuthoring()
        {
            BindLoadedScenes();
            PruneDestroyedAuthoring();
            if (!TryRestoreCompositeLifecycleSource(out string sourceIssue))
            {
                _diagnostic =
                    "Scene Local Player admission runtime reconciled loaded scenes, " +
                    "but could not restore the composite Activity lifecycle source. " +
                    sourceIssue;
                return;
            }

            _diagnostic =
                $"Scene Local Player admission runtime reconciled loaded scenes. " +
                $"surfaces='{BoundAuthoringCount}' activeAdmissions='{ActiveAdmissionCount}' " +
                "lifecycleSource='SceneLocalPlayerComposite'.";
        }

        private void PruneDestroyedAuthoring()
        {
            for (int index = _boundAuthoring.Count - 1; index >= 0; index--)
            {
                if (_boundAuthoring[index] == null)
                {
                    _boundAuthoring.RemoveAt(index);
                }
            }
        }

        private void BindScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                SceneLocalPlayerAdmissionAuthoring[] declarations =
                    roots[rootIndex].GetComponentsInChildren<SceneLocalPlayerAdmissionAuthoring>(true);
                for (int declarationIndex = 0; declarationIndex < declarations.Length; declarationIndex++)
                {
                    BindAuthoring(declarations[declarationIndex]);
                }
            }
        }

        private void BindAuthoring(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            if (authoring == null)
            {
                return;
            }

            if (ContainsAuthoring(authoring))
            {
                authoring.BindRuntime(this);
                TryRestoreCompositeLifecycleSource(out _);
                return;
            }

            _boundAuthoring.Add(authoring);
            authoring.BindRuntime(this);
            TryRestoreCompositeLifecycleSource(out _);
        }

        private bool ContainsAuthoring(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            for (int index = 0; index < _boundAuthoring.Count; index++)
            {
                if (ReferenceEquals(_boundAuthoring[index], authoring))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveAuthoring(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            for (int index = _boundAuthoring.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(_boundAuthoring[index], authoring))
                {
                    _boundAuthoring.RemoveAt(index);
                    return;
                }
            }
        }


        private RouteAsset ResolveActivityLifecycleRouteContext(
            ActivityAsset activity)
        {
            if (activity == null ||
                _activityLifecycleActivityContext == null ||
                !ReferenceEquals(_activityLifecycleActivityContext, activity))
            {
                return null;
            }

            return _activityLifecycleRouteContext;
        }

        private static bool IsDeclaredByActivityOrRoute(
            SceneLocalPlayerAdmissionAuthoring authoring,
            ActivityAsset activity,
            RouteAsset route)
        {
            return IsDeclaredByRoutePrimaryScene(authoring, route) ||
                IsDeclaredByActivity(authoring, activity);
        }

        private static bool IsDeclaredByRoutePrimaryScene(
            SceneLocalPlayerAdmissionAuthoring authoring,
            RouteAsset route)
        {
            if (authoring == null ||
                route == null ||
                !route.HasPrimaryScene ||
                !authoring.gameObject.scene.IsValid())
            {
                return false;
            }

            string scenePath =
                NormalizeScenePath(authoring.gameObject.scene.path);
            string routePrimaryScenePath =
                NormalizeScenePath(route.PrimaryScenePath);

            return !string.IsNullOrEmpty(scenePath) &&
                !string.IsNullOrEmpty(routePrimaryScenePath) &&
                string.Equals(
                    scenePath,
                    routePrimaryScenePath,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDeclaredByActivity(
            SceneLocalPlayerAdmissionAuthoring authoring,
            ActivityAsset activity)
        {
            if (authoring == null || activity == null ||
                activity.ActivityContentProfile == null ||
                !authoring.gameObject.scene.IsValid())
            {
                return false;
            }

            string scenePath = NormalizeScenePath(authoring.gameObject.scene.path);
            string sceneName = authoring.gameObject.scene.name ?? string.Empty;
            IReadOnlyList<ActivityContentSceneEntry> entries =
                activity.ActivityContentProfile.Scenes;
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityContentSceneEntry entry = entries[index];
                if (entry == null || !entry.HasScene)
                {
                    continue;
                }

                string entryPath = NormalizeScenePath(entry.ScenePath);
                if (!string.IsNullOrEmpty(scenePath) &&
                    !string.IsNullOrEmpty(entryPath) &&
                    string.Equals(scenePath, entryPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(sceneName) &&
                    !string.IsNullOrEmpty(entry.SceneName) &&
                    string.Equals(sceneName, entry.SceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeScenePath(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace('\\', '/');
        }

        private static bool ContainsReference<T>(IReadOnlyList<T> values, T candidate)
            where T : class
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (ReferenceEquals(values[index], candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PruneDestroyedAuthoring();
            BindScene(scene);
            if (!TryRestoreCompositeLifecycleSource(out string sourceIssue))
            {
                _diagnostic =
                    $"Scene Local Player admission runtime attached loaded scene '{scene.name}', " +
                    "but could not restore the composite Activity lifecycle source. " +
                    sourceIssue;
                return;
            }

            _diagnostic =
                $"Scene Local Player admission runtime attached loaded scene '{scene.name}'. " +
                $"surfaces='{BoundAuthoringCount}' activeAdmissions='{ActiveAdmissionCount}' " +
                "lifecycleSource='SceneLocalPlayerComposite'.";
        }

        private bool TryRestoreCompositeLifecycleSource(out string issue)
        {
            issue = string.Empty;
            if (_runtimeHost == null)
            {
                issue = "FrameworkRuntimeHost is unavailable.";
                return false;
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                _runtimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (preparation == null || !preparation.IsReady)
            {
                issue =
                    "Player Actor preparation authority is unavailable for Scene Local Player lifecycle composition.";
                return false;
            }

            return preparation.TryComposeSceneLocalPlayerAdmissionLifecycle(
                this,
                out issue);
        }

        private void OnDestroy()
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            var snapshot = new List<SceneLocalPlayerAdmissionAuthoring>(_boundAuthoring);
            for (int index = snapshot.Count - 1; index >= 0; index--)
            {
                SceneLocalPlayerAdmissionAuthoring authoring = snapshot[index];
                if (authoring == null)
                {
                    continue;
                }

                if (_runtime != null && _runtime.TryGetActiveToken(authoring, out SceneLocalPlayerAdmissionToken token))
                {
                    SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                        authoring,
                        token,
                        ContextualReleaseAuthority.SessionTermination,
                        default,
                        nameof(SceneLocalPlayerAdmissionRuntimeHostModule),
                        "runtime-host-shutdown-best-effort-release");
                    RecordOperation(result, true, true);
                }

                authoring.UnbindRuntime(this, "Session Scene Local Player admission runtime was released.");
            }

            _boundAuthoring.Clear();
            _activityLifecycleRouteContext = null;
            _activityLifecycleActivityContext = null;
            _runtime = null;
            _hostEvidenceOwner = null;
            _participationContext = null;
            _runtimeHost = null;
            _diagnostic = "Session Scene Local Player admission runtime was released.";
        }

        private void RecordOperation(
            SceneLocalPlayerAdmissionRuntimeResult result,
            bool hadActiveAdmission,
            bool releaseRequested)
        {
            if (result == null)
            {
                return;
            }

            PlayerSlotId slot = result.Token.PlayerSlotId.IsValid
                ? result.Token.PlayerSlotId
                : result.CurrentSlot.PlayerSlotId.IsValid
                    ? result.CurrentSlot.PlayerSlotId
                    : result.PreviousSlot.PlayerSlotId;
            ActorId actor = result.Authoring != null &&
                result.Authoring.SceneLogicalPlayerActor != null
                    ? result.Authoring.SceneLogicalPlayerActor.ActorId
                    : default;
            bool hostEvidencePresent = slot.IsValid &&
                _hostEvidenceOwner != null &&
                _hostEvidenceOwner.TryGetRetainedHostEvidence(slot, out _);
            PlayerParticipationSnapshot participation =
                _participationContext != null
                    ? _participationContext.CreateSnapshot()
                    : null;
            _lastDiagnostics = new SceneLocalPlayerAdmissionDiagnosticsSnapshot(
                result.Operation,
                result.Status,
                result.Source,
                result.Reason,
                result.Message,
                hadActiveAdmission,
                releaseRequested,
                result.Status == SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased,
                result.Status == SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyReleased,
                slot,
                actor,
                hostEvidencePresent,
                result.Token.IsValid,
                ActiveAdmissionCount,
                participation != null ? participation.JoinedCount : 0);
        }
    }
}
