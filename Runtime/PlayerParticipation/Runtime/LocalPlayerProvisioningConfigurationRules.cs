using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    public static class LocalPlayerProvisioningConfigurationRules
    {
        public static LocalPlayerProvisioningValidationResult Validate(
            IEnumerable<LocalPlayerProvisioningAuthoring> surfaces,
            bool required,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningConfigurationRules));
            var candidates = new List<LocalPlayerProvisioningAuthoring>();
            if (surfaces != null)
            {
                foreach (LocalPlayerProvisioningAuthoring surface in surfaces)
                {
                    if (surface != null) candidates.Add(surface);
                }
            }

            var issues = new List<LocalPlayerProvisioningIssue>();
            if (required && candidates.Count == 0)
            {
                Add(issues, LocalPlayerProvisioningIssueKind.MissingRequiredSurface, resolvedSource,
                    "One LocalPlayerProvisioningAuthoring surface is required.");
            }
            if (candidates.Count > 1)
            {
                Add(issues, LocalPlayerProvisioningIssueKind.DuplicateSurface, resolvedSource,
                    "At most one LocalPlayerProvisioningAuthoring surface may be loaded.");
            }

            LocalPlayerProvisioningAuthoring authoring = candidates.Count == 1 ? candidates[0] : null;
            if (authoring != null)
            {
                PlayerInputManager manager = authoring.PlayerInputManager;
                if (manager == null)
                    Add(issues, LocalPlayerProvisioningIssueKind.MissingPlayerInputManager, resolvedSource, "Provisioning requires an explicit PlayerInputManager.");
                else if (!ReferenceEquals(manager.gameObject, authoring.gameObject))
                    Add(issues, LocalPlayerProvisioningIssueKind.DivergentPlayerInputManager, resolvedSource, "PlayerInputManager must be owned by the provisioning surface GameObject.");
                if (authoring.PlayerPrefab == null)
                    Add(issues, LocalPlayerProvisioningIssueKind.MissingPlayerPrefab, resolvedSource, "Provisioning requires an explicit Player prefab.");
                else if (authoring.PlayerPrefab.GetComponent<LocalPlayerHostAuthoring>() == null)
                    Add(issues, LocalPlayerProvisioningIssueKind.InvalidPlayerHost, resolvedSource, "The Player prefab must contain LocalPlayerHostAuthoring on its root.");
                if (authoring.TechnicalMaxPlayerCount <= 0)
                    Add(issues, LocalPlayerProvisioningIssueKind.InvalidCapacity, resolvedSource, "Provisioning capacity must be greater than zero.");
                if (!authoring.UsesManualJoin)
                    Add(issues, LocalPlayerProvisioningIssueKind.ManualJoinRequired, resolvedSource, "PlayerInputManager must use manual join.");
                if (!authoring.UsesCSharpJoinNotifications)
                    Add(issues, LocalPlayerProvisioningIssueKind.CSharpEventsRequired, resolvedSource, "PlayerInputManager must use C# event notifications.");
            }

            return new LocalPlayerProvisioningValidationResult(authoring, candidates.Count, required, issues.ToArray(), resolvedSource, reason);
        }

        private static void Add(List<LocalPlayerProvisioningIssue> issues, LocalPlayerProvisioningIssueKind kind, string source, string message)
        {
            issues.Add(new LocalPlayerProvisioningIssue(kind, true, source, message));
        }
    }
}
