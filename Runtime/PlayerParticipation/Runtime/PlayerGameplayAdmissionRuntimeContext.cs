using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "P3K.5 gameplay admission aggregation authority.")]
    internal sealed class PlayerGameplayAdmissionRuntimeContext
    {
        private readonly string _sessionContextId;
        private readonly PlayerGameplayOccupancyRuntimeContext _occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext _inputContext;
        private readonly PlayerSlotId[] _orderedSlots;
        private readonly Dictionary<PlayerSlotId, PlayerGameplayAdmissionSummary> _slots = new();
        private int _revision = 1;
        private int _sequence;
        private PlayerGameplayAdmissionStatus _last;
        private string _message = "Gameplay admission runtime initialized.";

        private PlayerGameplayAdmissionRuntimeContext(string sessionContextId, PlayerGameplayOccupancyRuntimeContext occupancyContext, PlayerGameplayInputBindingRuntimeContext inputContext, PlayerSlotId[] orderedSlots)
        {
            _sessionContextId = sessionContextId;
            _occupancyContext = occupancyContext;
            _inputContext = inputContext;
            _orderedSlots = orderedSlots;
            foreach (PlayerSlotId slot in orderedSlots)
                _slots.Add(slot, PlayerGameplayAdmissionSummary.NotAdmitted(sessionContextId, slot, 0, nameof(PlayerGameplayAdmissionRuntimeContext), "runtime-initialization", "Configured Player Slot is not admitted."));
        }

        internal string SessionContextId => _sessionContextId;
        internal int Revision => _revision;

        internal static bool TryCreate(PlayerGameplayOccupancyRuntimeContext occupancyContext, PlayerGameplayInputBindingRuntimeContext inputContext, out PlayerGameplayAdmissionRuntimeContext context, out string issue)
        {
            context = null;
            issue = string.Empty;
            PlayerGameplayOccupancySnapshot snapshot = occupancyContext?.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized || inputContext == null)
            {
                issue = "Gameplay admission requires occupancy and input capability authorities.";
                return false;
            }

            var orderedSlots = new PlayerSlotId[snapshot.ConfiguredSlotCount];
            for (int index = 0; index < orderedSlots.Length; index++)
                orderedSlots[index] = snapshot.Slots[index].PlayerSlotId;
            context = new PlayerGameplayAdmissionRuntimeContext(snapshot.SessionContextId, occupancyContext, inputContext, orderedSlots);
            return true;
        }

        internal PlayerGameplayAdmissionResult TryAdmit(RuntimeContentOwner contextualOwner, PlayerGameplayOccupancySummary occupancy, PlayerGameplayInputBindingSummary input, string source, string reason)
        {
            const string operation = "AdmitGameplay";
            PlayerSlotId slot = occupancy.PlayerSlotId;
            PlayerGameplayAdmissionSummary previous = Get(slot);
            if (!contextualOwner.IsValid || contextualOwner.Scope != RuntimeContentScope.Activity)
                return Reject(PlayerGameplayAdmissionStatus.RejectedInvalidRequest, operation, slot, previous, "Gameplay admission requires a valid Activity contextual owner.");
            if (!Validate(contextualOwner, occupancy, input, out string issue))
                return Reject(PlayerGameplayAdmissionStatus.RejectedInvalidRequest, operation, slot, previous, issue);
            if (previous.IsAdmitted && previous.Owner == contextualOwner && previous.OccupancyToken == occupancy.Token && previous.InputBindingToken == input.Token)
                return Refresh(operation, previous, input, source, reason, PlayerGameplayAdmissionStatus.SucceededAlreadyAdmitted, "Gameplay admission is already current.");
            if (previous.IsAdmitted)
                return Reject(PlayerGameplayAdmissionStatus.RejectedSlotAlreadyAdmitted, operation, slot, previous, "Player Slot already has another current gameplay admission.");

            var token = new PlayerGameplayAdmissionToken(_sessionContextId, contextualOwner, slot, occupancy.ActorProfileId, occupancy.ActorId, occupancy.RuntimeContentIdentity, occupancy.Token.MaterializationRevision, occupancy.OccupancyRevision, input.BindingRevision, ++_sequence);
            PlayerGameplayAdmissionState state = input.IsAllowed ? PlayerGameplayAdmissionState.Ready : PlayerGameplayAdmissionState.BlockedByInputGate;
            var current = new PlayerGameplayAdmissionSummary(_sessionContextId, slot, state, occupancy.ActorProfileId, occupancy.ActorId, contextualOwner, occupancy.RuntimeContentIdentity, occupancy.PreparationToken, occupancy.Token, input.Token, token, token.AdmissionRevision, source, reason, state == PlayerGameplayAdmissionState.Ready ? "Gameplay admission aggregated current occupancy and input capabilities." : "Gameplay admission is blocked by the current Input Gate.");
            _slots[slot] = current;
            _revision++;
            return Result(state == PlayerGameplayAdmissionState.Ready ? PlayerGameplayAdmissionStatus.SucceededReady : PlayerGameplayAdmissionStatus.SucceededBlockedByInputGate, operation, slot, previous, current, false, false, string.Empty, current.Message);
        }

        internal PlayerGameplayAdmissionResult TryRefreshReadiness(PlayerSlotId slot, PlayerGameplayAdmissionToken expected, string source, string reason)
        {
            PlayerGameplayAdmissionSummary previous = Get(slot);
            if (!previous.IsAdmitted || previous.Token != expected)
                return Reject(PlayerGameplayAdmissionStatus.RejectedForeignOrStaleAdmission, "RefreshGameplayReadiness", slot, previous, "Refresh requires the current admission token.");
            if (!_inputContext.TryGetCurrentInputBinding(slot, out var input, out _))
                return Reject(PlayerGameplayAdmissionStatus.RejectedForeignOrStaleInputBinding, "RefreshGameplayReadiness", slot, previous, "Current Input capability is unavailable.");
            if (input.Owner != previous.Owner || input.Token.Owner != previous.Owner)
                return Reject(PlayerGameplayAdmissionStatus.RejectedForeignOrStaleInputBinding, "RefreshGameplayReadiness", slot, previous, "Current Input capability belongs to another Activity occurrence.");
            return Refresh("RefreshGameplayReadiness", previous, input, source, reason, PlayerGameplayAdmissionStatus.SucceededReadinessRefreshed, "Gameplay readiness refreshed.");
        }

        internal PlayerGameplayAdmissionResult TryRelease(PlayerSlotId slot, PlayerGameplayAdmissionToken expected, string source, string reason)
        {
            const string operation = "ReleaseGameplayAdmission";
            PlayerGameplayAdmissionSummary previous = Get(slot);
            if (previous.IsNotAdmitted)
                return expected.IsValid ? Reject(PlayerGameplayAdmissionStatus.RejectedForeignOrStaleAdmission, operation, slot, previous, "Admission is already released.") : Result(PlayerGameplayAdmissionStatus.SucceededAlreadyReleased, operation, slot, previous, previous, false, false, string.Empty, "Admission is already released.");
            if (previous.Token != expected)
                return Reject(PlayerGameplayAdmissionStatus.RejectedForeignOrStaleAdmission, operation, slot, previous, "Release requires the exact admission token.");

            PlayerGameplayAdmissionSummary current = PlayerGameplayAdmissionSummary.NotAdmitted(_sessionContextId, slot, previous.AdmissionRevision, source, reason, "Gameplay admission released; input and occupancy remain owned by their capabilities.");
            _slots[slot] = current;
            _revision++;
            return Result(PlayerGameplayAdmissionStatus.SucceededReleased, operation, slot, previous, current, false, false, string.Empty, current.Message);
        }

        internal bool TryReleaseAll(string source, string reason, out int released, out int failed, out string issue)
        {
            released = 0;
            failed = 0;
            var errors = new List<string>();
            foreach (PlayerSlotId slot in _orderedSlots)
            {
                PlayerGameplayAdmissionSummary current = Get(slot);
                if (!current.IsAdmitted) continue;
                PlayerGameplayAdmissionResult result = TryRelease(slot, current.Token, source, reason);
                if (result.Succeeded) released++; else { failed++; errors.Add(result.ToDiagnosticString()); }
            }
            issue = string.Join(" | ", errors);
            return failed == 0;
        }

        internal PlayerGameplayAdmissionSnapshot CreateSnapshot()
        {
            var ordered = new PlayerGameplayAdmissionSummary[_orderedSlots.Length];
            for (int index = 0; index < ordered.Length; index++) ordered[index] = _slots[_orderedSlots[index]];
            return new PlayerGameplayAdmissionSnapshot(_sessionContextId, _revision, ordered, _last, _message);
        }

        private bool Validate(RuntimeContentOwner contextualOwner, PlayerGameplayOccupancySummary occupancy, PlayerGameplayInputBindingSummary input, out string issue)
        {
            issue = string.Empty;
            if (!occupancy.IsOccupied || !input.IsBound) { issue = "Admission requires current occupancy and input capabilities."; return false; }
            if (input.Owner != contextualOwner || input.Token.Owner != contextualOwner) { issue = "Gameplay input binding does not belong to the current Activity occurrence."; return false; }
            if (occupancy.PlayerSlotId != input.PlayerSlotId || occupancy.PreparationToken != input.PreparationToken) { issue = "Capabilities do not describe the same prepared Player."; return false; }
            if (!_occupancyContext.TryGetSummary(occupancy.PlayerSlotId, out var currentOccupancy) || currentOccupancy.Token != occupancy.Token || !_inputContext.TryGetCurrentInputBinding(occupancy.PlayerSlotId, out var currentInput, out _) || currentInput.Token != input.Token) { issue = "One or more supplied capabilities are stale."; return false; }
            return true;
        }

        private PlayerGameplayAdmissionResult Refresh(string operation, PlayerGameplayAdmissionSummary previous, PlayerGameplayInputBindingSummary input, string source, string reason, PlayerGameplayAdmissionStatus status, string message)
        {
            PlayerGameplayAdmissionState state = input.IsAllowed ? PlayerGameplayAdmissionState.Ready : PlayerGameplayAdmissionState.BlockedByInputGate;
            var current = new PlayerGameplayAdmissionSummary(previous.SessionContextId, previous.PlayerSlotId, state, previous.ActorProfileId, previous.ActorId, previous.Owner, previous.RuntimeContentIdentity, previous.PreparationToken, previous.OccupancyToken, previous.InputBindingToken, previous.Token, previous.AdmissionRevision, source, reason, message);
            _slots[previous.PlayerSlotId] = current;
            return Result(status, operation, previous.PlayerSlotId, previous, current, false, false, string.Empty, message);
        }

        private PlayerGameplayAdmissionSummary Get(PlayerSlotId slot) => slot.IsValid && _slots.TryGetValue(slot, out var value) ? value : default;
        private PlayerGameplayAdmissionResult Reject(PlayerGameplayAdmissionStatus status, string operation, PlayerSlotId slot, PlayerGameplayAdmissionSummary current, string message) => Result(status, operation, slot, current, current, false, false, string.Empty, message);
        private PlayerGameplayAdmissionResult Result(PlayerGameplayAdmissionStatus status, string operation, PlayerSlotId slot, PlayerGameplayAdmissionSummary previous, PlayerGameplayAdmissionSummary current, bool rollbackAttempted, bool rollbackSucceeded, string rollbackIssue, string message)
        {
            _last = status;
            _message = message.NormalizeText();
            return new PlayerGameplayAdmissionResult(status, operation, slot, previous, current, CreateSnapshot(), rollbackAttempted, rollbackSucceeded, rollbackIssue, _message);
        }
    }
}
