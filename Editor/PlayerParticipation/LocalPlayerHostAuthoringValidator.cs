using System;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Non-mutating validation for invariants shared by all Local Player Host sources.
    /// Source-specific rules such as an empty Actor Mount belong to their provisioning
    /// or Scene-Provided composer validators.
    /// </summary>
    internal static class LocalPlayerHostAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport Validate(
            LocalPlayerHostAuthoring host)
        {
            var report = new FrameworkAuthoringValidationReport();

            if (host == null)
            {
                report.AddError(
                    "Local Player Host Authoring is missing.",
                    null);
                return report;
            }

            if (host.PlayerInput == null)
            {
                report.AddError(
                    "Local Player Host requires an explicit PlayerInput reference.",
                    host);
                return report;
            }

            if (!ReferenceEquals(
                    host.PlayerInput.gameObject,
                    host.gameObject))
            {
                report.AddError(
                    "Local Player Host PlayerInput must exist on the same GameObject as LocalPlayerHostAuthoring.",
                    host);
                return report;
            }

            PlayerInput[] playerInputs =
                host.GetComponentsInChildren<PlayerInput>(true);
            if (playerInputs.Length != 1 ||
                !ReferenceEquals(playerInputs[0], host.PlayerInput))
            {
                report.AddError(
                    $"Local Player Host requires exactly one PlayerInput in its hierarchy. Found '{playerInputs.Length}'.",
                    host);
                return report;
            }

            if (host.ActorMount == null)
            {
                report.AddError(
                    "Local Player Host requires an explicit Actor Mount child transform.",
                    host);
                return report;
            }

            if (ReferenceEquals(host.ActorMount, host.transform) ||
                !host.ActorMount.IsChildOf(host.transform))
            {
                report.AddError(
                    "Local Player Host Actor Mount must be a child of the technical host root.",
                    host);
                return report;
            }

            if (host.ActorMount.GetComponentInChildren<PlayerInput>(true) != null)
            {
                report.AddError(
                    "Local Player Host Actor Mount must not contain a second PlayerInput.",
                    host);
                return report;
            }

            report.AddInfo(
                $"Local Player Host technical structure is valid. playerInput='{host.PlayerInput.name}' actorMount='{host.ActorMount.name}' logicalActorPrepared='{host.HasLogicalActor}'. Source-specific composition remains validated by its owning product surface.",
                host);
            return report;
        }
    }
}
