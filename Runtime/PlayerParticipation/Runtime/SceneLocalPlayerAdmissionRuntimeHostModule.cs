using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
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
    internal sealed class SceneLocalPlayerAdmissionRuntimeHostModule : MonoBehaviour
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
        private SceneLocalPlayerAdmissionRuntime runtime;
        private string diagnostic = "Scene Local Player admission runtime is not initialized.";
        private bool shuttingDown;

        internal bool IsReady =>
            runtimeHost != null &&
            participationContext != null &&
            runtime != null;

        internal string Diagnostic => diagnostic;
        internal int BoundAuthoringCount => boundAuthoring.Count;
        internal int ActiveAdmissionCount => runtime?.ActiveAdmissionCount ?? 0;
        internal PlayerParticipationRuntimeContext ParticipationContext => participationContext;

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

            runtimeHost = targetRuntimeHost;
            participationContext = targetParticipationContext;
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

            SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryAdmit(
                authoring,
                source,
                reason);
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
            SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryRelease(
                authoring,
                token,
                source,
                reason);
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

            SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryRelease(
                authoring,
                expectedToken,
                source,
                reason);
            diagnostic = result.ToDiagnosticString();
            authoring.SetRuntimeResult(result, diagnostic);
            return result;
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
                    !IsDeclaredByActivity(candidate, activity))
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
                SceneLocalPlayerAdmissionRuntimeResult result = runtime.TryRelease(
                    authoring,
                    token,
                    nameof(SceneLocalPlayerAdmissionRuntimeHostModule),
                    "authoring-destroyed-best-effort-release");
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
                    runtime.TryRelease(
                        authoring,
                        token,
                        nameof(SceneLocalPlayerAdmissionRuntimeHostModule),
                        "runtime-host-shutdown-best-effort-release");
                }

                authoring.UnbindRuntime(this, "Session Scene Local Player admission runtime was released.");
            }

            boundAuthoring.Clear();
            runtime = null;
            participationContext = null;
            runtimeHost = null;
            diagnostic = "Session Scene Local Player admission runtime was released.";
        }
    }
}
