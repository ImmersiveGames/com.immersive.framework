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
    /// Scene-scoped composition for authored Pause request triggers.
    /// Physical Player Pause input follows the Session Local Player Host lifetime.
    /// </summary>
    internal sealed class PauseProductBindingSceneLifecycleParticipant :
        ISceneLifecycleParticipant
    {
        private readonly IPauseProductRequestPort _requestPort;
        private readonly FrameworkLogger _logger;

        internal PauseProductBindingSceneLifecycleParticipant(
            IPauseProductBindingPort port)
            : this(port, port as IPauseProductRequestPort)
        {
        }

        internal PauseProductBindingSceneLifecycleParticipant(
            IPauseProductBindingPort bindingPort,
            IPauseProductRequestPort requestPort)
        {
            _ = bindingPort ??
                throw new ArgumentNullException(nameof(bindingPort));
            _requestPort = requestPort ??
                throw new ArgumentException(
                    "Pause Scene Lifecycle composition requires an explicit request port.",
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
            List<PauseRequestTrigger> triggers =
                Collect<PauseRequestTrigger>(roots);
            var newlyBound = new List<PauseRequestTrigger>();

            for (int index = 0; index < triggers.Count; index++)
            {
                PauseRequestTrigger trigger = triggers[index];
                bool wasBound = trigger.HasPauseProductRequestBinding;
                if (!trigger.TryBindPauseProductRequest(
                        _requestPort,
                        out string issue))
                {
                    string rollback =
                        RollbackAvailable(newlyBound);
                    diagnostic =
                        $"Pause Scene Lifecycle rejected request trigger. " +
                        $"scene='{SceneLabel(scene)}' " +
                        $"component='{ObjectLabel(trigger)}' " +
                        $"issue='{issue.NormalizeTextOrFallback("unknown")}' " +
                        rollback;
                    LogFailure(
                        scene,
                        "SceneAvailable",
                        trigger,
                        issue,
                        rollback,
                        triggers.Count);
                    return false;
                }

                if (!wasBound)
                {
                    newlyBound.Add(trigger);
                }
            }

            diagnostic =
                $"Pause Scene Lifecycle composition completed. " +
                $"scene='{SceneLabel(scene)}' " +
                $"requestTriggers='{triggers.Count}' " +
                $"newRequestTriggers='{newlyBound.Count}'.";
            LogSuccess(
                scene,
                "SceneAvailable",
                triggers.Count,
                newlyBound.Count,
                string.Empty);
            return true;
        }

        public bool OnSceneReleasing(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            string reason,
            out string diagnostic)
        {
            List<PauseRequestTrigger> triggers =
                Collect<PauseRequestTrigger>(roots);
            var issues = new List<string>();

            for (int index = 0; index < triggers.Count; index++)
            {
                PauseRequestTrigger trigger = triggers[index];
                if (!trigger.TryReleasePauseProductRequest(
                        _requestPort,
                        out string issue))
                {
                    issues.Add(
                        $"requestTrigger='{ObjectLabel(trigger)}' " +
                        $"issue='{issue.NormalizeTextOrFallback("unknown")}'");
                }
            }

            if (issues.Count > 0)
            {
                diagnostic =
                    $"Pause Scene Lifecycle release failed. " +
                    $"scene='{SceneLabel(scene)}' " +
                    $"requestTriggers='{triggers.Count}'. " +
                    string.Join(" ", issues);
                _logger.Error(
                    "Pause Scene Lifecycle release failed.",
                    LogFields.Of(
                        LogFields.Field("operation", "SceneReleasing"),
                        LogFields.Field("scene", SceneLabel(scene)),
                        LogFields.Field(
                            "reason",
                            reason.NormalizeTextOrFallback("scene-release")),
                        LogFields.Field("requestTriggers", triggers.Count),
                        LogFields.Field("issues", string.Join(" ", issues))));
                return false;
            }

            diagnostic =
                $"Pause Scene Lifecycle release completed. " +
                $"scene='{SceneLabel(scene)}' " +
                $"requestTriggers='{triggers.Count}'.";
            LogSuccess(
                scene,
                "SceneReleasing",
                triggers.Count,
                0,
                reason);
            return true;
        }

        private string RollbackAvailable(
            IReadOnlyList<PauseRequestTrigger> triggers)
        {
            var issues = new List<string>();
            for (int index = triggers.Count - 1; index >= 0; index--)
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

            return issues.Count == 0
                ? "rollback='Succeeded'"
                : $"rollback='Failed' {string.Join(" ", issues)}";
        }

        private void LogSuccess(
            Scene scene,
            string operation,
            int triggerCount,
            int newTriggerCount,
            string reason)
        {
            if (triggerCount == 0)
            {
                return;
            }

            _logger.Info(
                "Pause Scene Lifecycle composition completed.",
                LogFields.Of(
                    LogFields.Field("operation", operation),
                    LogFields.Field("scene", SceneLabel(scene)),
                    LogFields.Field("requestTriggers", triggerCount),
                    LogFields.Field("newRequestTriggers", newTriggerCount),
                    LogFields.Field(
                        "reason",
                        reason.NormalizeText())));
        }

        private void LogFailure(
            Scene scene,
            string operation,
            Component component,
            string issue,
            string rollback,
            int triggerCount)
        {
            _logger.Error(
                "Pause Scene Lifecycle composition failed.",
                LogFields.Of(
                    LogFields.Field("operation", operation),
                    LogFields.Field("scene", SceneLabel(scene)),
                    LogFields.Field("component", ObjectLabel(component)),
                    LogFields.Field("requestTriggers", triggerCount),
                    LogFields.Field(
                        "issue",
                        issue.NormalizeTextOrFallback("unknown")),
                    LogFields.Field(
                        "rollback",
                        rollback.NormalizeTextOrFallback("unknown"))));
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
                    if (candidate != null && seen.Add(candidate))
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
