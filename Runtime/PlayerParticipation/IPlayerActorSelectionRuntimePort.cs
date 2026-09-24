using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Internal runtime boundary for explicit Session Player Actor selection requests.
    /// It exposes the canonical selection transactions without exposing composition
    /// or preparation implementation details.
    /// </summary>
    internal interface IPlayerActorSelectionRuntimePort
    {
        bool TryValidatePlayerActorSelectionRuntime(out string issue);

        PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request);

        PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason);

        PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request);

        PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request);
    }
}
