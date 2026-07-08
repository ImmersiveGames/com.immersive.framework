using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Passive validator that checks coherence between PlayerTopology validation output
    /// and authored PlayerControl snapshots. It does not bind InputActions, route PlayerInput, enable movement,
    /// activate ControlBinding, activate cameras or own runtime lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49J passive PlayerControl topology validator.")]
    public static class PlayerControlTopologyValidator
    {
        public static PlayerControlTopologyValidationResult Validate(
            PlayerTopologyValidationResult playerTopology,
            IEnumerable<PlayerControlSnapshot> playerControls,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerControlTopologyValidator));
            var controls = ToList(playerControls);
            var issues = new List<PlayerControlTopologyIssue>();

            if (playerTopology == null)
            {
                issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                    PlayerControlTopologyIssueKind.MissingPlayerTopologyValidation,
                    string.Empty,
                    normalizedSource,
                    "PlayerControl topology validation requires a PlayerTopologyValidationResult."));

                return new PlayerControlTopologyValidationResult(null, controls, issues, normalizedSource, reason);
            }

            AddPlayerTopologyIssues(playerTopology, issues, normalizedSource);

            var declaredSlots = new HashSet<PlayerSlotId>();
            if (playerTopology.PlayerSlotSet != null)
            {
                for (int i = 0; i < playerTopology.PlayerSlotSet.Descriptors.Count; i++)
                {
                    declaredSlots.Add(playerTopology.PlayerSlotSet.Descriptors[i].PlayerSlotId);
                }
            }

            var entriesBySlot = new Dictionary<PlayerSlotId, PlayerEntrySnapshot>();
            for (int i = 0; i < playerTopology.PlayerEntries.Count; i++)
            {
                PlayerEntrySnapshot entry = playerTopology.PlayerEntries[i];
                if (!entriesBySlot.ContainsKey(entry.PlayerSlotId))
                {
                    entriesBySlot.Add(entry.PlayerSlotId, entry);
                }
            }

            var participatingControlsBySlot = new Dictionary<PlayerSlotId, PlayerControlSnapshot>();
            for (int i = 0; i < controls.Count; i++)
            {
                PlayerControlSnapshot control = controls[i];
                if (control.IsReleased)
                {
                    continue;
                }

                if (participatingControlsBySlot.TryGetValue(control.PlayerSlotId, out PlayerControlSnapshot existingControl))
                {
                    issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                        PlayerControlTopologyIssueKind.DuplicatePlayerControlSlot,
                        control.PlayerSlotId.StableText,
                        normalizedSource,
                        $"A PlayerSlot can have only one participating PlayerControl in the current validation scope. ExistingState='{existingControl.State}' DuplicateState='{control.State}'."));
                }
                else
                {
                    participatingControlsBySlot.Add(control.PlayerSlotId, control);
                }

                if (!declaredSlots.Contains(control.PlayerSlotId))
                {
                    issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                        PlayerControlTopologyIssueKind.PlayerControlWithoutPlayerSlotDeclaration,
                        control.PlayerSlotId.StableText,
                        normalizedSource,
                        "PlayerControl references a PlayerSlot that has no declaration in the current PlayerTopology validation scope."));
                }

                if (!entriesBySlot.TryGetValue(control.PlayerSlotId, out PlayerEntrySnapshot entry))
                {
                    issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                        PlayerControlTopologyIssueKind.PlayerControlWithoutPlayerEntry,
                        control.PlayerSlotId.StableText,
                        normalizedSource,
                        "PlayerControl references a PlayerSlot that has no PlayerEntry in the current PlayerTopology validation scope."));
                    continue;
                }

                if (control.HasPlayerEntryEvidence
                    && (control.PlayerEntryState != entry.State || control.IsPlayerEntryReadyForControl != entry.IsActorReadyForControl))
                {
                    issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                        PlayerControlTopologyIssueKind.PlayerControlPlayerEntryEvidenceMismatch,
                        control.PlayerSlotId.StableText,
                        normalizedSource,
                        $"PlayerControl PlayerEntry evidence is stale or mismatched. ControlEvidenceState='{control.PlayerEntryState}' TopologyEntryState='{entry.State}' ControlEvidenceReadyForControl='{control.IsPlayerEntryReadyForControl}' TopologyReadyForControl='{entry.IsActorReadyForControl}'."));
                }

                if (control.IsBound && entry.State != PlayerEntryState.Active)
                {
                    issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                        PlayerControlTopologyIssueKind.BoundPlayerControlWithoutActiveEntry,
                        control.PlayerSlotId.StableText,
                        normalizedSource,
                        $"Bound PlayerControl requires PlayerEntry in Active state. TopologyEntry='{entry.State}'."));
                }

                if (control.IsActive && (entry.State != PlayerEntryState.Active || !entry.IsActorReadyForControl))
                {
                    issues.Add(PlayerControlTopologyIssue.BlockingIssue(
                        PlayerControlTopologyIssueKind.ActivePlayerControlWithoutActiveReadyEntry,
                        control.PlayerSlotId.StableText,
                        normalizedSource,
                        $"Active PlayerControl requires PlayerEntry in Active state with Actor readiness for control. TopologyEntry='{entry.State}' ReadyForControl='{entry.IsActorReadyForControl}'."));
                }
            }

            return new PlayerControlTopologyValidationResult(playerTopology, controls, issues, normalizedSource, reason);
        }

        private static void AddPlayerTopologyIssues(
            PlayerTopologyValidationResult playerTopology,
            ICollection<PlayerControlTopologyIssue> issues,
            string normalizedSource)
        {
            for (int i = 0; i < playerTopology.Issues.Count; i++)
            {
                PlayerTopologyIssue issue = playerTopology.Issues[i];
                issues.Add(new PlayerControlTopologyIssue(
                    PlayerControlTopologyIssueKind.PlayerTopologyIssue,
                    issue.PlayerSlotIdText,
                    normalizedSource,
                    $"PlayerTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static List<PlayerControlSnapshot> ToList(IEnumerable<PlayerControlSnapshot> playerControls)
        {
            var controls = new List<PlayerControlSnapshot>();
            if (playerControls == null)
            {
                return controls;
            }

            foreach (PlayerControlSnapshot control in playerControls)
            {
                controls.Add(control);
            }

            return controls;
        }
    }
}
