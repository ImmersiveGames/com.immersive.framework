using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Temporary migration/diagnostic-only discovery for loaded provisioning declarations.
    /// It must not be used by the canonical bootstrap path.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "Legacy migration/diagnostic loaded-scene Local Player provisioning discovery.")]
    internal static class LocalPlayerProvisioningAuthoringDiscovery
    {
        internal static bool TryResolveLoadedForLegacyMigration(
            bool legacyModeEnabled,
            out LocalPlayerProvisioningAuthoring authoring,
            out int candidateCount,
            out string diagnostic)
        {
            authoring = null;
            candidateCount = 0;

            if (!legacyModeEnabled)
            {
                diagnostic =
                    "Legacy Local Player provisioning discovery is disabled. Configure a UIGlobal Local Player Provisioning Host Registration instead.";
                return false;
            }

            LocalPlayerProvisioningAuthoring[] discovered =
                Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                    FindObjectsInactive.Include);
            var loadedCandidates = new List<LocalPlayerProvisioningAuthoring>();

            for (int index = 0; index < discovered.Length; index++)
            {
                LocalPlayerProvisioningAuthoring candidate = discovered[index];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                loadedCandidates.Add(candidate);
            }

            candidateCount = loadedCandidates.Count;
            if (candidateCount == 0)
            {
                diagnostic =
                    "Legacy Local Player provisioning discovery found no loaded authoring surface.";
                return false;
            }

            if (candidateCount > 1)
            {
                diagnostic =
                    $"Expected at most one loaded LocalPlayerProvisioningAuthoring, but found '{candidateCount}'.";
                return false;
            }

            authoring = loadedCandidates[0];
            diagnostic =
                $"Resolved Local Player provisioning authoring '{authoring.name}' from loaded Scene '{authoring.gameObject.scene.name}'.";
            return true;
        }
    }
}
