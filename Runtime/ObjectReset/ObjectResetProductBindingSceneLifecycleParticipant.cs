using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Reset;
using Immersive.Framework.SceneLifecycle;
using Immersive.Logging.Records;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// Composes authored Object Reset request surfaces from explicit Scene Lifecycle roots.
    /// It owns no reset authority; the FrameworkRuntimeHost supplies the canonical port.
    /// </summary>
    internal sealed class ObjectResetProductBindingSceneLifecycleParticipant :
        ISceneLifecycleParticipant
    {
        private readonly IResetExecutionRuntimePort resetExecutionRuntime;
        private readonly FrameworkLogger logger;

        internal ObjectResetProductBindingSceneLifecycleParticipant(
            IResetExecutionRuntimePort resetExecutionRuntime)
        {
            this.resetExecutionRuntime = resetExecutionRuntime ??
                throw new System.ArgumentNullException(nameof(resetExecutionRuntime));
            logger = FrameworkLogger.Create<ObjectResetProductBindingSceneLifecycleParticipant>();
        }

        public bool OnSceneAvailable(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            ObjectResetTriggerBindingResult result =
                ObjectResetTriggerBinding.TryBind(roots, resetExecutionRuntime);
            diagnostic = result.Message;
            if (!result.Succeeded)
            {
                logger.Error(
                    "Object Reset Scene Lifecycle binding rejected.",
                    LogFields.Of(
                        LogFields.Field("operation", "SceneAvailable"),
                        LogFields.Field("scene", SceneLabel(scene)),
                        LogFields.Field("issue", result.Message)));
                return false;
            }

            logger.Info(
                result.TriggerCount == 0
                    ? "Object Reset Scene Lifecycle composition found no authored request surfaces."
                    : "Object Reset Scene Lifecycle composition completed.",
                LogFields.Of(
                    LogFields.Field("operation", "SceneAvailable"),
                    LogFields.Field("scene", SceneLabel(scene)),
                    LogFields.Field("requestTriggers", result.TriggerCount),
                    LogFields.Field("newRequestTriggers", result.BoundCount),
                    LogFields.Field("idempotentRequestTriggers", result.IdempotentCount),
                    LogFields.Field("rejectedRequestTriggers", result.RejectedCount)));
            return true;
        }

        public bool OnSceneReleasing(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            string reason,
            out string diagnostic)
        {
            // ObjectResetTrigger owns no release API. Scene-owned triggers are destroyed with
            // their scene; persistent roots retain the same canonical host lifetime binding.
            int triggerCount = CountTriggers(roots);
            diagnostic = $"Object Reset Scene Lifecycle release completed. scene='{SceneLabel(scene)}' requestTriggers='{triggerCount}'.";
            logger.Info(
                "Object Reset Scene Lifecycle release completed.",
                LogFields.Of(
                    LogFields.Field("operation", "SceneReleasing"),
                    LogFields.Field("scene", SceneLabel(scene)),
                    LogFields.Field("reason", reason.NormalizeTextOrFallback("scene-release")),
                    LogFields.Field("requestTriggers", triggerCount)));
            return true;
        }

        private static int CountTriggers(IReadOnlyList<GameObject> roots)
        {
            var found = new HashSet<ObjectResetTrigger>();
            if (roots == null) return 0;
            for (int index = 0; index < roots.Count; index++)
            {
                GameObject root = roots[index];
                if (root == null) continue;
                foreach (ObjectResetTrigger trigger in root.GetComponentsInChildren<ObjectResetTrigger>(true))
                    if (trigger != null) found.Add(trigger);
            }
            return found.Count;
        }

        private static string SceneLabel(Scene scene) =>
            scene.IsValid() ? scene.name.NormalizeTextOrFallback("<unnamed>") : "<invalid>";
    }
}
