using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class ActivityPlayerActorLeaveLedgerTests
    {
        [Test]
        public void LeaveRetirement_ExcludesLeavingSlotWhileContextualAssignmentIsStillActive()
        {
            PlayerSlotId leavingSlot = PlayerSlotId.Player1;
            PlayerSlotId remainingSlot = PlayerSlotId.Player2;
            var contextualSlots = new List<PlayerSlotId>
            {
                leavingSlot,
                remainingSlot
            };

            IReadOnlyList<PlayerSlotId> retired =
                ActivityPlayerActorLifecycleParticipant.FilterContextualSlotsForLeave(
                    contextualSlots,
                    leavingSlot);

            Assert.That(retired, Is.EquivalentTo(new[] { remainingSlot }));
        }

        [Test]
        public void StageCWithoutActivityRepresentation_DoesNotRequireContextualProjection()
        {
            Assert.That(
                ActivityPlayerActorLifecycleParticipant
                    .TryResolveNoRepresentationStageCStatus(
                        hadActivityRepresentation: false,
                        out SessionPlayerActivityRepresentationReleaseStatus status),
                Is.True);
            Assert.That(
                status,
                Is.EqualTo(SessionPlayerActivityRepresentationReleaseStatus.SucceededNoCurrentRepresentation));
        }

        [Test]
        public void StageCWithActivityRepresentation_ReleasesContextualProjection()
        {
            Assert.That(
                ActivityPlayerActorLifecycleParticipant
                    .TryResolveNoRepresentationStageCStatus(
                        hadActivityRepresentation: true,
                        out _),
                Is.False);
        }
    }
}
