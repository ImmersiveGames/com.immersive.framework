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
    internal sealed class PauseProductBindingSceneLifecycleParticipant :
        ISceneLifecycleParticipant
    {
        private readonly IPauseProductBindingPort _bindingPort;
        private readonly IPauseProductRequestPort _requestPort;
        private readonly FrameworkLogger _logger;

        internal PauseProductBindingSceneLifecycleParticipant(
            IPauseProductBindingPort port)
            : this(
                port,
                port as IPauseProductRequestPort)
        {
        }

        internal PauseProductBindingSceneLifecycleParticipant(
            IPauseProductBindingPort bindingPort,
            IPauseProductRequestPort requestPort)
        {
            _bindingPort = bindingPort ??
                throw new ArgumentNullException(nameof(bindingPort));
            _requestPort = requestPort ??
                throw new ArgumentException(
                    "Pause Scene Lifecycle composition requires explicit binding and request ports.",
                    nameof(requestPort));
            _logger =
                FrameworkLogger.Create<
                    PauseProductBindingSceneLifecycleParticipant>();
        }

        public bool OnSceneAvailable(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            List<PlayerPauseInput> playerBindings =
                Collect<PlayerPauseInput>(roots);
            List<PauseRequestTrigger> requestTriggers =
                Collect<PauseRequestTrigger>(roots);
            var newlyBoundPlayers =
                new List<PlayerPauseInput>();
            var newlyBoundTriggers =
                new List<PauseRequestTrigger>();

            for (int index = 0;
                 index < playerBindings.Count;
                 index++)
            {
                PlayerPauseInput binding =
                    playerBindings[index];
                bool wasBound = binding.HasActiveBinding;
                if (!binding.TryInjectBindingPort(
                        _bindingPort,
                        out string issue))
                {
                    string rollback = RollbackAvailable(
                        newlyBoundTriggers,
                        newlyBoundPlayers,
                        "scene-available-player-binding-failed");
                    diagnostic = BuildFailureDiagnostic(
                        scene,
                        "PlayerInput binding",
                        binding,
                        issue,
                        rollback);
                    LogCompositionFailure(
                        scene,
                        "PlayerInputBinding",
                        binding,
                        issue,
                        rollback,
                        playerBindings.Count,
                        requestTriggers.Count);
                    return false;
                }

                if (!wasBound)
                {
                    newlyBoundPlayers.Add(binding);
                }
            }

            for (int index = 0;
                 index < requestTriggers.Count;
                 index++)
            {
                PauseRequestTrigger trigger =
                    requestTriggers[index];
                bool wasBound =
                    trigger.HasPauseProductRequestBinding;
                if (!trigger.TryBindPauseProductRequest(
                        _requestPort,
                        out string issue))
                {
                    string rollback = RollbackAvailable(
                        newlyBoundTriggers,
                        newlyBoundPlayers,
                        "scene-available-request-trigger-failed");
                    diagnostic = BuildFailureDiagnostic(
                        scene,
                        "request trigger",
                        trigger,
                        issue,
                        rollback);
                    LogCompositionFailure(
                        scene,
                        "PauseRequestTrigger",
                        trigger,
                        issue,
                        rollback,
                        playerBindings.Count,
                        requestTriggers.Count);
                    return false;
                }

                if (!wasBound)
                {
                    newlyBoundTriggers.Add(trigger);
                }
            }

            diagnostic =
                $"Pause Scene Lifecycle composition completed. " +
                $"scene='{SceneLabel(scene)}' " +
                $"playerBindings='{playerBindings.Count}' " +
                $"requestTriggers='{requestTriggers.Count}' " +
                $"newPlayerBindings='{newlyBoundPlayers.Count}' " +
                $"newRequestTriggers='{newlyBoundTriggers.Count}'.";

            LogCompositionSuccess(
                scene,
                playerBindings.Count,
                requestTriggers.Count,
                newlyBoundPlayers.Count,
                newlyBoundTriggers.Count);
            return true;
        }

        public bool OnSceneReleasing(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            string reason,
            out string diagnostic)
        {
            List<PauseRequestTrigger> requestTriggers =
                Collect<PauseRequestTrigger>(roots);
            List<PlayerPauseInput> playerBindings =
                Collect<PlayerPauseInput>(roots);
            var issues = new List<string>();

            for (int index = 0;
                 index < requestTriggers.Count;
                 index++)
            {
                PauseRequestTrigger trigger =
                    requestTriggers[index];
                if (!trigger.TryReleasePauseProductRequest(
                        _requestPort,
                        out string issue))
                {
                    issues.Add(
                        $"requestTrigger='{ObjectLabel(trigger)}' " +
                        $"issue='{issue.NormalizeTextOrFallback("unknown")}'");
                }
            }

            for (int index = playerBindings.Count - 1;
                 index >= 0;
                 index--)
            {
                PlayerPauseInput binding =
                    playerBindings[index];
                if (!binding.ReleaseForSceneLifecycle(
                        reason,
                        out string issue))
                {
                    issues.Add(
                        $"playerBinding='{ObjectLabel(binding)}' " +
                        $"issue='{issue.NormalizeTextOrFallback("unknown")}'");
                }
            }

            if (issues.Count > 0)
            {
                diagnostic =
                    $"Pause Scene Lifecycle release failed. " +
                    $"scene='{SceneLabel(scene)}' " +
                    $"requestTriggers='{requestTriggers.Count}' " +
                    $"playerBindings='{playerBindings.Count}'. " +
                    string.Join(" ", issues);
                LogReleaseFailure(
                    scene,
                    reason,
                    playerBindings.Count,
                    requestTriggers.Count,
                    issues);
                return false;
            }

            diagnostic =
                $"Pause Scene Lifecycle release completed. " +
                $"scene='{SceneLabel(scene)}' " +
                $"requestTriggers='{requestTriggers.Count}' " +
                $"playerBindings='{playerBindings.Count}'.";

            LogReleaseSuccess(
                scene,
                reason,
                playerBindings.Count,
                requestTriggers.Count);
            return true;
        }

        private string RollbackAvailable(
            IReadOnlyList<PauseRequestTrigger> triggers,
            IReadOnlyList<PlayerPauseInput> players,
            string reason)
        {
            var issues = new List<string>();

            for (int index = triggers.Count - 1;
                 index >= 0;
                 index--)
            {
                if (!triggers[index].TryReleasePauseProductRequest(
                        _requestPort,
                        out string issue))
                {
                    issues.Add(
                        $"requestTriggerRollback='{ObjectLabel(triggers[index])}' " +
                        $"issue='{issue.NormalizeTextOrFallback("unknown")}'");
                }
            }

            for (int index = players.Count - 1;
                 index >= 0;
                 index--)
            {
                if (!players[index].ReleaseForSceneLifecycle(
                        reason,
                        out string issue))
                {
                    issues.Add(
                        $"playerBindingRollback='{ObjectLabel(players[index])}' " +
                        $"issue='{issue.NormalizeTextOrFallback("unknown")}'");
                }
            }

            return issues.Count == 0
                ? "rollback='Succeeded'"
                : $"rollback='Failed' {string.Join(" ", issues)}";
        }

        private void LogCompositionSuccess(
            Scene scene,
            int playerBindingCount,
            int requestTriggerCount,
            int newPlayerBindingCount,
            int newRequestTriggerCount)
        {
            if (playerBindingCount == 0 &&
                requestTriggerCount == 0)
            {
                return;
            }

            _logger.Info(
                "Pause Scene Lifecycle composition completed.",
                LogFields.Of(
                    LogFields.Field(
                        "operation",
                        "SceneAvailable"),
                    LogFields.Field(
                        "scene",
                        SceneLabel(scene)),
                    LogFields.Field(
                        "playerBindings",
                        playerBindingCount),
                    LogFields.Field(
                        "requestTriggers",
                        requestTriggerCount),
                    LogFields.Field(
                        "newPlayerBindings",
                        newPlayerBindingCount),
                    LogFields.Field(
                        "newRequestTriggers",
                        newRequestTriggerCount)));
        }

        private void LogCompositionFailure(
            Scene scene,
            string kind,
            Component component,
            string issue,
            string rollback,
            int playerBindingCount,
            int requestTriggerCount)
        {
            _logger.Error(
                "Pause Scene Lifecycle composition failed.",
                LogFields.Of(
                    LogFields.Field(
                        "operation",
                        "SceneAvailable"),
                    LogFields.Field(
                        "scene",
                        SceneLabel(scene)),
                    LogFields.Field(
                        "bindingKind",
                        kind.NormalizeTextOrFallback("Unknown")),
                    LogFields.Field(
                        "component",
                        ObjectLabel(component)),
                    LogFields.Field(
                        "playerBindings",
                        playerBindingCount),
                    LogFields.Field(
                        "requestTriggers",
                        requestTriggerCount),
                    LogFields.Field(
                        "issue",
                        issue.NormalizeTextOrFallback("unknown")),
                    LogFields.Field(
                        "rollback",
                        rollback.NormalizeTextOrFallback("unknown"))));
        }

        private void LogReleaseSuccess(
            Scene scene,
            string reason,
            int playerBindingCount,
            int requestTriggerCount)
        {
            if (playerBindingCount == 0 &&
                requestTriggerCount == 0)
            {
                return;
            }

            _logger.Info(
                "Pause Scene Lifecycle release completed.",
                LogFields.Of(
                    LogFields.Field(
                        "operation",
                        "SceneReleasing"),
                    LogFields.Field(
                        "scene",
                        SceneLabel(scene)),
                    LogFields.Field(
                        "reason",
                        reason.NormalizeTextOrFallback(
                            "scene-release")),
                    LogFields.Field(
                        "playerBindings",
                        playerBindingCount),
                    LogFields.Field(
                        "requestTriggers",
                        requestTriggerCount)));
        }

        private void LogReleaseFailure(
            Scene scene,
            string reason,
            int playerBindingCount,
            int requestTriggerCount,
            IReadOnlyList<string> issues)
        {
            _logger.Error(
                "Pause Scene Lifecycle release failed.",
                LogFields.Of(
                    LogFields.Field(
                        "operation",
                        "SceneReleasing"),
                    LogFields.Field(
                        "scene",
                        SceneLabel(scene)),
                    LogFields.Field(
                        "reason",
                        reason.NormalizeTextOrFallback(
                            "scene-release")),
                    LogFields.Field(
                        "playerBindings",
                        playerBindingCount),
                    LogFields.Field(
                        "requestTriggers",
                        requestTriggerCount),
                    LogFields.Field(
                        "issueCount",
                        issues?.Count ?? 0),
                    LogFields.Field(
                        "issues",
                        issues == null ||
                        issues.Count == 0
                            ? "none"
                            : string.Join(" ", issues))));
        }

        private static string BuildFailureDiagnostic(
            Scene scene,
            string kind,
            Component component,
            string issue,
            string rollback)
        {
            return
                $"Pause Scene Lifecycle rejected {kind}. " +
                $"scene='{SceneLabel(scene)}' " +
                $"component='{ObjectLabel(component)}' " +
                $"issue='{issue.NormalizeTextOrFallback("unknown")}' " +
                rollback;
        }

        private static List<T> Collect<T>(
            IReadOnlyList<GameObject> roots)
            where T : Component
        {
            var result = new List<T>();
            var seen = new HashSet<T>();
            if (roots == null)
            {
                return result;
            }

            for (int rootIndex = 0;
                 rootIndex < roots.Count;
                 rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                T[] candidates =
                    root.GetComponentsInChildren<T>(true);
                for (int candidateIndex = 0;
                     candidateIndex < candidates.Length;
                     candidateIndex++)
                {
                    T candidate = candidates[candidateIndex];
                    if (candidate != null &&
                        seen.Add(candidate))
                    {
                        result.Add(candidate);
                    }
                }
            }

            return result;
        }

        private static string SceneLabel(Scene scene) =>
            scene.IsValid()
                ? scene.name.NormalizeTextOrFallback("<unnamed>")
                : "<invalid>";

        private static string ObjectLabel(Component component) =>
            component != null
                ? component.name.NormalizeTextOrFallback(
                    component.GetType().Name)
                : "<missing>";
    }
}
