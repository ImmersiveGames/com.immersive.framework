using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable, Session-scoped notification of one committed participation
    /// change. The authoritative Session remains the only state owner.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-08 scoped Player Session change observation.")]
    public sealed class PlayerSessionChange
    {
        private PlayerSessionChange(
            PlayerSessionChangeKind kind,
            int sessionRevision,
            PlayerSlotId playerSlotId,
            bool previousJoiningOpen,
            bool currentJoiningOpen,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot)
        {
            Kind = kind;
            SessionRevision = sessionRevision;
            PlayerSlotId = playerSlotId;
            PreviousJoiningOpen = previousJoiningOpen;
            CurrentJoiningOpen = currentJoiningOpen;
            PreviousSlot = previousSlot;
            CurrentSlot = currentSlot;
        }

        public PlayerSessionChangeKind Kind { get; }

        /// <summary>
        /// Revision of the Session state already observable when this callback
        /// is invoked.
        /// </summary>
        public int SessionRevision { get; }

        /// <summary>
        /// Valid for Slot allocation and Actor selection changes only.
        /// </summary>
        public PlayerSlotId PlayerSlotId { get; }

        public bool PreviousJoiningOpen { get; }
        public bool CurrentJoiningOpen { get; }
        public PlayerSlotRuntimeSnapshot PreviousSlot { get; }
        public PlayerSlotRuntimeSnapshot CurrentSlot { get; }

        internal static PlayerSessionChange Joining(
            int sessionRevision,
            bool previousJoiningOpen,
            bool currentJoiningOpen)
        {
            return new PlayerSessionChange(
                PlayerSessionChangeKind.JoiningChanged,
                sessionRevision,
                default,
                previousJoiningOpen,
                currentJoiningOpen,
                default,
                default);
        }

        internal static PlayerSessionChange SlotAllocation(
            int sessionRevision,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot)
        {
            return new PlayerSessionChange(
                PlayerSessionChangeKind.SlotAllocationChanged,
                sessionRevision,
                currentSlot.PlayerSlotId,
                false,
                false,
                previousSlot,
                currentSlot);
        }

        internal static PlayerSessionChange ActorSelection(
            int sessionRevision,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot)
        {
            return new PlayerSessionChange(
                PlayerSessionChangeKind.ActorSelectionChanged,
                sessionRevision,
                currentSlot.PlayerSlotId,
                false,
                false,
                previousSlot,
                currentSlot);
        }
    }
}
