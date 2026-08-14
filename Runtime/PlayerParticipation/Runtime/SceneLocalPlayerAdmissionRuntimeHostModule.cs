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

        private readonly List<SceneLocalPlayerAdmissionAuthoring> boundAuthoring = new();
        private FrameworkRuntimeHost runtimeHost;
        private PlayerParticipationRuntimeContext participationContext;
        private PlayerActorPreparationRuntimeHostModule hostEvidenceOwner;
        private SceneLocalPlayerAdmissionRuntime runtime;
        private RouteAsset activityLifecycleRouteContext;
        private ActivityAsset activityLifecycleActivityContext;
        private string diagnostic = "Scene Local Player admission runtime is not initialized.";
        private SceneLocalPlayerAdmissionDiagnosticsSnapshot lastDiagnostics =
            SceneLocalPlayerAdmissionDiagnosticsSnapshot.Empty(
                "No Scene-Provided Player admission operation has been recorded.");
        private bool shuttingDown;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            runtime != null;

        internal string Diagnostic => diagnostic;
        internal int BoundAuthoringCount => boundAuthoring.Count;
        internal int ActiveAdmissionCount => runtime?.ActiveAdmissionCount ?? 0;
        internal PlayerParticipationRuntimeContext ParticipationContext => participationContext;
        internal SceneLocalPlayerAdmissionDiagnosticsSnapshot LastDiagnostics => lastDiagnostics;

        internal void SetActivityLifecycleContext(
            RouteAsset route,
            ActivityAsset nextActivity)
        {
            activityLifecycleRouteContext = route;
            activityLifecycleActivityContext = nextActivity;
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
                if (ReferenceEquals(runtimeHost, targetRuntimeHost) &&
                    ReferenceEquals(participationContext, targetParticipationContext))
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
                diagnostic = issue;
                return false;
            }

            PlayerActorPreparationRuntimeHostModule targetHostEvidenceOwner =
                targetRuntimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>();
            if (targetHostEvidenceOwner == null ||
                !targetHostEvidenceOwner.IsReady)
            {
                issue =
                    "Scene Local Player admission requires the ready host-scoped physical Host evidence projection.";
                diagnostic = issue;
                return false;
            }

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipationContext;
            hostEvidenceOwner = targetHostEvidenceOwner;
            runtime = new SceneLocalPlayerAdmissionRuntime(targetParticipationContext);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            BindLoadedScenes();
            diagnostic =
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
                        : diagnostic);
            }

            bool hadActiveAdmission = runtime.TryGetActiveToken(
                authoring,
                out _);
            SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryAdmit(
                authoring,
                assignmentOwner,
                source,
                reason);
            if (result != null && result.Succeeded)
            {
                PlayerHostEvidenceResult registration =
                    hostEvidenceOwner.RegisterHostEvidence(
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
                        runtime.TryRelease(
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
            }

            RecordOperation(result, hadActiveAdmission, false);
            diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, diagnostic);
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
                        : diagnostic);
            }

            runtime.TryGetActiveToken(authoring, out SceneLocalPlayerAdmissionToken token);
            SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                authoring,
                token,
                source,
                reason);
            RecordOperation(result, token.IsValid, true);
            diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, diagnostic);
            return result;
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryRetireContextualRepresentation(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            if (!IsReadyFor(authoring))
            {
                return SceneLocalPlayerAdmissionRuntimeResult.RuntimeUnavailable(
                    "RetireSceneLocalPlayerContext", authoring, source, reason, diagnostic);
            }

            SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryRetireContextualRepresentation(
                authoring, expectedToken, source, reason);
            RecordOperation(result, expectedToken.IsValid, true);
            diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, diagnostic);
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
                        : diagnostic);
            }

            SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                authoring,
                expectedToken,
                source,
                reason);
            RecordOperation(result, expectedToken.IsValid, true);
            diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, diagnostic);
            return result;
        }

        private SceneLocalPlayerAdmissionRuntimeResult TryReleaseWithHostEvidence(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            if (!expectedToken.IsValid)
            {
                return runtime.TryRelease(
                    authoring,
                    expectedToken,
                    source,
                    reason);
            }

            if (!runtime.TryGetActiveToken(
                    authoring,
                    out SceneLocalPlayerAdmissionToken activeToken) ||
                activeToken != expectedToken)
            {
                return runtime.TryRelease(
                    authoring,
                    expectedToken,
                    source,
                    reason);
            }

            PlayerHostEvidenceResult evidenceRelease =
                hostEvidenceOwner.ReleaseHostEvidence(
                    expectedToken.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    expectedToken.AssignmentToken.HostBindingIdentity,
                    authoring.LocalPlayerHost,
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

            SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryRelease(
                authoring,
                expectedToken,
                source,
                reason);
            if (result != null && result.Succeeded)
            {
                return result;
            }

            PlayerHostEvidenceResult restoration =
                hostEvidenceOwner.RegisterHostEvidence(
                    expectedToken.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    expectedToken.AssignmentToken,
                    expectedToken.AssignmentToken.HostBindingIdentity,
                    authoring.LocalPlayerHost,
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
                $"Physical Host evidence operation failed. {evidence.ToDiagnosticString()} " +
                $"compensationSucceeded='{(compensation != null && compensation.Succeeded)}'.",
                basis?.AssignmentResult,
                compensation?.AssignmentCompensationResult);
        }

        internal bool TryGetActiveToken(
            SceneLocalPlayerAdmissionAuthoring authoring,
            out SceneLocalPlayerAdmissionToken token)
        {
            token = default;
            return runtime != null && runtime.TryGetActiveToken(authoring, out token);
        }


        internal bool TryGetSlotSnapshot(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            snapshot = default;
            return participationContext != null &&
                participationContext.TryGetSlotSnapshot(playerSlotId, out snapshot);
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return participationContext != null
                ? participationContext.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    diagnostic);
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return participationContext != null
                ? participationContext.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    diagnostic);
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
                issue = diagnostic;
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

            PlayerParticipationSnapshot snapshot = participationContext.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized)
            {
                issue = "Scene Local Player automatic admission requires an initialized Session participation snapshot.";
                return false;
            }

            var slotIds = new HashSet<PlayerSlotId>();
            var hosts = new List<LocalPlayerHostAuthoring>();
            var actors = new List<PlayerActorDeclaration>();

            for (int index = 0; index < boundAuthoring.Count; index++)
            {
                SceneLocalPlayerAdmissionAuthoring candidate = boundAuthoring[index];
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
            if (shuttingDown || ReferenceEquals(authoring, null))
            {
                return;
            }

            if (runtime != null && runtime.TryGetActiveToken(authoring, out SceneLocalPlayerAdmissionToken token))
            {
                SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                    authoring,
                    token,
                    nameof(SceneLocalPlayerAdmissionRuntimeHostModule),
                    "authoring-destroyed-best-effort-release");
                RecordOperation(result, true, true);
                diagnostic = result.ToDiagnosticString();
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
                diagnostic =
                    "Scene Local Player admission runtime reconciled loaded scenes, " +
                    "but could not restore the composite Activity lifecycle source. " +
                    sourceIssue;
                return;
            }

            diagnostic =
                $"Scene Local Player admission runtime reconciled loaded scenes. " +
                $"surfaces='{BoundAuthoringCount}' activeAdmissions='{ActiveAdmissionCount}' " +
                "lifecycleSource='SceneLocalPlayerComposite'.";
        }

        private void PruneDestroyedAuthoring()
        {
            for (int index = boundAuthoring.Count - 1; index >= 0; index--)
            {
                if (boundAuthoring[index] == null)
                {
                    boundAuthoring.RemoveAt(index);
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

            boundAuthoring.Add(authoring);
            authoring.BindRuntime(this);
            TryRestoreCompositeLifecycleSource(out _);
        }

        private bool ContainsAuthoring(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            for (int index = 0; index < boundAuthoring.Count; index++)
            {
                if (ReferenceEquals(boundAuthoring[index], authoring))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveAuthoring(SceneLocalPlayerAdmissionAuthoring authoring)
        {
            for (int index = boundAuthoring.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(boundAuthoring[index], authoring))
                {
                    boundAuthoring.RemoveAt(index);
                    return;
                }
            }
        }


        private RouteAsset ResolveActivityLifecycleRouteContext(
            ActivityAsset activity)
        {
            if (activity == null ||
                activityLifecycleActivityContext == null ||
                !ReferenceEquals(activityLifecycleActivityContext, activity))
            {
                return null;
            }

            return activityLifecycleRouteContext;
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
                diagnostic =
                    $"Scene Local Player admission runtime attached loaded scene '{scene.name}', " +
                    "but could not restore the composite Activity lifecycle source. " +
                    sourceIssue;
                return;
            }

            diagnostic =
                $"Scene Local Player admission runtime attached loaded scene '{scene.name}'. " +
                $"surfaces='{BoundAuthoringCount}' activeAdmissions='{ActiveAdmissionCount}' " +
                "lifecycleSource='SceneLocalPlayerComposite'.";
        }

        private bool TryRestoreCompositeLifecycleSource(out string issue)
        {
            issue = string.Empty;
            if (runtimeHost == null)
            {
                issue = "FrameworkRuntimeHost is unavailable.";
                return false;
            }

            PlayerActorPreparationRuntimeHostModule preparation =
                runtimeHost.GetComponent<PlayerActorPreparationRuntimeHostModule>();
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
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            var snapshot = new List<SceneLocalPlayerAdmissionAuthoring>(boundAuthoring);
            for (int index = snapshot.Count - 1; index >= 0; index--)
            {
                SceneLocalPlayerAdmissionAuthoring authoring = snapshot[index];
                if (authoring == null)
                {
                    continue;
                }

                if (runtime != null && runtime.TryGetActiveToken(authoring, out SceneLocalPlayerAdmissionToken token))
                {
                    SceneLocalPlayerAdmissionRuntimeResult result = TryReleaseWithHostEvidence(
                        authoring,
                        token,
                        nameof(SceneLocalPlayerAdmissionRuntimeHostModule),
                        "runtime-host-shutdown-best-effort-release");
                    RecordOperation(result, true, true);
                }

                authoring.UnbindRuntime(this, "Session Scene Local Player admission runtime was released.");
            }

            boundAuthoring.Clear();
            activityLifecycleRouteContext = null;
            activityLifecycleActivityContext = null;
            runtime = null;
            hostEvidenceOwner = null;
            participationContext = null;
            runtimeHost = null;
            diagnostic = "Session Scene Local Player admission runtime was released.";
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
                hostEvidenceOwner != null &&
                hostEvidenceOwner.TryGetRetainedHostEvidence(slot, out _);
            PlayerParticipationSnapshot participation =
                participationContext != null
                    ? participationContext.CreateSnapshot()
                    : null;
            lastDiagnostics = new SceneLocalPlayerAdmissionDiagnosticsSnapshot(
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
