using System;
using System.Collections.Generic;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable evaluation input produced by an Activity participation projection Profile.
    /// It carries no mutable Session state and performs no runtime lookup.
    /// </summary>
    public readonly struct ActivityParticipationProjectionDescriptor
    {
        public ActivityParticipationProjectionDescriptor(
            ActivityParticipationProjectionMode mode,
            ActivityParticipationZeroParticipantPolicy zeroParticipantPolicy,
            IReadOnlyList<PlayerSlotProfile> explicitSlotProfiles)
        {
            Mode = mode;
            ZeroParticipantPolicy = zeroParticipantPolicy;
            ExplicitSlotProfiles = explicitSlotProfiles ?? Array.Empty<PlayerSlotProfile>();
        }

        public ActivityParticipationProjectionMode Mode { get; }

        public ActivityParticipationZeroParticipantPolicy ZeroParticipantPolicy { get; }

        public IReadOnlyList<PlayerSlotProfile> ExplicitSlotProfiles { get; }

        public bool ProjectsNoSlots => Mode == ActivityParticipationProjectionMode.NoSlots;

        public bool ProjectsAllJoinedSlots => Mode == ActivityParticipationProjectionMode.AllJoinedSlots;

        public bool ProjectsExplicitSlots => Mode == ActivityParticipationProjectionMode.ExplicitSlots;

        public bool AllowsZeroParticipants =>
            ZeroParticipantPolicy == ActivityParticipationZeroParticipantPolicy.Allowed;
    }
}
