using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Explicit projection of the Local Player Hosts admitted by the official Activity Player lifecycle.
    /// It is intentionally not a hierarchy query and is valid only for the exact Activity owner.
    /// </summary>
    internal interface IPauseActivityBindingPlayerEvidence
    {
        bool TryResolveAdmittedHosts(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            out IReadOnlyList<LocalPlayerHostAuthoring> hosts,
            out string diagnostic);
    }
}
