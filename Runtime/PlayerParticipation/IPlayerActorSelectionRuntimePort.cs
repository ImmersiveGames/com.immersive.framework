using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal runtime boundary for explicit Session Player Actor selection requests.
    /// It exposes selection readiness and the default-selection transaction without
    /// exposing composition or preparation implementation details.
    /// </summary>
    internal interface IPlayerActorSelectionRuntimePort
    {
        bool TryValidatePlayerActorSelectionRuntime(out string issue);

        PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason);
    }
}
