using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.SceneLifecycle;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Internal. IF-ADR-005 Cut C. Scene Lifecycle participant that discovers
    /// IPauseSurfaceAdapter components from Route Primary, RouteContent and ActivityContent
    /// scenes, symmetrically to how PauseProductBindingSceneLifecycleParticipant already
    /// discovers request triggers from the same scene roots. It is intentionally
    /// a separate participant: Pause Surface lifecycle is not mixed with Player Pause input
    /// binding lifecycle. Persistent Content uses a Session composition scope and is released
    /// explicitly at Session shutdown.
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

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            PauseSurfaceRuntime surfaceRuntime = _surfaceRuntime;
            if (surfaceRuntime == null)
            {
                return SceneCompositionResult.Completed(
                    scope,
                    SceneCompositionOperation.Available,
                    $"Pause surface target is not initialized for scope '{scope.Label}'.");
            }

            List<IPauseSurfaceAdapter> adapters = Collect(roots);
            PauseSnapshot currentSnapshot = default;
            _pauseSnapshotSource.TryGetPauseSnapshot(out currentSnapshot);

            surfaceRuntime.SetSceneContribution(
                scope,
                adapters,
                currentSnapshot,
                nameof(PauseSurfaceSceneLifecycleParticipant),
                "scope-available");

            if (adapters.Count > 0)
            {
                _logger.Debug(
                    "Pause surface scene contribution registered.",
                    LogFields.Of(
                        LogFields.Field("scope", scope.Label),
                        LogFields.Field("adapterCount", adapters.Count)));
            }

            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Available,
                $"Pause surface contribution composed for scope '{scope.Label}' adapters='{adapters.Count}'.");
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            _surfaceRuntime?.ReleaseSceneContribution(scope);
            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Releasing,
                $"Pause surface contribution released for scope '{scope.Label}' reason='{reason.NormalizeTextOrFallback("scope-release")}'.");
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

    }
}
