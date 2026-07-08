using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Passive validator that checks coherence between PlayerTopology validation output
    /// and authored PlayerView snapshots. It does not activate cameras, drive Cinemachine, bind input, bind control,
    /// create PlayerViews or own runtime lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49H passive PlayerView topology validator.")]
    public static class PlayerViewTopologyValidator
    {
        public static PlayerViewTopologyValidationResult Validate(
            PlayerTopologyValidationResult playerTopology,
            IEnumerable<PlayerViewSnapshot> playerViews,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerViewTopologyValidator));
            var views = ToList(playerViews);
            var issues = new List<PlayerViewTopologyIssue>();

            if (playerTopology == null)
            {
                issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                    PlayerViewTopologyIssueKind.MissingPlayerTopologyValidation,
                    string.Empty,
                    normalizedSource,
                    "PlayerView topology validation requires a PlayerTopologyValidationResult."));

                return new PlayerViewTopologyValidationResult(null, views, issues, normalizedSource, reason);
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

            var participatingViewsBySlot = new Dictionary<PlayerSlotId, PlayerViewSnapshot>();
            for (int i = 0; i < views.Count; i++)
            {
                PlayerViewSnapshot view = views[i];
                if (view.IsReleased)
                {
                    continue;
                }

                if (participatingViewsBySlot.TryGetValue(view.PlayerSlotId, out PlayerViewSnapshot existingView))
                {
                    issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                        PlayerViewTopologyIssueKind.DuplicatePlayerViewSlot,
                        view.PlayerSlotId.StableText,
                        normalizedSource,
                        $"A PlayerSlot can have only one participating PlayerView in the current validation scope. ExistingState='{existingView.State}' DuplicateState='{view.State}'."));
                }
                else
                {
                    participatingViewsBySlot.Add(view.PlayerSlotId, view);
                }

                if (!declaredSlots.Contains(view.PlayerSlotId))
                {
                    issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                        PlayerViewTopologyIssueKind.PlayerViewWithoutPlayerSlotDeclaration,
                        view.PlayerSlotId.StableText,
                        normalizedSource,
                        "PlayerView references a PlayerSlot that has no declaration in the current PlayerTopology validation scope."));
                }

                if (!entriesBySlot.TryGetValue(view.PlayerSlotId, out PlayerEntrySnapshot entry))
                {
                    issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                        PlayerViewTopologyIssueKind.PlayerViewWithoutPlayerEntry,
                        view.PlayerSlotId.StableText,
                        normalizedSource,
                        "PlayerView references a PlayerSlot that has no PlayerEntry in the current PlayerTopology validation scope."));
                    continue;
                }

                if (view.HasPlayerEntryEvidence && view.PlayerEntryState != entry.State)
                {
                    issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                        PlayerViewTopologyIssueKind.PlayerViewPlayerEntryStateMismatch,
                        view.PlayerSlotId.StableText,
                        normalizedSource,
                        $"PlayerView PlayerEntry state evidence is stale or mismatched. ViewEvidence='{view.PlayerEntryState}' TopologyEntry='{entry.State}'."));
                }

                if (view.IsBound && !PlayerViewSnapshot.IsViewBoundOrActiveEntry(entry.State))
                {
                    issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                        PlayerViewTopologyIssueKind.BoundPlayerViewWithoutViewBoundOrActiveEntry,
                        view.PlayerSlotId.StableText,
                        normalizedSource,
                        $"Bound PlayerView requires PlayerEntry in ViewBound or Active state. TopologyEntry='{entry.State}'."));
                }

                if (view.IsActive && !PlayerViewSnapshot.IsViewBoundOrActiveEntry(entry.State))
                {
                    issues.Add(PlayerViewTopologyIssue.BlockingIssue(
                        PlayerViewTopologyIssueKind.ActivePlayerViewWithoutViewBoundOrActiveEntry,
                        view.PlayerSlotId.StableText,
                        normalizedSource,
                        $"Active PlayerView requires PlayerEntry in ViewBound or Active state. TopologyEntry='{entry.State}'."));
                }
            }

            return new PlayerViewTopologyValidationResult(playerTopology, views, issues, normalizedSource, reason);
        }

        private static void AddPlayerTopologyIssues(
            PlayerTopologyValidationResult playerTopology,
            ICollection<PlayerViewTopologyIssue> issues,
            string normalizedSource)
        {
            for (int i = 0; i < playerTopology.Issues.Count; i++)
            {
                PlayerTopologyIssue issue = playerTopology.Issues[i];
                issues.Add(new PlayerViewTopologyIssue(
                    PlayerViewTopologyIssueKind.PlayerTopologyIssue,
                    issue.PlayerSlotIdText,
                    normalizedSource,
                    $"PlayerTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static List<PlayerViewSnapshot> ToList(IEnumerable<PlayerViewSnapshot> playerViews)
        {
            var views = new List<PlayerViewSnapshot>();
            if (playerViews == null)
            {
                return views;
            }

            foreach (PlayerViewSnapshot view in playerViews)
            {
                views.Add(view);
            }

            return views;
        }
    }
}
