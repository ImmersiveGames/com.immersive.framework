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
    }
}
