using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
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
        "P3M4B1 host-scoped Scene Local Player admission transaction composition adapter.")]
    internal sealed class SceneLocalPlayerAdmissionRuntimeHostModule : MonoBehaviour
    {
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
            return IsReady &&
                authoring != null &&
                ContainsAuthoring(authoring);
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
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                BindScene(SceneManager.GetSceneAt(index));
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
                return;
            }

            boundAuthoring.Add(authoring);
            authoring.BindRuntime(this);
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

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindScene(scene);
            diagnostic =
                $"Scene Local Player admission runtime attached loaded scene '{scene.name}'. surfaces='{BoundAuthoringCount}'.";
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
