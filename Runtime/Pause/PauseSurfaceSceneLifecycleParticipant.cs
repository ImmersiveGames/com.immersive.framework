using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.SceneLifecycle;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. IF-ADR-005 Cut C. Scene Lifecycle participant that discovers
    /// IPauseSurfaceAdapter components from Route Primary, RouteContent and ActivityContent
    /// scenes, symmetrically to how PauseProductBindingSceneLifecycleParticipant already
    /// discovers PlayerPauseInput/PauseRequestTrigger from the same scenes. It is intentionally
    /// a separate participant: Pause Surface lifecycle is not mixed with PlayerPauseInput
    /// binding lifecycle. Persistent Content is not discovered here; it remains the fixed
    /// baseline collected once at boot by GlobalUiSceneRuntime/FrameworkRuntimeHost, since it is
    /// never loaded/unloaded through SceneLifecycleRuntime.
    /// </summary>
    internal sealed class PauseSurfaceSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IPauseProductRequestPort _pauseSnapshotSource;
        private readonly FrameworkLogger _logger;
        private PauseSurfaceRuntime _surfaceRuntime;

        internal PauseSurfaceSceneLifecycleParticipant(
            IPauseProductRequestPort pauseSnapshotSource)
        {
            _pauseSnapshotSource = pauseSnapshotSource ??
                throw new ArgumentNullException(nameof(pauseSnapshotSource));
            _logger = FrameworkLogger.Create<PauseSurfaceSceneLifecycleParticipant>();
        }

        /// <summary>
        /// Wires the presentation target. PauseSurfaceRuntime is created later in the boot
        /// sequence than SceneLifecycleRuntime's participant list, so this participant starts
        /// with no target and becomes active once FrameworkRuntimeHost calls this after creating
        /// PauseSurfaceRuntime. Every Route/Activity scene load happens after that point.
        /// </summary>
        internal void SetSurfaceRuntime(PauseSurfaceRuntime surfaceRuntime)
        {
            _surfaceRuntime = surfaceRuntime;
        }

        public bool OnSceneAvailable(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            PauseSurfaceRuntime surfaceRuntime = _surfaceRuntime;
            if (surfaceRuntime == null)
            {
                return true;
            }

            List<IPauseSurfaceAdapter> adapters = Collect(roots);
            PauseSnapshot currentSnapshot = default;
            _pauseSnapshotSource.TryGetPauseSnapshot(out currentSnapshot);

            surfaceRuntime.SetSceneContribution(
                scene.handle.GetRawData(),
                adapters,
                currentSnapshot,
                nameof(PauseSurfaceSceneLifecycleParticipant),
                "scene-available");

            if (adapters.Count > 0)
            {
                diagnostic =
                    $"Pause surface scene contribution registered. scene='{SceneLabel(scene)}' adapters='{adapters.Count}'.";
                _logger.Debug(
                    "Pause surface scene contribution registered.",
                    LogFields.Of(
                        LogFields.Field("scene", SceneLabel(scene)),
                        LogFields.Field("adapterCount", adapters.Count)));
            }

            return true;
        }

        public bool OnSceneReleasing(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            _surfaceRuntime?.ReleaseSceneContribution(scene.handle.GetRawData());
            return true;
        }

        private static List<IPauseSurfaceAdapter> Collect(
            IReadOnlyList<GameObject> roots)
        {
            var result = new List<IPauseSurfaceAdapter>();
            var seen = new HashSet<IPauseSurfaceAdapter>();
            if (roots == null)
            {
                return result;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    if (behaviours[behaviourIndex] is IPauseSurfaceAdapter adapter &&
                        seen.Add(adapter))
                    {
                        result.Add(adapter);
                    }
                }
            }

            return result;
        }

        private static string SceneLabel(Scene scene) =>
            scene.IsValid()
                ? scene.name.NormalizeTextOrFallback("<unnamed>")
                : "<invalid>";
    }
}
