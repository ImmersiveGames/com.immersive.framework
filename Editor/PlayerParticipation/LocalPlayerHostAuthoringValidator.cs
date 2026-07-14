using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Non-mutating product validation for the stable Local Player technical host.
    /// </summary>
    internal static class LocalPlayerHostAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport Validate(
            LocalPlayerHostAuthoring host)
        {
            var report = new FrameworkAuthoringValidationReport();

            if (host == null)
            {
                report.AddError("Local Player Host Authoring is missing.", null);
                return report;
            }

            if (!host.TryValidateConfiguration(out string issue))
            {
                report.AddError(issue, host);
                return report;
            }

            report.AddInfo(
                $"Local Player Host is valid. playerInput='{host.PlayerInput.name}' actorMount='{host.ActorMount.name}' logicalActorPrepared='{host.HasLogicalActor}'.",
                host);
            return report;
        }
    }
}
