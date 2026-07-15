using Immersive.Framework.Actors;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal readonly struct ActivityPlayerGameplayAdoptedSlot
    {
        internal ActivityPlayerGameplayAdoptedSlot(
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayAdmissionToken admissionToken,
            string message)
        {
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            PreparationToken = preparationToken;
            AdmissionToken = admissionToken;
            Message = message.NormalizeText();
        }

        internal PlayerSlotId PlayerSlotId { get; }
        internal ActorProfileId ActorProfileId { get; }
        internal PlayerActorPreparationToken PreparationToken { get; }
        internal PlayerGameplayAdmissionToken AdmissionToken { get; }
        internal string Message { get; }

        internal bool IsValid =>
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            PreparationToken.IsValid &&
            AdmissionToken.IsValid;
    }
}
