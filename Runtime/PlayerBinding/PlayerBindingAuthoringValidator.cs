using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;
using Immersive.Framework.PlayerViews;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Passive authoring validator for the Player binding evidence chain.
    /// It discovers or receives authored components, builds passive topology/readiness diagnostics and never performs binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50A passive Player binding authoring validator.")]
    public static class PlayerBindingAuthoringValidator
    {
        public static PlayerBindingAuthoringValidationReport ValidateHierarchy(
            GameObject validationRoot,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerBindingAuthoringValidator));
            if (validationRoot == null)
            {
                var issues = new List<PlayerBindingAuthoringIssue>
                {
                    PlayerBindingAuthoringIssue.BlockingIssue(
                        PlayerBindingAuthoringIssueKind.MissingValidationRoot,
                        string.Empty,
                        normalizedSource,
                        "Player binding authoring validation requires a validation root GameObject.")
                };

                PlayerBindingDiagnosticReport missingReport = PlayerBindingDiagnosticReporter.CreateReport(
                    null,
                    normalizedSource,
                    reason);
                AddDiagnosticErrors(missingReport, issues, normalizedSource);

                return new PlayerBindingAuthoringValidationReport(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    null,
                    null,
                    null,
                    null,
                    missingReport,
                    issues,
                    normalizedSource,
                    reason);
            }

            return ValidateComponents(
                validationRoot.GetComponentsInChildren<PlayerSlotDeclaration>(true),
                validationRoot.GetComponentsInChildren<PlayerSlotOccupancy>(true),
                validationRoot.GetComponentsInChildren<ActorReadinessBehaviour>(true),
                validationRoot.GetComponentsInChildren<PlayerEntryBehaviour>(true),
                validationRoot.GetComponentsInChildren<PlayerViewBehaviour>(true),
                validationRoot.GetComponentsInChildren<PlayerControlBehaviour>(true),
                normalizedSource,
                reason);
        }

        public static PlayerBindingAuthoringValidationReport ValidateComponents(
            IEnumerable<PlayerSlotDeclaration> playerSlotDeclarations,
            IEnumerable<PlayerSlotOccupancy> playerSlotOccupancies,
            IEnumerable<ActorReadinessBehaviour> actorReadinessBehaviours,
            IEnumerable<PlayerEntryBehaviour> playerEntryBehaviours,
            IEnumerable<PlayerViewBehaviour> playerViewBehaviours,
            IEnumerable<PlayerControlBehaviour> playerControlBehaviours,
            string source = null,
            string reason = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(PlayerBindingAuthoringValidator));
            var issues = new List<PlayerBindingAuthoringIssue>();
            List<PlayerSlotDeclaration> slotDeclarations = ToList(playerSlotDeclarations);
            List<PlayerSlotOccupancy> slotOccupancies = ToList(playerSlotOccupancies);
            List<ActorReadinessBehaviour> readinessBehaviours = ToList(actorReadinessBehaviours);
            List<PlayerEntryBehaviour> entryBehaviours = ToList(playerEntryBehaviours);
            List<PlayerViewBehaviour> viewBehaviours = ToList(playerViewBehaviours);
            List<PlayerControlBehaviour> controlBehaviours = ToList(playerControlBehaviours);

            AddRequiredComponentIssues(
                slotDeclarations,
                slotOccupancies,
                readinessBehaviours,
                entryBehaviours,
                viewBehaviours,
                controlBehaviours,
                issues,
                normalizedSource);

            List<PlayerSlotDescriptor> descriptors = BuildPlayerSlotDescriptors(slotDeclarations, issues, normalizedSource);
            List<PlayerSlotOccupancyDescriptor> occupancies = BuildPlayerSlotOccupancies(slotOccupancies, issues, normalizedSource);
            PlayerSlotSet playerSlotSet = PlayerSlotSet.FromDescriptors(
                descriptors,
                occupancies,
                normalizedSource,
                reason);
            AddPlayerSlotSetIssues(playerSlotSet, issues, normalizedSource);

            List<PlayerEntrySnapshot> entries = BuildPlayerEntries(entryBehaviours, issues, normalizedSource);
            List<PlayerViewSnapshot> views = BuildPlayerViews(viewBehaviours, issues, normalizedSource);
            List<PlayerControlSnapshot> controls = BuildPlayerControls(controlBehaviours, issues, normalizedSource);

            PlayerTopologyValidationResult playerTopology = PlayerTopologyValidator.Validate(
                playerSlotSet,
                entries,
                normalizedSource,
                reason);
            AddPlayerTopologyIssues(playerTopology, issues, normalizedSource);

            PlayerViewTopologyValidationResult viewTopology = PlayerViewTopologyValidator.Validate(
                playerTopology,
                views,
                normalizedSource,
                reason);
            AddPlayerViewTopologyIssues(viewTopology, issues, normalizedSource);

            PlayerControlTopologyValidationResult controlTopology = PlayerControlTopologyValidator.Validate(
                playerTopology,
                controls,
                normalizedSource,
                reason);
            AddPlayerControlTopologyIssues(controlTopology, issues, normalizedSource);

            PlayerBindingReadinessSummary readiness = PlayerBindingReadinessSummarizer.Summarize(
                playerTopology,
                viewTopology,
                controlTopology,
                normalizedSource,
                reason);
            AddReadinessIssues(readiness, issues, normalizedSource);

            PlayerBindingDiagnosticReport diagnosticReport = PlayerBindingDiagnosticReporter.CreateReport(
                readiness,
                normalizedSource,
                reason);
            AddDiagnosticErrors(diagnosticReport, issues, normalizedSource);

            return new PlayerBindingAuthoringValidationReport(
                slotDeclarations.Count,
                slotOccupancies.Count,
                readinessBehaviours.Count,
                entryBehaviours.Count,
                viewBehaviours.Count,
                controlBehaviours.Count,
                playerTopology,
                viewTopology,
                controlTopology,
                readiness,
                diagnosticReport,
                issues,
                normalizedSource,
                reason);
        }

        private static void AddRequiredComponentIssues(
            List<PlayerSlotDeclaration> slotDeclarations,
            List<PlayerSlotOccupancy> slotOccupancies,
            List<ActorReadinessBehaviour> readinessBehaviours,
            List<PlayerEntryBehaviour> entryBehaviours,
            List<PlayerViewBehaviour> viewBehaviours,
            List<PlayerControlBehaviour> controlBehaviours,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (slotDeclarations.Count == 0)
            {
                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotDeclaration, string.Empty, source, "Player binding authoring requires at least one PlayerSlotDeclaration."));
            }

            if (slotOccupancies.Count == 0)
            {
                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotOccupancy, string.Empty, source, "Player binding authoring requires at least one PlayerSlotOccupancy."));
            }

            if (readinessBehaviours.Count == 0)
            {
                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(PlayerBindingAuthoringIssueKind.MissingActorReadinessBehaviour, string.Empty, source, "Player binding authoring requires at least one ActorReadinessBehaviour."));
            }

            if (entryBehaviours.Count == 0)
            {
                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(PlayerBindingAuthoringIssueKind.MissingPlayerEntryBehaviour, string.Empty, source, "Player binding authoring requires at least one PlayerEntryBehaviour."));
            }

            if (viewBehaviours.Count == 0)
            {
                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(PlayerBindingAuthoringIssueKind.MissingPlayerViewBehaviour, string.Empty, source, "Player binding authoring requires at least one PlayerViewBehaviour."));
            }

            if (controlBehaviours.Count == 0)
            {
                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(PlayerBindingAuthoringIssueKind.MissingPlayerControlBehaviour, string.Empty, source, "Player binding authoring requires at least one PlayerControlBehaviour."));
            }
        }

        private static List<PlayerSlotDescriptor> BuildPlayerSlotDescriptors(
            IEnumerable<PlayerSlotDeclaration> declarations,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            var descriptors = new List<PlayerSlotDescriptor>();
            foreach (PlayerSlotDeclaration declaration in declarations)
            {
                if (declaration == null)
                {
                    continue;
                }

                if (declaration.TryCreateDescriptor(source, out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue issue))
                {
                    descriptors.Add(descriptor);
                    continue;
                }

                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(
                    PlayerBindingAuthoringIssueKind.PlayerSlotDeclarationIssue,
                    declaration.name,
                    source,
                    issue.Message));
            }

            return descriptors;
        }

        private static List<PlayerSlotOccupancyDescriptor> BuildPlayerSlotOccupancies(
            IEnumerable<PlayerSlotOccupancy> occupancies,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            var descriptors = new List<PlayerSlotOccupancyDescriptor>();
            foreach (PlayerSlotOccupancy occupancy in occupancies)
            {
                if (occupancy == null)
                {
                    continue;
                }

                if (occupancy.TryCreateDescriptor(source, out PlayerSlotOccupancyDescriptor descriptor, out PlayerSlotSetIssue issue))
                {
                    descriptors.Add(descriptor);
                    continue;
                }

                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(
                    PlayerBindingAuthoringIssueKind.PlayerSlotOccupancyIssue,
                    occupancy.name,
                    source,
                    issue.Message));
            }

            return descriptors;
        }

        private static List<PlayerEntrySnapshot> BuildPlayerEntries(
            IEnumerable<PlayerEntryBehaviour> behaviours,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            var snapshots = new List<PlayerEntrySnapshot>();
            foreach (PlayerEntryBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                try
                {
                    snapshots.Add(behaviour.CreateSnapshot());
                }
                catch (Exception exception)
                {
                    issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(
                        PlayerBindingAuthoringIssueKind.PlayerEntrySnapshotFailure,
                        behaviour.name,
                        source,
                        exception.Message));
                }
            }

            return snapshots;
        }

        private static List<PlayerViewSnapshot> BuildPlayerViews(
            IEnumerable<PlayerViewBehaviour> behaviours,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            var snapshots = new List<PlayerViewSnapshot>();
            foreach (PlayerViewBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                try
                {
                    snapshots.Add(behaviour.CreateSnapshot());
                }
                catch (Exception exception)
                {
                    issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(
                        PlayerBindingAuthoringIssueKind.PlayerViewSnapshotFailure,
                        behaviour.name,
                        source,
                        exception.Message));
                }
            }

            return snapshots;
        }

        private static List<PlayerControlSnapshot> BuildPlayerControls(
            IEnumerable<PlayerControlBehaviour> behaviours,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            var snapshots = new List<PlayerControlSnapshot>();
            foreach (PlayerControlBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                try
                {
                    snapshots.Add(behaviour.CreateSnapshot());
                }
                catch (Exception exception)
                {
                    issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(
                        PlayerBindingAuthoringIssueKind.PlayerControlSnapshotFailure,
                        behaviour.name,
                        source,
                        exception.Message));
                }
            }

            return snapshots;
        }

        private static void AddPlayerSlotSetIssues(
            PlayerSlotSet playerSlotSet,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (playerSlotSet == null)
            {
                return;
            }

            for (int i = 0; i < playerSlotSet.Issues.Count; i++)
            {
                PlayerSlotSetIssue issue = playerSlotSet.Issues[i];
                issues.Add(new PlayerBindingAuthoringIssue(
                    PlayerBindingAuthoringIssueKind.PlayerSlotSetIssue,
                    issue.PlayerSlotIdText,
                    source,
                    $"PlayerSlotSet issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddPlayerTopologyIssues(
            PlayerTopologyValidationResult topology,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (topology == null)
            {
                return;
            }

            for (int i = 0; i < topology.Issues.Count; i++)
            {
                PlayerTopologyIssue issue = topology.Issues[i];
                issues.Add(new PlayerBindingAuthoringIssue(
                    PlayerBindingAuthoringIssueKind.PlayerTopologyIssue,
                    issue.PlayerSlotIdText,
                    source,
                    $"PlayerTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddPlayerViewTopologyIssues(
            PlayerViewTopologyValidationResult topology,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (topology == null)
            {
                return;
            }

            for (int i = 0; i < topology.Issues.Count; i++)
            {
                PlayerViewTopologyIssue issue = topology.Issues[i];
                issues.Add(new PlayerBindingAuthoringIssue(
                    PlayerBindingAuthoringIssueKind.PlayerViewTopologyIssue,
                    issue.PlayerSlotIdText,
                    source,
                    $"PlayerViewTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddPlayerControlTopologyIssues(
            PlayerControlTopologyValidationResult topology,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (topology == null)
            {
                return;
            }

            for (int i = 0; i < topology.Issues.Count; i++)
            {
                PlayerControlTopologyIssue issue = topology.Issues[i];
                issues.Add(new PlayerBindingAuthoringIssue(
                    PlayerBindingAuthoringIssueKind.PlayerControlTopologyIssue,
                    issue.PlayerSlotIdText,
                    source,
                    $"PlayerControlTopology issue propagated. kind='{issue.Kind}' source='{issue.Source}' message='{issue.Message}'",
                    issue.Blocking));
            }
        }

        private static void AddReadinessIssues(
            PlayerBindingReadinessSummary readiness,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (readiness == null)
            {
                return;
            }

            for (int i = 0; i < readiness.Issues.Count; i++)
            {
                PlayerBindingReadinessIssue issue = readiness.Issues[i];
                issues.Add(new PlayerBindingAuthoringIssue(
                    PlayerBindingAuthoringIssueKind.BindingReadinessIssue,
                    issue.Kind.ToString(),
                    source,
                    issue.Message,
                    issue.Blocking));
            }
        }

        private static void AddDiagnosticErrors(
            PlayerBindingDiagnosticReport report,
            ICollection<PlayerBindingAuthoringIssue> issues,
            string source)
        {
            if (report == null)
            {
                return;
            }

            for (int i = 0; i < report.Messages.Count; i++)
            {
                PlayerBindingDiagnosticMessage message = report.Messages[i];
                if (message.Severity != PlayerBindingDiagnosticSeverity.Error)
                {
                    continue;
                }

                issues.Add(PlayerBindingAuthoringIssue.BlockingIssue(
                    PlayerBindingAuthoringIssueKind.BindingDiagnosticError,
                    message.Kind.ToString(),
                    source,
                    message.Text));
            }
        }

        private static List<T> ToList<T>(IEnumerable<T> values) where T : class
        {
            var list = new List<T>();
            if (values == null)
            {
                return list;
            }

            foreach (T value in values)
            {
                if (value != null)
                {
                    list.Add(value);
                }
            }

            return list;
        }
    }
}
