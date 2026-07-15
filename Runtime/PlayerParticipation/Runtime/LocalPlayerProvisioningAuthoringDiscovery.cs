using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Centralized composition-root discovery for the single loaded authoring declaration.
    /// Resolution is by component type and loaded Scene ownership only; names, tags,
    /// hierarchy paths and PlayerInputManager.instance are never used.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G.4 loaded-scene local Player provisioning authoring resolution.")]
    internal static class LocalPlayerProvisioningAuthoringDiscovery
    {
        internal static bool TryResolveLoaded(
            out LocalPlayerProvisioningAuthoring authoring,
            out int candidateCount,
            out string diagnostic)
        {
            authoring = null;
            candidateCount = 0;

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
                    "No loaded LocalPlayerProvisioningAuthoring is configured. Local Player join remains explicitly unavailable.";
                return true;
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
